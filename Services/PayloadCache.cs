using CodeFormatter.Models;

namespace CodeFormatter.Services;

/// <summary>
/// Keeps a copy of what a self-extracting launcher unpacks, so that later runs can start the
/// real program directly instead of paying for the unpacking again.
///
/// The copy lives under %LOCALAPPDATA%\CodeFormatter\cache in a folder named after the launcher's
/// size and timestamp. A new version of the extension brings a different launcher, hence a
/// different folder, so an old payload is never run for a new launcher.
/// </summary>
public class PayloadCache
{
    private readonly string _root;

    public PayloadCache()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodeFormatter", "cache"))
    {
    }

    public PayloadCache(string root)
    {
        _root = root;
    }

    /// <summary>
    /// The folder holding the complete payload of this launcher, or null if we have no copy yet.
    /// </summary>
    public string? Find(string launcherPath, LauncherPayload payload)
    {
        var dir = FolderFor(launcherPath);
        return dir is not null && payload.Files.All(f => File.Exists(Path.Combine(dir, f))) ? dir : null;
    }

    /// <summary>
    /// Call after the launcher has run: copies what it unpacked. Best effort; if anything goes
    /// wrong the launcher simply keeps being used.
    /// </summary>
    public void Capture(string launcherPath, LauncherPayload payload)
    {
        try
        {
            var dir = FolderFor(launcherPath);
            var source = Path.Combine(Path.GetTempPath(), payload.TempFolder);
            if (dir is null || Directory.Exists(dir) || !payload.Files.All(f => File.Exists(Path.Combine(source, f))))
                return;

            // Payloads of launchers that are gone (the extension was updated)
            var prefix = Path.GetFileNameWithoutExtension(launcherPath) + "-";
            if (Directory.Exists(_root))
            {
                foreach (var old in Directory.GetDirectories(_root, prefix + "*"))
                    TryDelete(old);
            }

            // Copy under another name and rename when complete: a half-copied payload must never
            // look like a usable one, whatever interrupts us.
            var partial = dir + ".partial-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(partial);
            foreach (var file in payload.Files)
                File.Copy(Path.Combine(source, file), Path.Combine(partial, file));
            Directory.Move(partial, dir);
        }
        catch
        {
            // Another run got there first, the disk is full, the temp folder was cleaned...
        }
    }

    public void Discard(string payloadDir) => TryDelete(payloadDir);

    /// <summary>
    /// What Java and PHP say when the program file they were given is damaged or missing.
    /// </summary>
    public static bool LooksBroken(string stderr) =>
        stderr.Contains("corrupt jarfile", StringComparison.OrdinalIgnoreCase)
        || stderr.Contains("Unable to access jarfile", StringComparison.OrdinalIgnoreCase)
        || stderr.Contains("Could not open input file", StringComparison.OrdinalIgnoreCase)
        || stderr.Contains("PharException", StringComparison.OrdinalIgnoreCase);

    private string? FolderFor(string launcherPath)
    {
        var launcher = new FileInfo(launcherPath);
        if (!launcher.Exists)
            return null; // Resolved through PATH: not one of our bundled launchers

        var name = Path.GetFileNameWithoutExtension(launcher.Name);
        return Path.Combine(_root, $"{name}-{launcher.Length:x}-{launcher.LastWriteTimeUtc.Ticks:x}");
    }

    private static void TryDelete(string dir)
    {
        try { Directory.Delete(dir, recursive: true); } catch { /* In use, or not ours to delete */ }
    }
}
