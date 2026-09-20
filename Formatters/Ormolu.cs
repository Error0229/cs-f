using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// Ormolu. No style options by design. The two --no- flags stop it applying a .cabal or .ormolu
/// file found by walking up from the working directory.
/// See docs/research/formatters/ormolu.md.
/// </summary>
internal static class Ormolu
{
    public static FormatterSpec Spec() => new()
    {
        Command = "ormolu",
        Args = ["--no-cabal", "--no-dot-ormolu", "--color", "never"]
    };
}
