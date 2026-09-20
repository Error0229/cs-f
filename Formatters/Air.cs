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
        ConfigText = values => "[format]\n" + string.Concat(values.Select(v => $"{v.Key} = {Emit.TomlValue(v)}\n"))
    };
}
