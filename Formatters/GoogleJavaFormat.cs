using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// google-java-format. Flags only; it reads no config files, so there is nothing to isolate.
/// See docs/research/formatters/google-java-format.md.
/// </summary>
internal static class GoogleJavaFormat
{
    public static FormatterSpec Spec() => new()
    {
        Command = "google-java-format",
        SettingArgs = values => Emit.Flags(values),
        TrailingArgs = ["-"]
    };
}
