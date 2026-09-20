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
        SettingArgs = values => Emit.Pairs("-C", values)
    };
}
