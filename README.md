# Code Formatter for DevToys

A code formatting extension for [DevToys 2.0](https://devtoys.app/) that supports 46 languages with automatic live formatting.

## Features

- **Live Auto-Format**: Code is automatically formatted as you type (500ms debounce)
- **46 Languages**: Python, JavaScript, TypeScript, JSON, Markdown, TOML, CSS, SCSS, Less, HTML, Vue, Svelte, Astro, Angular templates, Jinja, Twig, Nunjucks, Vento, Handlebars, Mustache, YAML, GraphQL, Dockerfile, Java, SQL, C, C++, C#, Go, Go Assembly, Shell/Bash, Lua, R, Delphi/Pascal, Kotlin, Perl, PHP, MATLAB, Ruby, Objective-C, Haskell, Julia, CMake, LaTeX, BibTeX, Typst
- **No External Dependencies, No Network**: All formatters are bundled, as native binaries or as dprint plugins that ship with the extension
- **Real Formatter Options**: Each language exposes the options of its formatter, 571 in all, plus a free-text field for the rest
- **Swap & Clear**: Quickly swap input/output or clear both editors
- **File Loading**: Load code directly from files

## Supported Languages & Formatters

| Language | Formatter | Notes |
|----------|-----------|-------|
| Python | [Ruff](https://github.com/astral-sh/ruff) | Bundled binary |
| JavaScript | [dprint](https://dprint.dev/) | Bundled binary |
| TypeScript | [dprint](https://dprint.dev/) | Bundled binary |
| JSON | [dprint](https://dprint.dev/) | Bundled binary |
| Markdown | [dprint](https://dprint.dev/) | Bundled binary |
| TOML | [dprint](https://dprint.dev/) | Bundled binary |
| CSS | [dprint](https://dprint.dev/) | Bundled binary |
| SCSS | [dprint](https://dprint.dev/) | Bundled binary |
| Less | [dprint](https://dprint.dev/) | Bundled binary |
| HTML | [dprint](https://dprint.dev/) | Bundled binary |
| Vue | [dprint](https://dprint.dev/) | Bundled binary |
| Svelte | [dprint](https://dprint.dev/) | Bundled binary |
| Astro | [dprint](https://dprint.dev/) | Bundled binary |
| Angular templates, Jinja, Twig, Nunjucks, Vento, Handlebars, Mustache | [dprint](https://dprint.dev/) + [markup_fmt](https://github.com/g-plane/markup_fmt) | Same plugin as HTML |
| YAML | [dprint](https://dprint.dev/) | Bundled binary |
| GraphQL | [dprint](https://dprint.dev/) | Bundled binary |
| Dockerfile | [dprint](https://dprint.dev/) | Bundled binary |
| Java | [google-java-format](https://github.com/google/google-java-format) | Native GraalVM binary |
| SQL | [sqruff](https://github.com/quarylabs/sqruff) | Native Rust binary |
| C | [clang-format](https://clang.llvm.org/docs/ClangFormat.html) | Bundled binary |
| C++ | [clang-format](https://clang.llvm.org/docs/ClangFormat.html) | Bundled binary |
| C# | [CSharpier](https://csharpier.com/) | Bundled binary |
| Go | [gofumpt](https://github.com/mvdan/gofumpt) | Bundled binary |
| Go Assembly | [asmfmt](https://github.com/klauspost/asmfmt) | Bundled binary |
| Shell/Bash | [shfmt](https://github.com/mvdan/sh) | Bundled binary |
| Lua | [StyLua](https://github.com/JohnnyMorganz/StyLua) | Bundled binary |
| R | [air](https://github.com/posit-dev/air) | Bundled binary |
| Delphi/Pascal | [pasfmt](https://github.com/AntumDeluge/pasfmt) | Bundled binary |
| Kotlin | [ktlint](https://github.com/pinterest/ktlint) | Bundled binary |
| Perl | [Perl::Tidy](https://github.com/perltidy/perltidy) | Bundled binary |
| PHP | [dprint](https://dprint.dev/) + [Mago](https://github.com/carthage-software/mago) | Bundled plugin |
| MATLAB | [MH Style](https://github.com/florianschanda/miss_hit) | Bundled binary |
| Ruby | [Rufo](https://github.com/ruby-formatter/rufo) | Bundled binary |
| Objective-C | [Uncrustify](https://github.com/uncrustify/uncrustify) | Bundled binary |
| Haskell | [Ormolu](https://github.com/tweag/ormolu) | Bundled binary |
| Julia | [dprint](https://dprint.dev/) + [fatou](https://github.com/jolars/fatou) | Bundled plugin |
| CMake | [dprint](https://dprint.dev/) + [cmakefmt](https://github.com/sargunv/dprint-cmakefmt) | Bundled plugin |
| LaTeX | [dprint](https://dprint.dev/) + [badness](https://github.com/jolars/badness) | Bundled plugin |
| BibTeX | [dprint](https://dprint.dev/) + [bibtex-tidy](https://github.com/apcamargo/dprint-plugin-bibtex-tidy) | Bundled plugin |
| Typst | [dprint](https://dprint.dev/) + [typstyle](https://github.com/apcamargo/dprint-plugin-typstyle) | Bundled plugin |

## Requirements

- [DevToys 2.0](https://devtoys.app/) (Preview or later)
- Windows x64 (all formatters are bundled as native binaries)
- Kotlin only: Java 11 or newer (ktlint runs on the JVM). It is found through `JAVA_HOME`, `PATH` or the usual install folders

## Installation

### From NuGet Package

1. Download the `.nupkg` file from [Releases](../../releases)
2. In DevToys, go to **Manage Extensions**
3. Click **Install from file** and select the `.nupkg`
4. Restart DevToys

### Manual Installation

1. Extract the `.nupkg` (it's a ZIP file) to:
   - **Windows**: `%LocalAppData%\DevToys\Plugins\CodeFormatter.DevToys.1.0.0\`
2. Restart DevToys

## Usage

1. Open DevToys and find **Code Formatter** under the **Formatters** category
2. Select your language from the dropdown
3. Paste or type code in the left editor
4. Formatted output appears automatically in the right editor

### Buttons

- **Swap**: Move output to input (useful for re-formatting)
- **Clear**: Clear both editors
- **Settings (gear icon)**: Configure formatter settings for each language
- **Load**: Load code from a file

## Configuration

Click the gear icon to open the settings of the selected language. The options are the
formatter's own: each one is passed to the tool under the tool's own name, and an option left
on its default is not passed at all, so the tool's defaults are what you get. Options are
spread over tabs, a handful per tab.

| Language | Formatter | Options | Extra options field |
|----------|-----------|---------|---------------------|
| Python | ruff | 10 (everything `ruff format` has) | |
| JavaScript, TypeScript | dprint typescript | 19 | JSON properties |
| JSON | dprint json | 7 | JSON properties |
| Markdown | dprint markdown | 6 | JSON properties |
| TOML | dprint toml | 5 | JSON properties |
| Dockerfile | dprint dockerfile | 2 | |
| CSS, SCSS, Less | dprint malva | 12 | JSON properties |
| HTML | dprint markup_fmt | 12 | JSON properties |
| Vue / Svelte / Astro | dprint markup_fmt | 18 / 16 / 14 | JSON properties |
| Angular, Jinja, Twig, Nunjucks, Vento, Handlebars, Mustache | dprint markup_fmt | 12, as HTML | JSON properties |
| YAML | dprint pretty_yaml | 9 | JSON properties |
| GraphQL | dprint pretty_graphql | 10 | JSON properties |
| C, C++ | clang-format | 36 | `Key: Value, ...` as in `.clang-format` |
| Objective-C | uncrustify | 29 | `name=value ...` |
| Java | google-java-format | 6 (all it has) | |
| Kotlin | ktlint | 11 | `.editorconfig` properties |
| C# | CSharpier | 4 (all it has) | |
| SQL | sqruff | 16 | lines of a `.sqruff` file |
| Go | gofumpt | 2 | |
| Shell/Bash | shfmt | 9 (all it has) | |
| Lua | StyLua | 11 (all it has) | |
| R | air | 9 | |
| Delphi/Pascal | pasfmt | 7 (all it has) | |
| Perl | perltidy | 31 + the `pbp` / `gnu` presets | command-line flags |
| PHP | dprint mago | 30 | JSON properties |
| MATLAB | MISS_HIT | 7 | |
| Ruby | Rufo | 5 (all it has) | |
| Haskell | Ormolu | language extensions, operator fixities | |
| Go Assembly | asmfmt | none: it has no options | |
| Julia | dprint fatou | 3 (all it has) | |
| CMake | dprint cmakefmt | 15 | JSON properties |
| LaTeX | dprint badness | 6 | JSON properties |
| BibTeX | dprint bibtex-tidy | 21 | JSON properties |
| Typst | dprint typstyle | 7 (all it has) | |

**Extra options.** The formatters with hundreds of options (the TypeScript plugin has 186,
perltidy 390, uncrustify 857, Mago 98) get controls for the ones people actually
change, plus a text field that takes anything else in the tool's own syntax. For example, for
TypeScript:

```
"binaryExpression.spaceSurroundingBitwiseAndArithmeticOperator": false, "enumDeclaration.memberSpacing": "blankLine"
```

If the tool rejects what you typed, its error message is shown in the output panel.

**Presets.** Where a tool has base styles (clang-format `BasedOnStyle`, ktlint code style,
perltidy `-pbp`/`-gnu`), the defaults of the other options depend on the
style. Those options show `style` until you set them.

**Isolation.** Every format runs in a private temporary directory, and each tool is told to
ignore configuration files around it. A `.clang-format`, `.editorconfig`, `dprint.json`,
`pyproject.toml` and the like on your machine never change the result; only the settings here do.

**Embedded code.** `<script>` and `<style>` blocks in HTML, Vue, Svelte and Astro are formatted
too, and follow the page's line width, indent and line ending.

### config.toml

Settings are saved to `%APPDATA%\DevToys\CodeFormatter\config.toml`. Only what differs from the
default is written:

```toml
[defaults]
lastLanguage = "python"

[formatters.python.settings]
"line-length" = 100
"format.quote-style" = "single"
```

To run a formatter your own way, give the language a command. It then runs exactly as written,
and the settings dialog no longer applies to it:

```toml
[formatters.python]
command = "black"
args = ["-q", "-"]
```

Code goes to stdin and is read back from stdout. For a tool that only works on files, add
`usesTempFile = true` and `tempFileExtension = "py"`, and put `{file}` in `args`.

How each bundled formatter takes its configuration, with every option it has, is written up in
[docs/research/formatters](docs/research/formatters/README.md).

## Building from Source

### Prerequisites

- .NET 8.0 SDK

### Build

```bash
# Clone the repository
git clone https://github.com/Error0229/cs-f.git
cd cs-f

# Download formatter binaries (from GitHub Release)
curl -L -o formatter-binaries.zip "https://github.com/Error0229/cs-f/releases/download/binaries-v2/formatter-binaries.zip"
unzip formatter-binaries.zip -d Binaries

# Build
dotnet build -c Release

# Run tests
dotnet test

# Create NuGet package
dotnet pack -c Release -o ./nupkg
```

### Development

For development with hot reload:

1. Set environment variables:
   ```powershell
   [Environment]::SetEnvironmentVariable("DevToysGuiDebugEntryPoint", "C:\path\to\DevToys.exe", "User")
   ```

2. Press F5 in Visual Studio/VS Code/Rider to debug with DevToys

The `Properties/launchSettings.json` is configured for debugging with the `EXTRAPLUGIN` environment variable.

## Project Structure

```
cs-f/
├── CodeFormatterTool.cs      # Main UI and tool implementation
├── Models/
│   ├── Language.cs           # Language enum and extensions
│   ├── FormatterConfig.cs    # config.toml model
│   ├── FormatterSpec.cs      # How one formatter is run, configured and judged
│   └── SettingDefinition.cs  # One option of a formatter
├── Formatters/
│   ├── FormatterSpecs.cs     # Language -> formatter spec
│   ├── Dprint.cs, Ruff.cs, ClangFormat.cs, ...   # One spec per tool, with its options
│   └── Emit.cs               # Shared ways of spelling options
├── Services/
│   ├── FormatterService.cs   # Runs a spec: private directory, config file, arguments, result
│   ├── ConfigManager.cs      # TOML config read/write
│   ├── JavaLocator.cs        # Finds Java 11+ for ktlint
│   └── ProcessRunner.cs      # External process execution
├── Resources/
│   └── CodeFormatterStrings.resx  # Localized strings
├── Binaries/                 # Bundled formatter executables (18) and dprint plugins
│   ├── ruff.exe              # Python
│   ├── dprint.exe            # 29 languages, through the plugins below
│   ├── plugins/*.wasm        # dprint plugins, used from disk: no download, no network
│   ├── clang-format.exe      # C/C++
│   ├── gofumpt.exe           # Go
│   ├── shfmt.exe             # Shell/Bash
│   ├── google-java-format.exe # Java
│   ├── sqruff.exe            # SQL
│   ├── csharpier.exe         # C#
│   ├── stylua.exe            # Lua
│   ├── air.exe               # R
│   ├── pasfmt.exe            # Delphi/Pascal
│   ├── ktlint.exe            # Kotlin
│   ├── perltidy.exe          # Perl
│   ├── mh_style.exe          # MATLAB
│   ├── rufo.exe              # Ruby
│   ├── asmfmt.exe            # Go Assembly
│   ├── uncrustify.exe        # Objective-C
│   └── ormolu.exe            # Haskell
└── CodeFormatter.Tests/      # Integration tests
```

## License

MIT

## Acknowledgments

- [DevToys](https://devtoys.app/) - The extensible developer toolbox
- [Ruff](https://github.com/astral-sh/ruff) - Fast Python formatter
- [dprint](https://dprint.dev/) - Pluggable code formatter
- [clang-format](https://clang.llvm.org/docs/ClangFormat.html) - LLVM C/C++ formatter
- [gofumpt](https://github.com/mvdan/gofumpt) - Stricter gofmt for Go
- [shfmt](https://github.com/mvdan/sh) - Shell script formatter
- [google-java-format](https://github.com/google/google-java-format) - Java formatter with GraalVM native binary
- [sqruff](https://github.com/quarylabs/sqruff) - Native Rust SQL linter and formatter
- [CSharpier](https://csharpier.com/) - Opinionated C# formatter
- [StyLua](https://github.com/JohnnyMorganz/StyLua) - Lua code formatter
- [air](https://github.com/posit-dev/air) - R formatter by Posit
- [pasfmt](https://github.com/AntumDeluge/pasfmt) - Delphi/Pascal formatter
- [ktlint](https://github.com/pinterest/ktlint) - Kotlin linter and formatter
- [Perl::Tidy](https://github.com/perltidy/perltidy) - Perl code beautifier
- [PHP-CS-Fixer](https://github.com/PHP-CS-Fixer/PHP-CS-Fixer) - PHP coding standards fixer
- [MH Style](https://github.com/florianschanda/miss_hit) - MATLAB formatter
- [Rufo](https://github.com/ruby-formatter/rufo) - Ruby formatter
- [asmfmt](https://github.com/klauspost/asmfmt) - Go assembly formatter
- [Uncrustify](https://github.com/uncrustify/uncrustify) - Code beautifier for C-style languages
- [Ormolu](https://github.com/tweag/ormolu) - Haskell formatter
