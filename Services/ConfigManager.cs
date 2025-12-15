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

        // Merge any missing formatters from default config (for version upgrades)
        var defaultConfig = CreateDefaultConfig();
        foreach (var (key, entry) in defaultConfig.Formatters)
        {
            if (!_cachedConfig.Formatters.ContainsKey(key))
            {
                _cachedConfig.Formatters[key] = entry;
            }
        }

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
        return config.Defaults.LastLanguage.ToLowerInvariant() switch
        {
            "python" => Language.Python,
            "javascript" => Language.JavaScript,
            "typescript" => Language.TypeScript,
            "json" => Language.Json,
            "markdown" => Language.Markdown,
            "toml" => Language.Toml,
            "css" => Language.Css,
            "scss" => Language.Scss,
            "less" => Language.Less,
            "html" => Language.Html,
            "vue" => Language.Vue,
            "svelte" => Language.Svelte,
            "astro" => Language.Astro,
            "yaml" => Language.Yaml,
            "graphql" => Language.GraphQL,
            "dockerfile" => Language.Dockerfile,
            "java" => Language.Java,
            "sql" => Language.Sql,
            "c" => Language.C,
            "cpp" => Language.Cpp,
            "go" => Language.Go,
            "shell" => Language.Shell,
            "lua" => Language.Lua,
            "r" => Language.R,
            "delphi" => Language.Delphi,
            "csharp" => Language.CSharpFormatted,
            "assembly" => Language.Assembly,
            "objc" => Language.ObjectiveC,
            "kotlin" => Language.Kotlin,
            "haskell" => Language.Haskell,
            "perl" => Language.Perl,
            "php" => Language.Php,
            "matlab" => Language.Matlab,
            "ruby" => Language.Ruby,
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

    public void SaveFormatterEntry(Language language, string command, string[] args, bool requiresNode)
    {
        var config = LoadConfig();
        var key = language.ToConfigKey();

        // Preserve existing settings if entry exists
        var existingSettings = config.Formatters.TryGetValue(key, out var existing)
            ? existing.Settings
            : new Dictionary<string, object>();

        config.Formatters[key] = new FormatterEntry
        {
            Command = command,
            Args = args,
            RequiresNode = requiresNode,
            Settings = existingSettings
        };

        SaveConfig(config);
    }

    public void ResetFormatterEntry(Language language)
    {
        var defaultConfig = CreateDefaultConfig();
        var key = language.ToConfigKey();

        if (!defaultConfig.Formatters.TryGetValue(key, out var defaultEntry))
            return;

        var config = LoadConfig();
        config.Formatters[key] = defaultEntry;
        SaveConfig(config);
    }

    /// <summary>
    /// Gets the settings dictionary for a language, with defaults applied
    /// </summary>
    public Dictionary<string, object> GetSettingsWithDefaults(Language language)
    {
        var definitions = FormatterSettingsDefinitions.GetSettings(language);
        var result = new Dictionary<string, object>();

        // First apply defaults
        foreach (var def in definitions)
        {
            result[def.Key] = def.DefaultValue;
        }

        // Then override with saved settings
        var entry = GetFormatterEntry(language);
        if (entry?.Settings is not null)
        {
            foreach (var (key, value) in entry.Settings)
            {
                if (result.ContainsKey(key))
                {
                    result[key] = value;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Saves a single setting for a language
    /// </summary>
    public void SaveSetting(Language language, string key, object value)
    {
        var config = LoadConfig();
        var langKey = language.ToConfigKey();

        if (!config.Formatters.TryGetValue(langKey, out var entry))
        {
            // Create entry from defaults if it doesn't exist
            var defaultConfig = CreateDefaultConfig();
            if (defaultConfig.Formatters.TryGetValue(langKey, out var defaultEntry))
            {
                entry = defaultEntry;
                config.Formatters[langKey] = entry;
            }
            else
            {
                return; // Unknown language
            }
        }

        entry.Settings[key] = value;
        SaveConfig(config);
    }

    /// <summary>
    /// Saves all settings for a language at once
    /// </summary>
    public void SaveAllSettings(Language language, Dictionary<string, object> settings)
    {
        var config = LoadConfig();
        var langKey = language.ToConfigKey();

        if (!config.Formatters.TryGetValue(langKey, out var entry))
        {
            var defaultConfig = CreateDefaultConfig();
            if (defaultConfig.Formatters.TryGetValue(langKey, out var defaultEntry))
            {
                entry = defaultEntry;
                config.Formatters[langKey] = entry;
            }
            else
            {
                return;
            }
        }

        entry.Settings = settings;
        SaveConfig(config);
    }

    /// <summary>
    /// Resets settings for a language to defaults
    /// </summary>
    public void ResetSettings(Language language)
    {
        var config = LoadConfig();
        var langKey = language.ToConfigKey();

        if (config.Formatters.TryGetValue(langKey, out var entry))
        {
            entry.Settings.Clear();
            SaveConfig(config);
        }
    }

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

    // dprint plugin URLs (update versions as needed)
    private const string DprintPluginTypeScript = "https://plugins.dprint.dev/typescript-0.95.13.wasm";
    private const string DprintPluginJson = "https://plugins.dprint.dev/json-0.21.0.wasm";
    private const string DprintPluginMarkdown = "https://plugins.dprint.dev/markdown-0.20.0.wasm";
    private const string DprintPluginToml = "https://plugins.dprint.dev/toml-0.7.0.wasm";
    private const string DprintPluginMalva = "https://plugins.dprint.dev/g-plane/malva-v0.15.1.wasm";
    private const string DprintPluginMarkupFmt = "https://plugins.dprint.dev/g-plane/markup_fmt-v0.25.1.wasm";
    private const string DprintPluginYaml = "https://plugins.dprint.dev/g-plane/pretty_yaml-v0.5.1.wasm";
    private const string DprintPluginGraphQL = "https://plugins.dprint.dev/g-plane/pretty_graphql-v0.2.3.wasm";
    private const string DprintPluginDockerfile = "https://plugins.dprint.dev/dockerfile-0.3.3.wasm";

    private static CodeFormatterConfig CreateDefaultConfig() => new()
    {
        Defaults = new DefaultsConfig { LastLanguage = "python" },
        Formatters = new Dictionary<string, FormatterEntry>
        {
            // Python - standalone Ruff
            ["python"] = new() { Command = "ruff", Args = ["format", "-"] },

            // JavaScript/TypeScript - dprint typescript plugin
            ["javascript"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.js", "--plugins", DprintPluginTypeScript] },
            ["typescript"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.ts", "--plugins", DprintPluginTypeScript] },

            // JSON - dprint json plugin
            ["json"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.json", "--plugins", DprintPluginJson] },

            // Markdown - dprint markdown plugin
            ["markdown"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.md", "--plugins", DprintPluginMarkdown] },

            // TOML - dprint toml plugin
            ["toml"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.toml", "--plugins", DprintPluginToml] },

            // CSS family - dprint malva plugin
            ["css"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.css", "--plugins", DprintPluginMalva] },
            ["scss"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.scss", "--plugins", DprintPluginMalva] },
            ["less"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.less", "--plugins", DprintPluginMalva] },

            // HTML family - dprint markup_fmt plugin
            ["html"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.html", "--plugins", DprintPluginMarkupFmt] },
            ["vue"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.vue", "--plugins", DprintPluginMarkupFmt] },
            ["svelte"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.svelte", "--plugins", DprintPluginMarkupFmt] },
            ["astro"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.astro", "--plugins", DprintPluginMarkupFmt] },

            // YAML - dprint pretty_yaml plugin
            ["yaml"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.yaml", "--plugins", DprintPluginYaml] },

            // GraphQL - dprint pretty_graphql plugin
            ["graphql"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.graphql", "--plugins", DprintPluginGraphQL] },

            // Dockerfile - dprint dockerfile plugin
            ["dockerfile"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "Dockerfile", "--plugins", DprintPluginDockerfile] },

            // Java - google-java-format (native GraalVM binary)
            ["java"] = new() { Command = "google-java-format", Args = ["-"] },

            // SQL - sqruff (native Rust binary)
            ["sql"] = new() { Command = "sqruff", Args = ["fix", "-"] },

            // Standalone formatters (bundled binaries)
            // C/C++ - clang-format (LLVM)
            ["c"] = new() { Command = "clang-format", Args = ["--assume-filename=file.c"] },
            ["cpp"] = new() { Command = "clang-format", Args = ["--assume-filename=file.cpp"] },

            // Go - gofumpt (stricter gofmt, backwards compatible)
            ["go"] = new() { Command = "gofumpt", Args = [] },

            // Shell/Bash - shfmt
            ["shell"] = new() { Command = "shfmt", Args = ["--filename", "script.sh"] },

            // New standalone formatters
            // Lua - stylua (reads from stdin with -)
            ["lua"] = new() { Command = "stylua", Args = ["-"] },

            // R - air (uses temp file - no stdin support)
            ["r"] = new() { Command = "air", Args = ["format", "{file}"], UsesTempFile = true, TempFileExtension = "r" },

            // Delphi - pasfmt (reads stdin by default)
            ["delphi"] = new() { Command = "pasfmt", Args = [] },

            // C# - csharpier (uses temp file - doesn't support stdin well)
            ["csharp"] = new() { Command = "csharpier", Args = ["format", "{file}"], UsesTempFile = true, TempFileExtension = "cs" },

            // Assembly - asmfmt (reads stdin by default)
            ["assembly"] = new() { Command = "asmfmt", Args = [] },

            // Objective-C - uncrustify (-l OC for language, -c - for default config, -q for quiet)
            ["objc"] = new() { Command = "uncrustify", Args = ["-l", "OC", "-c", "-", "-q"] },

            // Kotlin - ktlint (--stdin with --format)
            ["kotlin"] = new() { Command = "ktlint", Args = ["--stdin", "--format"] },

            // Haskell - ormolu (--stdin-input-file to specify stdin)
            ["haskell"] = new() { Command = "ormolu", Args = ["--stdin-input-file", "stdin.hs"] },

            // Perl - perltidy (-st for stdout, -se for stderr)
            ["perl"] = new() { Command = "perltidy", Args = ["-st", "-se"] },

            // PHP - php-cs-fixer (uses temp file - needs file path and explicit rules to avoid config lookup)
            ["php"] = new() { Command = "php-cs-fixer", Args = ["fix", "{file}", "--rules=@PSR12", "--using-cache=no", "--quiet"], UsesTempFile = true, TempFileExtension = "php" },

            // MATLAB - mh_style (uses temp file - doesn't support stdin)
            ["matlab"] = new() { Command = "mh_style", Args = ["--single", "--fix", "{file}"], UsesTempFile = true, TempFileExtension = "m" },

            // Ruby - rufo (reads from stdin by default)
            ["ruby"] = new() { Command = "rufo", Args = [] }
        }
    };

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
                    if (value is not TomlTable formatterTable)
                        continue;

                    var entry = new FormatterEntry();
                    if (formatterTable.TryGetValue("command", out var cmd))
                        entry.Command = cmd?.ToString() ?? "";
                    if (formatterTable.TryGetValue("args", out var argsObj) && argsObj is TomlArray args)
                        entry.Args = args.Select(a => a?.ToString() ?? "").ToArray();
                    if (formatterTable.TryGetValue("requiresNode", out var reqNode))
                        entry.RequiresNode = reqNode is bool b && b;

                    // Parse settings
                    if (formatterTable.TryGetValue("settings", out var settingsObj) && settingsObj is TomlTable settings)
                    {
                        foreach (var (settingKey, settingValue) in settings)
                        {
                            // Convert TOML types to appropriate .NET types
                            entry.Settings[settingKey] = settingValue switch
                            {
                                bool boolVal => boolVal,
                                long longVal => (int)longVal,
                                double doubleVal => (int)doubleVal,
                                string strVal => strVal,
                                _ => settingValue?.ToString() ?? ""
                            };
                        }
                    }

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
        catch
        {
            return CreateDefaultConfig();
        }
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

            // Write settings if any exist
            if (entry.Settings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"[formatters.{key}.settings]");
                foreach (var (settingKey, settingValue) in entry.Settings)
                {
                    var valueStr = settingValue switch
                    {
                        bool b => b.ToString().ToLowerInvariant(),
                        int i => i.ToString(),
                        string s => $"\"{s}\"",
                        _ => $"\"{settingValue}\""
                    };
                    sb.AppendLine($"{settingKey} = {valueStr}");
                }
            }

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
