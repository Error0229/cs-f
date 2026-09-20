using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// pasfmt (Delphi). Values are -C key=value overrides. --config-file pointing at our own empty
/// file is what stops it reading (and choking on) a pasfmt.toml around the working directory.
/// See docs/research/formatters/pasfmt.md.
/// </summary>
internal static class Pasfmt
{
    public static FormatterSpec Spec() => new()
    {
        Command = "pasfmt",
        // The default encoding is the ANSI code page, for stdin too. We pipe UTF-8.
        Args = ["--config-file", "{config}", "-C", "encoding=utf-8"],
        ConfigFileName = "pasfmt.toml",
        ConfigText = _ => "",
        SettingArgs = values => Emit.Pairs("-C", values),
        Settings = Settings
    };

    // All the keys of pasfmt 0.7 except encoding, which is pinned above
    private static readonly SettingDefinition[] Settings =
    [
        new("wrap_column", "Wrap Column", SettingType.Integer, 120, Min: 20, Max: 1000,
            Description: "Target line length before wrapping"),
        new("use_tabs", "Use Tabs", SettingType.Boolean, false,
            Description: "Indent with tabs instead of spaces"),
        new("tab_width", "Indent Width", SettingType.Integer, 2, Min: 1, Max: 16,
            Description: "Spaces per indent level"),
        new("continuation_indents", "Continuation Indents", SettingType.Integer, 2, Min: 0, Max: 8,
            Description: "Indent of wrapped lines, in indent levels"),
        new("begin_style", "Begin Style", SettingType.Choice, "auto",
            Choices: ["auto", "always_wrap"],
            Description: "always_wrap puts begin on its own line after if, for, while"),
        new("line_ending", "Line Ending", SettingType.Choice, "native",
            Choices: ["native", "lf", "crlf"],
            Description: "native is CRLF on Windows"),
        new("format_multiline_strings", "Format Multiline Strings", SettingType.Boolean, true,
            Description: "Re-indent the inside of triple-quoted strings")
    ];
}
