using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// The options of each dprint plugin worth a control, with the plugin's own key names and defaults
/// at the pinned versions. Everything else is reachable through the extra-options field.
/// Full tables: docs/research/formatters/dprint.md and dprint-gplane-plugins.md.
///
/// "keep" stands for a key the plugin leaves unset (null = leave the code as written).
/// </summary>
internal static class DprintSettings
{
    private const string Layout = "Layout";
    private const string Style = "Style";

    private const string Keep = "keep";
    private static readonly string[] KeepOrBool = [Keep, "true", "false"];

    // lineWidth, indentWidth, useTabs and newLineKind are dprint's global keys: every loaded plugin
    // inherits them, including the ones formatting code embedded in markup.
    private static SettingDefinition LineWidth(int toolDefault) =>
        new("lineWidth", "Line Width", SettingType.Integer, toolDefault, Min: 20, Max: 1000,
            Description: "Width the printer tries to stay under", Group: Layout);

    private static readonly SettingDefinition IndentWidth =
        new("indentWidth", "Indent Width", SettingType.Integer, 2, Min: 1, Max: 16,
            Description: "Number of columns for an indent", Group: Layout);

    private static readonly SettingDefinition UseTabs =
        new("useTabs", "Use Tabs", SettingType.Boolean, false,
            Description: "Indent with tabs instead of spaces", Group: Layout);

    private static readonly SettingDefinition NewLineKind =
        new("newLineKind", "Line Ending", SettingType.Choice, "lf",
            Choices: ["lf", "crlf", "auto", "system"],
            Description: "auto keeps the input's line endings", Group: Layout);

    // The g-plane plugins only know lf and crlf
    private static readonly SettingDefinition LineBreak =
        new("lineBreak", "Line Ending", SettingType.Choice, "lf",
            Choices: ["lf", "crlf"], Group: Layout);

    private static readonly SettingDefinition Extra =
        Emit.Extra("Any other plugin option, as JSON properties: \"key\": value, \"key\": value");

    public static readonly SettingDefinition[] TypeScript =
    [
        LineWidth(120), IndentWidth, UseTabs, NewLineKind,
        new("semiColons", "Semicolons", SettingType.Choice, "prefer",
            Choices: ["always", "prefer", "asi"],
            Description: "asi: only where automatic semicolon insertion needs them", Group: Style),
        new("quoteStyle", "Quote Style", SettingType.Choice, "alwaysDouble",
            Choices: ["alwaysDouble", "alwaysSingle", "preferDouble", "preferSingle"],
            Description: "prefer*: switches to the other quote when that saves escapes", Group: Style),
        new("jsx.quoteStyle", "JSX Quote Style", SettingType.Choice, "preferDouble",
            Choices: ["preferDouble", "preferSingle"],
            Description: "Quotes of JSX attributes", Group: Style),
        new("quoteProps", "Quote Properties", SettingType.Choice, "preserve",
            Choices: ["asNeeded", "consistent", "preserve"],
            Description: "When object property names are quoted", Group: Style),
        new("trailingCommas", "Trailing Commas", SettingType.Choice, "onlyMultiLine",
            Choices: ["never", "always", "onlyMultiLine"], Group: Style),
        new("arrowFunction.useParentheses", "Arrow Function Parentheses", SettingType.Choice, "maintain",
            Choices: ["force", "maintain", "preferNone"],
            Description: "Parentheses around a single arrow function parameter", Group: Style),
        new("spaceSurroundingProperties", "Space Surrounding Properties", SettingType.Boolean, true,
            Description: "{ a: 1 } rather than {a: 1}", Group: Style),
        new("module.sortImportDeclarations", "Sort Imports", SettingType.Choice, "caseInsensitive",
            Choices: ["maintain", "caseSensitive", "caseInsensitive"],
            Description: "maintain leaves import order alone", Group: Style),
        new("useBraces", "Use Braces", SettingType.Choice, "whenNotSingleLine",
            Choices: ["maintain", "whenNotSingleLine", "always", "preferNone"],
            Description: "Braces around the body of if, for and while", Group: "Braces"),
        new("bracePosition", "Brace Position", SettingType.Choice, "sameLineUnlessHanging",
            Choices: ["maintain", "sameLine", "nextLine", "sameLineUnlessHanging"],
            Description: "Where the opening brace goes", Group: "Braces"),
        new("nextControlFlowPosition", "Next Control Flow Position", SettingType.Choice, "sameLine",
            Choices: ["maintain", "sameLine", "nextLine"],
            Description: "Where else, catch and finally go", Group: "Braces"),
        new("singleBodyPosition", "Single Body Position", SettingType.Choice, "maintain",
            Choices: ["maintain", "sameLine", "nextLine"],
            Description: "Where a brace-less body goes: if (x) return;", Group: "Braces"),
        new("operatorPosition", "Operator Position", SettingType.Choice, "nextLine",
            Choices: ["maintain", "sameLine", "nextLine"],
            Description: "Where the operator goes when an expression is split over lines", Group: "Wrapping"),
        new("preferSingleLine", "Prefer Single Line", SettingType.Boolean, false,
            Description: "Join code back onto one line when it fits", Group: "Wrapping"),
        new("preferHanging", "Prefer Hanging", SettingType.Boolean, false,
            Description: "Hanging indent rather than one item per line", Group: "Wrapping"),
        Extra
    ];

