using CodeFormatter.Formatters;
using CodeFormatter.Models;

namespace CodeFormatter.Services;

public record FormatResult(bool Success, string Output);

public class FormatterService
{
    private readonly ConfigManager _configManager;
    private readonly ProcessRunner _processRunner;
    private readonly JavaLocator _javaLocator;
    private readonly string[] _binarySearchPaths;

    public FormatterService(ConfigManager configManager, ProcessRunner processRunner)
    {
        _configManager = configManager;
        _processRunner = processRunner;
        _javaLocator = new JavaLocator(processRunner);

        // Get the directory where this assembly is located (the plugin's directory)
        // Assembly is at: CodeFormatter/lib/net8.0/CodeFormatter.dll
        // Plugin root is: CodeFormatter/
        var assemblyDir = Path.GetDirectoryName(typeof(FormatterService).Assembly.Location) ?? "";
        var libDir = Path.GetDirectoryName(assemblyDir) ?? assemblyDir;      // Go up from net8.0 to lib
        var pluginRoot = Path.GetDirectoryName(libDir) ?? libDir;            // Go up from lib to plugin root

        // Search paths for bundled binaries (in order of preference)
        _binarySearchPaths =
        [
            Path.Combine(assemblyDir, "Binaries"),                           // Local development (next to DLL)
            Path.Combine(pluginRoot, "runtimes", "win-x64", "native"),        // NuGet package (Windows x64)
            Path.Combine(pluginRoot, "runtimes", "win", "native"),            // NuGet package (Windows any)
            Path.Combine(AppContext.BaseDirectory, "Binaries"),              // Fallback: app base directory
            assemblyDir                                                      // Direct in assembly directory
        ];
    }

    public async Task<FormatResult> FormatAsync(string code, Language language, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new FormatResult(false, "No code to format.");

        var userEntry = _configManager.GetUserEntry(language);
        var spec = userEntry is not null ? FormatterSpecs.FromUserEntry(userEntry) : FormatterSpecs.For(language);
        if (spec is null)
            return new FormatResult(false, $"No formatter configured for {language.ToDisplayName()}.");

        var environment = new Dictionary<string, string>(spec.Environment);
        if (spec.NeedsJava)
        {
            var javaBin = await _javaLocator.FindJava11BinAsync(cancellationToken);
            if (javaBin is null)
            {
                return new FormatResult(false,
                    $"Java Required\n\n" +
                    $"{language.ToDisplayName()} formatting requires Java 11 or newer.\n" +
                    $"Install a JDK and set JAVA_HOME, or put its bin directory on PATH.\n\n" +
                    $"Download: https://adoptium.net");
            }
            environment["PATH"] = javaBin + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
        }

        // Every run happens in a directory of its own. Nearly every formatter looks for config
        // files around its working directory or its input file; here there are only ours.
        var dir = Path.Combine(Path.GetTempPath(), "CodeFormatter", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            var settings = _configManager.GetChangedSettings(language, spec);

            string[] args;
            try
            {
                args = await PrepareAsync(spec, settings, dir, code, cancellationToken);
            }
            catch (Exception ex) when (ex is FormatException or System.Text.Json.JsonException)
            {
                return new FormatResult(false, $"Formatting Error\n\nInvalid extra options: {ex.Message}");
            }

            var result = await _processRunner.RunAsync(
                ResolveCommand(spec.Command), args,
                spec.InputFileName is null ? code : null,
                dir, environment, cancellationToken);

            string formatted, diagnostics;
            if (spec.InputFileName is null)
            {
                formatted = result.Output;
                diagnostics = result.Error;
            }
            else
            {
                // The tool rewrote the file; whatever it printed is a report, not code
                formatted = await File.ReadAllTextAsync(Path.Combine(dir, spec.InputFileName), cancellationToken);
                diagnostics = string.Join("\n", new[] { result.Output, result.Error }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            if (result.ExitCode is not { } exitCode || !spec.Success.IsSuccess(exitCode, formatted, diagnostics))
                return Failure(diagnostics);

            return new FormatResult(true, spec.PostProcess?.Invoke(formatted) ?? formatted);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* Ignore cleanup errors */ }
        }
    }

    /// <summary>
    /// Writes the config and source files the spec asks for and returns the final argument list.
    /// </summary>
    private static async Task<string[]> PrepareAsync(
        FormatterSpec spec, IReadOnlyList<SettingValue> settings, string dir, string code, CancellationToken cancellationToken)
    {
        var configPath = spec.ConfigFileName is null ? "" : Path.Combine(dir, spec.ConfigFileName);
        var filePath = spec.InputFileName is null ? "" : Path.Combine(dir, spec.InputFileName);

        // File.WriteAllText writes UTF-8 without a BOM. Several tools reject a config file with one.
        if (spec.ConfigFileName is not null)
            await File.WriteAllTextAsync(configPath, spec.ConfigText?.Invoke(settings) ?? "", cancellationToken);
        if (spec.InputFileName is not null)
            await File.WriteAllTextAsync(filePath, code, cancellationToken);

        IEnumerable<string> args = [.. spec.Args, .. spec.SettingArgs?.Invoke(settings) ?? [], .. spec.TrailingArgs];
        return args
            .Select(a => a.Replace("{dir}", dir).Replace("{config}", configPath).Replace("{file}", filePath))
            .ToArray();
    }

    private FormatResult Failure(string diagnostics)
    {
        var errorMessage = string.IsNullOrWhiteSpace(diagnostics)
            ? "Unknown formatting error occurred."
            : diagnostics.Trim();

        // Add debug info if binary not found
        if (errorMessage.Contains("cannot find the file"))
            errorMessage += $"\n\n{GetDebugInfo()}";

        return new FormatResult(false, $"Formatting Error\n\n{errorMessage}");
    }

    private string ResolveCommand(string command)
    {
        // Check for custom path in config
        var customPath = _configManager.GetCustomPath(command);
        if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
            return customPath;

        // Check for bundled binary in all search paths
        foreach (var searchPath in _binarySearchPaths)
        {
            var bundledPath = Path.Combine(searchPath, $"{command}.exe");
            if (File.Exists(bundledPath))
                return bundledPath;
        }

        // Fall back to PATH lookup
        return command;
    }

    // For debugging - shows where we're looking for binaries
    public string GetDebugInfo()
    {
        var assemblyLocation = typeof(FormatterService).Assembly.Location;
        var lines = new List<string>
        {
            $"Assembly: {assemblyLocation}",
            "Search paths:"
        };
        foreach (var path in _binarySearchPaths)
        {
            var ruffPath = Path.Combine(path, "ruff.exe");
            var exists = File.Exists(ruffPath);
            lines.Add($"  {path} -> ruff.exe exists: {exists}");
        }
        return string.Join("\n", lines);
    }
}
