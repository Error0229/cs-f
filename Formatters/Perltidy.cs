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
    private const string Preset = "preset";
    private const string LineEnding = "output-line-ending";
    private const string None = "none";

    public static FormatterSpec Spec() => new()
    {
        Command = "perltidy",
        // -st: result to stdout, -se: errors to stderr instead of a perltidy.ERR file
        Args = ["-npro", "-st", "-se", "-enc=utf8"],
        SlowToStart = true, // a packed Perl runtime: two seconds before it reads a line
        SettingArgs = Flags,
        Settings = Settings
    };

    // Setting keys are perltidy's long option names: --name=value, and --name / --noname for switches
    private static IEnumerable<string> Flags(IReadOnlyList<SettingValue> values)
    {
        // A preset is a bundle of options; it goes first so that explicit ones win
        if (values.Find(Preset) is { } preset)
            yield return "-" + preset;

        foreach (var v in values.Modelled().Where(v => v.Key != Preset))
        {
            yield return v.Value switch
            {
                true => $"--{v.Key}",
                false => $"--no{v.Key}",
                _ => $"--{v.Key}={v.Text}"
            };
        }

        foreach (var arg in Emit.SplitArgs(Emit.ExtraText(values)))
            yield return arg;
    }

    private static readonly SettingDefinition[] Settings =
    [
        new(Preset, "Style Preset", SettingType.Choice, None,
            Choices: [None, "pbp", "gnu"],
            Description: "pbp: Perl Best Practices. gnu: GNU coding standards. The options below override it"),

        new("indent-columns", "Indent Columns", SettingType.Integer, 4, Min: 0, Max: 16,
            Description: "Spaces per indent level", Group: "Layout"),
        new("continuation-indentation", "Continuation Indentation", SettingType.Integer, 2, Min: 0, Max: 16,
            Description: "Extra indent of a continued statement", Group: "Layout"),
        new("maximum-line-length", "Maximum Line Length", SettingType.Integer, 80, Min: 0, Max: 1000,
            Description: "0 = unlimited", Group: "Layout"),
        new("entab-leading-whitespace", "Tabs For Leading Spaces", SettingType.Integer, 0, Min: 0, Max: 16,
            Description: "Turn every N leading spaces into a tab; 0 = spaces only", Group: "Layout"),
        new(LineEnding, "Line Ending", SettingType.Choice, None,
            Choices: [None, "unix", "win", "mac"],
            Description: "none leaves it to perltidy: LF", Group: "Layout"),

        new("opening-brace-on-new-line", "Opening Brace On New Line", SettingType.Boolean, false,
            Description: "Put the { of a block on its own line", Group: "Braces"),
        new("opening-sub-brace-on-new-line", "Opening Sub Brace On New Line", SettingType.Boolean, false,
            Description: "Put the { of a sub on its own line", Group: "Braces"),
        new("brace-left-and-indent", "Brace Left And Indent", SettingType.Boolean, false,
            Description: "Whitesmiths style: brace on its own line, indented with the block", Group: "Braces"),
        new("cuddled-else", "Cuddled Else", SettingType.Boolean, false,
            Description: "} else { on one line", Group: "Braces"),

        new("paren-tightness", "Parenthesis Tightness", SettingType.Integer, 1, Min: 0, Max: 2,
            Description: "0: always a space inside ( ), 2: never", Group: "Spacing"),
        new("square-bracket-tightness", "Square Bracket Tightness", SettingType.Integer, 1, Min: 0, Max: 2,
            Description: "The same for [ ]", Group: "Spacing"),
        new("brace-tightness", "Brace Tightness", SettingType.Integer, 1, Min: 0, Max: 2,
            Description: "The same for the { } of hashes", Group: "Spacing"),
        new("block-brace-tightness", "Block Brace Tightness", SettingType.Integer, 0, Min: 0, Max: 2,
            Description: "The same for the { } of code blocks", Group: "Spacing"),
        new("space-for-semicolon", "Space For Semicolon", SettingType.Boolean, true,
            Description: "Space before the semicolons of for ( ; ; )", Group: "Spacing"),
        new("space-function-paren", "Space Before Function Parenthesis", SettingType.Boolean, false,
            Description: "f (x) rather than f(x)", Group: "Spacing"),
        new("space-keyword-paren", "Space Before Keyword Parenthesis", SettingType.Boolean, false,
            Description: "An extra space between a keyword and its (", Group: "Spacing"),

        new("line-up-parentheses", "Line Up Parentheses", SettingType.Boolean, false,
            Description: "Align continuation lines under the opening parenthesis", Group: "Wrapping"),
        new("vertical-tightness", "Vertical Tightness", SettingType.Integer, 0, Min: 0, Max: 2,
            Description: "Keep an opening token together with the line after it", Group: "Wrapping"),
        new("vertical-tightness-closing", "Vertical Tightness Closing", SettingType.Integer, 0, Min: 0, Max: 3,
            Description: "Keep a closing token together with the line before it", Group: "Wrapping"),
        new("break-before-all-operators", "Break Before All Operators", SettingType.Boolean, false,
            Description: "A split line starts with the operator", Group: "Wrapping"),
        new("break-after-all-operators", "Break After All Operators", SettingType.Boolean, false,
            Description: "A split line ends with the operator", Group: "Wrapping"),
        new("weld-nested-containers", "Weld Nested Containers", SettingType.Boolean, false,
            Description: "Keep ({ and }) together", Group: "Wrapping"),
        new("outdent-long-quotes", "Outdent Long Quotes", SettingType.Boolean, true,
            Description: "Move an over-long string to the left margin", Group: "Wrapping"),

        new("blanks-before-blocks", "Blank Line Before Blocks", SettingType.Boolean, true,
            Description: "Blank line before if, for, while blocks", Group: "Blank Lines"),
        new("blanks-before-comments", "Blank Line Before Comments", SettingType.Boolean, true,
            Description: "Blank line before a full-line comment", Group: "Blank Lines"),
        new("blank-lines-before-subs", "Blank Lines Before Subs", SettingType.Integer, 1, Min: 0, Max: 10,
            Group: "Blank Lines"),
        new("maximum-consecutive-blank-lines", "Maximum Consecutive Blank Lines", SettingType.Integer, 1, Min: 0, Max: 10,
            Group: "Blank Lines"),
        new("keep-old-blank-lines", "Keep Old Blank Lines", SettingType.Integer, 1, Min: 0, Max: 2,
            Description: "0: drop them, 1: keep up to the maximum, 2: keep all", Group: "Blank Lines"),

        new("delete-semicolons", "Delete Extra Semicolons", SettingType.Boolean, true,
            Group: "Semicolons"),
        new("add-semicolons", "Add Missing Semicolons", SettingType.Boolean, true,
            Description: "Before the } that closes a multi-line block", Group: "Semicolons"),

        Emit.Extra("Any other perltidy flags, as on its command line: -csc -wbb=\"+ - / *\"")
    ];
}
