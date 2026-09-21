using System.Text;
using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// MISS_HIT mh_style (MATLAB). Fixes a file in place and reads miss_hit.cfg from the file's
/// directory and every directory above it, until one says project_root. Ours always does.
/// See docs/research/formatters/mh_style.md.
/// </summary>
internal static class MhStyle
{
    private const string Dialect = "_dialect";
    private const string SuppressRule = "suppress_rule";

    // Rules that only report and cannot fix anything. They would just be noise here.
    private static readonly string[] ReportOnlyRules =
    [
        "copyright_notice", "naming_classes", "naming_functions", "naming_scripts", "naming_parameters",
        "naming_enumerations", "line_length", "file_length", "unicode", "builtin_shadow"
    ];

    public static FormatterSpec Spec() => new()
    {
        Command = "mh_style",
        Args = ["--single", "--fix", "--brief", "--input-encoding", "utf-8", "{file}"],
        SlowToStart = true, // a packed runtime that unpacks itself on every start
        InputFileName = "input.m",
        ConfigFileName = "miss_hit.cfg",
        ConfigText = Config,
        Environment = new Dictionary<string, string> { ["PYTHONIOENCODING"] = "UTF-8" },
        // Exit 1 also means "formatted, but style messages that cannot be auto-fixed remain".
        // Real failures are reported as errors on stdout.
        Success = SuccessRule.Codes(0, 1).FailsOn(@": error:|lex error:"),
        Settings = Settings
    };

    private static string Config(IReadOnlyList<SettingValue> values)
    {
        var sb = new StringBuilder("project_root\n");

        if (values.Find(Dialect) == "octave")
            sb.AppendLine("octave: \"latest\"");

        foreach (var v in values.Where(v => v.Key != Dialect && v.Key != SuppressRule))
            sb.AppendLine($"{v.Key}: {(v.Value is string s ? Emit.TomlString(s) : v.Text)}");

        var suppressed = Emit.Names(values.Find(SuppressRule));
        foreach (var rule in ReportOnlyRules.Concat(suppressed).Distinct())
            sb.AppendLine($"{SuppressRule}: {Emit.TomlString(rule)}");

        return sb.ToString();
    }

    private static readonly SettingDefinition[] Settings =
    [
        new(Dialect, "Dialect", SettingType.Choice, "matlab",
            Choices: ["matlab", "octave"],
            Description: "octave also accepts # comments and != "),
        new("tab_width", "Indent Width", SettingType.Integer, 4, Min: 2, Max: 16,
            Description: "Spaces per indent level; tabs are always replaced"),
        new("newline_style", "Line Ending", SettingType.Choice, "native",
            Choices: ["native", "lf", "crlf"],
            Description: "native is CRLF on Windows"),
        new("indent_function_file_body", "Indent Function File Body", SettingType.Boolean, true,
            Description: "Indent the body of a file that holds only functions"),
        new("align_round_brackets", "Align Under Round Brackets", SettingType.Boolean, true,
            Description: "Align continuation lines under the opening ("),
        new("align_other_brackets", "Align Under Other Brackets", SettingType.Boolean, true,
            Description: "The same for [ and {"),
        new(SuppressRule, "Suppress Rules", SettingType.Text, "",
            Description: "Style rules to switch off, separated by commas: operator_whitespace, redundant_brackets, end_of_statements")
    ];
}
