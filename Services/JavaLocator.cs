using System.Text.RegularExpressions;

namespace CodeFormatter.Services;

/// <summary>
/// Finds a Java 11+ runtime for formatters that launch a JVM.
/// ktlint's launcher runs whatever "java" is first on PATH and ignores JAVA_HOME; on many
/// machines that is an old Java 8, which cannot start it. So we look ourselves and put the
/// right bin directory first on the child process's PATH.
/// </summary>
public class JavaLocator
{
    private readonly ProcessRunner _processRunner;
    private string? _javaBin;

    public JavaLocator(ProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    /// <summary>
    /// Directory holding java.exe of version 11 or newer, or null if there is none.
    /// </summary>
    public async Task<string?> FindJava11BinAsync(CancellationToken cancellationToken = default)
    {
        // Only a hit is remembered: the user may install Java and try again
        if (_javaBin is not null)
            return _javaBin;

        foreach (var bin in Candidates().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (await MajorVersionAsync(bin, cancellationToken) >= 11)
                return _javaBin = bin;
        }
        return null;
    }

    private static IEnumerable<string> Candidates()
    {
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
            yield return Path.Combine(javaHome, "bin");

        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            if (!string.IsNullOrWhiteSpace(dir))
                yield return dir.Trim();
        }

        // Usual install locations, for JDKs that are on neither
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] roots =
        [
            Path.Combine(programFiles, "Eclipse Adoptium"),
            Path.Combine(programFiles, "Microsoft"),
            Path.Combine(programFiles, "Java"),
            Path.Combine(programFiles, "Zulu"),
            Path.Combine(userProfile, "scoop", "apps"),
        ];
        foreach (var root in roots.Where(Directory.Exists))
        {
            foreach (var bin in SafeDirectories(root).SelectMany(jdk => new[]
                     {
                         Path.Combine(jdk, "bin"),
                         Path.Combine(jdk, "current", "bin") // scoop
                     }))
            {
                yield return bin;
            }
        }
    }

    private static string[] SafeDirectories(string root)
    {
        try { return Directory.GetDirectories(root); }
        catch { return []; }
    }

    private async Task<int> MajorVersionAsync(string bin, CancellationToken cancellationToken)
    {
        var java = Path.Combine(bin, "java.exe");
        if (!File.Exists(java))
            return 0;

        var result = await _processRunner.RunAsync(java, ["-version"], null, Path.GetTempPath(), null, cancellationToken);

        // java version "1.8.0_401"  /  openjdk version "25.0.2" 2026-01-20
        var match = Regex.Match(result.Error + result.Output, @"version ""(\d+)(?:\.(\d+))?");
        if (!match.Success)
            return 0;

        var first = int.Parse(match.Groups[1].Value);
        return first == 1 && match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : first;
    }
}
