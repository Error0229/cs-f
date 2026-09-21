using System.Text.Json;
using System.Text.Json.Nodes;
using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// CSharpier. Reads stdin when given no path. --config-path replaces its search for
/// .csharpierrc / .editorconfig from the working directory upwards, so it is always passed.
/// See docs/research/formatters/csharpier.md.
/// </summary>
internal static class Csharpier
{
    public static FormatterSpec Spec() => new()
    {
        Command = "csharpier",
        Args = ["format", "--config-path", "{config}"],
        ConfigFileName = "csharpierrc.json",
        ConfigText = values =>
        {
            var root = new JsonObject();
            foreach (var v in values)
                root[v.Key] = Emit.Json(v);
            return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        },
        DocsUrl = "https://csharpier.com/docs/Configuration",
        Note = "CSharpier is opinionated: these are all the options it has.",
        Settings = Settings
    };

    // CSharpier is opinionated: these four are all there is for C#
    private static readonly SettingDefinition[] Settings =
    [
        new("printWidth", "Print Width", SettingType.Integer, 100, Min: 20, Max: 1000,
            Description: "Line width the printer tries to stay under"),
        new("indentSize", "Indent Size", SettingType.Integer, 4, Min: 1, Max: 16,
            Description: "Spaces per indent level"),
        new("useTabs", "Use Tabs", SettingType.Boolean, false,
            Description: "Indent with tabs instead of spaces"),
        new("endOfLine", "Line Ending", SettingType.Choice, "auto",
            Choices: ["auto", "lf", "crlf"],
            Description: "auto keeps the line endings of the input")
    ];
}
