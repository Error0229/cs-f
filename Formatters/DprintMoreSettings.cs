using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// Options of the dprint plugins for PHP, LaTeX, BibTeX, Typst, Julia and CMake, with each plugin's
/// own key names and defaults at the pinned versions (from the schema each plugin publishes).
/// Groups are sized to one tab of the settings dialog each.
/// </summary>
internal static class DprintMoreSettings
{
    private const string Layout = "Layout";
    private const string Style = "Style";

    private static SettingDefinition LineWidth(int toolDefault) =>
        new("lineWidth", "Line Width", SettingType.Integer, toolDefault, Min: 20, Max: 1000,
            Description: "Width the printer tries to stay under", Group: Layout);

    private static SettingDefinition IndentWidth(int toolDefault) =>
        new("indentWidth", "Indent Width", SettingType.Integer, toolDefault, Min: 1, Max: 16,
            Description: "Spaces per indent level", Group: Layout);

    private static readonly SettingDefinition UseTabs =
        new("useTabs", "Use Tabs", SettingType.Boolean, false,
            Description: "Indent with tabs instead of spaces", Group: Layout);

    private static SettingDefinition LineEnding(string key, string toolDefault, params string[] choices) =>
        new(key, "Line Ending", SettingType.Choice, toolDefault, Choices: choices,
            Description: choices.Contains("auto") ? "auto keeps the line endings of the input" : null, Group: Layout);

    private static SettingDefinition Extra =>
        Emit.Extra("Any other plugin option, as JSON properties: \"key\": value, \"key\": value");

    private static readonly string[] BraceStyle = ["same-line", "next-line", "always-next-line"];

    /// <summary>Mago, 22 of its 98 options.</summary>
    public static readonly SettingDefinition[] Php =
    [
        new("printWidth", "Print Width", SettingType.Integer, 120, Min: 20, Max: 1000,
            Description: "Maximum line length before wrapping", Group: Layout),
        new("tabWidth", "Indent Width", SettingType.Integer, 4, Min: 1, Max: 16,
            Description: "Spaces per indent level", Group: Layout),
        UseTabs,
        LineEnding("endOfLine", "lf", "lf", "crlf", "auto"),

        new("singleQuote", "Single Quotes", SettingType.Boolean, true,
            Description: "Prefer 'a' over \"a\" where nothing is interpolated", Group: Style),
        new("trailingComma", "Trailing Commas", SettingType.Boolean, true,
            Description: "In multi-line arrays, argument and parameter lists", Group: Style),
        new("nullTypeHint", "Nullable Types", SettingType.Choice, "question",
            Choices: ["question", "null-pipe", "null-pipe-last"],
            Description: "?T, null|T or T|null", Group: Style),
        new("uppercaseLiteralKeyword", "Uppercase true, false, null", SettingType.Boolean, false,
            Group: Style),
        new("removeTrailingCloseTag", "Remove Closing ?>", SettingType.Boolean, true,
            Description: "Drop the ?> at the end of a file", Group: Style),

        new("classlikeBraceStyle", "Classes", SettingType.Choice, "always-next-line", Choices: BraceStyle,
            Description: "next-line: only when the signature spans several lines", Group: "Braces"),
        new("methodBraceStyle", "Methods", SettingType.Choice, "next-line", Choices: BraceStyle, Group: "Braces"),
        new("functionBraceStyle", "Functions", SettingType.Choice, "next-line", Choices: BraceStyle, Group: "Braces"),
        new("closureBraceStyle", "Closures", SettingType.Choice, "same-line", Choices: BraceStyle, Group: "Braces"),
        new("controlBraceStyle", "if, for, while", SettingType.Choice, "same-line", Choices: BraceStyle, Group: "Braces"),

        new("followingClauseOnNewline", "else, catch On A New Line", SettingType.Boolean, false,
            Description: "} else { or else on its own line", Group: "Structure"),
        new("methodChainBreakingStyle", "Method Chains", SettingType.Choice, "next-line",
            Choices: ["next-line", "same-line"],
            Description: "Where the first ->call() of a broken chain goes", Group: "Structure"),
        new("lineBeforeBinaryOperator", "Operator Starts The Line", SettingType.Boolean, true,
            Description: "A wrapped expression breaks before its operator rather than after", Group: "Structure"),
        new("preserveBreakingArrayLike", "Keep Multi-line Arrays", SettingType.Boolean, true,
            Description: "An array written over several lines stays that way", Group: "Structure"),
        new("preserveBreakingArgumentList", "Keep Multi-line Arguments", SettingType.Boolean, false,
            Group: "Structure"),

        new("spaceAroundConcatenationBinaryOperator", "Around Concatenation", SettingType.Boolean, true,
            Description: "$a . $b rather than $a.$b", Group: "Spacing"),
        new("spaceAfterCastUnaryPrefixOperators", "After Cast", SettingType.Boolean, true,
            Description: "(int) $a rather than (int)$a", Group: "Spacing"),
        new("spaceAfterLogicalNotUnaryPrefixOperator", "After Not Operator", SettingType.Boolean, false,
            Description: "! $a rather than !$a", Group: "Spacing"),
        new("alignAssignmentLike", "Align Assignments", SettingType.Boolean, false,
            Description: "Line up = and => over consecutive lines", Group: "Spacing"),

        new("sortUses", "Sort Imports", SettingType.Choice, "alphanumeric-ascending",
            Choices: ["alphanumeric-ascending", "alphanumeric-descending", "preserve"], Group: "Imports"),
        new("separateUseTypes", "Separate Import Kinds", SettingType.Boolean, true,
            Description: "Blank line between class, function and const imports", Group: "Imports"),
        new("expandUseGroups", "Expand Grouped Imports", SettingType.Boolean, true,
            Description: "use A\\{B, C}; becomes two use statements", Group: "Imports"),
        new("emptyLineBeforeReturn", "Blank Line Before return", SettingType.Boolean, false, Group: "Blank Lines"),
        new("emptyLineAfterControlStructure", "Blank Line After if, for, while", SettingType.Boolean, false, Group: "Blank Lines"),
        new("emptyLineAfterMethod", "Blank Line After Methods", SettingType.Boolean, true, Group: "Blank Lines"),
        new("emptyLineAfterOpeningTag", "Blank Line After <?php", SettingType.Boolean, true, Group: "Blank Lines"),

        Extra
    ];

