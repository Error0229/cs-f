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
    // C# (csharpier)
    [InlineData(Language.CSharpFormatted, "indentSize", 2, "class A{void M(){}}", "\n  void M")]
    [InlineData(Language.CSharpFormatted, "useTabs", true, "class A{void M(){}}", "\n\tvoid M")]
    [InlineData(Language.CSharpFormatted, "printWidth", 20, "class A{void M(){Foo(1111111,2222222,3333333);}}", "Foo(\n")]
    [InlineData(Language.CSharpFormatted, "endOfLine", "crlf", "class A{}\n", "\r\n")]
    // Lua (stylua)
    [InlineData(Language.Lua, "indent_type", "Spaces", "if x then\nreturn 1\nend", "\n    return")]
    [InlineData(Language.Lua, "quote_style", "ForceSingle", "print(\"a\")", "'a'")]
    [InlineData(Language.Lua, "call_parentheses", "None", "print(\"a\")", "print \"a\"")]
    [InlineData(Language.Lua, "collapse_simple_statement", "Always", "if x then\nreturn\nend", "if x then return end")]
    [InlineData(Language.Lua, "block_newline_gaps", "Preserve", "if x then\n\nf()\n\nend", "then\n\n")]
    [InlineData(Language.Lua, "sort_requires.enabled", true, "local b = require(\"b\")\nlocal a = require(\"a\")", "local a = require(\"a\")\nlocal b")]
    [InlineData(Language.Lua, "line_endings", "Windows", "local a = 1\nlocal b = 2\n", "\r\n")]
    // R (air)
    [InlineData(Language.R, "indent-width", 4, "f <- function(a) {\nx\n}", "\n    x")]
    [InlineData(Language.R, "indent-style", "tab", "f <- function(a) {\nx\n}", "\n\tx")]
    [InlineData(Language.R, "line-width", 20, "x <- c(1111111, 2222222, 3333333)", "c(\n")]
    [InlineData(Language.R, "skip", "c, list", "x <- c(1,2)", "c(1,2)")]
    // Ruby (rufo)
    [InlineData(Language.Ruby, "quote_style", "single", "x = \"a\"", "'a'")]
    [InlineData(Language.Ruby, "trailing_commas", false, "x = [\n1,\n2,\n]", "2\r\n]")]
    // SQL (sqruff)
    [InlineData(Language.Sql, "sqruff:rules:capitalisation.keywords/capitalisation_policy", "upper", "select a from b", "SELECT a FROM b")]
    [InlineData(Language.Sql, "sqruff:rules:capitalisation.identifiers/extended_capitalisation_policy", "upper", "select a from Foo", "FOO")]
    [InlineData(Language.Sql, "sqruff:indentation/tab_space_size", 2, "select aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa, bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb, cccccccccccccccccccccccccccccc from t", "\n  aaaa")]
    [InlineData(Language.Sql, "sqruff:layout:type:comma/line_position", "leading", "select aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa, bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb, cccccccccccccccccccccccccccccc from t", ", bbbb")]
    [InlineData(Language.Sql, "sqruff/max_line_length", 200, "select aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa, bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb, cccccccccccccccccccccccccccccc from t", "cccc from t")]
    [InlineData(Language.Sql, "_extra", "[sqruff:rules:capitalisation.keywords]; capitalisation_policy = upper", "select a from b", "SELECT a FROM b")]
    // Kotlin (ktlint)
    [InlineData(Language.Kotlin, "indent_size", 2, "fun main() {\nprintln(1)\n}", "\n  println")]
    [InlineData(Language.Kotlin, "ktlint_function_signature_rule_force_multiline_when_parameter_count_greater_or_equal_than", "unset", "fun foo(a: Int, b: String): String = a.toString()", "foo(a: Int, b: String)")]
    [InlineData(Language.Kotlin, "_extra", "indent_size = 8", "fun main() {\nprintln(1)\n}", "\n        println")]
    // MATLAB (mh_style)
    [InlineData(Language.Matlab, "tab_width", 2, "function r=f(a)\nif a>1\nr=1;\nend\nend", "\n    r = 1;")]
    [InlineData(Language.Matlab, "newline_style", "lf", "function r=f(a)\nr=1;\nend", "r = 1;\n")]
    [InlineData(Language.Matlab, "suppress_rule", "operator_whitespace", "function r=f(a,b)\nr = a+b;\nend", "a+b")]
    // PHP (php-cs-fixer)
    [InlineData(Language.Php, "_indent", "2 spaces", "<?php\nclass X{function m(){return 1;}}", "\n    return 1;")]
    [InlineData(Language.Php, "_lineEnding", "crlf", "<?php\n$a = 1;\n$b = 2;\n", "\r\n")]
    [InlineData(Language.Php, "array_syntax", "long", "<?php\n$x = [1, 2];", "array(1, 2)")]
    [InlineData(Language.Php, "concat_space", "one", "<?php\n$a = $b.$c;", "$b . $c")]
    [InlineData(Language.Php, "single_quote", "on", "<?php\n$a = \"x\";", "'x'")]
    [InlineData(Language.Php, "yoda_style", "yoda", "<?php\nif ($a === null) {\n}", "null === $a")]
    [InlineData(Language.Php, "declare_strict_types", "on", "<?php\n$a = 1;", "declare(strict_types=1);")]
    [InlineData(Language.Php, "_ruleset", "@Symfony", "<?php\n$a = $b.$c;\nreturn $a;", "$b.$c;\n\nreturn")]
    [InlineData(Language.Php, "_extra", "\"array_syntax\": {\"syntax\": \"long\"}", "<?php\n$x = [1, 2];", "array(1, 2)")]
    // Java (google-java-format)
    [InlineData(Language.Java, "--aosp", true, "class A{void m(){int x=1;}}", "\n    void m()")]
    [InlineData(Language.Java, "--skip-removing-unused-imports", true, "import java.util.List;\nclass A{}", "import java.util.List;")]
    // Delphi (pasfmt)
    [InlineData(Language.Delphi, "tab_width", 4, "program P;\nbegin\nWriteLn('a');\nend.", "\n    WriteLn")]
    [InlineData(Language.Delphi, "use_tabs", true, "program P;\nbegin\nWriteLn('a');\nend.", "\n\tWriteLn")]
    [InlineData(Language.Delphi, "line_ending", "lf", "program P;\nbegin\nWriteLn('a');\nend.", ";\nbegin")]
    [InlineData(Language.Delphi, "begin_style", "always_wrap", "program P;\nvar x: Integer;\nbegin\nif x > 1 then begin\nWriteLn('a');\nend;\nend.", "then\r\n  begin")]
    // Perl (perltidy)
    [InlineData(Language.Perl, "indent-columns", 2, "sub f {\nmy $a = 1;\n}", "\n  my $a")]
    [InlineData(Language.Perl, "cuddled-else", true, "if ($a) {\nf();\n}\nelse {\ng();\n}", "} else {")]
    [InlineData(Language.Perl, "opening-brace-on-new-line", true, "if ($a) {\nf();\n}", "if ($a)\n{")]
    [InlineData(Language.Perl, "paren-tightness", 2, "f( $a, $b );", "f($a")]
    [InlineData(Language.Perl, "preset", "gnu", "if ($a) {\nf();\n}", "if ($a)\n")]
    [InlineData(Language.Perl, "output-line-ending", "win", "my $a = 1;\nmy $b = 2;\n", "\r\n")]
    [InlineData(Language.Perl, "_extra", "-i=1 -nsfs", "for(my $i=0;$i<3;$i++){\nf();\n}", "\n f();")]
    // Objective-C (uncrustify)
    [InlineData(Language.ObjectiveC, "_braces", "Allman", "void f(){if(a){b();}}", "if (a)\n")]
    [InlineData(Language.ObjectiveC, "indent_columns", 2, "void f(){if(a){b();}}", "\n  if (a)")]
    [InlineData(Language.ObjectiveC, "indent_with_tabs", 2, "void f(){if(a){b();}}", "\n\tif (a)")]
    [InlineData(Language.ObjectiveC, "sp_arith", "remove", "void f(){x=a + b;}", "a+b")]
    [InlineData(Language.ObjectiveC, "sp_before_ptr_star", "remove", "void f(){NSString *s;}", "NSString*")]
    [InlineData(Language.ObjectiveC, "mod_full_brace_if", "add", "void f(){if(a)b();}", "if (a) {")]
    [InlineData(Language.ObjectiveC, "_extra", "sp_inside_fparen=force", "void f(){g(a);}", "g( a )")]
    // Shell dialect and simplify
    [InlineData(Language.Shell, "-s", true, "[[ \"$a\" == b ]]", "[[ $a == b ]]")]
    // Python (ruff)
    [InlineData(Language.Python, "indent-width", 2, "if x:\n    y = 1", "\n  y = 1")]
    [InlineData(Language.Python, "format.skip-magic-trailing-comma", true, "x = [1, 2,]", "x = [1, 2]")]
    [InlineData(Language.Python, "format.docstring-code-format", true, "def f():\n    \"\"\"\n    >>> x=1\n    \"\"\"", ">>> x = 1")]
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

    [Fact]
    public async Task ObjectiveC_DefaultsActuallyFormat()
    {
        // Uncrustify's built-in defaults only re-indent; the base profile is what formats
        var input = "@implementation A\n-(void)m:(int)x{if(x>1){NSLog(@\"%d\",x);}else{x=x+1;}}\n@end\n";
        var result = await TestFormatter.Create().FormatAsync(input, Language.ObjectiveC);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Equal(
            "@implementation A\n" +
            "- (void)m:(int)x {\n" +
            "    if (x > 1) {\n" +
            "        NSLog(@\"%d\", x);\n" +
            "    } else {\n" +
            "        x = x + 1;\n" +
            "    }\n" +
            "}\n" +
            "@end\n",
            result.Output.ReplaceLineEndings("\n"));
    }

    [Theory]
    [InlineData("Arrows", "{-# LANGUAGE NoImplicitPrelude #-}\nmodule M where\nf = proc x -> do\n  returnA -< x\n")]
    [InlineData("-XArrows, MagicHash", "module M where\nf = proc x -> do\n  returnA -< x\n")]
    public async Task Haskell_ExtensionsLetOrmoluParse(string extensions, string input)
    {
        var without = await TestFormatter.Create().FormatAsync(input, Language.Haskell);
        var with = await TestFormatter.With(Language.Haskell, ("ghc-opt", extensions)).FormatAsync(input, Language.Haskell);

        Assert.False(without.Success, "proc notation should not parse without Arrows");
        Assert.True(with.Success, $"Format failed: {with.Output}");
    }

    [Fact]
    public async Task Perl_PreservesNonLatinText()
    {
        var result = await TestFormatter.Create().FormatAsync("my $s='فارسی 中文 日本語';", Language.Perl);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("'فارسی 中文 日本語'", result.Output);
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
