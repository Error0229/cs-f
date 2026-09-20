using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// clang-format. All options travel in one argument: --style={BasedOnStyle: X, Key: Value}.
/// An explicit --style is also the isolation switch: without it clang-format searches the
/// working directory and its parents for a .clang-format file.
/// See docs/research/formatters/clang-format.md.
/// </summary>
internal static class ClangFormat
{
    private const string BasedOnStyle = "BasedOnStyle";

    public static FormatterSpec Spec(string assumeFilename) => new()
    {
        Command = "clang-format",
        Args = [$"--assume-filename={assumeFilename}"],
        SettingArgs = values => [Style(values)],
        Settings = Settings
    };

    private static string Style(IReadOnlyList<SettingValue> values)
    {
        var parts = new List<string> { $"{BasedOnStyle}: {values.Find(BasedOnStyle) ?? "LLVM"}" };
        parts.AddRange(values.Modelled().Where(v => v.Key != BasedOnStyle).Select(v => $"{v.Key}: {v.Text}"));
        if (Emit.ExtraText(values) is { } extra)
            parts.Add(string.Join(", ", Emit.Lines(extra)).Trim().Trim('{', '}', ','));
        return "--style={" + string.Join(", ", parts) + "}";
    }

    private static readonly SettingDefinition[] Settings =
    [
        new(BasedOnStyle, "Style", SettingType.Choice, "LLVM",
            Choices: ["LLVM", "Google", "Chromium", "Mozilla", "WebKit", "Microsoft", "GNU"],
            Description: "Base coding style")
    ];
}
