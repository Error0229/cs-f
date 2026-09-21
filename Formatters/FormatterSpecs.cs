using CodeFormatter.Models;

namespace CodeFormatter.Formatters;

/// <summary>
/// Which formatter runs for which language.
/// Add a language here and in LanguageRegistry; nothing else switches on Language.
/// </summary>
public static class FormatterSpecs
{
    private static readonly Dictionary<Language, FormatterSpec> _specs = new()
    {
        [Language.Python] = Ruff.Spec(),

        [Language.JavaScript] = Dprint.Spec("file.js", "typescript", DprintSettings.TypeScript, Dprint.TypeScript),
        [Language.TypeScript] = Dprint.Spec("file.ts", "typescript", DprintSettings.TypeScript, Dprint.TypeScript),
        [Language.Json] = Dprint.Spec("file.json", "json", DprintSettings.Json, Dprint.Json),
        [Language.Markdown] = Dprint.Spec("file.md", "markdown", DprintSettings.Markdown, Dprint.Markdown),
        [Language.Toml] = Dprint.Spec("file.toml", "toml", DprintSettings.Toml, Dprint.Toml),
        [Language.Css] = Dprint.Spec("file.css", "malva", DprintSettings.Css, Dprint.Malva),
        [Language.Scss] = Dprint.Spec("file.scss", "malva", DprintSettings.Css, Dprint.Malva),
        [Language.Less] = Dprint.Spec("file.less", "malva", DprintSettings.Css, Dprint.Malva),
        [Language.Html] = Markup("file.html", DprintSettings.Html),
        [Language.Vue] = Markup("file.vue", DprintSettings.Vue),
        [Language.Svelte] = Markup("file.svelte", DprintSettings.Svelte),
        [Language.Astro] = Markup("file.astro", DprintSettings.Astro),
        [Language.Angular] = Markup("file.component.html", DprintSettings.Html),
        [Language.Jinja] = Markup("file.jinja", DprintSettings.Html),
        [Language.Twig] = Markup("file.twig", DprintSettings.Html),
        [Language.Nunjucks] = Markup("file.njk", DprintSettings.Html),
        [Language.Vento] = Markup("file.vto", DprintSettings.Html),
        [Language.Handlebars] = Markup("file.hbs", DprintSettings.Html),
        [Language.Mustache] = Markup("file.mustache", DprintSettings.Html),
        [Language.Yaml] = Dprint.Spec("file.yaml", "yaml", DprintSettings.Yaml, Dprint.Yaml),
        [Language.GraphQL] = Dprint.Spec("file.graphql", "graphql", DprintSettings.GraphQL, Dprint.GraphQL),
        [Language.Dockerfile] = Dprint.Spec("Dockerfile", "dockerfile", DprintSettings.Dockerfile, Dprint.Dockerfile),

        [Language.Java] = GoogleJavaFormat.Spec(),
        [Language.Sql] = Sqruff.Spec(),
        [Language.C] = ClangFormat.Spec("file.c"),
        [Language.Cpp] = ClangFormat.Spec("file.cpp"),
        [Language.CSharpFormatted] = Csharpier.Spec(),
        [Language.Go] = Gofumpt.Spec(),
        [Language.Assembly] = new FormatterSpec
        {
            Command = "asmfmt", // reads no config
            DocsUrl = "https://github.com/klauspost/asmfmt#readme",
            Note = "asmfmt has no options at all."
        },
        [Language.Shell] = Shfmt.Spec(),
        [Language.Lua] = Stylua.Spec(),
        [Language.R] = Air.Spec(),
        [Language.Delphi] = Pasfmt.Spec(),
        [Language.ObjectiveC] = Uncrustify.Spec("OC"),
        [Language.Kotlin] = Ktlint.Spec(),
        [Language.Haskell] = Ormolu.Spec(),
        [Language.Perl] = Perltidy.Spec(),
        [Language.Php] = Dprint.Spec("file.php", "mago", DprintMoreSettings.Php, Dprint.Mago),
        [Language.Matlab] = MhStyle.Spec(),
        [Language.Ruby] = Rufo.Spec(),

        [Language.Julia] = Dprint.Spec("file.jl", "fatou", DprintMoreSettings.Julia, Dprint.Fatou),
        [Language.CMake] = Dprint.Spec("CMakeLists.txt", "cmakefmt", DprintMoreSettings.CMake, Dprint.CMakeFmt),
        [Language.Latex] = Dprint.Spec("file.tex", "badness", DprintMoreSettings.Latex, Dprint.Badness),
        [Language.Bibtex] = Dprint.Spec("file.bib", "bibtex-tidy", DprintMoreSettings.Bibtex, Dprint.BibtexTidy),
        [Language.Typst] = Dprint.Spec("file.typ", "typstyle", DprintMoreSettings.Typst, Dprint.Typstyle),
    };

