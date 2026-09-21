using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// ruff format. Every option is an inline TOML override: --config key=value.
/// --isolated keeps ruff.toml / pyproject.toml found near the working directory out of it.
/// See docs/research/formatters/ruff.md.
/// </summary>
internal static class Ruff
{
    public static FormatterSpec Spec() => new()
    {
        Command = "ruff",
        Args = ["format", "--isolated", "--stdin-filename", "snippet.py"],
        SettingArgs = values => values.SelectMany(v => new[] { "--config", $"{v.Key}={Emit.TomlValue(v)}" }),
        TrailingArgs = ["-"],
        Settings = Settings
    };

    // This is the whole surface of ruff format at 0.14: it is Black-compatible and deliberately small
    private static readonly SettingDefinition[] Settings =
    [
        new("line-length", "Line Length", SettingType.Integer, 88, Min: 1, Max: 320,
            Description: "Maximum line length", Group: "Layout"),
        new("indent-width", "Indent Width", SettingType.Integer, 4, Min: 1, Max: 16,
            Description: "Spaces per indent level", Group: "Layout"),
        new("format.indent-style", "Indent Style", SettingType.Choice, "space",
            Choices: ["space", "tab"],
            Description: "Use spaces or tabs for indentation", Group: "Layout"),
        new("format.line-ending", "Line Ending", SettingType.Choice, "auto",
            Choices: ["auto", "lf", "cr-lf", "native"],
            Description: "auto keeps the line endings of the input", Group: "Layout"),
        new("format.quote-style", "Quote Style", SettingType.Choice, "double",
            Choices: ["double", "single", "preserve"],
            Description: "Preferred quote style for strings", Group: "Style"),
        new("format.skip-magic-trailing-comma", "Skip Magic Trailing Comma", SettingType.Boolean, false,
            Description: "A trailing comma no longer forces one item per line", Group: "Style"),
        new("format.docstring-code-format", "Format Docstring Code", SettingType.Boolean, false,
            Description: "Reformat code examples inside docstrings", Group: "Style"),
        new("format.docstring-code-line-length", "Docstring Code Line Length", SettingType.Integer, 0, Min: 0, Max: 320,
            Description: "0 = follow the line length", Group: "Style"),
        new("target-version", "Target Python Version", SettingType.Choice, "py310",
            Choices: ["py37", "py38", "py39", "py310", "py311", "py312", "py313", "py314"],
            Description: "Oldest Python the code must run on; decides which syntax may be used", Group: "Advanced"),
        new("format.preview", "Preview Style", SettingType.Boolean, false,
            Description: "Enable the unstable preview formatting style", Group: "Advanced")
    ];
}
