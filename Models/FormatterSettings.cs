namespace CodeFormatter.Models;

/// <summary>
/// Defines the type of a formatter setting
/// </summary>
public enum SettingType
{
    Boolean,
    Integer,
    Choice  // String with predefined options
}

/// <summary>
/// Defines a single formatter setting
/// </summary>
public record SettingDefinition(
    string Key,
    string DisplayName,
    SettingType Type,
    object DefaultValue,
    int? Min = null,
    int? Max = null,
    string[]? Choices = null,
    string? Description = null
);

/// <summary>
/// Provides setting definitions for each formatter/language
/// </summary>
public static class FormatterSettingsDefinitions
{
    public static SettingDefinition[] GetSettings(Language language) => language switch
    {
        Language.Python => PythonSettings,
        Language.JavaScript or Language.TypeScript => TypeScriptSettings,
        Language.Json => JsonSettings,
        Language.Markdown => MarkdownSettings,
        Language.Toml => TomlSettings,
        Language.Css or Language.Scss or Language.Less => CssSettings,
        Language.Html or Language.Vue or Language.Svelte or Language.Astro => HtmlSettings,
        Language.Yaml => YamlSettings,
        Language.GraphQL => GraphQLSettings,
        Language.Dockerfile => DockerfileSettings,
        Language.Java => JavaSettings,
        Language.Sql => SqlSettings,
        _ => []
    };

    /// <summary>
    /// Ruff settings for Python (matches ruff format config fields)
    /// </summary>
    private static readonly SettingDefinition[] PythonSettings =
    [
        new("line-length", "Line Length", SettingType.Integer, 88, Min: 40, Max: 400,
            Description: "Maximum line length"),
        new("indent-style", "Indent Style", SettingType.Choice, "space",
            Choices: ["space", "tab"],
            Description: "Use spaces or tabs for indentation"),
        new("quote-style", "Quote Style", SettingType.Choice, "double",
            Choices: ["double", "single", "preserve"],
            Description: "Preferred quote style for strings"),
        new("line-ending", "Line Ending", SettingType.Choice, "auto",
            Choices: ["auto", "lf", "cr-lf", "native"],
            Description: "Line ending style")
    ];

    /// <summary>
    /// dprint TypeScript plugin settings (JavaScript/TypeScript)
    /// </summary>
    private static readonly SettingDefinition[] TypeScriptSettings =
    [
        new("lineWidth", "Line Width", SettingType.Integer, 120, Min: 40, Max: 400,
            Description: "Maximum line width"),
        new("indentWidth", "Indent Width", SettingType.Integer, 2, Min: 1, Max: 16,
            Description: "Number of spaces for indentation"),
        new("useTabs", "Use Tabs", SettingType.Boolean, false,
            Description: "Use tabs instead of spaces"),
        new("semiColons", "Semicolons", SettingType.Choice, "prefer",
            Choices: ["prefer", "asi"],
            Description: "Whether to use semicolons"),
        new("quoteStyle", "Quote Style", SettingType.Choice, "double",
            Choices: ["double", "single", "preferDouble", "preferSingle"],
            Description: "Preferred quote style")
    ];

    /// <summary>
    /// dprint JSON plugin settings
    /// </summary>
    private static readonly SettingDefinition[] JsonSettings =
    [
        new("lineWidth", "Line Width", SettingType.Integer, 120, Min: 40, Max: 400,
            Description: "Maximum line width"),
        new("indentWidth", "Indent Width", SettingType.Integer, 2, Min: 1, Max: 16,
            Description: "Number of spaces for indentation"),
        new("useTabs", "Use Tabs", SettingType.Boolean, false,
            Description: "Use tabs instead of spaces"),
        new("newLineKind", "Line Ending", SettingType.Choice, "lf",
            Choices: ["auto", "lf", "crlf", "system"],
            Description: "Line ending style"),
        new("trailingCommas", "Trailing Commas", SettingType.Choice, "jsonc",
            Choices: ["never", "jsonc", "always"],
            Description: "When to use trailing commas")
    ];

    /// <summary>
    /// dprint Markdown plugin settings
    /// </summary>
    private static readonly SettingDefinition[] MarkdownSettings =
    [
        new("lineWidth", "Line Width", SettingType.Integer, 80, Min: 40, Max: 400,
            Description: "Maximum line width"),
        new("textWrap", "Text Wrap", SettingType.Choice, "maintain",
            Choices: ["always", "never", "maintain"],
            Description: "How to wrap text")
    ];

    /// <summary>
    /// dprint TOML plugin settings
    /// </summary>
    private static readonly SettingDefinition[] TomlSettings =
    [
        new("lineWidth", "Line Width", SettingType.Integer, 120, Min: 40, Max: 400,
            Description: "Maximum line width"),
        new("indentWidth", "Indent Width", SettingType.Integer, 2, Min: 1, Max: 16,
            Description: "Number of spaces for indentation"),
        new("useTabs", "Use Tabs", SettingType.Boolean, false,
            Description: "Use tabs instead of spaces")
    ];

