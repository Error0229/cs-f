using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// PHP CS Fixer. No usable stdin mode: it fixes a file in place. Only --config fully pins its
/// behaviour; --rules alone still loads a config found next to the file or in the working directory.
/// See docs/research/formatters/php-cs-fixer.md.
/// </summary>
internal static class PhpCsFixer
{
    public static FormatterSpec Spec() => new()
    {
        Command = "php-cs-fixer",
        // -n: never prompt. Without it a missing config makes it write one into the working directory.
        Args = ["fix", "{file}", "--config={config}", "--using-cache=no", "-n", "--no-ansi", "--show-progress=none"],
        InputFileName = "input.php",
        ConfigFileName = "config.php",
        ConfigText = Config,
        // A PHP syntax error still exits 0 and leaves the file alone; only the report says so
        Success = SuccessRule.ExitZero.FailsOn("not fixed due to errors")
    };

    private static string Config(IReadOnlyList<SettingValue> values) =>
        """
        <?php
        return (new PhpCsFixer\Config())
            ->setUsingCache(false)
            ->setRules(json_decode('{"@PSR12":true}', true));

        """;
}
