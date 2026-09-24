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
    private const string ModPath = "-modpath";

    public static FormatterSpec Spec() => new()
    {
        Command = "gofumpt",
        SettingArgs = values =>
        [
            Lang, values.Find(Lang) ?? (string)LangSetting.DefaultValue,
            // "_" matches no import path: no module
            ModPath, values.Find(ModPath) is { Length: > 0 } module ? module.Trim() : "_",
            .. Emit.Flags(values.Where(v => v.Key != Lang && v.Key != ModPath))
        ],
        DocsUrl = "https://github.com/mvdan/gofumpt#readme",
        Note = "Go formatting is not configurable, by design. These are all the options gofumpt has.",
        Settings = [LangSetting, Extra, ModPathSetting]
    };

    // An invalid -lang makes gofumpt panic, so this is a list rather than free text
    private static readonly SettingDefinition LangSetting =
        new(Lang, "Go Version", SettingType.Choice, "go1.25",
            Choices: ["go1", "go1.13", "go1.18", "go1.20", "go1.21", "go1.22", "go1.23", "go1.24", "go1.25"],
            Description: "Target Go version. From go1.13, octal literals are rewritten as 0o777");

    private static readonly SettingDefinition ModPathSetting =
        new(ModPath, "Module Path", SettingType.Text, "",
            Description: "Only for a module name without a dot, e.g. myapp: keeps its imports out of the standard library group");

    private static readonly SettingDefinition Extra =
        new("-extra", "Extra Rules", SettingType.Boolean, false,
            Description: "Group adjacent parameters of the same type, make naked returns explicit");
}