    /// <summary>badness: all its options but the language code and the abbreviation list.</summary>
    public static readonly SettingDefinition[] Latex =
    [
        LineWidth(80), IndentWidth(2),
        LineEnding("lineEnding", "auto", "auto", "lf", "crlf", "native"),
        new("wrap", "Paragraph Wrapping", SettingType.Choice, "by file kind",
            Choices: ["by file kind", "reflow", "stable", "sentence", "semantic", "preserve"],
            Description: "by file kind: .tex reflows, .sty and .cls keep their breaks. sentence: one sentence per line", Group: Style),
        new("mathWrap", "Display Math Wrapping", SettingType.Choice, "auto",
            Choices: ["auto", "preserve", "single-line", "break"], Group: Style),
        new("itemIndent", "List Item Continuation", SettingType.Choice, "hang",
            Choices: ["hang", "indent", "none"],
            Description: "How the lines after \\item are indented", Group: Style),
        Extra
    ];

    /// <summary>bibtex-tidy: 22 of its 30 options; the rest take lists or templates.</summary>
    public static readonly SettingDefinition[] Bibtex =
    [
        IndentWidth(2), UseTabs,
        new("align", "Align Values At Column", SettingType.Integer, 14, Min: 1, Max: 40,
            Description: "1 = a single space after the =", Group: Layout),
        new("blankLines", "Blank Line Between Entries", SettingType.Boolean, false, Group: Layout),
        new("wrap", "Wrap Long Values", SettingType.Boolean, false,
            Description: "At column 80", Group: Layout),

        new("curly", "Braces Around Values", SettingType.Boolean, false,
            Description: "Turn \"quoted\" values into {braced} ones", Group: "Values"),
        new("numeric", "Bare Numbers", SettingType.Boolean, false,
            Description: "{1998} becomes 1998", Group: "Values"),
        new("months", "Month Macros", SettingType.Boolean, false,
            Description: "Month names become jan, feb, ...", Group: "Values"),
        new("stripEnclosingBraces", "Strip Double Braces", SettingType.Boolean, false,
            Description: "{{Journal}} becomes {Journal}", Group: "Values"),
        new("dropAllCaps", "Title-Case ALL CAPS", SettingType.Boolean, false, Group: "Values"),

        new("lowercase", "Lowercase Field Names", SettingType.Boolean, true,
            Description: "Also entry types: @ARTICLE becomes @article", Group: "Fields"),
        new("trailingCommas", "Trailing Comma", SettingType.Boolean, false,
            Description: "After the last field of an entry", Group: "Fields"),
        new("sortFields", "Sort Fields", SettingType.Boolean, false,
            Description: "Into the standard order: title, author, year, ...", Group: "Fields"),
        new("removeEmptyFields", "Remove Empty Fields", SettingType.Boolean, false, Group: "Fields"),
        new("removeDuplicateFields", "Remove Duplicate Fields", SettingType.Boolean, true, Group: "Fields"),

        new("sort", "Sort Entries", SettingType.Boolean, false,
            Description: "By citation key", Group: "Entries"),
        new("stripComments", "Remove Comments", SettingType.Boolean, false, Group: "Entries"),
        new("tidyComments", "Tidy Comments", SettingType.Boolean, true,
            Description: "Trim the whitespace around comments", Group: "Entries"),
        new("escape", "Escape Special Characters", SettingType.Boolean, true,
            Description: "An umlaut becomes its LaTeX macro", Group: "Characters"),
        new("unescape", "Unescape To Unicode", SettingType.Boolean, false,
            Description: "The reverse: LaTeX macros become characters", Group: "Characters"),
        new("encodeUrls", "Percent-Encode URLs", SettingType.Boolean, false, Group: "Characters"),

        Extra
    ];

