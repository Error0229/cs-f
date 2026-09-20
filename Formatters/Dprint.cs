using System.Text.Json;
using System.Text.Json.Nodes;
using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// dprint 0.50 + wasm plugins. Plugin options can only be passed through a config file,
/// and passing --config is also what stops dprint from walking up the directory tree
/// looking for somebody else's dprint.json.
/// See docs/research/formatters/dprint.md and dprint-gplane-plugins.md.
/// </summary>
internal static class Dprint
{
    // Plugin URLs (update versions as needed; option keys are checked against these versions)
    public const string TypeScript = "https://plugins.dprint.dev/typescript-0.95.13.wasm";
    public const string Json = "https://plugins.dprint.dev/json-0.21.0.wasm";
    public const string Markdown = "https://plugins.dprint.dev/markdown-0.20.0.wasm";
    public const string Toml = "https://plugins.dprint.dev/toml-0.7.0.wasm";
    public const string Malva = "https://plugins.dprint.dev/g-plane/malva-v0.15.1.wasm";
    public const string MarkupFmt = "https://plugins.dprint.dev/g-plane/markup_fmt-v0.25.1.wasm";
    public const string Yaml = "https://plugins.dprint.dev/g-plane/pretty_yaml-v0.5.1.wasm";
    public const string GraphQL = "https://plugins.dprint.dev/g-plane/pretty_graphql-v0.2.3.wasm";
    public const string Dockerfile = "https://plugins.dprint.dev/dockerfile-0.3.3.wasm";

    // Keys dprint hands to every loaded plugin. Written at the top level so that code embedded
    // in HTML/Vue/Svelte/Astro is indented the same way as the markup around it.
    private static readonly string[] GlobalKeys = ["lineWidth", "indentWidth", "useTabs", "newLineKind"];

    /// <param name="stdinName">Bare fake file name; selects the plugin. Never an absolute path.</param>
    /// <param name="section">Config section of the plugin that owns the settings.</param>
    /// <param name="plugins">First the language's own plugin, then any needed for embedded code.</param>
    public static FormatterSpec Spec(string stdinName, string section, SettingDefinition[] settings, params string[] plugins) => new()
    {
        Command = "dprint",
        Args = ["fmt", "--stdin", stdinName, "--config", "{config}", "--plugins", .. plugins],
        ConfigFileName = "dprint.json",
        ConfigText = values => Config(section, values),
        Settings = settings
    };

    private static string Config(string section, IReadOnlyList<SettingValue> values)
    {
        var root = new JsonObject();
        var plugin = new JsonObject();

        foreach (var v in values.Modelled())
        {
            var target = GlobalKeys.Contains(v.Key) ? root : plugin;
            target[v.Key] = Emit.Json(v);
        }

        // Free text: the inside of the plugin's JSON object, e.g.  "arrowFunction.useParentheses": "force"
        if (Emit.ExtraText(values) is { } extra)
        {
            var parsed = JsonNode.Parse("{" + extra.Trim().TrimEnd(',') + "}",
                documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            foreach (var (key, node) in parsed!.AsObject())
                plugin[key] = node?.DeepClone();
        }

        if (plugin.Count > 0)
            root[section] = plugin;

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
