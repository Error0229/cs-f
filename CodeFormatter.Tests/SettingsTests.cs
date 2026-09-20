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
    // JavaScript / TypeScript (dprint typescript)
    [InlineData(Language.TypeScript, "quoteStyle", "alwaysSingle", "const a = \"x\";", "'x'")]
    [InlineData(Language.TypeScript, "semiColons", "asi", "const a = 1;", "= 1\n")]
    [InlineData(Language.TypeScript, "indentWidth", 4, "function f() {\nreturn 1;\n}", "\n    return")]
    [InlineData(Language.TypeScript, "useTabs", true, "function f() {\nreturn 1;\n}", "\n\treturn")]
    [InlineData(Language.TypeScript, "lineWidth", 20, "const x = [1111111, 2222222, 3333333];", "[\n")]
    [InlineData(Language.TypeScript, "newLineKind", "crlf", "const a = 1;\nconst b = 2;\n", "\r\n")]
    [InlineData(Language.TypeScript, "bracePosition", "nextLine", "function f() {\nreturn 1;\n}", "f()\n{")]
    [InlineData(Language.TypeScript, "arrowFunction.useParentheses", "force", "const f = x => x;", "(x) =>")]
    [InlineData(Language.TypeScript, "module.sortImportDeclarations", "maintain", "import b from \"b\";\nimport a from \"a\";", "\"b\";\nimport a")]
    [InlineData(Language.JavaScript, "trailingCommas", "never", "const x = [\n1,\n2,\n];", "2\n]")]
    [InlineData(Language.JavaScript, "useBraces", "always", "if (x) y();", "{")]
    // JSON, Markdown, TOML, Dockerfile (dprint)
    [InlineData(Language.Json, "indentWidth", 4, "{\n\"a\":1\n}", "\n    \"a\"")]
    [InlineData(Language.Json, "trailingCommas", "always", "{\n\"a\":1\n}", "1,\n")]
    [InlineData(Language.Markdown, "emphasisKind", "asterisks", "some _text_ here", "*text*")]
    [InlineData(Language.Markdown, "unorderedListKind", "asterisks", "- a\n- b", "* a")]
    [InlineData(Language.Toml, "comment.forceLeadingSpace", false, "#c\na = 1", "#c\n")]
    [InlineData(Language.Dockerfile, "newLineKind", "crlf", "FROM a\nRUN b\n", "\r\n")]
    // CSS family (dprint malva)
    [InlineData(Language.Css, "quotes", "alwaysSingle", "a{content:\"x\"}", "'x'")]
    [InlineData(Language.Css, "hexCase", "upper", "a{color:#fff}", "#FFF")]
    [InlineData(Language.Css, "hexColorLength", "long", "a{color:#fff}", "#ffffff")]
    [InlineData(Language.Css, "indentWidth", 4, "a{color:red}", "\n    color")]
    [InlineData(Language.Css, "lineWidth", 20, "a{margin:1111px 2222px 3333px 4444px}", "1111px\n")]
    [InlineData(Language.Css, "declarationOrder", "alphabetical", "a{z-index:1;color:red}", "color: red;\n  z-index")]
    [InlineData(Language.Scss, "indentWidth", 4, "a{b{color:red}}", "\n        color")]
    [InlineData(Language.Less, "useTabs", true, "a{color:red}", "\n\tcolor")]
    // HTML family (dprint markup_fmt)
    [InlineData(Language.Html, "quotes", "single", "<div class=\"a\"></div>", "class='a'")]
    [InlineData(Language.Html, "html.void.selfClosing", "true", "<div><br></div>", "<br />")]
    [InlineData(Language.Html, "indentWidth", 4, "<div><p>text</p><p>more</p></div>", "\n    <p>")]
    [InlineData(Language.Html, "maxAttrsPerLine", 1, "<div class=\"a\" id=\"b\"></div>", "\n  id=")]
    [InlineData(Language.Vue, "vBindStyle", "long", "<template><div :a=\"b\"></div></template>", "v-bind:a")]
    [InlineData(Language.Svelte, "svelteAttrShorthand", "true", "<Input value={value}></Input>", "<Input {value}")]
    [InlineData(Language.Astro, "astroAttrShorthand", "true", "<div title={title}></div>", "<div {title}")]
    // YAML, GraphQL (dprint pretty_yaml, pretty_graphql)
    [InlineData(Language.Yaml, "quotes", "forceSingle", "a: \"x\"", "'x'")]
    [InlineData(Language.Yaml, "indentWidth", 4, "a:\n  b: 1", "\n    b")]
    [InlineData(Language.Yaml, "bracketSpacing", true, "a: [1, 2]", "[ 1, 2 ]")]
    [InlineData(Language.GraphQL, "indentWidth", 4, "query {\n  a\n}", "\n    a")]
    [InlineData(Language.GraphQL, "braceSpacing", false, "query { a(x: {y: 1}) }", "{y: 1}")]
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

    [Fact]
    public async Task Markup_LayoutSettingsReachEmbeddedCode()
    {
        // Indent and tabs are dprint-global, so the script inside the page follows the page
        var input = "<div></div>\n<script>function f(){return 1}</script>\n<style>a{color:red}</style>";
        var result = await TestFormatter.With(Language.Html, ("indentWidth", 4)).FormatAsync(input, Language.Html);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("\n    return 1;", result.Output);
        Assert.Contains("\n    color: red;", result.Output);
    }

    [Theory]
    [InlineData(Language.TypeScript, "\"binaryExpression.spaceSurroundingBitwiseAndArithmeticOperator\": false", "const x = 1 + 2;", "1+2")]
    [InlineData(Language.TypeScript, "\"memberExpression.linePerExpression\": true,\n\"enumDeclaration.memberSpacing\": \"blankLine\",", "enum E { A, B }", "A,\n\n  B")]
    [InlineData(Language.Css, "\"selectorOverrideCommentDirective\": \"x\", \"blockSelectorLinebreak\": \"always\"", "a,b{color:red}", "a,\nb")]
    public async Task ExtraOptions_ReachThePlugin(Language language, string extra, string input, string expected)
    {
        var result = await TestFormatter.With(language, (Emit.ExtraKey, extra)).FormatAsync(input, language);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains(expected, result.Output);
    }

    [Theory]
    [InlineData("not json at all", "Invalid extra options")]
    [InlineData("\"noSuchOption\": 1", "Unknown property in configuration")]   // dprint's own words
    public async Task ExtraOptions_ProblemsAreExplained(string extra, string expected)
    {
        var result = await TestFormatter.With(Language.TypeScript, (Emit.ExtraKey, extra)).FormatAsync("const a = 1;", Language.TypeScript);

        Assert.False(result.Success);
        Assert.Contains(expected, result.Output);
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
