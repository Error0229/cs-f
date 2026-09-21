using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// shfmt. Flags only. -i is always passed, even at its default: giving any formatting flag is
/// what makes shfmt ignore .editorconfig files around the working directory.
/// See docs/research/formatters/shfmt.md.
/// </summary>
internal static class Shfmt
{
    private const string Indent = "-i";

    public static FormatterSpec Spec() => new()
    {
        Command = "shfmt",
        Args = ["--filename", "script.sh"],
        SettingArgs = values =>
        [
            Indent, values.Find(Indent) ?? "0",
            .. Emit.Flags(values.Where(v => v.Key != Indent))
        ],
        Settings = Settings
    };

    private static readonly SettingDefinition[] Settings =
    [
        new(Indent, "Indent Width", SettingType.Integer, 0, Min: 0, Max: 16,
            Description: "Number of spaces for indentation (0 for tabs)", Group: "Layout"),
        new("-bn", "Binary Next Line", SettingType.Boolean, false,
            Description: "Place binary operators at start of next line", Group: "Layout"),
        new("-ci", "Case Indent", SettingType.Boolean, false,
            Description: "Indent case labels", Group: "Layout"),
        new("-sr", "Space Redirects", SettingType.Boolean, false,
            Description: "Add space after redirect operators", Group: "Layout"),
        new("-kp", "Keep Padding", SettingType.Boolean, false,
            Description: "Keep column alignment padding", Group: "More"),
        new("-fn", "Function Next Line", SettingType.Boolean, false,
            Description: "Place function opening brace on next line", Group: "Layout"),
        // No zsh at 3.12
        new("-ln", "Shell Dialect", SettingType.Choice, "auto",
            Choices: ["auto", "bash", "posix", "mksh", "bats"],
            Description: "auto goes by the shebang, else bash", Group: "More"),
        new("-s", "Simplify", SettingType.Boolean, false,
            Description: "Simplify the code, e.g. drop needless quotes inside [[ ]]. Changes tokens, not just layout", Group: "More"),
        new("-mn", "Minify", SettingType.Boolean, false,
            Description: "Make the script as small as possible; drops comments", Group: "More")
    ];
}