    /// <summary>typstyle: all 7 options.</summary>
    public static readonly SettingDefinition[] Typst =
    [
        LineWidth(80), IndentWidth(2),
        LineEnding("lineEnding", "lf", "lf", "crlf"),
        new("wrapMode", "Text Wrapping", SettingType.Choice, "none",
            Choices: ["none", "fill", "sentence"],
            Description: "fill: pack lines to the width. sentence: one sentence per line", Group: Style),
        new("blankLinesUpperBound", "Maximum Blank Lines", SettingType.Integer, 1, Min: 0, Max: 10,
            Group: Style),
        new("collapseMarkupSpaces", "Collapse Spaces In Markup", SettingType.Boolean, false,
            Description: "Runs of spaces in text become one", Group: Style),
        new("reorderImportItems", "Sort Import Items", SettingType.Boolean, true, Group: Style)
    ];

    /// <summary>fatou: all 3 options.</summary>
    public static readonly SettingDefinition[] Julia =
    [
        LineWidth(92), IndentWidth(4),
        LineEnding("lineEnding", "auto", "auto", "lf", "crlf", "native")
    ];

    /// <summary>cmakefmt: 15 of its 20 options.</summary>
    public static readonly SettingDefinition[] CMake =
    [
        LineWidth(80), IndentWidth(2), UseTabs,
        LineEnding("newLineKind", "lf", "lf", "crlf", "auto"),
        new("maxEmptyLines", "Maximum Blank Lines", SettingType.Integer, 1, Min: 0, Max: 10, Group: Layout),

        new("commandCase", "Command Case", SettingType.Choice, "lower",
            Choices: ["lower", "upper", "unchanged"],
            Description: "add_executable or ADD_EXECUTABLE", Group: Style),
        new("keywordCase", "Keyword Case", SettingType.Choice, "upper",
            Choices: ["upper", "lower", "unchanged"],
            Description: "PUBLIC, REQUIRED, ...", Group: Style),
        new("dangleParens", "Closing Parenthesis On Its Own Line", SettingType.Boolean, false,
            Description: "For a command that spans several lines", Group: Style),
        new("dangleAlign", "Align That Parenthesis With", SettingType.Choice, "prefix",
            Choices: ["prefix", "open", "close"],
            Description: "prefix: the start of the command", Group: Style),
        new("continuationAlign", "Continuation Lines", SettingType.Choice, "under-first-value",
            Choices: ["under-first-value", "same-indent"], Group: Style),

        new("wrapAfterFirstArg", "Wrap After First Argument", SettingType.Boolean, false, Group: "Wrapping"),
        new("maxLinesHwrap", "Lines Before One-Per-Line", SettingType.Integer, 2, Min: 0, Max: 20,
            Description: "Arguments packed onto more lines than this get one line each", Group: "Wrapping"),
        new("maxRowsCmdline", "Rows Of A Command Line", SettingType.Integer, 2, Min: 0, Max: 20,
            Description: "In COMMAND arguments, before they are split one per line", Group: "Wrapping"),
        new("enableSort", "Sort Sortable Lists", SettingType.Boolean, false,
            Description: "Argument lists the command marks as sortable", Group: "Wrapping"),
        new("autosort", "Guess Sortable Lists", SettingType.Boolean, false,
            Description: "Also sort lists that merely look like file names", Group: "Wrapping"),

        Extra
    ];
}
