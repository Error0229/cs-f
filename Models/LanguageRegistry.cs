namespace CodeFormatter.Models;

/// <summary>
/// Centralized registry of all supported languages.
/// Add new languages here - they will automatically appear in the UI and config.
/// </summary>
public static class LanguageRegistry
{
    /// <summary>
    /// All supported languages with their metadata.
    /// Order determines display order in the dropdown.
    /// </summary>
    public static IReadOnlyList<LanguageInfo> All { get; } =
    [
        // Standalone formatter (Ruff)
        new(Language.Python, "Python", "python", "py", "python"),

        // dprint typescript plugin
        new(Language.JavaScript, "JavaScript", "javascript", "js", "javascript"),
        new(Language.TypeScript, "TypeScript", "typescript", "ts", "typescript"),

        // dprint json plugin
        new(Language.Json, "JSON", "json", "json", "json"),

        // dprint markdown plugin
        new(Language.Markdown, "Markdown", "markdown", "md", "markdown"),

        // dprint toml plugin
        new(Language.Toml, "TOML", "toml", "toml", "toml"),

        // dprint malva plugin (CSS family)
        new(Language.Css, "CSS", "css", "css", "css"),
        new(Language.Scss, "SCSS", "scss", "scss", "scss"),
        new(Language.Less, "Less", "less", "less", "less"),

        // dprint markup_fmt plugin (HTML family)
        new(Language.Html, "HTML", "html", "html", "html"),
        new(Language.Vue, "Vue", "vue", "vue", "html"),
        new(Language.Svelte, "Svelte", "svelte", "svelte", "html"),
        new(Language.Astro, "Astro", "astro", "astro", "html"),

        // dprint plugins
        new(Language.Yaml, "YAML", "yaml", "yaml", "yaml"),
        new(Language.GraphQL, "GraphQL", "graphql", "graphql", "graphql"),
        new(Language.Dockerfile, "Dockerfile", "dockerfile", "Dockerfile", "dockerfile"),

        // Standalone formatters
        new(Language.Java, "Java", "java", "java", "java"),
        new(Language.Sql, "SQL", "sql", "sql", "sql"),
        new(Language.C, "C", "c", "c", "c"),
        new(Language.Cpp, "C++", "cpp", "cpp", "cpp"),
        new(Language.CSharpFormatted, "C#", "csharp", "cs", "csharp"),
        new(Language.Go, "Go", "go", "go", "go"),
        new(Language.Assembly, "Go Assembly", "assembly", "s", "plaintext"),
        new(Language.Shell, "Shell/Bash", "shell", "sh", "shell"),
        new(Language.Lua, "Lua", "lua", "lua", "lua"),
        new(Language.R, "R", "r", "r", "r"),
        new(Language.Delphi, "Delphi/Pascal", "delphi", "pas", "pascal"),
        new(Language.ObjectiveC, "Objective-C", "objc", "m", "objective-c"),
        new(Language.Kotlin, "Kotlin", "kotlin", "kt", "kotlin"),
        new(Language.Haskell, "Haskell", "haskell", "hs", "haskell"),
        new(Language.Perl, "Perl", "perl", "pl", "perl"),
        new(Language.Php, "PHP", "php", "php", "php"),
        new(Language.Matlab, "MATLAB", "matlab", "m", "plaintext"),
        new(Language.Ruby, "Ruby", "ruby", "rb", "ruby"),
    ];

    private static readonly Dictionary<Language, LanguageInfo> _byLanguage =
        All.ToDictionary(x => x.Language);

    private static readonly Dictionary<string, LanguageInfo> _byConfigKey =
        All.ToDictionary(x => x.ConfigKey, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Get language info by enum value.
    /// </summary>
    public static LanguageInfo Get(Language language) =>
        _byLanguage.TryGetValue(language, out var info)
            ? info
            : new LanguageInfo(language, language.ToString(), language.ToString().ToLowerInvariant(), "txt", "plaintext");

    /// <summary>
    /// Get language info by config key (e.g., "python", "javascript").
    /// Returns null if not found.
    /// </summary>
    public static LanguageInfo? GetByConfigKey(string configKey) =>
        _byConfigKey.TryGetValue(configKey, out var info) ? info : null;
}
