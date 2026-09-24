# CSharpier — options research

Legend: **VERIFIED** = ran against the bundled exe on this machine (2026-09-19). **DOCS** = https://csharpier.com/docs/Configuration and https://csharpier.com/docs/CLI (current docs = 1.x line).

## Bundled version

`csharpier.exe --version` → `1.2.1` (self-contained single-file .NET app, 51 MB). VERIFIED.
Startup: ~1.2–1.5 s per invocation (stdin 1504 ms; file mode self-reported 1239–1301 ms). VERIFIED.

Subcommands at this version: `format`, `check`, `pipe-files`, `server`. (`csharpier <file>` without subcommand is pre-1.0 syntax.)

## Recommended invocation & config-passing mechanism

**stdin works at 1.2.1 — the temp-file hack can be dropped.** `csharpier format` with no path argument reads stdin and writes the formatted result to stdout; no `--write-stdout` needed. VERIFIED.

```
cfg  = %TEMP%\<ext>\csharpier\<settings-hash>\csharpierrc.json
args = [ "format", "--config-path", cfg ]                       # C# via stdin → stdout
# XML-family input (csproj/props/targets/xaml/…): same args also work — see "Other file types"
args = [ "format", "--config-path", cfg, "--stdin-path", "<ABSOLUTE>\\Snippet.csproj" ]   # optional explicit type
cwd  = directory of cfg (belt and braces; keeps .csharpierignore/.editorconfig lookups away from user dirs)
```

Minimal generated config (JSON; file name is free when passed via `--config-path`, `.json`/`.yaml`/`.yml` extension decides the parser, `.editorconfig` also accepted):

```json
{
  "printWidth": 100,
  "useTabs": false,
  "indentSize": 4,
  "endOfLine": "auto"
}
```

YAML equivalent: `printWidth: 100\nindentSize: 4\n…`. VERIFIED both forms.

If the temp-file mode is kept instead: `["format", "--config-path", cfg, "--no-cache", "{file}"]` — prints `Formatted 1 files in 1239ms.` to stdout (harmless in file mode) and writes in place.

## stdout / stderr / exit-code contract (stdin mode, VERIFIED)

| Situation | exit | stdout | stderr |
|---|---|---|---|
| Success | 0 | formatted code only (no banner) | empty |
| C# does not compile (`class {`) | 1 | empty | `Error <cwd path> - Failed to compile so was not formatted.` + `(1,7): error CS1001: Identifier expected` |
| same + `--compilation-errors-as-warnings` | 0 | **empty** | same text as `Warning` |
| same + `--log-level None` | 1 | empty | empty |
| Unknown flag (`--bogus`) | 1 | empty | `There was no file or directory found at --bogus` (flag is parsed as a path) |
| Config has wrong type (`"printWidth": "abc"`) | 1 | empty | `Unhandled exception: System.Text.Json.JsonException: The JSON value could not be converted to System.Int32. Path: $.printWidth …` + stack trace (~5 KB) |
| Config has bad enum (`"endOfLine": "bogus"`) | 1 | empty | `Unhandled exception: … Unable to parse 'bogus' to enum of type CSharpier.Core.EndOfLine.` |
| `--config-path` file missing | 1 | empty | `Unhandled exception: System.IO.FileNotFoundException: Could not find file '…'` |
| Unknown config key (`"bogus": 1`, or old `"tabWidth"`) | 0 | normal | nothing — silently ignored |
| Relative `--stdin-path x.csproj` | 1 | empty | `Unhandled exception: System.ArgumentException: The path is empty. (Parameter 'path')` → **must be absolute** |

stdout is clean; exit code is reliable. `endOfLine: auto` keeps the input's EOL (CRLF in → CRLF out). VERIFIED.

## Isolation / discovery gotchas (VERIFIED)

