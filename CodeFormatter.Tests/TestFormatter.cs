using CodeFormatter.Models;
using CodeFormatter.Services;

namespace CodeFormatter.Tests;

/// <summary>
/// Builds a FormatterService on a throwaway config file, so tests neither read nor touch the
/// config of the DevToys installation on this machine.
/// </summary>
internal static class TestFormatter
{
    public static string NewConfigPath() =>
        Path.Combine(Path.GetTempPath(), "CodeFormatter.Tests", Guid.NewGuid().ToString("N"), "config.toml");

    public static FormatterService Create() => Create(new ConfigManager(NewConfigPath()));

    public static FormatterService Create(ConfigManager configManager) =>
        new(configManager, new ProcessRunner());

    /// <summary>
    /// A service whose config has the given settings saved for one language.
    /// </summary>
    public static FormatterService With(Language language, params (string Key, object Value)[] settings)
    {
        var configManager = new ConfigManager(NewConfigPath());
        configManager.SaveAllSettings(language, settings.ToDictionary(s => s.Key, s => s.Value));
        return Create(configManager);
    }
}
