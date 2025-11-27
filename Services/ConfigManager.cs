using CodeFormatter.Models;
using Tomlyn;
using Tomlyn.Model;

namespace CodeFormatter.Services;

public class ConfigManager
{
    private readonly string _configPath;
    private CodeFormatterConfig? _cachedConfig;

    public ConfigManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appData, "DevToys", "CodeFormatter");
        _configPath = Path.Combine(configDir, "config.toml");
    }

    public CodeFormatterConfig LoadConfig()
    {
        if (_cachedConfig is not null)
            return _cachedConfig;

        if (!File.Exists(_configPath))
        {
            _cachedConfig = CreateDefaultConfig();
            SaveConfig(_cachedConfig);
            return _cachedConfig;
        }

        var toml = File.ReadAllText(_configPath);
        _cachedConfig = ParseConfig(toml);
        return _cachedConfig;
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
        return config.Defaults.LastLanguage.ToLowerInvariant() switch
        {
            "python" => Language.Python,
            "javascript" => Language.JavaScript,
            "typescript" => Language.TypeScript,
            "java" => Language.Java,
            "sql" => Language.Sql,
            _ => Language.Python
        };
    }

    public FormatterEntry? GetFormatterEntry(Language language)
    {
        var config = LoadConfig();
        var key = language.ToConfigKey();
        return config.Formatters.TryGetValue(key, out var entry) ? entry : null;
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

    private void SaveConfig(CodeFormatterConfig config)
    {
        var dir = Path.GetDirectoryName(_configPath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var toml = GenerateToml(config);
        File.WriteAllText(_configPath, toml);
    }

    private static CodeFormatterConfig CreateDefaultConfig() => new()
    {
        Defaults = new DefaultsConfig { LastLanguage = "python" },
        Formatters = new Dictionary<string, FormatterEntry>
        {
            ["python"] = new() { Command = "ruff", Args = ["format", "-"] },
            ["javascript"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.js"] },
            ["typescript"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.ts"] },
            ["java"] = new() { Command = "npx", Args = ["prettier", "--parser", "java"], RequiresNode = true },
            ["sql"] = new() { Command = "npx", Args = ["sql-formatter", "--language", "postgresql"], RequiresNode = true }
        }
    };

    private static CodeFormatterConfig ParseConfig(string toml)
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
                if (value is not TomlTable formatterTable)
                    continue;

                var entry = new FormatterEntry();
                if (formatterTable.TryGetValue("command", out var cmd))
                    entry.Command = cmd?.ToString() ?? "";
                if (formatterTable.TryGetValue("args", out var argsObj) && argsObj is TomlArray args)
                    entry.Args = args.Select(a => a?.ToString() ?? "").ToArray();
                if (formatterTable.TryGetValue("requiresNode", out var reqNode))
                    entry.RequiresNode = reqNode is bool b && b;

                config.Formatters[key] = entry;
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

    private static string GenerateToml(CodeFormatterConfig config)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Code Formatter Configuration");
        sb.AppendLine();
        sb.AppendLine("[defaults]");
        sb.AppendLine($"lastLanguage = \"{config.Defaults.LastLanguage}\"");
        sb.AppendLine();

        foreach (var (key, entry) in config.Formatters)
        {
            sb.AppendLine($"[formatters.{key}]");
            sb.AppendLine($"command = \"{entry.Command}\"");
            var argsStr = string.Join(", ", entry.Args.Select(a => $"\"{a}\""));
            sb.AppendLine($"args = [{argsStr}]");
            if (entry.RequiresNode)
                sb.AppendLine("requiresNode = true");
            sb.AppendLine();
        }

        if (config.Paths is not null)
        {
            sb.AppendLine("[paths]");
            if (config.Paths.Ruff is not null)
                sb.AppendLine($"ruff = \"{config.Paths.Ruff}\"");
            if (config.Paths.Dprint is not null)
                sb.AppendLine($"dprint = \"{config.Paths.Dprint}\"");
        }

        return sb.ToString();
    }
}
