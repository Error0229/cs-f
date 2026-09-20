using System.Text;
using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// The handful of ways formatters spell their options, shared by the specs.
/// </summary>
internal static class Emit
{
    /// <summary>
    /// Key of the free-text setting that carries options we do not model, in the tool's own syntax.
    /// </summary>
    public const string ExtraKey = "_extra";

    public static SettingDefinition Extra(string description) =>
        new(ExtraKey, "Extra options", SettingType.Text, "", Description: description, Group: "Advanced");

    public static string? ExtraText(IReadOnlyList<SettingValue> values) =>
        values.Where(v => v.Key == ExtraKey).Select(v => v.Text.Trim()).FirstOrDefault(t => t.Length > 0);

    public static IEnumerable<SettingValue> Modelled(this IReadOnlyList<SettingValue> values) =>
        values.Where(v => v.Key != ExtraKey);

    public static string? Find(this IReadOnlyList<SettingValue> values, string key) =>
        values.Where(v => v.Key == key).Select(v => v.Text).FirstOrDefault();

    /// <summary>
    /// The setting key is the flag itself. A switch is present or absent; anything else is "flag value".
    /// </summary>
    public static IEnumerable<string> Flags(IEnumerable<SettingValue> values)
    {
        foreach (var v in values)
        {
            if (v.Value is bool on)
            {
                if (on) yield return v.Key;
                continue;
            }
            yield return v.Key;
            yield return v.Text;
        }
    }

    /// <summary>
    /// "flag key=value" pairs, e.g. uncrustify --set, pasfmt -C.
    /// </summary>
    public static IEnumerable<string> Pairs(string flag, IEnumerable<SettingValue> values) =>
        values.SelectMany(v => new[] { flag, $"{v.Key}={v.Text}" });

    /// <summary>
    /// Splits free text into arguments on whitespace, keeping "double quoted" runs together.
    /// </summary>
    public static IEnumerable<string> SplitArgs(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var current = new StringBuilder();
        var quoted = false;
        var started = false;
        foreach (var c in text)
        {
            if (c == '"')
            {
                quoted = !quoted;
                started = true;
            }
            else if (char.IsWhiteSpace(c) && !quoted)
            {
                if (started) yield return current.ToString();
                current.Clear();
                started = false;
            }
            else
            {
                current.Append(c);
                started = true;
            }
        }
        if (started) yield return current.ToString();
    }

    public static IEnumerable<string> Lines(string? text) =>
        (text ?? "").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0);

    public static System.Text.Json.Nodes.JsonNode Json(SettingValue v) => v.Value switch
    {
        bool b => System.Text.Json.Nodes.JsonValue.Create(b),
        int i => System.Text.Json.Nodes.JsonValue.Create(i),
        _ => System.Text.Json.Nodes.JsonValue.Create(v.Text)
    };

    public static string TomlValue(SettingValue v) => v.Value is string s ? TomlString(s) : v.Text;

    /// <summary>
    /// TOML basic string. JSON string escaping is a subset of it, and is also what JSON needs.
    /// </summary>
    public static string TomlString(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (var c in s)
        {
            sb.Append(c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                < ' ' => $"\\u{(int)c:X4}",
                _ => c
            });
        }
        return sb.Append('"').ToString();
    }
}