    /// <summary>
    /// dprint Malva plugin settings (CSS/SCSS/Less)
    /// </summary>
    private static readonly SettingDefinition[] CssSettings =
    [
        new("printWidth", "Print Width", SettingType.Integer, 80, Min: 40, Max: 400,
            Description: "Maximum line width"),
        new("tabWidth", "Tab Width", SettingType.Integer, 2, Min: 1, Max: 16,
            Description: "Number of spaces per tab"),
        new("useTabs", "Use Tabs", SettingType.Boolean, false,
            Description: "Use tabs instead of spaces"),
        new("singleQuote", "Single Quotes", SettingType.Boolean, false,
            Description: "Use single quotes instead of double quotes")
    ];

    /// <summary>
    /// dprint markup_fmt plugin settings (HTML/Vue/Svelte/Astro)
    /// </summary>
    private static readonly SettingDefinition[] HtmlSettings =
    [
        new("printWidth", "Print Width", SettingType.Integer, 80, Min: 40, Max: 400,
            Description: "Maximum line width"),
        new("tabWidth", "Tab Width", SettingType.Integer, 2, Min: 1, Max: 16,
            Description: "Number of spaces per tab"),
        new("useTabs", "Use Tabs", SettingType.Boolean, false,
            Description: "Use tabs instead of spaces"),
        new("closingBracketSameLine", "Closing Bracket Same Line", SettingType.Boolean, false,
            Description: "Put closing bracket on same line as last attribute")
    ];

    /// <summary>
    /// dprint pretty_yaml plugin settings
    /// </summary>
    private static readonly SettingDefinition[] YamlSettings =
    [
        new("lineWidth", "Line Width", SettingType.Integer, 80, Min: 40, Max: 400,
            Description: "Maximum line width"),
        new("indentWidth", "Indent Width", SettingType.Integer, 2, Min: 1, Max: 16,
            Description: "Number of spaces for indentation"),
        new("quotes", "Quote Style", SettingType.Choice, "preferDouble",
            Choices: ["preferDouble", "preferSingle", "forceDouble", "forceSingle"],
            Description: "Preferred quote style")
    ];

    /// <summary>
    /// dprint pretty_graphql plugin settings
    /// </summary>
    private static readonly SettingDefinition[] GraphQLSettings =
    [
        new("lineWidth", "Line Width", SettingType.Integer, 80, Min: 40, Max: 400,
            Description: "Maximum line width"),
        new("indentWidth", "Indent Width", SettingType.Integer, 2, Min: 1, Max: 16,
            Description: "Number of spaces for indentation"),
        new("useTabs", "Use Tabs", SettingType.Boolean, false,
            Description: "Use tabs instead of spaces")
    ];

    /// <summary>
    /// dprint Dockerfile plugin settings (minimal options)
    /// </summary>
    private static readonly SettingDefinition[] DockerfileSettings =
    [
        new("lineWidth", "Line Width", SettingType.Integer, 120, Min: 40, Max: 400,
            Description: "Maximum line width")
    ];

    /// <summary>
    /// Prettier settings for Java
    /// </summary>
    private static readonly SettingDefinition[] JavaSettings =
    [
        new("printWidth", "Print Width", SettingType.Integer, 80, Min: 40, Max: 400,
            Description: "Maximum line width"),
        new("tabWidth", "Tab Width", SettingType.Integer, 4, Min: 1, Max: 16,
            Description: "Number of spaces per tab"),
        new("useTabs", "Use Tabs", SettingType.Boolean, false,
            Description: "Use tabs instead of spaces")
    ];

    /// <summary>
    /// sql-formatter settings for SQL
    /// </summary>
    private static readonly SettingDefinition[] SqlSettings =
    [
        new("tabWidth", "Tab Width", SettingType.Integer, 2, Min: 1, Max: 16,
            Description: "Number of spaces per tab"),
        new("useTabs", "Use Tabs", SettingType.Boolean, false,
            Description: "Use tabs instead of spaces"),
        new("keywordCase", "Keyword Case", SettingType.Choice, "preserve",
            Choices: ["preserve", "upper", "lower"],
            Description: "Case for SQL keywords"),
        new("dataTypeCase", "Data Type Case", SettingType.Choice, "preserve",
            Choices: ["preserve", "upper", "lower"],
            Description: "Case for data types"),
        new("functionCase", "Function Case", SettingType.Choice, "preserve",
            Choices: ["preserve", "upper", "lower"],
            Description: "Case for function names"),
        new("language", "SQL Dialect", SettingType.Choice, "postgresql",
            Choices: ["sql", "postgresql", "mysql", "mariadb", "transactsql", "sqlite", "bigquery", "spark", "redshift"],
            Description: "SQL dialect to use")
    ];
}
