# Formatter options research

**Date**: 2026-09-19
**Status**: Implemented (2026-09-20). The design below is what `Formatters/` and `Models/FormatterSpec.cs` now do.

What changed since the research was done, so the per-tool files are read correctly:

- Three binaries were replaced (release `binaries-v2`): clang-format 23.1.1 from the official LLVM
  archive (183 keys; `clang-format.md` describes the old 12.0.0 snapshot, its mechanism section
  still holds), StyLua 2.5.2 official build (all Lua dialects; `--syntax` is meaningful now),
  air 0.11.0 (stdin mode through `--stdin-file-path`, new `assignment-style` key, whose default
  `arrow` rewrites `x = 1` as `x <- 1`).
- ktlint and php-cs-fixer are started through their launcher once; after that the payload the
  launcher left in `%TEMP%` is kept under `%LOCALAPPDATA%\CodeFormatter\cache` and run directly
  (ktlint 3.9 s -> about 1.2 s with a JVM class-data-sharing archive, php-cs-fixer 3.4 s -> 1.3 s).
- Found while implementing: sqruff 0.29.3 panics in rule LT06 on any function call under the
  `redshift` dialect (the rule is excluded for that dialect); uncrustify's `cmt_width` tops out at
  256; MISS_HIT's `tab_width` must be at least 2; StyLua's config file spells `LuaJIT` where its
  `--help` says `LuaJit`.

How each bundled formatter actually takes configuration, verified by running the
exes in `Binaries/` (release `binaries-v1`). One file per tool in this directory;
claims inside are marked VERIFIED (ran it) or DOCS (cited). This file is the index
and the conclusions.

## Summary

| Language(s) | Tool (bundled version) | Options | Transport | Isolation needed | Startup |
|---|---|---|---|---|---|
| JS, TS | dprint 0.50.2 + typescript 0.95.13 | 186 (19 headline) | config file (`--config x.json`) | `--config` | 0.25 s |
| JSON | + json 0.21.0 | 13 | config file | `--config` | 0.25 s |
| Markdown | + markdown 0.20.0 | 12 | config file | `--config` | 0.25 s |
| TOML | + toml 0.7.0 | 7 | config file | `--config` | 0.25 s |
| Dockerfile | + dockerfile 0.3.3 | 3 (2 useful) | config file | `--config` | 0.25 s |
| CSS, SCSS, Less | + malva 0.15.1 | 35 | config file | `--config` | 0.25 s |
| HTML, Vue, Svelte, Astro | + markup_fmt 0.25.1 | 46 | config file | `--config` | 0.25 s |
| YAML | + pretty_yaml 0.5.1 | 16 | config file | `--config` | 0.25 s |
| GraphQL | + pretty_graphql 0.2.3 | 48 | config file | `--config` | 0.25 s |
| Python | ruff 0.14.6 | 12 (8 headline) | inline (`--config k=v`) | `--isolated` | fast |
| C, C++ | clang-format "12.0.0" (really ~11) | 114 (~25 headline) | inline (`--style={...}`) | explicit `--style` | fast |
| Objective-C | uncrustify 0.82.0 | 857 (~25 headline) | flags (`--set k=v`) | `-c -` (already) | fast |
| Java | google-java-format 1.33.0 | 6 | flags | none | 0.15 s |
| Kotlin | ktlint 1.8.0 | ~13 + rule toggles | `.editorconfig` in private dir | `root = true` + `--stdin-path` | 3.5-4 s |
| C# | csharpier 1.2.1 | 4 | config file (`--config-path`) | `--config-path` | fast |
| SQL | sqruff 0.29.3 | ~115 (~12 headline) | config file (`--config`) | `--config` | fast |
| Go | gofumpt 0.9.2 | 3 | flags | private cwd (`go.mod`) | fast |
| Shell | shfmt 3.12.0 | 9 | flags | any format flag | fast |
| Lua | stylua 2.3.1 (Lua 5.1-only build) | 11 | config file (`--config-path`) | `--no-editorconfig` | fast |
| Go asm | asmfmt 1.3.2 | 0 | - | none | fast |
| Ruby | rufo 0.18.1 | 5 | `.rufo` in private dir | empty `.rufo` + `--filename` | 2.6 s |
| Delphi | pasfmt 0.7.0 | 8 | flags (`-C k=v`) | `--config-file <empty>` | fast |
| R | air 0.8.0 | 10 | `air.toml` in private dir | private dir | fast |
| Haskell | ormolu 0.8.0.2 | 0 style; extensions list | flags | `--no-cabal --no-dot-ormolu` | 0.2-0.5 s |
| Perl | perltidy 20250912 | 390 (~40 headline) | flags | `-npro` | 2 s |
| PHP | php-cs-fixer 3.92.0 | 294 rules, 101 sets (~25 headline) | `config.php` in private dir | `--config=` + `-n` | 3 s |
| MATLAB | mh_style 0.9.44 | 4 + ~32 rule toggles | `miss_hit.cfg` in private dir | `project_root` in cfg | 1.7 s |

