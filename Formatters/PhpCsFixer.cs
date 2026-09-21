using System.Text.Json;
using System.Text.Json.Nodes;
using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// PHP CS Fixer. No usable stdin mode: it fixes a file in place. Only --config fully pins its
/// behaviour; --rules alone still loads a config found next to the file or in the working directory,
/// and indent and line ending can only be set from a config file anyway.
/// See docs/research/formatters/php-cs-fixer.md.
/// </summary>
internal static class PhpCsFixer
{
    private const string RuleSet = "_ruleset";
    private const string Indent = "_indent";
    private const string LineEnding = "_lineEnding";
    private const string AllowRisky = "_allowRisky";
    private const string StrictTypes = "declare_strict_types";

    // A rule left on this value is not mentioned, so the rule set decides
    private const string FromRuleSet = "rule set";
    private const string On = "on";
    private const string Off = "off";

    public static FormatterSpec Spec() => new()
    {
        Command = "php-cs-fixer",
        // -n: never prompt. Without it a missing config makes it write one into the working directory.
        Args = ["fix", "{file}", "--config={config}", "--using-cache=no", "-n", "--no-ansi", "--show-progress=none"],
        InputFileName = "input.php",
        ConfigFileName = "config.php",
        ConfigText = Config,
        // A PHP syntax error still exits 0 and leaves the file alone; only the report says so
        Success = SuccessRule.ExitZero.FailsOn("not fixed due to errors"),
        SlowToStart = true,
        // php-cs-fixer.exe rewrites a 20 MB PHP runtime and the phar on every start
        Payload = new LauncherPayload("php-cs-fixer-launcher",
            ["php.exe", "php8.dll", "libcrypto-3-x64.dll", "libssl-3-x64.dll", "php-cs-fixer.phar"],
            (dir, _) => (Path.Combine(dir, "php.exe"), [Path.Combine(dir, "php-cs-fixer.phar")])),
        DocsUrl = "https://cs.symfony.com/doc/rules/index.html",
        Note = "PHP CS Fixer has about 290 rules. Any of them can go in Extra options.",
        Settings = Settings
    };

