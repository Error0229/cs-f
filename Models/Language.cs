namespace CodeFormatter.Models;

public enum Language
{
    Python,
    JavaScript,
    TypeScript,
    Java,
    Sql
}

public static class LanguageExtensions
{
    public static string ToDisplayName(this Language language) => language switch
    {
        Language.Python => "Python",
        Language.JavaScript => "JavaScript",
        Language.TypeScript => "TypeScript",
        Language.Java => "Java",
        Language.Sql => "SQL",
        _ => language.ToString()
    };

    public static string ToConfigKey(this Language language) => language switch
    {
        Language.Python => "python",
        Language.JavaScript => "javascript",
        Language.TypeScript => "typescript",
        Language.Java => "java",
        Language.Sql => "sql",
        _ => language.ToString().ToLowerInvariant()
    };
}