    public static readonly SettingDefinition[] Json =
    [
        LineWidth(120), IndentWidth, UseTabs, NewLineKind,
        new("trailingCommas", "Trailing Commas", SettingType.Choice, "jsonc",
            Choices: ["never", "jsonc", "maintain", "always"],
            Description: "jsonc: only in .jsonc files, so never here", Group: Style),
        new("preferSingleLine", "Prefer Single Line", SettingType.Boolean, false,
            Description: "Collapse arrays and objects onto one line when they fit", Group: Style),
        new("commentLine.forceSpaceAfterSlashes", "Space After Comment Slashes", SettingType.Boolean, true,
            Description: "// comment rather than //comment", Group: Style),
        Extra
    ];

    public static readonly SettingDefinition[] Markdown =
    [
        LineWidth(80), NewLineKind,
        new("textWrap", "Text Wrap", SettingType.Choice, "maintain",
            Choices: ["always", "never", "maintain"],
            Description: "always wraps at the line width, never joins paragraphs onto one line", Group: Layout),
        new("emphasisKind", "Emphasis", SettingType.Choice, "underscores",
            Choices: ["asterisks", "underscores"],
            Description: "*italic* or _italic_", Group: Style),
        new("strongKind", "Strong Emphasis", SettingType.Choice, "asterisks",
            Choices: ["asterisks", "underscores"],
            Description: "**bold** or __bold__", Group: Style),
        new("unorderedListKind", "Bullet", SettingType.Choice, "dashes",
            Choices: ["dashes", "asterisks"], Group: Style),
        Extra
    ];

    public static readonly SettingDefinition[] Toml =
    [
        LineWidth(120), IndentWidth, UseTabs, NewLineKind,
        new("comment.forceLeadingSpace", "Space After Comment Hash", SettingType.Boolean, true,
            Description: "# comment rather than #comment", Group: Style),
        Extra
    ];

    public static readonly SettingDefinition[] Dockerfile =
    [
        LineWidth(120), NewLineKind
    ];

    public static readonly SettingDefinition[] Css =
    [
        LineWidth(80), IndentWidth, UseTabs, LineBreak,
        new("quotes", "Quote Style", SettingType.Choice, "alwaysDouble",
            Choices: ["alwaysDouble", "alwaysSingle", "preferDouble", "preferSingle"], Group: Style),
        new("hexCase", "Hex Colour Case", SettingType.Choice, "lower",
            Choices: ["ignore", "lower", "upper"], Group: Style),
        new("hexColorLength", "Hex Colour Length", SettingType.Choice, Keep,
            Choices: [Keep, "short", "long"],
            Description: "#ffffff to #fff, or the reverse", Group: Style),
        new("omitNumberLeadingZero", "Omit Leading Zero", SettingType.Boolean, false,
            Description: "0.5 to .5", Group: Style),
        new("declarationOrder", "Declaration Order", SettingType.Choice, Keep,
            Choices: [Keep, "alphabetical", "smacss", "concentric"],
            Description: "Sort the declarations inside a block", Group: Style),
        new("blockSelectorLinebreak", "Selector Line Breaks", SettingType.Choice, "consistent",
            Choices: ["always", "consistent", "wrap"],
            Description: "Line breaks after the commas of a selector list", Group: "Wrapping"),
        new("preferSingleLine", "Prefer Single Line", SettingType.Boolean, false,
            Description: "Collapse lists onto one line when they fit", Group: "Wrapping"),
        new("singleLineBlockThreshold", "Single Line Block Threshold", SettingType.Integer, 0, Min: 0, Max: 20,
            Description: "Blocks with up to this many declarations stay on one line; 0 = off", Group: "Wrapping"),
        Extra
    ];

    private static readonly SettingDefinition[] MarkupBase =
    [
        LineWidth(80), IndentWidth, UseTabs,
        // Global key rather than markup_fmt's lineBreak, so embedded script and style follow
        NewLineKind with { Choices = ["lf", "crlf"], Description = null },
        new("quotes", "Attribute Quotes", SettingType.Choice, "double",
            Choices: ["double", "single"], Group: Style),
        new("html.void.selfClosing", "Self-Close Void Elements", SettingType.Choice, Keep,
            Choices: KeepOrBool,
            Description: "<br /> or <br>", Group: Style),
        new("whitespaceSensitivity", "Whitespace Sensitivity", SettingType.Choice, "css",
            Choices: ["css", "strict", "ignore"],
            Description: "ignore reads best but may change how inline content renders", Group: Style),
        new("scriptIndent", "Indent Script", SettingType.Boolean, false,
            Description: "Indent the code inside <script>", Group: Style),
        new("styleIndent", "Indent Style", SettingType.Boolean, false,
            Description: "Indent the code inside <style>", Group: Style),
        new("closingBracketSameLine", "Closing Bracket Same Line", SettingType.Boolean, false,
            Description: "Put > of a multi-line tag after the last attribute", Group: "Attributes"),
        new("preferAttrsSingleLine", "Prefer Attributes On One Line", SettingType.Boolean, false,
            Description: "Cannot be combined with Max Attributes Per Line", Group: "Attributes"),
        new("maxAttrsPerLine", "Max Attributes Per Line", SettingType.Integer, 0, Min: 0, Max: 20,
            Description: "0 = no limit", Group: "Attributes"),
    ];

