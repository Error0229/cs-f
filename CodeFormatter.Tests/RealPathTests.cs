using CodeFormatter.Services;

namespace CodeFormatter.Tests;

public class RealPathTests
{
    [Fact]
    public void AnOrdinaryFile_KeepsItsPath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "CodeFormatter.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "a.wasm");
        File.WriteAllText(file, "x");

        Assert.Equal(file, RealPath.Of(file), ignoreCase: true);
        Assert.DoesNotContain(@"\\?\", RealPath.Of(file));
    }

    [Fact]
    public void ALink_ResolvesToItsTarget()
    {
        // The nearest thing to MSIX redirection that a test can set up: a junction
        var root = Path.Combine(Path.GetTempPath(), "CodeFormatter.Tests", Guid.NewGuid().ToString("N"));
        var real = Path.Combine(root, "real");
        var link = Path.Combine(root, "link");
        Directory.CreateDirectory(real);
        File.WriteAllText(Path.Combine(real, "a.wasm"), "x");
        Directory.CreateSymbolicLink(link, real);

        Assert.Equal(Path.Combine(real, "a.wasm"), RealPath.Of(Path.Combine(link, "a.wasm")), ignoreCase: true);
    }

    [Fact]
    public void ADirectory_ResolvesToo()
    {
        var root = Path.Combine(Path.GetTempPath(), "CodeFormatter.Tests", Guid.NewGuid().ToString("N"));
        var real = Path.Combine(root, "real");
        var link = Path.Combine(root, "link");
        Directory.CreateDirectory(real);
        Directory.CreateSymbolicLink(link, real);

        Assert.Equal(real, RealPath.Of(link), ignoreCase: true);
        Assert.Equal(real, RealPath.Of(real), ignoreCase: true);
    }

    [Fact]
    public void AMissingFile_KeepsItsPath()
    {
        Assert.Equal(@"C:\nope\a.wasm", RealPath.Of(@"C:\nope\a.wasm"));
    }
}
