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
        }
    };
}
