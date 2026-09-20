using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// gofumpt. Flags only. Left alone it reads the Go version and module path from a go.mod in the
/// working directory or above, so both are always passed.
/// See docs/research/formatters/gofumpt.md.
/// </summary>
internal static class Gofumpt
{
    private const string Lang = "-lang";

    public static FormatterSpec Spec() => new()
    {
        Command = "gofumpt",
        // "_" matches no import path: no module
        Args = ["-modpath", "_"],
        SettingArgs = values =>
        [
            Lang, values.Find(Lang) ?? (string)LangSetting.DefaultValue,
            .. Emit.Flags(values.Where(v => v.Key != Lang))
        ],
        Settings = [LangSetting, Extra]
    };

    // An invalid -lang makes gofumpt panic, so this is a list rather than free text
    private static readonly SettingDefinition LangSetting =
        new(Lang, "Go Version", SettingType.Choice, "go1.25",
            Choices: ["go1", "go1.13", "go1.18", "go1.20", "go1.21", "go1.22", "go1.23", "go1.24", "go1.25"],
            Description: "Target Go version. From go1.13, octal literals are rewritten as 0o777");

    private static readonly SettingDefinition Extra =
        new("-extra", "Extra Rules", SettingType.Boolean, false,
            Description: "Group adjacent parameters of the same type, make naked returns explicit");
}
