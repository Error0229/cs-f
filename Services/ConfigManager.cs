using CodeFormatter.Formatters;
using CodeFormatter.Models;
using Tomlyn;
using Tomlyn.Model;

namespace CodeFormatter.Services;

/// <summary>
/// Reads and writes config.toml. The file holds only what the user chose: the last language,
/// settings that differ from their default, and formatter commands they wrote by hand.
/// How each formatter is invoked by default lives in FormatterSpecs, not here.
/// </summary>
public class ConfigManager
{
    private readonly string _configPath;
    private CodeFormatterConfig? _cachedConfig;

    public ConfigManager()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DevToys", "CodeFormatter", "config.toml"))
    {
    }

    public ConfigManager(string configPath)
    {
        _configPath = configPath;
    }

    public CodeFormatterConfig LoadConfig()
    {
        if (_cachedConfig is not null)
            return _cachedConfig;

        _cachedConfig = File.Exists(_configPath)
            ? ParseConfig(File.ReadAllText(_configPath))
            : new CodeFormatterConfig();

        return _cachedConfig;
    }

    /// <summary>
    /// Clears the cached config, forcing a reload on next access.
    /// Useful for testing.
    /// </summary>
    public void ClearCache()
    {
        _cachedConfig = null;
    }

    public void SaveLastLanguage(Language language)
    {
        var config = LoadConfig();
        config.Defaults.LastLanguage = language.ToConfigKey();
        SaveConfig(config);
    }

    public Language GetLastLanguage()
    {
        var config = LoadConfig();
        var info = LanguageRegistry.GetByConfigKey(config.Defaults.LastLanguage);
        return info?.Language ?? Language.Python;
    }

    /// <summary>
    /// The formatter command the user wrote by hand for this language, if any.
    /// </summary>
    public FormatterEntry? GetUserEntry(Language language)
    {
        var entry = GetEntry(language);
        return entry is { IsUserDefined: true } ? entry : null;
    }

    public string? GetCustomPath(string formatter)
    {
        var config = LoadConfig();
        return formatter.ToLowerInvariant() switch
        {
            "ruff" => config.Paths?.Ruff,
            "dprint" => config.Paths?.Dprint,
            _ => null
        };
    }

    /// <summary>
    /// Gets the settings dictionary for a language, with defaults applied
    /// </summary>
    public Dictionary<string, object> GetSettingsWithDefaults(Language language)
    {
        var saved = GetEntry(language)?.Settings;
        var result = new Dictionary<string, object>();

        foreach (var def in FormatterSpecs.SettingsFor(language))
        {
            result[def.Key] = saved is not null && saved.TryGetValue(def.Key, out var value) && def.Accepts(value)
                ? value
                : def.DefaultValue;
        }

        return result;
    }

    /// <summary>
    /// The settings that must be passed to the formatter: those the user moved off their default.
    /// Everything else is left to the tool, whose defaults are the ones that count.
    /// </summary>
    public IReadOnlyList<SettingValue> GetChangedSettings(Language language, FormatterSpec spec)
    {
        var current = GetSettingsWithDefaults(language);
        return spec.Settings
            .Where(def => current.TryGetValue(def.Key, out var value) && !Equals(value, def.DefaultValue))
            .Select(def => new SettingValue(def, current[def.Key]))
            .ToList();
    }

    /// <summary>
    /// Saves all settings for a language at once
    /// </summary>
    public void SaveAllSettings(Language language, Dictionary<string, object> settings)
    {
        var config = LoadConfig();
        var langKey = language.ToConfigKey();

        if (!config.Formatters.TryGetValue(langKey, out var entry))
            config.Formatters[langKey] = entry = new FormatterEntry();

        // Defaults are not written down: they belong to the formatter and may change with it
        entry.Settings = FormatterSpecs.SettingsFor(language)
            .Where(def => settings.TryGetValue(def.Key, out var value) && def.Accepts(value) && !Equals(value, def.DefaultValue))
            .ToDictionary(def => def.Key, def => settings[def.Key]);

        SaveConfig(config);
    }

    /// <summary>
    /// Resets settings for a language to defaults
    /// </summary>
    public void ResetSettings(Language language)
    {
        var config = LoadConfig();

        if (config.Formatters.TryGetValue(language.ToConfigKey(), out var entry))
        {
            entry.Settings.Clear();
            SaveConfig(config);
        }
    }

    private FormatterEntry? GetEntry(Language language) =>
        LoadConfig().Formatters.GetValueOrDefault(language.ToConfigKey());

