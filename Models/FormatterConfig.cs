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
    /// If true, the formatter doesn't support stdin and requires a temp file.
    /// The formatter will modify the file in-place (args should include {file} placeholder).
    /// </summary>
    public bool UsesTempFile { get; set; } = false;

    /// <summary>
    /// File extension for temp files (e.g., "cs", "php", "m").
    /// Required when UsesTempFile is true.
    /// </summary>
    public string TempFileExtension { get; set; } = "txt";

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