    private static readonly SettingDefinition ComponentSelfClosing =
        new("component.selfClosing", "Self-Close Components", SettingType.Choice, Keep,
            Choices: KeepOrBool,
            Description: "<Foo /> or <Foo></Foo>", Group: "Components");

    public static readonly SettingDefinition[] Html = [.. MarkupBase, Extra];

    public static readonly SettingDefinition[] Vue =
    [
        .. MarkupBase,
        ComponentSelfClosing,
        new("vBindStyle", "v-bind Style", SettingType.Choice, Keep,
            Choices: [Keep, "short", "long"],
            Description: ":x or v-bind:x", Group: "Vue"),
        new("vOnStyle", "v-on Style", SettingType.Choice, Keep,
            Choices: [Keep, "short", "long"],
            Description: "@x or v-on:x", Group: "Vue"),
        new("vSlotStyle", "v-slot Style", SettingType.Choice, Keep,
            Choices: [Keep, "short", "long", "vSlot"],
            Description: "#x or v-slot:x", Group: "Vue"),
        new("vForDelimiterStyle", "v-for Delimiter", SettingType.Choice, Keep,
            Choices: [Keep, "in", "of"], Group: "Vue"),
        new("vueComponentCase", "Component Case", SettingType.Choice, "ignore",
            Choices: ["ignore", "pascalCase", "kebabCase"],
            Description: "Component tag names in templates", Group: "Vue"),
        Extra
    ];

    public static readonly SettingDefinition[] Svelte =
    [
        .. MarkupBase,
        ComponentSelfClosing,
        new("svelteAttrShorthand", "Attribute Shorthand", SettingType.Choice, Keep,
            Choices: KeepOrBool,
            Description: "value={value} to {value}", Group: "Svelte"),
        new("svelteDirectiveShorthand", "Directive Shorthand", SettingType.Choice, Keep,
            Choices: KeepOrBool,
            Description: "bind:name={name} to bind:name", Group: "Svelte"),
        new("strictSvelteAttr", "Strict Attributes", SettingType.Boolean, false,
            Description: "Attribute values in their quoted form", Group: "Svelte"),
        Extra
    ];

    public static readonly SettingDefinition[] Astro =
    [
        .. MarkupBase,
        ComponentSelfClosing,
        new("astroAttrShorthand", "Attribute Shorthand", SettingType.Choice, Keep,
            Choices: KeepOrBool,
            Description: "title={title} to {title}", Group: "Astro"),
        Extra
    ];

    public static readonly SettingDefinition[] Yaml =
    [
        LineWidth(80), IndentWidth, LineBreak,
        new("quotes", "Quote Style", SettingType.Choice, "preferDouble",
            Choices: ["preferDouble", "preferSingle", "forceDouble", "forceSingle"], Group: Style),
        new("formatComments", "Format Comments", SettingType.Boolean, false,
            Description: "#comment to # comment", Group: Style),
        new("indentBlockSequenceInMap", "Indent Sequences In Maps", SettingType.Boolean, true,
            Description: "Indent \"- item\" under its map key", Group: Style),
        new("braceSpacing", "Brace Spacing", SettingType.Boolean, true,
            Description: "{ a: 1 } rather than {a: 1}", Group: Style),
        new("bracketSpacing", "Bracket Spacing", SettingType.Boolean, false,
            Description: "[ 1, 2 ] rather than [1, 2]", Group: Style),
        new("preferSingleLine", "Prefer Single Line", SettingType.Boolean, false,
            Description: "Collapse flow collections onto one line when they fit", Group: Style),
        Extra
    ];

    public static readonly SettingDefinition[] GraphQL =
    [
        LineWidth(80), IndentWidth, UseTabs, LineBreak,
        new("comma", "Commas", SettingType.Choice, "onlySingleLine",
            Choices: ["always", "never", "noTrailing", "onlySingleLine"],
            Description: "Commas between list items", Group: Style),
        new("singleLine", "Single Line", SettingType.Choice, "smart",
            Choices: ["prefer", "smart", "never"],
            Description: "Collapse lists onto one line, or never", Group: Style),
        new("parenSpacing", "Parenthesis Spacing", SettingType.Boolean, false,
            Description: "( a: 1 ) rather than (a: 1)", Group: Style),
        new("bracketSpacing", "Bracket Spacing", SettingType.Boolean, false,
            Description: "[ 1, 2 ] rather than [1, 2]", Group: Style),
        new("braceSpacing", "Brace Spacing", SettingType.Boolean, true,
            Description: "{ x: 1 } rather than {x: 1}", Group: Style),
        new("formatComments", "Format Comments", SettingType.Boolean, false,
            Description: "#comment to # comment", Group: Style),
        Extra
    ];
}
