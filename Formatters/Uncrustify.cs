using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// uncrustify. Options are --set name=value pairs. "-c -" means built-in defaults and no config
/// file, which also makes it ignore the UNCRUSTIFY_CONFIG environment variable.
///
/// Those built-in defaults do almost nothing: 405 of its 857 options are "ignore", so out of the
/// box it only re-indents, with tabs. A base profile is therefore always sent first; the settings
/// below show the profile's values as their defaults and override it when changed.
/// See docs/research/formatters/uncrustify.md.
/// </summary>
internal static class Uncrustify
{
    private const string BraceStyle = "_braces";
    private const string IndentColumns = "indent_columns";
    private const string KAndR = "K&R";
    private const string Allman = "Allman";
    private const string AsWritten = "as written";

    private static readonly string[] Iarf = ["ignore", "add", "remove", "force"];

    /// <param name="language">uncrustify -l value: C, CPP, D, CS, JAVA, PAWN, OC, OC+, VALA</param>
    public static FormatterSpec Spec(string language) => new()
    {
        Command = "uncrustify",
        Args = ["-l", language, "-c", "-", "-q"],
        SettingArgs = Pairs,
        Settings = Settings
    };

    private static IEnumerable<string> Pairs(IReadOnlyList<SettingValue> values)
    {
        // --set is "last one wins": profile, then brace style, then what the user changed
        var pairs = Settings
            .Where(def => def.Key != BraceStyle && def.Key != Emit.ExtraKey)
            .Select(def => $"{def.Key}={new SettingValue(def, def.DefaultValue).Text}")
            .Concat(ProfileOnly);

        var braces = values.Find(BraceStyle) ?? KAndR;
        if (braces != AsWritten)
            pairs = pairs.Concat(BraceOptions.Select(o => $"{o}={(braces == Allman ? "force" : "remove")}"));

        pairs = pairs
            .Concat(values.Modelled().Where(v => v.Key != BraceStyle).Select(v => $"{v.Key}={v.Text}"))
            // A tab is one indent level wide, or tab indentation comes out as a mix of tabs and spaces
            .Append($"output_tab_size={values.Find(IndentColumns) ?? "4"}")
            .Concat(Emit.SplitArgs(Emit.ExtraText(values)));

        return pairs.SelectMany(p => new[] { "--set", p });
    }

    // Brace placement is not one knob in uncrustify but a dozen
    private static readonly string[] BraceOptions =
    [
        "nl_if_brace", "nl_else_brace", "nl_brace_else", "nl_elseif_brace", "nl_for_brace", "nl_while_brace",
        "nl_do_brace", "nl_brace_while", "nl_switch_brace", "nl_fdef_brace", "nl_oc_mdef_brace",
        "nl_struct_brace", "nl_enum_brace", "nl_union_brace", "nl_class_brace"
    ];

    // Part of the profile, not worth a control each
    private static readonly string[] ProfileOnly =
    [
        "sp_inside_paren=remove", "sp_inside_sparen=remove", "sp_inside_fparen=remove", "sp_paren_paren=remove",
        "sp_before_comma=remove", "sp_before_semi=remove", "sp_after_semi_for=force",
        "sp_else_brace=force", "sp_brace_else=force", "sp_fparen_brace=force",
        "sp_func_call_paren=remove", "sp_func_def_paren=remove", "sp_func_proto_paren=remove",
        "sp_after_oc_type=remove", "sp_after_oc_colon=remove", "sp_before_oc_colon=remove",
        "sp_before_send_oc_colon=remove", "sp_after_oc_property=force",
        "nl_end_of_file=force", "nl_end_of_file_min=1"
    ];

