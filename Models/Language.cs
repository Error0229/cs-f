namespace CodeFormatter.Models;

public enum Language
{
    // Standalone formatter (Ruff)
    Python,

    // dprint typescript plugin
    JavaScript,
    TypeScript,

    // dprint json plugin
    Json,

    // dprint markdown plugin
    Markdown,

    // dprint toml plugin
    Toml,

    // dprint malva plugin (CSS family)
    Css,
    Scss,
    Less,

    // dprint markup_fmt plugin (HTML family)
    Html,
    Vue,
    Svelte,
    Astro,

    // dprint pretty_yaml plugin
    Yaml,

    // dprint pretty_graphql plugin
    GraphQL,

    // dprint dockerfile plugin
    Dockerfile,

    // Node.js required
    Java,
    Sql,

    // Standalone formatters (bundled)
    C,
    Cpp,
    Go,
    Shell,

    // New formatters (standalone binaries)
    Lua,
    R,
    Delphi,
    CSharpFormatted,  // Avoid conflict with existing C# types
    Assembly,
    ObjectiveC,
    Kotlin,
    Haskell,
    Perl,
    Php,
    Matlab,
    Ruby
}

public static class LanguageExtensions
{
    public static string ToDisplayName(this Language language) =>
        LanguageRegistry.Get(language).DisplayName;

    public static string ToConfigKey(this Language language) =>
        LanguageRegistry.Get(language).ConfigKey;

    public static string ToFileExtension(this Language language) =>
        LanguageRegistry.Get(language).FileExtension;

    public static string ToMonacoLanguage(this Language language) =>
        LanguageRegistry.Get(language).MonacoLanguage;
}
