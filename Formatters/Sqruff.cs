using System.Text;
using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// sqruff fix. Configured through an ini file; --config replaces the lookup of .sqruff/.sqlfluff
/// in the working directory. Setting keys are "section/key" of that file.
/// See docs/research/formatters/sqruff.md.
/// </summary>
internal static class Sqruff
{
    private const string DialectKey = "sqruff/dialect";
    private const string Rules = "sqruff/rules";
    private const string ExcludeRules = "sqruff/exclude_rules";
    private const string IdentifierCase = "sqruff:rules:capitalisation.identifiers/extended_capitalisation_policy";
    private const string CommaPosition = "sqruff:layout:type:comma/line_position";
    private const string OperatorPosition = "sqruff:layout:type:binary_operator/line_position";
    private const string Keep = "keep";

    public static FormatterSpec Spec() => new()
    {
        Command = "sqruff",
        // -f human: with GITHUB_ACTIONS set, sqruff would switch to annotation format by itself
        Args = ["fix", "-f", "human", "--config", "{config}", "-"],
        ConfigFileName = "config.sqruff",
        ConfigText = Config,
        // Exit 1 is normal whenever a rule that cannot be auto-fixed fires; the SQL is still formatted.
        // On a parse error sqruff echoes the input back, which only stderr gives away.
        Success = SuccessRule.Codes(0, 1).FailsOn(@"\?{4}"),
        // sqruff prints one newline too many
        PostProcess = sql => sql.EndsWith("\n\n") ? sql[..^1] : sql,
        DocsUrl = "https://github.com/quarylabs/sqruff/blob/main/docs/reference/sample-configurations.md",
        Note = "sqruff reads a .sqruff file with about 115 keys. More of its lines can go in Extra options.",
        Settings = Settings
    };

    private static string Config(IReadOnlyList<SettingValue> values)
    {
        var sections = new Dictionary<string, List<string>> { ["sqruff"] = [] };
        void Add(string sectionAndKey, string value)
        {
            var (section, key) = (sectionAndKey[..sectionAndKey.IndexOf('/')], sectionAndKey[(sectionAndKey.IndexOf('/') + 1)..]);
            if (!sections.TryGetValue(section, out var lines))
                sections[section] = lines = [];
            lines.Add($"{key} = {value}");
        }

        // Comma and operator position are enforced by LT04 and LT03, which the core rules leave out
        var rules = new List<string> { values.Find(Rules) ?? "core" };
        if (values.Find(CommaPosition) is not null) rules.Add("LT04");
        if (values.Find(OperatorPosition) is not null) rules.Add("LT03");
        Add(Rules, string.Join(",", rules));

        // CP02 rewrites the case of identifiers (Foo -> foo), which changes meaning on
        // case-sensitive databases. A formatter must not do that unless asked.
        var excluded = Emit.Names(values.Find(ExcludeRules)).ToList();
        if (values.Find(IdentifierCase) is null)
            excluded.Add("CP02");
        // sqruff 0.29.3 panics in LT06 (space before a function's parenthesis) on any function call
        // under the redshift dialect. Without the rule, Redshift SQL formats; with it, nothing does.
        if (values.Find(DialectKey) == "redshift")
            excluded.Add("LT06");
        if (excluded.Count > 0)
            Add(ExcludeRules, string.Join(",", excluded));

        foreach (var v in values.Modelled().Where(v => v.Key != Rules && v.Key != ExcludeRules))
            Add(v.Key, v.Text);

        var sb = new StringBuilder();
        foreach (var (section, lines) in sections)
        {
            sb.AppendLine($"[{section}]");
            lines.ForEach(l => sb.AppendLine(l));
            sb.AppendLine();
        }

        // Free text: more lines of the same file, "; " standing in for a line break
        foreach (var line in (Emit.ExtraText(values) ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            sb.AppendLine(line);

        return sb.ToString();
    }

    private static readonly string[] Case = ["consistent", "upper", "lower", "capitalise"];
    private static readonly string[] ExtendedCase = ["consistent", "upper", "lower", "capitalise", "pascal"];

    private static readonly SettingDefinition[] Settings =
    [
        // The dialects sqruff 0.29 accepts; any other name makes it panic
        new(DialectKey, "Dialect", SettingType.Choice, "ansi",
            Choices: ["ansi", "athena", "bigquery", "clickhouse", "databricks", "duckdb", "mysql", "postgres",
                "redshift", "snowflake", "sparksql", "sqlite", "trino", "tsql"]),
        new(Rules, "Rules", SettingType.Choice, "core",
            Choices: ["core", "layout", "all"],
            Description: "layout: whitespace only. all: also rewrites constructs, e.g. drops ELSE NULL"),
        new(ExcludeRules, "Exclude Rules", SettingType.Text, "",
            Description: "Rule codes to switch off, separated by commas: LT05, AL01"),

        new("sqruff/max_line_length", "Maximum Line Length", SettingType.Integer, 80, Min: 0, Max: 1000,
            Description: "0 = never wrap", Group: "Layout"),
        new("sqruff:indentation/indent_unit", "Indent Unit", SettingType.Choice, "space",
            Choices: ["space", "tab"], Group: "Layout"),
        new("sqruff:indentation/tab_space_size", "Indent Size", SettingType.Integer, 4, Min: 1, Max: 16,
            Description: "Spaces per indent level", Group: "Layout"),
        new("sqruff:indentation/indented_joins", "Indent Joins", SettingType.Boolean, false,
            Description: "Indent JOIN under FROM", Group: "Indentation"),
        new("sqruff:indentation/indented_ctes", "Indent CTEs", SettingType.Boolean, false, Group: "Indentation"),
        new("sqruff:indentation/allow_implicit_indents", "Allow Implicit Indents", SettingType.Boolean, false,
            Description: "Allow WHERE a = 1 with an indented AND under it, instead of a break after WHERE", Group: "Indentation"),
        new(CommaPosition, "Comma Position", SettingType.Choice, "trailing",
            Choices: ["trailing", "leading"], Group: "Layout"),
        new(OperatorPosition, "Operator Position", SettingType.Choice, "leading",
            Choices: ["leading", "trailing"],
            Description: "AND, OR, + at the start or the end of a line", Group: "Layout"),

        new("sqruff:rules:capitalisation.keywords/capitalisation_policy", "Keywords", SettingType.Choice, "consistent",
            Choices: Case,
            Description: "consistent: whichever case the first one uses", Group: "Capitalisation"),
        new("sqruff:rules:capitalisation.functions/extended_capitalisation_policy", "Function Names", SettingType.Choice, "consistent",
            Choices: ExtendedCase, Group: "Capitalisation"),
        new("sqruff:rules:capitalisation.literals/capitalisation_policy", "NULL, TRUE, FALSE", SettingType.Choice, "consistent",
            Choices: Case, Group: "Capitalisation"),
        new("sqruff:rules:capitalisation.types/extended_capitalisation_policy", "Data Types", SettingType.Choice, "consistent",
            Choices: ExtendedCase, Group: "Capitalisation"),
        new(IdentifierCase, "Identifiers", SettingType.Choice, Keep,
            Choices: [Keep, .. ExtendedCase],
            Description: "Table and column names. Anything but keep can change meaning on a case-sensitive database", Group: "Capitalisation"),

        Emit.Extra("More lines for the sqruff config file, separated by semicolons: [sqruff:rules:convention.not_equal]; preferred_not_equal_style = c_style")
    ];
}
