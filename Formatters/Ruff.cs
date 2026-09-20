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

    private static readonly SettingDefinition[] Settings =
    [
        new("line-length", "Line Length", SettingType.Integer, 88, Min: 1, Max: 320,
            Description: "Maximum line length"),
        new("format.indent-style", "Indent Style", SettingType.Choice, "space",
            Choices: ["space", "tab"],
            Description: "Use spaces or tabs for indentation"),
        new("format.quote-style", "Quote Style", SettingType.Choice, "double",
            Choices: ["double", "single", "preserve"],
            Description: "Preferred quote style for strings"),
        new("format.line-ending", "Line Ending", SettingType.Choice, "auto",
            Choices: ["auto", "lf", "cr-lf", "native"],
            Description: "Line ending style")
    ];
}
