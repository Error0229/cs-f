using System.Text.RegularExpressions;

namespace CodeFormatter.Models;

/// <summary>
/// Everything needed to run one formatter for one language: how to invoke it in isolation,
/// how its settings reach it, and how to tell whether it worked.
///
/// Settings travel one of two ways, and a tool may use both:
///   - SettingArgs:  changed settings become command-line arguments
///   - ConfigText:   changed settings are rendered into ConfigFileName inside the private directory
///
/// Every run gets a fresh private working directory. Placeholders in Args/TrailingArgs:
///   {dir}     the private directory
///   {config}  full path of the generated config file
///   {file}    full path of the source file (only when InputFileName is set)
/// </summary>
public sealed record FormatterSpec
{
    public required string Command { get; init; }

    /// <summary>Fixed arguments, including whatever switches off the tool's config discovery.</summary>
    public string[] Args { get; init; } = [];

    /// <summary>Arguments that must come after the setting arguments (e.g. "-" for stdin).</summary>
    public string[] TrailingArgs { get; init; } = [];

    public Func<IReadOnlyList<SettingValue>, IEnumerable<string>>? SettingArgs { get; init; }

    /// <summary>
    /// When set, the file is always written, even with no changed settings: for most tools the
    /// presence of our own config file is what stops them from picking up somebody else's.
    /// </summary>
    public string? ConfigFileName { get; init; }
    public Func<IReadOnlyList<SettingValue>, string>? ConfigText { get; init; }

    /// <summary>
    /// Null: source goes to stdin and the result is read from stdout.
    /// Otherwise: source is written to this file in the private directory, the tool rewrites it in place.
    /// </summary>
    public string? InputFileName { get; init; }

    public SuccessRule Success { get; init; } = SuccessRule.ExitZero;

    /// <summary>Extra environment variables for the process.</summary>
    public IReadOnlyDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();

    /// <summary>The tool launches a JVM from PATH and needs Java 11 or newer.</summary>
    public bool NeedsJava { get; init; }

    /// <summary>The executable is a self-extracting launcher whose payload can be run directly.</summary>
    public LauncherPayload? Payload { get; init; }

    /// <summary>
    /// Takes well over a second just to start. The UI waits longer after a keystroke before
    /// formatting with such a tool.
    /// </summary>
    public bool SlowToStart { get; init; }

    /// <summary>Fix-up for tools whose output is reliably off (e.g. one newline too many).</summary>
    public Func<string, string>? PostProcess { get; init; }

    public SettingDefinition[] Settings { get; init; } = [];

    /// <summary>Where the tool documents the options behind these settings.</summary>
    public string? DocsUrl { get; init; }

    /// <summary>
    /// One line for the settings dialog about the tool's options as a whole: that there are
    /// deliberately few, what the extra-options field reaches, and the like.
    /// </summary>
    public string? Note { get; init; }
}

/// <summary>
/// Some bundled tools are launchers: on every start they unpack a runtime and the real program
/// into a fixed folder under %TEMP%, then run it. That unpacking is most of their start-up time.
/// Once a launcher has run, its payload can be kept and started directly.
/// </summary>
/// <param name="TempFolder">Folder under %TEMP% the launcher unpacks into.</param>
/// <param name="Files">The files of the payload, all directly in that folder.</param>
/// <param name="Direct">
/// Given the folder holding a copy of the payload, and the Java bin directory when the spec
/// needs Java: the command to run instead of the launcher, and the arguments that go first.
/// </param>
public sealed record LauncherPayload(
    string TempFolder,
    string[] Files,
    Func<string, string?, (string Command, string[] FirstArgs)> Direct);

/// <summary>
/// How to tell a successful run from a failed one. Exit codes alone are not enough: several tools
/// exit non-zero after formatting correctly, and several exit zero after failing.
/// An empty result is a failure when the tool also had something to say (ktlint reports a syntax
/// error on stderr and exits 0); with a clean exit and silence it is what the tool made of the
/// input (a comment-only script minified by shfmt).
/// </summary>
public sealed record SuccessRule(int[] OkExitCodes, Regex? FailurePattern = null)
{
    public static readonly SuccessRule ExitZero = new([0]);

    /// <summary>
    /// What the extension did before specs existed. Kept only for formatter entries the user
    /// wrote by hand in config.toml, whose contract we cannot know.
    /// </summary>
    public static readonly SuccessRule Lenient = new([]);

    public static SuccessRule Codes(params int[] codes) => new(codes);

    public SuccessRule FailsOn(string pattern) =>
        this with { FailurePattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant) };

    /// <param name="diagnostics">Text the tool printed that is not the formatted result.</param>
    public bool IsSuccess(int exitCode, string result, string diagnostics)
    {
        if (FailurePattern is not null && FailurePattern.IsMatch(diagnostics))
            return false;
        if (string.IsNullOrWhiteSpace(result))
            return string.IsNullOrWhiteSpace(diagnostics) && OkExitCodes.Contains(exitCode);
        return OkExitCodes.Length == 0 || OkExitCodes.Contains(exitCode);
    }
}
