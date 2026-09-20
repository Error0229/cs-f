using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// Rufo (Ruby). Options come only from a .rufo file, searched from --filename's directory
/// upwards. There is no "no config" switch: our own .rufo, even empty, is what ends the search.
/// See docs/research/formatters/rufo.md.
/// </summary>
internal static class Rufo
{
    public static FormatterSpec Spec() => new()
    {
        Command = "rufo",
        // -x: exit 0 when the code was changed, instead of 3
        Args = [@"--filename={dir}\stdin.rb", "-x"],
        ConfigFileName = ".rufo",
        // One "name value" per line; enum values are Ruby symbols. A bad value is only a warning
        // and rufo carries on with its default, so nothing here may be free text.
        ConfigText = values => string.Concat(values.Select(v => $"{v.Key} {(v.Value is string ? ":" : "")}{v.Text}\n")),
        Settings = Settings
    };

    // All of rufo's formatting options. Indent (2 spaces) and line width (never wraps) are fixed.
    private static readonly SettingDefinition[] Settings =
    [
        new("quote_style", "Quote Style", SettingType.Choice, "double",
            Choices: ["double", "single", "mixed"],
            Description: "mixed leaves quotes as written"),
        new("trailing_commas", "Trailing Commas", SettingType.Boolean, true,
            Description: "In multi-line arrays, hashes and argument lists"),
        new("parens_in_def", "Parentheses In def", SettingType.Choice, "yes",
            Choices: ["yes", "dynamic"],
            Description: "yes: def foo(a, b). dynamic: keep def foo a, b as written"),
        new("align_case_when", "Align case/when", SettingType.Boolean, false,
            Description: "Align the bodies of consecutive one-line when branches"),
        new("align_chained_calls", "Align Chained Calls", SettingType.Boolean, false,
            Description: "Align leading .method lines under the first dot")
    ];
}
