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
    Shell
}

public static class LanguageExtensions
{
    public static string ToDisplayName(this Language language) => language switch
    {
        Language.Python => "Python",
        Language.JavaScript => "JavaScript",
        Language.TypeScript => "TypeScript",
        Language.Json => "JSON",
        Language.Markdown => "Markdown",
        Language.Toml => "TOML",
        Language.Css => "CSS",
        Language.Scss => "SCSS",
        Language.Less => "Less",
        Language.Html => "HTML",
        Language.Vue => "Vue",
        Language.Svelte => "Svelte",
        Language.Astro => "Astro",
        Language.Yaml => "YAML",
        Language.GraphQL => "GraphQL",
        Language.Dockerfile => "Dockerfile",
        Language.Java => "Java",
        Language.Sql => "SQL",
        Language.C => "C",
        Language.Cpp => "C++",
        Language.Go => "Go",
        Language.Shell => "Shell/Bash",
        _ => language.ToString()
    };

    public static string ToConfigKey(this Language language) => language switch
    {
        Language.Python => "python",
        Language.JavaScript => "javascript",
        Language.TypeScript => "typescript",
        Language.Json => "json",
        Language.Markdown => "markdown",
        Language.Toml => "toml",
        Language.Css => "css",
        Language.Scss => "scss",
        Language.Less => "less",
        Language.Html => "html",
        Language.Vue => "vue",
        Language.Svelte => "svelte",
        Language.Astro => "astro",
        Language.Yaml => "yaml",
        Language.GraphQL => "graphql",
        Language.Dockerfile => "dockerfile",
        Language.Java => "java",
        Language.Sql => "sql",
        Language.C => "c",
        Language.Cpp => "cpp",
        Language.Go => "go",
        Language.Shell => "shell",
        _ => language.ToString().ToLowerInvariant()
    };

    public static string ToFileExtension(this Language language) => language switch
    {
        Language.Python => "py",
        Language.JavaScript => "js",
        Language.TypeScript => "ts",
        Language.Json => "json",
        Language.Markdown => "md",
        Language.Toml => "toml",
        Language.Css => "css",
        Language.Scss => "scss",
        Language.Less => "less",
        Language.Html => "html",
        Language.Vue => "vue",
        Language.Svelte => "svelte",
        Language.Astro => "astro",
        Language.Yaml => "yaml",
        Language.GraphQL => "graphql",
        Language.Dockerfile => "Dockerfile",
        Language.Java => "java",
        Language.Sql => "sql",
        Language.C => "c",
        Language.Cpp => "cpp",
        Language.Go => "go",
        Language.Shell => "sh",
        _ => "txt"
    };
}
