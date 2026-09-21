using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// clang-format. All options travel in one argument: --style={BasedOnStyle: X, Key: Value}.
/// An explicit --style is also the isolation switch: without it clang-format searches the
/// working directory and its parents for a .clang-format file.
///
/// Every default depends on the base style, so an option left on "style" (or on the number that
/// stands for it) is simply not mentioned. Keys and values are those of clang-format 23.
/// See docs/research/formatters/clang-format.md.
/// </summary>
internal static class ClangFormat
{
    private const string BasedOnStyle = "BasedOnStyle";
    private const string Style = "style";

    public static FormatterSpec Spec(string assumeFilename) => new()
    {
        Command = "clang-format",
        Args = [$"--assume-filename={assumeFilename}"],
        SettingArgs = values => [StyleArgument(values)],
        Settings = Settings
    };

    private static string StyleArgument(IReadOnlyList<SettingValue> values)
    {
        var parts = new List<string> { $"{BasedOnStyle}: {values.Find(BasedOnStyle) ?? "LLVM"}" };
        parts.AddRange(values.Modelled().Where(v => v.Key != BasedOnStyle).Select(v => $"{v.Key}: {v.Text}"));

        // Free text: more "Key: Value" pairs, as they would appear in a .clang-format file
        if (Emit.ExtraText(values) is { } extra)
        {
            // Pasted with its outer braces? Only those go: a value may itself end in a brace.
            var text = extra.Trim().Trim(',', ' ');
            parts.Add(text.StartsWith('{') && text.EndsWith('}') ? text[1..^1].Trim() : text);
        }

        return "--style={" + string.Join(", ", parts) + "}";
    }

    private static SettingDefinition Choice(string key, string name, string group, string? description, params string[] values) =>
        new(key, name, SettingType.Choice, Style, Choices: [Style, .. values], Description: description, Group: group);

    private static SettingDefinition Switch(string key, string name, string group, string? description = null) =>
        Choice(key, name, group, description, "true", "false");

    private static readonly string[] AlignConsecutive =
        ["None", "Consecutive", "AcrossEmptyLines", "AcrossComments", "AcrossEmptyLinesAndComments"];

