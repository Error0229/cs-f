using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// perltidy. Every option is a CLI flag. -npro ignores .perltidyrc in the working directory,
/// the PERLTIDY environment variable and the profile in the user's home.
/// Exit 2 means errors in the Perl source; the input then comes back unchanged, so it is a failure.
/// See docs/research/formatters/perltidy.md.
/// </summary>
internal static class Perltidy
{
    public static FormatterSpec Spec() => new()
    {
        Command = "perltidy",
        // -st: result to stdout, -se: errors to stderr instead of a perltidy.ERR file
        Args = ["-npro", "-st", "-se"]
    };
}
