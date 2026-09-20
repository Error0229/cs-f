using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// MISS_HIT mh_style (MATLAB). Fixes a file in place and reads miss_hit.cfg from the file's
/// directory and every directory above it, until one says project_root. Ours always does.
/// See docs/research/formatters/mh_style.md.
/// </summary>
internal static class MhStyle
{
    public static FormatterSpec Spec() => new()
    {
        Command = "mh_style",
        Args = ["--single", "--fix", "--brief", "--input-encoding", "utf-8", "{file}"],
        InputFileName = "input.m",
        ConfigFileName = "miss_hit.cfg",
        ConfigText = values => "project_root\n" + string.Concat(values.Select(v => $"{v.Key}: {v.Text}\n")),
        Environment = new Dictionary<string, string> { ["PYTHONIOENCODING"] = "UTF-8" },
        // Exit 1 also means "formatted, but style messages that cannot be auto-fixed remain",
        // which is nearly always. Real failures are reported as errors on stdout.
        Success = SuccessRule.Codes(0, 1).FailsOn(@": error:|lex error:")
    };
}
