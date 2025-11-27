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
}

public class PathsConfig
{
    public string? Ruff { get; set; }
    public string? Dprint { get; set; }
}
