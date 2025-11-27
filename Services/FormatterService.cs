using CodeFormatter.Models;

namespace CodeFormatter.Services;

public record FormatResult(bool Success, string Output);

public class FormatterService
{
    private readonly ConfigManager _configManager;
    private readonly ProcessRunner _processRunner;
    private readonly string _bundledBinariesPath;

    public FormatterService(ConfigManager configManager, ProcessRunner processRunner)
    {
        _configManager = configManager;
        _processRunner = processRunner;
        _bundledBinariesPath = Path.Combine(AppContext.BaseDirectory, "Binaries");
    }

    public async Task<FormatResult> FormatAsync(string code, Language language, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new FormatResult(false, "No code to format.");

        var entry = _configManager.GetFormatterEntry(language);
        if (entry is null)
            return new FormatResult(false, $"No formatter configured for {language.ToDisplayName()}.");

        if (entry.RequiresNode && !_processRunner.IsNodeInstalled())
        {
            return new FormatResult(false,
                $"Node.js Required\n\n" +
                $"{language.ToDisplayName()} formatting requires Node.js to be installed.\n\n" +
                $"Download: https://nodejs.org");
        }

        var command = ResolveCommand(entry.Command);
        var result = await _processRunner.RunAsync(command, entry.Args, code, cancellationToken);

        if (!result.Success)
        {
            var errorMessage = string.IsNullOrWhiteSpace(result.Error)
                ? "Unknown formatting error occurred."
                : result.Error;
            return new FormatResult(false, $"Formatting Error\n\n{errorMessage}");
        }

        return new FormatResult(true, result.Output);
    }

    private string ResolveCommand(string command)
    {
        // Check for custom path in config
        var customPath = _configManager.GetCustomPath(command);
        if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
            return customPath;

        // Check for bundled binary
        var bundledPath = Path.Combine(_bundledBinariesPath, $"{command}.exe");
        if (File.Exists(bundledPath))
            return bundledPath;

        // Fall back to PATH lookup
        return command;
    }
}
