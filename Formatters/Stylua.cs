using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// StyLua. Options go through a generated stylua.toml: at 2.3.1 one option is broken as a CLI
/// flag, and an .editorconfig near the working directory beats explicit flags.
/// See docs/research/formatters/stylua.md.
/// </summary>
internal static class Stylua
{
    public static FormatterSpec Spec() => new()
    {
        Command = "stylua",
        Args = ["--config-path", "{config}", "--no-editorconfig", "-"],
        ConfigFileName = "stylua.toml",
        ConfigText = values => string.Concat(values.Select(v => $"{v.Key} = {Emit.TomlValue(v)}\n"))
    };
}
