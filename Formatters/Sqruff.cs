using System.Text;
using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// sqruff fix. Configured through an ini file; --config replaces the lookup of .sqruff/.sqlfluff
/// in the working directory.
/// See docs/research/formatters/sqruff.md.
/// </summary>
internal static class Sqruff
{
    public static FormatterSpec Spec() => new()
    {
        Command = "sqruff",
        Args = ["fix", "--config", "{config}", "-"],
        ConfigFileName = "config.sqruff",
        ConfigText = Config,
        // Exit 1 is normal whenever a rule that cannot be auto-fixed fires; the SQL is still formatted.
        // On a parse error sqruff echoes the input back, which only stderr gives away.
        Success = SuccessRule.Codes(0, 1).FailsOn(@"\|\s*\?{4}\s*\|"),
        // sqruff prints one newline too many
        PostProcess = sql => sql.EndsWith("\n\n") ? sql[..^1] : sql
    };

    private static string Config(IReadOnlyList<SettingValue> values)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[sqruff]");
        sb.AppendLine("dialect = ansi");
        // CP02 rewrites the case of identifiers (Foo -> foo), which changes meaning on
        // case-sensitive databases. A formatter must not do that unasked.
        sb.AppendLine("exclude_rules = CP02");
        return sb.ToString();
    }
}