    private static string Config(IReadOnlyList<SettingValue> values)
    {
        var rules = new JsonObject { [values.Find(RuleSet) ?? "@PSR12"] = true };

        foreach (var v in values.Modelled().Where(v => !v.Key.StartsWith('_')))
            rules[v.Key] = Rule(v.Key, v.Text);

        // Free text: more rules as JSON properties
        if (Emit.ExtraText(values) is { } extra)
        {
            var parsed = JsonNode.Parse("{" + extra.Trim().TrimEnd(',') + "}",
                documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            foreach (var (key, node) in parsed!.AsObject())
                rules[key] = node?.DeepClone();
        }

        var indent = values.Find(Indent) switch
        {
            "tab" => "\\t",
            "2 spaces" => "  ",
            _ => "    "
        };
        var lineEnding = values.Find(LineEnding) == "crlf" ? "\\r\\n" : "\\n";
        // Asking for a risky rule by name is asking for risky rules
        var risky = values.Find(AllowRisky) == "true" || values.Find(StrictTypes) == On ? "true" : "false";

        // The rules travel as JSON, so that nothing here has to write PHP array syntax
        var json = rules.ToJsonString().Replace("\\", "\\\\").Replace("'", "\\'");

        return $"""
            <?php
            return (new PhpCsFixer\Config())
                ->setUsingCache(false)
                ->setRiskyAllowed({risky})
                ->setIndent("{indent}")
                ->setLineEnding("{lineEnding}")
                ->setRules(json_decode('{json}', true));

            """;
    }

    /// <summary>
    /// A rule is true, false, or an object of its options.
    /// </summary>
    private static JsonNode Rule(string rule, string value) => (rule, value) switch
    {
        (_, On) => JsonValue.Create(true),
        (_, Off) => JsonValue.Create(false),
        ("braces_position", _) => new JsonObject
        {
            ["classes_opening_brace"] = value,
            ["functions_opening_brace"] = value
        },
        ("yoda_style", _) => new JsonObject
        {
            ["equal"] = value == "yoda",
            ["identical"] = value == "yoda",
            ["less_and_greater"] = value == "yoda"
        },
        _ => new JsonObject { [OptionOf[rule]] = value }
    };

    // The one option of each rule that its setting drives
    private static readonly Dictionary<string, string> OptionOf = new()
    {
        ["array_syntax"] = "syntax",
        ["concat_space"] = "spacing",
        ["cast_spaces"] = "space",
        ["binary_operator_spaces"] = "default",
        ["control_structure_continuation_position"] = "position",
        ["operator_linebreak"] = "position",
        ["ordered_imports"] = "sort_algorithm",
        ["increment_style"] = "style",
        ["return_type_declaration"] = "space_before",
        ["phpdoc_align"] = "align",
    };

    private static SettingDefinition Toggle(string rule, string name, string description, string group) =>
        new(rule, name, SettingType.Choice, FromRuleSet, Choices: [FromRuleSet, On, Off], Description: NullIfEmpty(description), Group: group);

    private static SettingDefinition Option(string rule, string name, string[] choices, string description, string group) =>
        new(rule, name, SettingType.Choice, FromRuleSet, Choices: [FromRuleSet, .. choices, Off], Description: NullIfEmpty(description), Group: group);

    private static string? NullIfEmpty(string text) => text.Length == 0 ? null : text;

    private static readonly SettingDefinition[] Settings =
    [
        // The @auto sets are left out: they depend on a composer.json in the working directory
        new(RuleSet, "Rule Set", SettingType.Choice, "@PSR12",
            Choices: ["@PSR12", "@PSR1", "@PSR2", "@PER-CS", "@Symfony", "@PhpCsFixer"],
            Description: "The base style. The rules below add to it or override it"),
        new(Indent, "Indent", SettingType.Choice, "4 spaces",
            Choices: ["4 spaces", "2 spaces", "tab"]),
        new(LineEnding, "Line Ending", SettingType.Choice, "lf",
            Choices: ["lf", "crlf"]),

        Option("array_syntax", "Array Syntax", ["short", "long"], "[] or array()", "Syntax"),
        Option("increment_style", "Increment Style", ["pre", "post"], "++$i or $i++", "Syntax"),
        Option("yoda_style", "Yoda Conditions", ["yoda", "not yoda"], "null === $a or $a === null", "Syntax"),
        Toggle("single_quote", "Single Quotes", "Turn \"a\" into 'a' where nothing is interpolated", "Syntax"),
        Toggle("trailing_comma_in_multiline", "Trailing Comma In Multiline Arrays", "", "Syntax"),

        Option("braces_position", "Class And Function Braces", ["same_line", "next_line_unless_newline_at_signature_end"],
            "Where the { of a class or function goes", "Structure"),
        Option("control_structure_continuation_position", "else, catch, finally", ["same_line", "next_line"],
            "} else { or else on its own line", "Structure"),
        Toggle("single_line_empty_body", "Empty Body On One Line", "function f() {}", "Structure"),

        Option("concat_space", "Around Concatenation", ["none", "one"], "$a.$b or $a . $b", "Spacing"),
        Option("cast_spaces", "After Cast", ["single", "none"], "(int) $a or (int)$a", "Spacing"),
        Option("binary_operator_spaces", "Around Binary Operators",
            ["single_space", "no_space", "align_single_space_minimal"],
            "align lines up = and => over consecutive lines", "Spacing"),
        Option("return_type_declaration", "Before Return Type Colon", ["none", "one"], "): int or ) : int", "Spacing"),
        Toggle("not_operator_with_successor_space", "After Not Operator", "! $a rather than !$a", "Spacing"),
        Option("operator_linebreak", "Operator Of A Wrapped Line", ["beginning", "end"], "", "Structure"),

        Option("ordered_imports", "Sort Imports", ["alpha", "length"], "Order of use statements", "Imports"),
        Toggle("no_unused_imports", "Remove Unused Imports", "", "Imports"),

        Toggle("blank_line_before_statement", "Blank Line Before return, throw, try", "", "Blank Lines"),
        Toggle("no_extra_blank_lines", "Remove Extra Blank Lines", "", "Blank Lines"),
        Toggle("array_indentation", "Re-indent Multiline Arrays", "", "Blank Lines"),
        Option("phpdoc_align", "Align PHPDoc", ["vertical", "left"], "Column alignment of @param and friends", "Blank Lines"),

        new(AllowRisky, "Allow Risky Rules", SettingType.Choice, "false",
            Choices: ["false", "true"],
            Description: "Risky rules can change what the code does. Needed for risky rules named under Extra options", Group: "Advanced"),
        Toggle(StrictTypes, "Add declare(strict_types=1)", "A risky rule: it changes how PHP converts argument types", "Advanced"),
        Emit.Extra("More rules as JSON properties: \"no_superfluous_phpdoc_tags\": true, \"visibility_required\": {\"elements\": [\"method\"]}")
    ];
}