    // markup_fmt hands <script>, <style> and JSON blocks to whichever loaded plugin claims them.
    // With only markup_fmt loaded, embedded code comes back untouched and nothing says so.
    private static FormatterSpec Markup(string stdinName, SettingDefinition[] settings) =>
        Dprint.Spec(stdinName, "markup", settings, Dprint.MarkupFmt, Dprint.TypeScript, Dprint.Malva, Dprint.Json);

    public static FormatterSpec? For(Language language) => _specs.GetValueOrDefault(language);

    public static SettingDefinition[] SettingsFor(Language language) => For(language)?.Settings ?? [];

    /// <summary>
    /// A formatter entry the user wrote by hand in config.toml: run exactly what it says.
    /// We know nothing about the tool's options or exit codes, so no settings and the old success rule.
    /// </summary>
    public static FormatterSpec FromUserEntry(FormatterEntry entry) => new()
    {
        Command = entry.Command,
        Args = entry.Args,
        InputFileName = entry.UsesTempFile ? $"input.{entry.TempFileExtension}" : null,
        Success = SuccessRule.Lenient
    };

    /// <summary>
    /// Up to 1.2.0 the defaults were written into config.toml on first run, so every existing
    /// install has them saved. Such an entry is not a user's choice and must not pin them to an
    /// old invocation forever. These are all the defaults that ever shipped.
    /// </summary>
    public static bool IsShippedDefault(FormatterEntry entry)
    {
        if (entry.RequiresNode)
            return true; // Prettier / sql-formatter era; those formatters are gone

        var args = entry.Args
            .Where(a => a != "--plugins" && a != "--config-discovery=false" && !a.StartsWith("https://plugins.dprint.dev/"))
            .ToArray();

        if (entry.Command == "dprint")
            return args.Length == 3 && args[0] == "fmt" && args[1] == "--stdin";

        return ShippedDefaults.Contains($"{entry.Command} {string.Join(' ', args)}".TrimEnd());
    }

    private static readonly HashSet<string> ShippedDefaults =
    [
        "ruff format -",
        "google-java-format -",
        "sqruff fix -",
        "clang-format --assume-filename=file.c",
        "clang-format --assume-filename=file.cpp",
        "gofumpt",
        "shfmt --filename script.sh",
        "stylua -",
        "air format --stdin",
        "air format {file}",
        "pasfmt",
        "csharpier --write-stdout",
        "csharpier format {file}",
        "asmfmt",
        "uncrustify -l OC -q",
        "uncrustify -l OC -c - -q",
        "ktlint --stdin --format",
        "ormolu --stdin-input-file stdin.hs",
        "perltidy -st -se",
        "php-cs-fixer fix --using-cache=no -",
        "php-cs-fixer fix {file} --rules=@PSR12 --using-cache=no --quiet",
        "mh_style --single -",
        "mh_style --single --fix {file}",
        "rufo",
    ];

    /// <summary>
    /// Setting keys saved by versions up to 1.2.0, mapped to the tool's own key.
    /// Old keys without an entry here never reached their formatter and are dropped.
    /// </summary>
    public static string MigrateSettingKey(Language language, string key) => language switch
    {
        Language.Python => key switch
        {
            "indent-style" or "quote-style" or "line-ending" => $"format.{key}",
            _ => key
        },
        // The CSS and HTML definitions used Prettier's names, which the dprint plugins reject
        Language.Css or Language.Scss or Language.Less or
        Language.Html or Language.Vue or Language.Svelte or Language.Astro => key switch
        {
            "printWidth" => "lineWidth",
            "tabWidth" => "indentWidth",
            _ => key
        },
        Language.C or Language.Cpp => key == "style" ? "BasedOnStyle" : key,
        Language.Go => key == "extra" ? "-extra" : key,
        Language.Shell => key switch
        {
            "indent" => "-i",
            "binaryNextLine" => "-bn",
            "caseIndent" => "-ci",
            "spaceRedirects" => "-sr",
            "keepPadding" => "-kp",
            "funcNextLine" => "-fn",
            _ => key
        },
        _ => key
    };
}
