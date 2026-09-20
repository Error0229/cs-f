using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// Air (R). 0.8.0 has no stdin mode and no config flag: it formats a file in place and looks for
/// air.toml from that file's directory upwards. Our own air.toml beside the file ends the search.
/// See docs/research/formatters/air.md.
/// </summary>
internal static class Air
{
    public static FormatterSpec Spec() => new()
    {
        Command = "air",
        Args = ["format", "--no-color", "{file}"],
        InputFileName = "input.R",
        ConfigFileName = "air.toml",
        ConfigText = values => "[format]\n" + string.Concat(values.Select(v => $"{v.Key} = {Toml(v)}\n")),
        Settings = Settings
    };

    // Function-name lists are typed as "a, b" and written as ["a", "b"]
    private static string Toml(SettingValue v) => v.Definition.Type == SettingType.Text
        ? "[" + string.Join(", ", Emit.Names(v.Text).Select(Emit.TomlString)) + "]"
        : Emit.TomlValue(v);

    private static readonly SettingDefinition[] Settings =
    [
        new("line-width", "Line Width", SettingType.Integer, 80, Min: 1, Max: 320,
            Description: "Preferred maximum line length"),
        new("indent-width", "Indent Width", SettingType.Integer, 2, Min: 1, Max: 24,
            Description: "Spaces per indent level"),
        new("indent-style", "Indent Style", SettingType.Choice, "space",
            Choices: ["space", "tab"]),
        new("line-ending", "Line Ending", SettingType.Choice, "auto",
            Choices: ["auto", "lf", "crlf", "native"],
            Description: "auto keeps the line endings of the input"),
        new("persistent-line-breaks", "Persistent Line Breaks", SettingType.Boolean, true,
            Description: "A line break you put after ( keeps the call expanded; off collapses whatever fits"),
        new("default-table", "Table Formatting", SettingType.Boolean, true,
            Description: "Align the arguments of tribble() and fcase() as tables"),
        new("table", "Extra Table Functions", SettingType.Text, "",
            Description: "More functions whose calls are laid out as tables, separated by commas"),
        new("skip", "Skip Functions", SettingType.Text, "",
            Description: "Functions whose calls are left exactly as written, separated by commas")
    ];
}