1. **Discovery is real and walks upward**, both in stdin mode (from cwd; or from the directory of `--stdin-path`) and in file mode (from the *file's* directory, not cwd):
   - cwd `disc\sub`, `disc\.csharpierrc` = `{"indentSize": 3}` → stdin output indented by 3.
   - `disc\.editorconfig` (`indent_size = 6`, `max_line_length = 40`) → picked up the same way.
   - file mode: file in `disc\sub`, cwd elsewhere → parent `.csharpierrc` applied; file in an empty dir, cwd = `disc\sub` → defaults (cwd irrelevant).
   - **Current extension risk**: the temp file lives under `%TEMP%` = `C:\Users\<user>\AppData\Local\Temp`, so a `.csharpierrc` or `.editorconfig` in `%TEMP%`, `AppData`, **the user profile** or `C:\` silently changes output (a `~/.editorconfig` is not unusual).
2. **`--config-path` replaces discovery entirely** (no merge): cwd had `.editorconfig` with indent 6 / width 40, `--config-path only.json` (`{"useTabs": false}`) → output used built-in defaults 4 / 100. Works with a config file located anywhere, unrelated to the source location. So always pass `--config-path`, even for all-default settings.
3. `--config-path` may also point at an `.editorconfig` file. VERIFIED.
4. File mode writes a cache at `%LOCALAPPDATA%\CSharpier\.formattingCache` (exists on this machine). Use `--no-cache` for temp files (each temp name is unique, so the cache only grows). Stdin mode does not need it.
5. `.csharpierignore` is also looked up from the base directory (cwd / stdin-path dir) — setting cwd to the private dir avoids surprises.
6. File mode on a `.csproj` also runs the "CSharpier.MsBuild version check" unless `--no-msbuild-check`; irrelevant for stdin.

## Options reference

Config keys (DOCS + VERIFIED where noted). CSharpier is opinionated: there are only these.

| Key | Type | Default | Values | Config form (`.csharpierrc` JSON/YAML) · editorconfig | Meaning | Headline? |
|---|---|---|---|---|---|---|
| printWidth | int | 100 | any int | `"printWidth": 80` · `max_line_length` | Target line width (soft limit). VERIFIED (30 → parameters and initializer exploded) | **Yes** |
| useTabs | bool | false | true/false | `"useTabs": true` · `indent_style = tab` | Indent with tabs. VERIFIED | **Yes** |
| indentSize | int | 4 (C#), 2 (XML) | any int | `"indentSize": 2` · `indent_size` | Spaces per indent level. VERIFIED. **Renamed from `tabWidth` in 1.0** — at 1.2.1 `"tabWidth": 2` is silently ignored (VERIFIED: output stayed at 4). | **Yes** |
| endOfLine | enum | `auto` | `auto`, `lf`, `crlf` (case-insensitive) | `"endOfLine": "lf"` · editorconfig `end_of_line` (no `auto` equivalent) | Output line endings; `auto` = keep what the input uses. VERIFIED (`crlf`) | **Yes** |
| xmlWhitespaceSensitivity | enum | `strict` (`ignore` for xaml/axaml) | `strict`, `ignore` | `"xmlWhitespaceSensitivity": "ignore"` | XML only: whether whitespace in element content is significant. DOCS (listed in current docs; not exercised on the bundled exe — verify before exposing) | Only if XML exposed |
| overrides | array | — | `[{ "files": "*.cst", "formatter": "csharp", "indentSize": 2, … }]` | JSON/YAML only · editorconfig: `csharpier_formatter = csharp|xml` in a glob section | Per-glob option overrides / map unknown extensions to a formatter. DOCS. Test: override `files: "*.csproj"` with `indentSize: 8` was **not** applied to `--stdin-path …\x.csproj` when config came via `--config-path` from another directory (globs are relative to the config file's dir) — do not rely on it; generate one flat config per language instead. | No |

CLI flags of `format` with relevance:

| Flag | Meaning | Use |
|---|---|---|
| `--config-path <file>` | explicit config, disables discovery | **always** |
| `--stdin-path <abs path>` | virtual path for stdin: selects formatter by extension and base dir for config/ignore lookup; must be absolute | optional (XML) |
| `--write-stdout` | file mode: print result instead of writing in place. Not needed for stdin | no |
| `--no-cache` | file mode only | if temp file kept |
| `--skip-validation` | skip syntax-tree equivalence check (slightly faster) | optional |
| `--compilation-errors-as-warnings` | exit 0 on uncompilable input; still **no output** | no |
| `--include-generated`, `--no-msbuild-check`, `--ignore-path`, `--skip-write`, `--log-format`, `--log-level` | not output-relevant | no |

Non-configurable behaviour enforced (DOCS): utf-8, final newline, trimmed trailing whitespace, `System` usings sorted first, no blank lines between using groups, Allman braces.

Counts: **4 options for C#** (+1 XML-only, + overrides). All 4 are headline.

## Other file types at 1.2.1 (VERIFIED)

1.x formats XML: `.csproj`, `.props`, `.targets`, `.xml`, `.config`, `.xaml`, `.axaml`, … (DOCS).
- stdin **without** `--stdin-path`: content is sniffed — `<Project><A>1</A></Project>` was formatted as XML (2-space indent), exit 0.
- stdin with absolute `--stdin-path …\x.csproj`: formatted as XML.
- XML default indent is 2, but a top-level `"indentSize": 4` in the supplied config applies to XML too (output used 4). Generate a separate config (or omit `indentSize`) if XML is ever exposed.

## Verified notes (input: one-line namespace/class/method with a long `Console.WriteLine(...)` call)

1. `format` < a.cs → Allman braces, 4-space, call arguments one per line (exceeds 100); exit 0, stderr empty, nothing written to cwd.
2. `--config-path my.json` (`printWidth 60, useTabs true, endOfLine crlf`) → tab indents, every line ends `\r\n`.
3. `--config-path my.yaml` (`printWidth: 50`, `indentSize: 2`) → 2-space indents.
4. `--config-path w30.json` (`printWidth 30`) → `void F(\n int a,\n string b\n)` and `new List<int>\n{` broken up.
5. `--config-path old.json` (`tabWidth: 2`) → unchanged 4-space output, no warning.
