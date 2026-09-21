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
    // Most defaults differ per code style, so "style" means: not set, the code style decides
    private const string Style = "style";

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
        NeedsJava = true,
        SlowToStart = true,
        // ktlint.exe unpacks a 71 MB jar on every start and then runs "java -jar" on it
        Payload = new LauncherPayload("ktlint-launcher", ["ktlint.jar"], (dir, javaBin) =>
        (
            Path.Combine(javaBin!, "java.exe"),
            [
                // Flags newer than the JVM at hand are skipped instead of stopping it
                "-XX:+IgnoreUnrecognizedVMOptions",
                // JVM log lines go to stdout, which is where the formatted code goes
                "-Xlog:disable",
                "-XX:TieredStopAtLevel=1", "-XX:+UseSerialGC",
                // Class data sharing (JDK 19+): the JVM keeps an archive of the loaded classes and
                // rebuilds it by itself when it no longer matches. Cuts start-up by more than half.
                "-XX:+AutoCreateSharedArchive", $"-XX:SharedArchiveFile={Path.Combine(dir, "ktlint.jsa")}",
                "--add-opens=java.base/java.lang=ALL-UNNAMED",
                "-jar", Path.Combine(dir, "ktlint.jar")
            ]
        )),
        Settings = Settings
    };

    private static string Config(IReadOnlyList<SettingValue> values)
    {
        var sb = new StringBuilder();
        sb.AppendLine("root = true");
        sb.AppendLine();
        sb.AppendLine("[*.{kt,kts}]");
        // --stdin-path gives the snippet a file name, and this rule then objects to it
        sb.AppendLine("ktlint_standard_filename = disabled");
        foreach (var v in values.Modelled())
            sb.AppendLine($"{v.Key} = {v.Text}");
        foreach (var line in (Emit.ExtraText(values) ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            sb.AppendLine(line);
        return sb.ToString();
    }

    private static readonly string[] StyleOrBool = [Style, "true", "false"];
    private static readonly string[] StyleOrCount = [Style, "unset", "1", "2", "3", "4", "5", "6", "8"];

    private static readonly SettingDefinition[] Settings =
    [
        new("ktlint_code_style", "Code Style", SettingType.Choice, "ktlint_official",
            Choices: ["ktlint_official", "intellij_idea", "android_studio"],
            Description: "Sets the defaults of the options below and which rules run", Group: "Layout"),

        new("indent_style", "Indent Style", SettingType.Choice, "space",
            Choices: ["space", "tab"], Group: "Layout"),
        new("indent_size", "Indent Size", SettingType.Integer, 4, Min: 1, Max: 16,
            Group: "Layout"),
        new("max_line_length", "Maximum Line Length", SettingType.Integer, 0, Min: 0, Max: 1000,
            Description: "0 = what the code style says: 140 official, 100 Android, off for IntelliJ", Group: "Layout"),

        new("ij_kotlin_allow_trailing_comma", "Trailing Comma In Declarations", SettingType.Choice, Style,
            Choices: StyleOrBool,
            Description: "true adds them, false removes them", Group: "Trailing Commas"),
        new("ij_kotlin_allow_trailing_comma_on_call_site", "Trailing Comma In Calls", SettingType.Choice, Style,
            Choices: StyleOrBool, Group: "Trailing Commas"),

        new("ktlint_function_signature_rule_force_multiline_when_parameter_count_greater_or_equal_than",
            "One Parameter Per Line From", SettingType.Choice, Style,
            Choices: StyleOrCount,
            Description: "Function signatures with this many parameters get one per line; unset = only when too long", Group: "Wrapping"),
        new("ktlint_class_signature_rule_force_multiline_when_parameter_count_greater_or_equal_than",
            "One Constructor Parameter Per Line From", SettingType.Choice, Style,
            Choices: StyleOrCount, Group: "Wrapping"),
        new("ktlint_chain_method_rule_force_multiline_when_chain_operator_count_greater_or_equal_than",
            "Break Call Chains From", SettingType.Choice, Style,
            Choices: StyleOrCount,
            Description: "a.b().c().d() with this many dots gets one call per line", Group: "Wrapping"),
        new("ktlint_function_signature_body_expression_wrapping", "Expression Body Wrapping", SettingType.Choice, Style,
            Choices: [Style, "default", "multiline", "always"],
            Description: "When the body of fun f() = ... moves to the next line", Group: "Wrapping"),

        new("ktlint_experimental", "Experimental Rules", SettingType.Choice, "disabled",
            Choices: ["disabled", "enabled"], Group: "Advanced"),

        Emit.Extra("More .editorconfig properties, separated by semicolons: ktlint_standard_no-wildcard-imports = disabled; ij_kotlin_imports_layout = *")
    ];
}
