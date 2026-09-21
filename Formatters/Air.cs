using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// Air (R). Has no config flag: it looks for air.toml from the directory of --stdin-file-path
/// upwards. The path need not exist; pointing it into our private directory, next to our own
/// air.toml, is what ends the search before it reaches anybody else's.
/// See docs/research/formatters/air.md (written for 0.8, which had no stdin mode yet).
/// </summary>
internal static class Air
{
    public static FormatterSpec Spec() => new()
    {
        Command = "air",
        Args = ["format", "--no-color", "--stdin-file-path", @"{dir}\input.R"],
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
            Description: "Preferred maximum line length", Group: "Layout"),
        new("indent-width", "Indent Width", SettingType.Integer, 2, Min: 1, Max: 24,
            Description: "Spaces per indent level", Group: "Layout"),
        new("indent-style", "Indent Style", SettingType.Choice, "space",
            Choices: ["space", "tab"], Group: "Layout"),
        new("line-ending", "Line Ending", SettingType.Choice, "auto",
            Choices: ["auto", "lf", "crlf", "native"],
            Description: "auto keeps the line endings of the input", Group: "Layout"),
        new("assignment-style", "Assignment Style", SettingType.Choice, "arrow",
            Choices: ["arrow", "equal", "preserve"],
            Description: "arrow rewrites x = 1 as x <- 1; preserve leaves assignments as written", Group: "Style"),
        new("persistent-line-breaks", "Persistent Line Breaks", SettingType.Boolean, true,
            Description: "A line break you put after ( keeps the call expanded; off collapses whatever fits", Group: "Style"),
        new("default-table", "Table Formatting", SettingType.Boolean, true,
            Description: "Align the arguments of tribble() and fcase() as tables", Group: "Style"),
        new("table", "Extra Table Functions", SettingType.Text, "",
            Description: "More functions whose calls are laid out as tables, separated by commas", Group: "Functions"),
        new("skip", "Skip Functions", SettingType.Text, "",
            Description: "Functions whose calls are left exactly as written, separated by commas", Group: "Functions")
    ];
}
