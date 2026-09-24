namespace CodeFormatter.Models;

/// <summary>
/// Defines the type of a formatter setting
/// </summary>
public enum SettingType
{
    Boolean,
    Integer,
    Choice, // String with predefined options
    Text    // Free text, passed to the tool in its own syntax
}

/// <summary>
/// Defines a single formatter setting.
/// Key is the tool's own name for the option, verbatim (a config key or a CLI flag).
/// A value equal to DefaultValue is never sent to the tool, so the tool's own default applies.
/// </summary>
public record SettingDefinition(
    string Key,
    string DisplayName,
    SettingType Type,
    object DefaultValue,
    int? Min = null,
    int? Max = null,
    string[]? Choices = null,
    string? Description = null,
    string? Group = null
)
{
    /// <summary>
    /// True when the value is usable for this setting. Anything else is dropped rather than
    /// handed to the tool: most formatters abort the whole run on one bad option.
    /// </summary>
    public bool Accepts(object? value) => Type switch
    {
        SettingType.Boolean => value is bool,
        SettingType.Integer => value is int i && i >= (Min ?? int.MinValue) && i <= (Max ?? int.MaxValue),
        SettingType.Choice => value is string s && Choices is not null && Choices.Contains(s),
        SettingType.Text => value is string,
        _ => false
    };
}

/// <summary>
/// A setting the user changed from its default, paired with its definition.
/// </summary>
public readonly record struct SettingValue(SettingDefinition Definition, object Value)
{
    public string Key => Definition.Key;

    /// <summary>
    /// The value as most tools spell it: true/false, a plain number, or the text itself.
    /// </summary>
    public string Text => Value switch
    {
        bool b => b ? "true" : "false",
        IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => Value.ToString() ?? ""
    };
}
