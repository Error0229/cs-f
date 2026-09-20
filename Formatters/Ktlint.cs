using System.Text;
using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// ktlint. Configured only through .editorconfig, found by walking up from the path given in
/// --stdin-path. Pointing that into our private directory, at a file with root = true, is the
/// only reliable isolation: --editorconfig loses to any file found near the working directory.
/// See docs/research/formatters/ktlint.md.
/// </summary>
internal static class Ktlint
{
    public static FormatterSpec Spec() => new()
    {
        Command = "ktlint",
        Args =
        [
            "--stdin", "--format",
            // Without this an slf4j INFO line is printed to stdout ahead of the code
            "--log-level=none",
            // Exit 0 even when a violation that cannot be auto-corrected remains; the code is still formatted
            "--ignore-autocorrect-failures",
            @"--stdin-path={dir}\Snippet.kt"
        ],
        ConfigFileName = ".editorconfig",
        ConfigText = Config,
        NeedsJava = true
    };

    private static string Config(IReadOnlyList<SettingValue> values)
    {
        var sb = new StringBuilder();
        sb.AppendLine("root = true");
        sb.AppendLine();
        sb.AppendLine("[*.{kt,kts}]");
        // --stdin-path gives the snippet a file name, and this rule then objects to it
        sb.AppendLine("ktlint_standard_filename = disabled");
        foreach (var v in values)
            sb.AppendLine($"{v.Key} = {v.Text}");
        return sb.ToString();
    }
}
