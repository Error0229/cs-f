using CodeFormatter.Formatters;
using CodeFormatter.Models;

namespace CodeFormatter.Tests;

/// <summary>
/// A setting shown in the UI must reach its formatter, be accepted by it, and change the output.
/// </summary>
public class SettingsTests
{
    /// <summary>
    /// Every value the UI can produce for every setting of every language, one at a time.
    /// Most formatters refuse to run at all on one unknown key or value, so this is what catches
    /// a definition that does not match the bundled binary.
    /// </summary>
    public static IEnumerable<object[]> EveryValue()
    {
        foreach (var info in LanguageRegistry.All)
        {
            foreach (var def in FormatterSpecs.SettingsFor(info.Language))
            {
                foreach (var value in ValuesOf(def).Where(v => !Equals(v, def.DefaultValue)))
                    yield return [info.Language, def.Key, value];
            }
        }
    }

    private static IEnumerable<object> ValuesOf(SettingDefinition def) => def.Type switch
    {
        SettingType.Boolean => [true, false],
        SettingType.Choice => def.Choices ?? [],
        SettingType.Integer => new object[] { def.Min ?? 1, def.Max ?? 100, Math.Clamp(3, def.Min ?? 3, def.Max ?? 3) }.Distinct(),
        _ => []
    };

    [Theory]
    [MemberData(nameof(EveryValue))]
    public async Task EveryValue_IsAcceptedByTheFormatter(Language language, string key, object value)
    {
        var result = await TestFormatter.With(language, (key, value)).FormatAsync(SampleCode.For(language), language);

        Assert.True(result.Success, $"{language} rejected {key} = {value}:\n{result.Output}");
    }

    [Theory]
    // Python (ruff)
    [InlineData(Language.Python, "line-length", 20, "x = [1111111, 2222222, 3333333]", "[\n")]
    [InlineData(Language.Python, "format.quote-style", "single", "x = \"a\"", "x = 'a'")]
    [InlineData(Language.Python, "format.indent-style", "tab", "if x:\n    y = 1", "\ty = 1")]
    [InlineData(Language.Python, "format.line-ending", "cr-lf", "x = 1\n", "x = 1\r\n")]
    // C / C++ (clang-format)
    [InlineData(Language.C, "BasedOnStyle", "WebKit", "int main(){int x=1;return x;}", "\n{\n    int x = 1;")]
    [InlineData(Language.Cpp, "BasedOnStyle", "GNU", "int main(){return 0;}", "main ()")]
    // Go (gofumpt)
    [InlineData(Language.Go, "-extra", true, "package main\n\nfunc f(a int, b int) {}\n", "a, b int")]
    [InlineData(Language.Go, "-lang", "go1", "package main\n\nconst x = 0777\n", "0777")]
    // Shell (shfmt)
    [InlineData(Language.Shell, "-i", 4, "if true; then\necho hi\nfi", "\n    echo hi")]
    [InlineData(Language.Shell, "-ci", true, "case $x in\na) echo a ;;\nesac", "\n\ta)")]
    [InlineData(Language.Shell, "-sr", true, "echo hi >out.txt", "> out.txt")]
    [InlineData(Language.Shell, "-fn", true, "foo() {\necho hi\n}", "foo()\n{")]
    public async Task Setting_ChangesTheOutput(Language language, string key, object value, string input, string expected)
    {
        var withDefault = await TestFormatter.Create().FormatAsync(input, language);
        var withSetting = await TestFormatter.With(language, (key, value)).FormatAsync(input, language);

        Assert.True(withDefault.Success, $"Format failed: {withDefault.Output}");
        Assert.True(withSetting.Success, $"Format failed: {withSetting.Output}");
        Assert.DoesNotContain(expected, withDefault.Output);
        Assert.Contains(expected, withSetting.Output);
    }

    [Theory]
    [InlineData(Language.Shell, "if true; then\necho hi\nfi", "\n\techo hi")]          // shfmt's own default is tabs
    [InlineData(Language.Go, "package main\n\nconst x = 0777\n", "0o777")]
    public async Task Defaults_AreTheToolsOwn(Language language, string input, string expected)
    {
        var result = await TestFormatter.Create().FormatAsync(input, language);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains(expected, result.Output);
    }
}
