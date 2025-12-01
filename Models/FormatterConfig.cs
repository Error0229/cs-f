namespace CodeFormatter.Models;

public class CodeFormatterConfig
{
    public DefaultsConfig Defaults { get; set; } = new();
    public Dictionary<string, FormatterEntry> Formatters { get; set; } = new();
    public PathsConfig? Paths { get; set; }
}

public class DefaultsConfig
{
    public string LastLanguage { get; set; } = "python";
}

public class FormatterEntry
{
    public string Command { get; set; } = string.Empty;
    public string[] Args { get; set; } = [];
    public bool RequiresNode { get; set; } = false;

    /// <summary>
    /// Formatter-specific settings (e.g., line-length, useTabs, etc.)
    /// Values can be bool, int, or string depending on the setting type.
    /// </summary>
    public Dictionary<string, object> Settings { get; set; } = new();
}

public class PathsConfig
{
    public string? Ruff { get; set; }
    public string? Dprint { get; set; }
}
