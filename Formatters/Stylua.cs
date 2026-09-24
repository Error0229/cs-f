using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// StyLua. Options go through a generated stylua.toml: at 2.3.1 one option is broken as a CLI
/// flag, and an .editorconfig near the working directory beats explicit flags.
/// See docs/research/formatters/stylua.md.
/// </summary>
internal static class Stylua
{
    public static FormatterSpec Spec() => new()
    {
        Command = "stylua",
        Args = ["--config-path", "{config}", "--no-editorconfig", "-"],
        ConfigFileName = "stylua.toml",
        // "sort_requires.enabled" is a TOML dotted key, which is the [sort_requires] table
        ConfigText = values => string.Concat(values.Select(v => $"{v.Key} = {Emit.TomlValue(v)}\n")),
        DocsUrl = "https://github.com/JohnnyMorganz/StyLua#options",
        Note = "These are all the options StyLua has.",
        Settings = Settings
    };

    // All of StyLua's options
    private static readonly SettingDefinition[] Settings =
    [
        new("column_width", "Column Width", SettingType.Integer, 120, Min: 20, Max: 1000,
            Description: "Width the printer tries to stay under", Group: "Layout"),
        new("indent_type", "Indent Type", SettingType.Choice, "Tabs",
            Choices: ["Tabs", "Spaces"], Group: "Layout"),
        new("indent_width", "Indent Width", SettingType.Integer, 4, Min: 1, Max: 16,
            Description: "Spaces per indent level; with tabs, how wide a tab counts", Group: "Layout"),
        new("line_endings", "Line Ending", SettingType.Choice, "Unix",
            Choices: ["Unix", "Windows"], Group: "Layout"),
        new("quote_style", "Quote Style", SettingType.Choice, "AutoPreferDouble",
            Choices: ["AutoPreferDouble", "AutoPreferSingle", "ForceDouble", "ForceSingle"],
            Description: "Auto: switches to the other quote when that saves escapes", Group: "Style"),
        new("call_parentheses", "Call Parentheses", SettingType.Choice, "Always",
            Choices: ["Always", "NoSingleString", "NoSingleTable", "None", "Input"],
            Description: "Parentheses on calls with one string or table argument: print \"x\"", Group: "Style"),
        new("collapse_simple_statement", "Collapse Simple Statements", SettingType.Choice, "Never",
            Choices: ["Never", "FunctionOnly", "ConditionalOnly", "Always"],
            Description: "Keep \"if x then return end\" on one line", Group: "Style"),
        new("space_after_function_names", "Space After Function Names", SettingType.Choice, "Never",
            Choices: ["Never", "Definitions", "Calls", "Always"],
            Description: "foo (x) rather than foo(x)", Group: "Style"),
        new("block_newline_gaps", "Blank Lines At Block Edges", SettingType.Choice, "Never",
            Choices: ["Never", "Preserve"],
            Description: "Preserve keeps blank lines at the start and end of a block", Group: "Style"),
        new("sort_requires.enabled", "Sort Requires", SettingType.Boolean, false,
            Description: "Sort blocks of local x = require(...) lines", Group: "Advanced"),
        new("syntax", "Lua Version", SettingType.Choice, "All",
            Choices: ["All", "Lua51", "Lua52", "Lua53", "Lua54", "LuaJIT", "Luau", "CfxLua"],
            Description: "All accepts every dialect; name one where they disagree, such as Luau", Group: "Advanced")
    ];
}
