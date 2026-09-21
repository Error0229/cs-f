using CodeFormatter.Models;

namespace CodeFormatter.Tests;

/// <summary>
/// What every formatter owes the user regardless of options: a failure is reported as a failure,
/// and the output is the formatted code and nothing else.
/// </summary>
public class FormatterContractTests
{
    public static IEnumerable<object[]> AllLanguages() =>
        LanguageRegistry.All.Select(info => new object[] { info.Language });

    [Theory]
    [MemberData(nameof(AllLanguages))]
    public async Task EveryLanguage_FormatsWithDefaults(Language language)
    {
        var result = await TestFormatter.Create().FormatAsync(SampleCode.For(language), language);

        Assert.True(result.Success, $"Format failed: {result.Output}");
    }

    // Formatters that reject nothing (asmfmt, pasfmt, uncrustify, clang-format, the CSS/HTML/Markdown
    // plugins) are absent on purpose.
    [Theory]
    [InlineData(Language.Python, "def (:")]
    [InlineData(Language.JavaScript, "const = ;")]
    [InlineData(Language.TypeScript, "let x: = ;")]
    [InlineData(Language.Json, "{\"a\": }")]
    [InlineData(Language.Toml, "a = = 1")]
    [InlineData(Language.Yaml, "a: [1, 2")]
    [InlineData(Language.GraphQL, "query { a(")]
    [InlineData(Language.Java, "class {")]
    [InlineData(Language.Sql, "SELECT FROM WHERE (((")]
    [InlineData(Language.CSharpFormatted, "class {")]
    [InlineData(Language.Go, "package main\nfunc {")]
    [InlineData(Language.Shell, "if then")]
    [InlineData(Language.Lua, "local = = 1")]
    [InlineData(Language.R, "x <- (((")]
    [InlineData(Language.Kotlin, "fun {")]
    [InlineData(Language.Haskell, "module where where")]
    [InlineData(Language.Perl, "sub f { { {")]
    [InlineData(Language.Php, "<?php function {")]
    [InlineData(Language.Matlab, "function = (")]
    [InlineData(Language.Ruby, "def (")]
    public async Task BrokenInput_IsReportedAsFailure(Language language, string input)
    {
        var result = await TestFormatter.Create().FormatAsync(input, language);

        Assert.False(result.Success, $"Reported success with output:\n{result.Output}");
        // The user must get the tool's explanation, not their own code back as if it were formatted
        Assert.StartsWith("Formatting Error", result.Output);
    }

    [Fact]
    public async Task Kotlin_OutputIsOnlyTheCode()
    {
        var result = await TestFormatter.Create().FormatAsync("fun main(){println(\"hi\")}", Language.Kotlin);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Equal("fun main() {\n    println(\"hi\")\n}\n", result.Output.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task Kotlin_UnfixableViolationIsNotAFailure()
    {
        // A wildcard import cannot be auto-corrected; ktlint still formats the rest
        var result = await TestFormatter.Create().FormatAsync("import java.util.*\nfun main(){}", Language.Kotlin);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("fun main() {", result.Output);
    }

    [Fact]
    public async Task Sql_KeepsIdentifierCase()
    {
        var result = await TestFormatter.Create().FormatAsync("select OrderId from Orders", Language.Sql);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("OrderId", result.Output);
        Assert.Contains("Orders", result.Output);
    }

    [Fact]
    public async Task Sql_EndsWithOneNewline()
    {
        var result = await TestFormatter.Create().FormatAsync("SELECT a FROM foo\n", Language.Sql);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Equal("SELECT a FROM foo\n", result.Output);
    }

    [Theory]
    [InlineData(Language.Html, "<div></div>\n<script>const x=1;function f(){return x}</script>\n<style>a{color:red}</style>")]
    [InlineData(Language.Vue, "<template><div></div></template>\n<script>const x=1;function f(){return x}</script>\n<style>a{color:red}</style>")]
    [InlineData(Language.Svelte, "<script>const x=1;function f(){return x}</script>\n<div></div>\n<style>a{color:red}</style>")]
    public async Task Markup_FormatsEmbeddedScriptAndStyle(Language language, string input)
    {
        var result = await TestFormatter.Create().FormatAsync(input, language);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("const x = 1;", result.Output);
        Assert.Contains("color: red;", result.Output);
    }

    [Theory]
    [InlineData(Language.Json, "{\"a\":1}")]
    [InlineData(Language.Html, "<div><p>x</p></div><script>const x=1</script><style>a{color:red}</style>")]
    public async Task Dprint_NeedsNoNetwork(Language language, string input)
    {
        // A plugin cache with nothing in it, and a proxy nobody listens on: only the bundled
        // .wasm files can make this work. Before they were bundled, first use meant a download.
        var cache = Path.Combine(Path.GetTempPath(), "CodeFormatter.Tests", Guid.NewGuid().ToString("N"));
        var saved = new[] { "DPRINT_CACHE_DIR", "HTTPS_PROXY", "HTTP_PROXY" }
            .ToDictionary(name => name, Environment.GetEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable("DPRINT_CACHE_DIR", cache);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", "http://127.0.0.1:9");
            Environment.SetEnvironmentVariable("HTTP_PROXY", "http://127.0.0.1:9");

            var result = await TestFormatter.Create().FormatAsync(input, language);

            Assert.True(result.Success, $"Format failed: {result.Output}");
        }
        finally
        {
            foreach (var (name, value) in saved)
                Environment.SetEnvironmentVariable(name, value);
            try { Directory.Delete(cache, recursive: true); } catch { /* Nothing to clean */ }
        }
    }

    [Fact]
    public async Task CSharp_FormatsThroughStdin()
    {
        var result = await TestFormatter.Create().FormatAsync("class A{void M(){int x=1;}}", Language.CSharpFormatted);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("int x = 1;", result.Output);
    }

    [Fact]
    public async Task Delphi_PreservesNonLatinText()
    {
        // pasfmt decodes stdin with the ANSI code page unless told otherwise
        var input = "program P; begin WriteLn('فارسی 中文 日本語'); end.";
        var result = await TestFormatter.Create().FormatAsync(input, Language.Delphi);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("'فارسی 中文 日本語'", result.Output);
    }

    [Fact]
    public async Task Cancellation_StopsTheRun()
    {
        using var cts = new CancellationTokenSource();
        // ktlint takes seconds to start: plenty of time to change our mind
        var task = TestFormatter.Create().FormatAsync("fun main(){}", Language.Kotlin, cts.Token);
        cts.CancelAfter(300);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }
}
