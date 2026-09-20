using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// Rufo (Ruby). Options come only from a .rufo file, searched from --filename's directory
/// upwards. There is no "no config" switch: our own .rufo, even empty, is what ends the search.
/// See docs/research/formatters/rufo.md.
/// </summary>
internal static class Rufo
{
    public static FormatterSpec Spec() => new()
    {
        Command = "rufo",
        // -x: exit 0 when the code was changed, instead of 3
        Args = [@"--filename={dir}\stdin.rb", "-x"],
        ConfigFileName = ".rufo",
        ConfigText = values => string.Concat(values.Select(v => $"{v.Key} {v.Text}\n"))
    };
}