    private void SaveConfig(CodeFormatterConfig config)
    {
        try
        {
            var dir = Path.GetDirectoryName(_configPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var toml = GenerateToml(config);
            File.WriteAllText(_configPath, toml);
            _cachedConfig = config;
        }
        catch
        {
            // Silently fail - config persistence is convenience, not critical
        }
    }

    private static CodeFormatterConfig ParseConfig(string toml)
    {
        try
        {
            var model = Toml.ToModel(toml);
            var config = new CodeFormatterConfig();

            if (model.TryGetValue("defaults", out var defaultsObj) && defaultsObj is TomlTable defaults)
            {
                if (defaults.TryGetValue("lastLanguage", out var lastLang))
                    config.Defaults.LastLanguage = lastLang?.ToString() ?? "python";
            }

            if (model.TryGetValue("formatters", out var formattersObj) && formattersObj is TomlTable formatters)
            {
                foreach (var (key, value) in formatters)
                {
                    if (value is TomlTable formatterTable)
                        config.Formatters[key] = ParseEntry(key, formatterTable);
                }
            }

            if (model.TryGetValue("paths", out var pathsObj) && pathsObj is TomlTable paths)
            {
                config.Paths = new PathsConfig();
                if (paths.TryGetValue("ruff", out var ruff))
                    config.Paths.Ruff = ruff?.ToString();
                if (paths.TryGetValue("dprint", out var dprint))
                    config.Paths.Dprint = dprint?.ToString();
            }

            return config;
        }
        catch
        {
            return new CodeFormatterConfig();
        }
    }

    private static FormatterEntry ParseEntry(string languageKey, TomlTable table)
    {
        var entry = new FormatterEntry();
        if (table.TryGetValue("command", out var cmd))
            entry.Command = cmd?.ToString() ?? "";
        if (table.TryGetValue("args", out var argsObj) && argsObj is TomlArray args)
            entry.Args = args.Select(a => a?.ToString() ?? "").ToArray();
        if (table.TryGetValue("requiresNode", out var reqNode))
            entry.RequiresNode = reqNode is bool b && b;
        if (table.TryGetValue("usesTempFile", out var usesTempFile))
            entry.UsesTempFile = usesTempFile is bool utf && utf;
        if (table.TryGetValue("tempFileExtension", out var tempExt))
            entry.TempFileExtension = tempExt?.ToString() ?? "txt";

        // Older versions saved their own defaults here. That is not the user talking.
        if (FormatterSpecs.IsShippedDefault(entry))
        {
            entry.Command = "";
            entry.Args = [];
            entry.RequiresNode = false;
            entry.UsesTempFile = false;
        }

        if (table.TryGetValue("settings", out var settingsObj) && settingsObj is TomlTable settings)
        {
            var language = LanguageRegistry.GetByConfigKey(languageKey)?.Language;
            foreach (var (settingKey, settingValue) in settings)
            {
                var key = language is { } l ? FormatterSpecs.MigrateSettingKey(l, settingKey) : settingKey;

                // Convert TOML types to appropriate .NET types
                entry.Settings[key] = settingValue switch
                {
                    bool boolVal => boolVal,
                    long longVal => (int)longVal,
                    double doubleVal => (int)doubleVal,
                    string strVal => strVal,
                    _ => settingValue?.ToString() ?? ""
                };
            }
        }

        return entry;
    }

    private static string GenerateToml(CodeFormatterConfig config)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Code Formatter Configuration");
        sb.AppendLine("#");
        sb.AppendLine("# Settings changed in the UI are saved under [formatters.<language>.settings].");
        sb.AppendLine("# To run a formatter your own way, give it a command and args:");
        sb.AppendLine("#");
        sb.AppendLine("#   [formatters.python]");
        sb.AppendLine("#   command = \"black\"");
        sb.AppendLine("#   args = [\"-q\", \"-\"]");
        sb.AppendLine("#");
        sb.AppendLine("# Code is piped to stdin and read back from stdout. For a tool that only works on files,");
        sb.AppendLine("# add usesTempFile = true and tempFileExtension, and put {file} in args.");
        sb.AppendLine();
        sb.AppendLine("[defaults]");
        sb.AppendLine($"lastLanguage = {Emit.TomlString(config.Defaults.LastLanguage)}");
        sb.AppendLine();

        foreach (var (key, entry) in config.Formatters)
        {
            if (!entry.IsUserDefined && entry.Settings.Count == 0)
                continue;

            sb.AppendLine($"[formatters.{key}]");
            if (entry.IsUserDefined)
            {
                sb.AppendLine($"command = {Emit.TomlString(entry.Command)}");
                sb.AppendLine($"args = [{string.Join(", ", entry.Args.Select(Emit.TomlString))}]");
                if (entry.UsesTempFile)
                {
                    sb.AppendLine("usesTempFile = true");
                    sb.AppendLine($"tempFileExtension = {Emit.TomlString(entry.TempFileExtension)}");
                }
            }

            // Write settings if any exist
            if (entry.Settings.Count > 0)
            {
                if (entry.IsUserDefined)
                    sb.AppendLine();
                sb.AppendLine($"[formatters.{key}.settings]");
                foreach (var (settingKey, settingValue) in entry.Settings)
                {
                    var valueStr = settingValue switch
                    {
                        bool b => b.ToString().ToLowerInvariant(),
                        int i => i.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        _ => Emit.TomlString(settingValue.ToString() ?? "")
                    };
                    // Quoted: the tools' own keys contain dots and dashes ("format.quote-style", "-i")
                    sb.AppendLine($"{Emit.TomlString(settingKey)} = {valueStr}");
                }
            }

            sb.AppendLine();
        }

        if (config.Paths is not null)
        {
            sb.AppendLine("[paths]");
            if (config.Paths.Ruff is not null)
                sb.AppendLine($"ruff = {Emit.TomlString(config.Paths.Ruff)}");
            if (config.Paths.Dprint is not null)
                sb.AppendLine($"dprint = {Emit.TomlString(config.Paths.Dprint)}");
        }

        return sb.ToString();
    }
}