    private static readonly SettingDefinition[] Settings =
    [
        new(BasedOnStyle, "Base Style", SettingType.Choice, "LLVM",
            Choices: ["LLVM", "Google", "Chromium", "Mozilla", "WebKit", "Microsoft", "GNU"],
            Description: "Decides every option below that is left on \"style\""),

        new("IndentWidth", "Indent Width", SettingType.Integer, 0, Min: 0, Max: 16,
            Description: "0 = as the base style says", Group: "Layout"),
        new("ColumnLimit", "Column Limit", SettingType.Integer, -1, Min: -1, Max: 1000,
            Description: "-1 = as the base style says, 0 = no limit", Group: "Layout"),
        Choice("UseTab", "Use Tabs", "Layout", null,
            "Never", "ForIndentation", "ForContinuationAndIndentation", "AlignWithSpaces", "Always"),
        new("TabWidth", "Tab Width", SettingType.Integer, 0, Min: 0, Max: 16,
            Description: "0 = as the base style says", Group: "Layout"),
        Choice("LineEnding", "Line Ending", "Layout", "Derive: follow the input, falling back to LF or CRLF",
            "LF", "CRLF", "DeriveLF", "DeriveCRLF"),
        new("MaxEmptyLinesToKeep", "Maximum Empty Lines", SettingType.Integer, -1, Min: -1, Max: 10,
            Description: "-1 = as the base style says", Group: "Layout"),

        Choice("BreakBeforeBraces", "Brace Style", "Braces", "Attach: { on the same line. Allman: { on its own line",
            "Attach", "Linux", "Mozilla", "Stroustrup", "Allman", "Whitesmiths", "GNU", "WebKit"),
        Switch("InsertBraces", "Insert Braces", "Braces", "Add braces around one-line bodies of if, for, while. Changes code, not just layout"),
        Switch("RemoveBracesLLVM", "Remove Braces", "Braces", "Remove braces the LLVM coding standard calls optional"),

        Choice("PointerAlignment", "Pointer Alignment", "Pointers And Qualifiers", "int* p, int *p or int * p",
            "Left", "Right", "Middle"),
        Switch("DerivePointerAlignment", "Derive Pointer Alignment", "Pointers And Qualifiers", "Follow what the file mostly does"),
        Choice("QualifierAlignment", "Qualifier Alignment", "Pointers And Qualifiers", "const int or int const",
            "Leave", "Left", "Right"),

        Choice("SpaceBeforeParens", "Space Before Parentheses", "Spacing", null,
            "Never", "ControlStatements", "ControlStatementsExceptControlMacros", "NonEmptyParentheses", "Always"),
        new("SpacesBeforeTrailingComments", "Spaces Before Trailing Comments", SettingType.Integer, -1, Min: -1, Max: 8,
            Description: "-1 = as the base style says", Group: "Spacing"),

        Choice("AllowShortFunctionsOnASingleLine", "Short Functions On One Line", "Short Statements", null,
            "None", "InlineOnly", "Empty", "Inline", "All"),
        Choice("AllowShortIfStatementsOnASingleLine", "Short if On One Line", "Short Statements", null,
            "Never", "WithoutElse", "OnlyFirstIf", "AllIfsAndElse"),
        Choice("AllowShortBlocksOnASingleLine", "Short Blocks On One Line", "Short Statements", null,
            "Never", "Empty", "Always"),
        Switch("AllowShortLoopsOnASingleLine", "Short Loops On One Line", "Short Statements"),
        Switch("AllowShortCaseLabelsOnASingleLine", "Short case Labels On One Line", "Short Statements"),

        Switch("IndentCaseLabels", "Indent case Labels", "Indentation"),
        Choice("NamespaceIndentation", "Namespace Indentation", "Indentation", null,
            "None", "Inner", "All"),
        Choice("IndentPPDirectives", "Indent Preprocessor Directives", "Indentation", null,
            "None", "AfterHash", "BeforeHash"),
        Choice("EmptyLineBeforeAccessModifier", "Empty Line Before public:/private:", "Indentation", null,
            "Never", "Leave", "LogicalBlock", "Always"),

        Choice("AlignConsecutiveAssignments", "Align Consecutive Assignments", "Alignment", null, AlignConsecutive),
        Choice("AlignConsecutiveDeclarations", "Align Consecutive Declarations", "Alignment", null, AlignConsecutive),
        Switch("AlignTrailingComments", "Align Trailing Comments", "Alignment"),

        Switch("BinPackArguments", "Bin-Pack Arguments", "Wrapping", "false: a call that does not fit gets one argument per line"),
        Choice("BinPackParameters", "Bin-Pack Parameters", "Wrapping", null,
            "BinPack", "OnePerLine", "AlwaysOnePerLine"),
        Choice("BreakBeforeBinaryOperators", "Break Before Binary Operators", "Wrapping", null,
            "None", "NonAssignment", "All"),
        Choice("BreakConstructorInitializers", "Break Constructor Initializers", "Wrapping", null,
            "BeforeColon", "BeforeComma", "AfterColon"),
        Choice("ReflowComments", "Reflow Comments", "Wrapping", "Re-wrap comments that pass the column limit",
            "Never", "IndentOnly", "Always"),

        Choice("SortIncludes", "Sort Includes", "Code Changes", null,
            "Never", "CaseSensitive", "CaseInsensitive"),
        Choice("SeparateDefinitionBlocks", "Separate Definition Blocks", "Code Changes", "Blank line between functions, classes, ...",
            "Leave", "Always", "Never"),
        Switch("FixNamespaceComments", "Fix Namespace Comments", "Code Changes", "Add or correct the // namespace foo after a closing brace"),
        Switch("InsertNewlineAtEOF", "Newline At End Of File", "Code Changes"),

        Emit.Extra("Any other option, as in a .clang-format file: AccessModifierOffset: -4, Cpp11BracedListStyle: false")
    ];
}
