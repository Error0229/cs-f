using CodeFormatter.Models;
using CodeFormatter.Services;

namespace CodeFormatter.Tests;

/// <summary>
/// Running a launcher's payload directly is an optimisation. It must never run the wrong
/// program, and it must never be what stops formatting from working.
/// </summary>
public class PayloadCacheTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "CodeFormatter.Tests", Guid.NewGuid().ToString("N"));
    private readonly string _unpackFolder = "cf-test-launcher-" + Guid.NewGuid().ToString("N");

    private string CacheRoot => Path.Combine(_root, "cache");
    private string UnpackDir => Path.Combine(Path.GetTempPath(), _unpackFolder);

    private LauncherPayload Payload => new(_unpackFolder, ["app.bin", "lib.dll"], (dir, _) => (Path.Combine(dir, "app.bin"), []));

    public void Dispose()
    {
        foreach (var dir in new[] { _root, UnpackDir })
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* Nothing to clean */ }
        }
    }

    private string NewLauncher(string content = "launcher v1")
    {
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, "tool.exe");
        File.WriteAllText(path, content);
        return path;
    }

    private void Unpack(params string[] files)
    {
        Directory.CreateDirectory(UnpackDir);
        foreach (var file in files)
            File.WriteAllText(Path.Combine(UnpackDir, file), "payload of " + file);
    }

    [Fact]
    public void NothingCached_UntilTheLauncherHasRun()
    {
        var cache = new PayloadCache(CacheRoot);

        Assert.Null(cache.Find(NewLauncher(), Payload));
    }

    [Fact]
    public void Capture_KeepsWhatTheLauncherUnpacked()
    {
        var cache = new PayloadCache(CacheRoot);
        var launcher = NewLauncher();
        Unpack("app.bin", "lib.dll");

        cache.Capture(launcher, Payload);

        var dir = cache.Find(launcher, Payload);
        Assert.NotNull(dir);
        Assert.Equal("payload of app.bin", File.ReadAllText(Path.Combine(dir, "app.bin")));
        Assert.StartsWith(CacheRoot, dir);
    }

    [Fact]
    public void IncompletePayload_IsNotCaptured()
    {
        var cache = new PayloadCache(CacheRoot);
        var launcher = NewLauncher();
        Unpack("app.bin"); // lib.dll is missing

        cache.Capture(launcher, Payload);

        Assert.Null(cache.Find(launcher, Payload));
        Assert.False(Directory.Exists(CacheRoot) && Directory.GetDirectories(CacheRoot).Length > 0,
            "A partial copy was left behind");
    }

    [Fact]
    public void ANewLauncher_DoesNotGetTheOldPayload()
    {
        var cache = new PayloadCache(CacheRoot);
        var launcher = NewLauncher();
        Unpack("app.bin", "lib.dll");
        cache.Capture(launcher, Payload);
        var oldDir = cache.Find(launcher, Payload);

        // The extension was updated: same path, different file
        File.WriteAllText(launcher, "launcher v2, a bit longer");

        Assert.Null(cache.Find(launcher, Payload));

        // Once the new launcher has run, its payload replaces the old one
        cache.Capture(launcher, Payload);
        Assert.NotNull(cache.Find(launcher, Payload));
        Assert.False(Directory.Exists(oldDir), "The payload of the replaced launcher was kept");
    }

    [Fact]
    public void LauncherFoundOnPath_IsLeftAlone()
    {
        var cache = new PayloadCache(CacheRoot);
        Unpack("app.bin", "lib.dll");

        // Not a file we can identify: a bare command name resolved by the OS
        cache.Capture("tool", Payload);

        Assert.Null(cache.Find("tool", Payload));
    }

    [Fact]
    public void Discard_SendsTheNextRunBackToTheLauncher()
    {
        var cache = new PayloadCache(CacheRoot);
        var launcher = NewLauncher();
        Unpack("app.bin", "lib.dll");
        cache.Capture(launcher, Payload);

        cache.Discard(cache.Find(launcher, Payload)!);

        Assert.Null(cache.Find(launcher, Payload));
    }

    [Fact]
    public async Task Kotlin_BrokenPayload_CostsOneFormatThenHeals()
    {
        var cache = new PayloadCache(CacheRoot);
        var service = new FormatterService(new ConfigManager(TestFormatter.NewConfigPath()), new ProcessRunner(), cache);
        const string input = "fun main(){println(1)}";
        const string expected = "fun main() {\n    println(1)\n}\n";

        // Through the launcher; its payload is captured
        var first = await service.FormatAsync(input, Language.Kotlin);
        Assert.True(first.Success, $"Format failed: {first.Output}");
        var jar = Directory.GetFiles(CacheRoot, "ktlint.jar", SearchOption.AllDirectories).Single();

        // Directly, and the output is still only the code: no JVM log lines
        var direct = await service.FormatAsync(input, Language.Kotlin);
        Assert.True(direct.Success, $"Format failed: {direct.Output}");
        Assert.Equal(expected, direct.Output.ReplaceLineEndings("\n"));

        // Something damages the copy
        File.WriteAllText(jar, "not a jar");
        var broken = await service.FormatAsync(input, Language.Kotlin);
        Assert.False(broken.Success);
        Assert.False(File.Exists(jar), "The broken payload was kept");

        var healed = await service.FormatAsync(input, Language.Kotlin);
        Assert.True(healed.Success, $"Format failed: {healed.Output}");
        Assert.Equal(expected, healed.Output.ReplaceLineEndings("\n"));
    }
}
