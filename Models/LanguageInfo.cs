namespace CodeFormatter.Models;

/// <summary>
/// Immutable record containing all metadata for a language.
/// This is the single source of truth for language information.
/// </summary>
public record LanguageInfo(
    Language Language,
    string DisplayName,
    string ConfigKey,
    string FileExtension,
    string MonacoLanguage
);
