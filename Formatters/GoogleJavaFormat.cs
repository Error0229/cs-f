using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// google-java-format. Flags only; it reads no config files, so there is nothing to isolate.
/// See docs/research/formatters/google-java-format.md.
/// </summary>
internal static class GoogleJavaFormat
{
    public static FormatterSpec Spec() => new()
    {
        Command = "google-java-format",
        SettingArgs = values => Emit.Flags(values),
        TrailingArgs = ["-"],
        Settings = Settings
    };

    // Line width, indent and brace style are not configurable, by design
    private static readonly SettingDefinition[] Settings =
    [
        new("--aosp", "AOSP Style", SettingType.Boolean, false,
            Description: "Android style: 4-space indent instead of 2", Group: "Style"),
        new("--skip-sorting-imports", "Skip Sorting Imports", SettingType.Boolean, false,
            Description: "Leave the order of imports alone", Group: "Imports"),
        new("--skip-removing-unused-imports", "Keep Unused Imports", SettingType.Boolean, false,
            Description: "Useful for snippets: an import may be used by code that was not pasted", Group: "Imports"),
        new("--fix-imports-only", "Fix Imports Only", SettingType.Boolean, false,
            Description: "Sort and prune imports, format nothing else", Group: "Imports"),
        new("--skip-reflowing-long-strings", "Skip Reflowing Long Strings", SettingType.Boolean, false,
            Description: "Do not split string literals that pass column 100", Group: "Style"),
        new("--skip-javadoc-formatting", "Skip Javadoc Formatting", SettingType.Boolean, false,
            Description: "Leave Javadoc comments untouched", Group: "Style")
    ];
}
