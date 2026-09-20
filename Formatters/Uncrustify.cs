using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// uncrustify. Options are --set name=value pairs. "-c -" means built-in defaults and no config
/// file, which also makes it ignore the UNCRUSTIFY_CONFIG environment variable.
/// See docs/research/formatters/uncrustify.md.
/// </summary>
internal static class Uncrustify
{
    /// <param name="language">uncrustify -l value: C, CPP, D, CS, JAVA, PAWN, OC, OC+, VALA</param>
    public static FormatterSpec Spec(string language) => new()
    {
        Command = "uncrustify",
        Args = ["-l", language, "-c", "-", "-q"],
        SettingArgs = values => Emit.Pairs("--set", values)
    };
}