## There are only three transports

Every tool fits one of these. This is the whole design.

1. **Flags** - each setting becomes argv elements (`-i 4`, `--set k=v`, `-C k=v`, `--aosp`).
   gofumpt, shfmt, google-java-format, uncrustify, pasfmt, perltidy, ormolu.
2. **Inline** - all settings fold into one argument or a repeated `--config k=v`.
   ruff (`--config 'format.quote-style="single"'`), clang-format (`--style={BasedOnStyle: X, K: V}`).
   Mechanically the same as flags with a different joiner.
3. **Config file** - settings are rendered into a file in a private temp directory, and the tool is
   pointed at it (or the source file is placed beside it). dprint (json), csharpier (json),
   sqruff (ini), stylua (toml), air (toml), ktlint (editorconfig), rufo (ruby DSL),
   php-cs-fixer (php), mh_style (cfg).

## Bugs found in the current code (independent of new options)

Ordered by user impact.

1. **dprint settings are a no-op for 16 languages.** `BuildDprintArgs` returns `baseArgs`. The UI shows
   options that do nothing.
2. **Several defined keys/values are invalid and would hard-fail once wired** (dprint exits 1, empty stdout
   on any bad key): CSS/HTML `tabWidth` -> `indentWidth`; CSS `singleQuote` -> `quotes`; YAML/GraphQL
   `lineWidth` -> `printWidth`; TS `quoteStyle` values `double|single` -> `alwaysDouble|alwaysSingle|...`;
   TS `semiColons` lacks `always`; JSON `trailingCommas` lacks `maintain`.
3. **Embedded `<script>`/`<style>` is never formatted** in HTML/Vue/Svelte/Astro: only markup_fmt is loaded,
   and dprint silently passes unmatched embedded code through. Fix: also pass the typescript, malva (and json)
   plugin URLs.
4. **Kotlin output is polluted**: ktlint writes an INFO log line to stdout ahead of the code. Needs
   `--log-level=none --ignore-autocorrect-failures`. Tests use `Contains` so they don't see it.
5. **SQL identifiers are rewritten** (`Foo` -> `foo`, rule CP02) - a semantic change on case-sensitive
   databases. Exclude CP02 by default.
6. **Failures reported as success.** `RunWithTempFileAsync` treats "file non-empty" as success, which is always
   true: air (exit 255), php-cs-fixer, csharpier, mh_style failures all hand the user their input back as
   "formatted". `RunAsync`'s "any stdout = success" has the same effect for perltidy and sqruff, which echo
   the input on a parse error. Success must be declared per tool (exit codes / stderr pattern / stdout).
7. **Results depend on DevToys' working directory.** No cwd is set, and almost every tool discovers config
   from cwd or its ancestors: dprint, ruff, clang-format, stylua, rufo, pasfmt, gofumpt (`go.mod`), ormolu,
   perltidy, sqruff, ktlint; and the temp-file tools (csharpier, air, php-cs-fixer, mh_style) walk up from
   `%TEMP%` into the user profile. A stray `.clang-format` with a modern key makes C/C++ formatting fail outright.
8. **php-cs-fixer without `-n`** can write `.php-cs-fixer.dist.php` into the cwd.
9. **shfmt defaults differ from the tool**: indent 2 / `-bn` / `-ci` vs shfmt's tabs / off / off.
10. **ruff builder prefixes every key with `format.`** - fine for today's keys, breaks `indent-width` and
    `target-version` (top-level) with exit 2.
11. **pasfmt reads stdin as the ANSI code page** - the issue #1 bug again, on the tool side. Pass `-C encoding=utf-8`.
12. **Java and SQL setting definitions describe Prettier and sql-formatter**, which are no longer used.
    `BuildPrettierArgs`, `BuildSqlFormatterArgs`, `ResolveNpmBinaryPaths`, the Node/npm checks and
    `RequiresNode` are dead code.