    private static readonly SettingDefinition[] Settings =
    [
        new(BraceStyle, "Brace Style", SettingType.Choice, KAndR,
            Choices: [KAndR, Allman, AsWritten],
            Description: "K&R: { on the same line. Allman: { on its own line"),

        new(IndentColumns, "Indent Columns", SettingType.Integer, 4, Min: 1, Max: 16),
        new("indent_with_tabs", "Indent With Tabs", SettingType.Integer, 0, Min: 0, Max: 2,
            Description: "0: spaces, 1: tabs to the brace level and spaces after, 2: tabs throughout"),
        new("indent_switch_case", "Indent Case Labels", SettingType.Integer, 4, Min: 0, Max: 16,
            Description: "Columns to indent case from its switch"),
        new("code_width", "Code Width", SettingType.Integer, 0, Min: 0, Max: 1000,
            Description: "Try to keep code within this many columns; 0 = off", Group: "Lines"),
        new("cmt_width", "Comment Width", SettingType.Integer, 0, Min: 0, Max: 256,
            Description: "Wrap comments at this column; 0 = off", Group: "Lines"),
        new("newlines", "Line Ending", SettingType.Choice, "auto",
            Choices: ["auto", "lf", "crlf"],
            Description: "auto keeps the line endings of the input"),
        new("nl_max", "Maximum Consecutive Newlines", SettingType.Integer, 3, Min: 0, Max: 10,
            Description: "3 = at most two blank lines; 0 = no limit", Group: "Lines"),

        new("sp_arith", "Around Arithmetic Operators", SettingType.Choice, "force", Choices: Iarf,
            Description: "a + b", Group: "Operators"),
        new("sp_assign", "Around Assignment", SettingType.Choice, "force", Choices: Iarf,
            Description: "a = b", Group: "Operators"),
        new("sp_compare", "Around Comparison", SettingType.Choice, "force", Choices: Iarf,
            Description: "a == b", Group: "Operators"),
        new("sp_bool", "Around Boolean Operators", SettingType.Choice, "force", Choices: Iarf,
            Description: "a && b", Group: "Operators"),
        new("sp_after_comma", "After Comma", SettingType.Choice, "force", Choices: Iarf, Group: "Operators"),
        new("sp_before_sparen", "Before Control Parenthesis", SettingType.Choice, "force", Choices: Iarf,
            Description: "if (x) rather than if(x)", Group: "Spacing"),
        new("sp_sparen_brace", "Between ) And {", SettingType.Choice, "force", Choices: Iarf,
            Group: "Spacing"),
        new("sp_after_cast", "After Cast", SettingType.Choice, "remove", Choices: Iarf,
            Description: "(int)a rather than (int) a", Group: "Spacing"),
        new("sp_before_ptr_star", "Before Pointer Star", SettingType.Choice, "force", Choices: Iarf,
            Description: "NSString *name", Group: "Spacing"),
        new("sp_after_ptr_star", "After Pointer Star", SettingType.Choice, "remove", Choices: Iarf,
            Group: "Spacing"),

        new("sp_after_oc_scope", "After Method Scope", SettingType.Choice, "force", Choices: Iarf,
            Description: "- (void) rather than -(void)", Group: "Objective-C"),
        new("sp_after_oc_return_type", "After Method Return Type", SettingType.Choice, "remove", Choices: Iarf,
            Description: "- (int)f rather than - (int) f", Group: "Objective-C"),
        new("sp_after_send_oc_colon", "After Message Colon", SettingType.Choice, "remove", Choices: Iarf,
            Description: "[obj do:x] rather than [obj do: x]", Group: "Objective-C"),
        new("align_oc_msg_colon_span", "Align Message Colons", SettingType.Integer, 0, Min: 0, Max: 20,
            Description: "Align the colons of a multi-line message send over this many lines, as Xcode does; 0 = off", Group: "Objective-C"),
        new("nl_oc_msg_args", "One Message Argument Per Line", SettingType.Boolean, false,
            Group: "Objective-C"),

        new("align_assign_span", "Align Assignments", SettingType.Integer, 0, Min: 0, Max: 20,
            Description: "Align = over this many lines; 0 = off", Group: "Align"),
        new("align_right_cmt_span", "Align Trailing Comments", SettingType.Integer, 0, Min: 0, Max: 20,
            Description: "Align trailing comments over this many lines; 0 = off", Group: "Align"),

        new("mod_full_brace_if", "Braces On Single-Statement if", SettingType.Choice, "ignore", Choices: Iarf,
            Description: "add or remove braces around a one-line body", Group: "Code"),
        new("mod_full_brace_for", "Braces On Single-Statement for", SettingType.Choice, "ignore", Choices: Iarf, Group: "Code"),
        new("mod_full_brace_while", "Braces On Single-Statement while", SettingType.Choice, "ignore", Choices: Iarf, Group: "Code"),
        new("mod_remove_extra_semicolon", "Remove Extra Semicolons", SettingType.Boolean, false, Group: "Code"),

        Emit.Extra("Any other uncrustify options as name=value, separated by spaces: sp_inside_braces=add nl_after_func_body=2")
    ];
}
