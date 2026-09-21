using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// Ormolu. No style options by design: what it takes only helps it parse the code. The two --no-
/// flags stop it applying a .cabal or .ormolu file found by walking up from the working directory.
/// See docs/research/formatters/ormolu.md.
/// </summary>
internal static class Ormolu
{
    private const string Extensions = "ghc-opt";
    private const string Fixities = "fixity";

    public static FormatterSpec Spec() => new()
    {
        Command = "ormolu",
        Args = ["--no-cabal", "--no-dot-ormolu", "--color", "never"],
        SettingArgs = values =>
        [
            .. Split(values.Find(Extensions), ',', ' ', ';')
                .SelectMany(e => new[] { "-o", "-X" + (e.StartsWith("-X") ? e[2..] : e) }),
            .. Split(values.Find(Fixities), ';')
                .SelectMany(f => new[] { "-f", f })
        ],
        DocsUrl = "https://github.com/tweag/ormolu#readme",
        Note = "Ormolu has no style options, by design. These only help it parse your code.",
        Settings =
        [
            new(Extensions, "Language Extensions", SettingType.Text, "",
                Description: "Extensions needed to parse the code that Ormolu does not enable itself, e.g. Arrows, MagicHash, TemplateHaskell. LANGUAGE pragmas in the source work too"),
            new(Fixities, "Operator Fixities", SettingType.Text, "",
                Description: "Fixity of your own operators, separated by semicolons: infixl 6 +++; infixr 5 |>")
        ]
    };

    private static string[] Split(string? text, params char[] separators) =>
        (text ?? "").Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