13. **Line endings**: dprint plugins, sqruff emit LF; rufo, mh_style, pasfmt emit CRLF. Inconsistent across
    languages; pick one policy (suggest: `auto`/preserve where the tool has it, else normalise to the input's).

## Binaries worth replacing before exposing options

- **clang-format**: reports 12.0.0, behaves like 11 (114 keys). Rejects `InsertBraces`, `QualifierAlignment`,
  `SpaceBeforeParensOptions`; `SortIncludes`/`AlignConsecutive*` are plain bools. An options UI written from
  current docs fails on this binary. Upgrade first, then define options against the new `--dump-config`.
- **stylua**: built with Lua 5.1 syntax only; `goto`, `//`, `<const>`, Luau fail to parse. The official release
  binary supports all syntaxes and makes the `syntax` option meaningful.
- **air**: 0.9.0 adds `--stdin-file-path`, removing the temp file.
- **rufo / perltidy / php-cs-fixer / mh_style / ktlint**: packed runtimes, 1.7-4 s per run, some re-extract
  7-70 MB to `%TEMP%` each time. Not an options problem, but live formatting is unusable for these; they need
  format-on-demand or a long debounce. php-cs-fixer drops to ~1.3 s if the extracted `php.exe` + phar are
  invoked directly.

## Proposed design

The current shape - a `Language` switch over hand-written `BuildXArgs` functions, with setting definitions in
a separate file that knows nothing about how a setting is delivered - is why the dprint options could exist in
the UI while doing nothing. Put delivery in the data and the switch disappears.

```csharp
// One per tool, not per language. C and C++ share one; the 16 dprint languages share one.
record FormatterSpec(
    string Command,
    string[] BaseArgs,              // includes the isolation flags, always
    Transport Transport,            // Flags | Inline | ConfigFile
    IConfigWriter? ConfigWriter,    // ConfigFile only: json / toml / ini / editorconfig / php / ...
    InputMode Input,                // Stdin | FileBesideConfig
    SuccessRule Success,            // exit codes that mean OK, optional stderr/stdout failure pattern
    SettingDefinition[] Settings);

record SettingDefinition(
    string Key,                     // the tool's own key, verbatim: "format.quote-style", "indentWidth", "-i"
    string DisplayName,
    SettingType Type,
    object ToolDefault,             // the TOOL's default. Used for display only.
    string? Group = null,           // "Indentation", "Braces", ... -> collapsible sections in the dialog
    bool Advanced = false,
    ...);
```

Rules:

1. **Emit only what the user changed.** A setting equal to `ToolDefault` is not sent. The tool's defaults stay
   the source of truth, upgrading a binary can't silently disagree with us, and `config.toml` stays small.
2. **`Key` is the tool's key, verbatim.** No camelCase-to-kebab translation layers. The wrapper is thin.
3. **Isolation is part of `BaseArgs`, not optional**, and every process runs with cwd = a private empty temp dir.
4. **Success is declared per tool**, not guessed from "did we get some stdout".
5. **Headline vs advanced.** Expose the headline set (8-25 per tool) as typed controls, grouped. For the tools
   with hundreds of options (typescript 186, uncrustify 857, perltidy 390, php-cs-fixer 294 rules, clang-format
   114) add one free-text "extra options" field in the tool's native syntax, passed through verbatim. That
   honours "wrapper over the CLI" without building 1,900 controls, and it is the escape hatch for anything we
   didn't model.
6. **Presets first** where the tool has them: clang-format `BasedOnStyle`, perltidy `-pbp`/`-gnu`,
   php-cs-fixer rule sets, ktlint `ktlint_code_style`, google-java-format `--aosp`.
7. **Back-compat** ("never break userspace"): existing `config.toml` files contain the wrong keys listed above.
   Migrate the five renamed keys on load; drop unknown keys instead of passing them on (they would hard-fail).
   User-edited `command`/`args` keep working.

Tests: one table-driven test per setting - non-default value in, assert the output differs from the default
output and the process succeeded. That is what would have caught the no-op dprint settings.

## Suggested order

1. **Foundation + fix what's broken** - `FormatterSpec`/transports, isolation + private cwd, per-tool success
   rules, the 13 bugs above. No new options yet; existing tests must stay green, Kotlin/SQL tests tightened to
   `Equal`.
2. **dprint** - one transport unlocks 16 languages. Correct keys, headline options, shared top-level
   `lineWidth`/`indentWidth`/`useTabs`/`newLineKind`, embedded-language plugins.
3. **Flag/inline tools** - ruff, shfmt, gofumpt, google-java-format, pasfmt, ormolu, perltidy, uncrustify
   (with a sane base profile, since its defaults barely format).
4. **Config-file tools** - csharpier (and move it to stdin), sqruff, stylua, air, ktlint, rufo, mh_style,
   php-cs-fixer.
5. **Binary upgrades** - clang-format, stylua, air; then clang-format's options against the new binary.
