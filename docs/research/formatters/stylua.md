# StyLua (Lua)

Legend: **VERIFIED** = ran against `Binaries/stylua.exe`; **DOCS** = https://github.com/JohnnyMorganz/StyLua/blob/main/README.md

## Bundled version
`stylua.exe --version` → `stylua 2.3.1` (VERIFIED).

**RISK — this is a Lua-5.1-only build.** `--syntax` lists only `[possible values: All, Lua51]`. Panic paths in the binary show `C:\Users\cato\...\.cargo\registry\...full_moon-2.0.0`, i.e. it was built with a plain `cargo install stylua`, which per DOCS "builds for Lua 5.1 only"; the official GitHub release binaries enable Lua 5.2/5.3/5.4/LuaJIT/Luau. VERIFIED consequences (all exit 2, parse error), with `--syntax All`:
- `goto done ::done::` (5.2), `7 // 2` (5.3), `local x <const> = 1` (5.4), `local x: number = 1` and `x += 1` (Luau) all fail.
- A backtick (CfxLua/Luau interpolated string) causes a Rust **panic** (`internal error: entered unreachable code`, exit 2).
Recommendation: replace with the official release binary; then `--syntax` gains `Lua52, Lua53, Lua54, LuaJIT, Luau, CfxLua` and becomes a meaningful UI option. With the current binary a syntax selector is pointless.

## `--help` formatting section (VERIFIED, exit 0)
```
FORMATTING OPTIONS:
  --call-parentheses <..>            [Always, NoSingleString, NoSingleTable, None, Input]
  --collapse-simple-statement <..>   [Never, FunctionOnly, ConditionalOnly, Always]
  --column-width <COLUMN_WIDTH>
  --indent-type <..>                 [Tabs, Spaces]
  --indent-width <INDENT_WIDTH>
  --line-endings <..>                [Unix, Windows]
  --preserve-block-newline-gaps <..> [Never, Preserve]
  --quote-style <..>                 [AutoPreferDouble, AutoPreferSingle, ForceDouble, ForceSingle]
  --sort-requires
  --space-after-function-names <..>  [Never, Definitions, Calls, Always]
  --syntax <..>                      [All, Lua51]
```
Other relevant flags: `-f/--config-path <file>`, `--no-editorconfig` ("Has no effect if a stylua.toml configuration file is found"), `-s/--search-parent-directories`, `--stdin-filepath <path>` ("only used to help determine where to find the configuration file"), `--verify`, `-c/--check`, `--color`, `--range-start/--range-end`, `--respect-ignores`, `-g/--glob`, `-a`, `--num-threads`, `--output-format`, `-v`, `--lsp`. Enum values are case-insensitive (`--syntax all` works).

## Recommended invocation — config file via `--config-path`
Pure CLI flags are NOT sufficient at this version (two verified defects, below). Write an extension-owned toml per invocation (or per settings-change) and run:
```
["--config-path", "<abs path to generated stylua.toml>", "--no-editorconfig", "-"]
```
Generated file (all keys, tool defaults shown):
```toml
syntax = "All"
column_width = 120
line_endings = "Unix"
indent_type = "Tabs"
indent_width = 4
quote_style = "AutoPreferDouble"
call_parentheses = "Always"
collapse_simple_statement = "Never"
space_after_function_names = "Never"
block_newline_gaps = "Never"

[sort_requires]
enabled = false
```
VERIFIED: a full toml with non-default values for every key is accepted and applied. Unknown keys / bad enum values are hard errors (exit 2).

Why not flags only:
1. **`--preserve-block-newline-gaps Preserve` has no effect on the CLI** (output identical to `Never`), whereas `block_newline_gaps = "Preserve"` in toml works (VERIFIED with the same input).
2. **`.editorconfig` overrides explicit CLI flags**: in a dir with `.editorconfig` (`indent_style=space, indent_size=7`), `--indent-type Tabs --indent-width 4 -` still gives 7 spaces. Only `--no-editorconfig` (or a found/explicit stylua.toml) stops that.
3. `--sort-requires` is an enable-only flag; it cannot turn OFF a discovered `sort_requires.enabled = true`.
If flags are used anyway, the minimum safe form is `["--no-editorconfig", "--config-path", "<empty toml>", <flags...>, "-"]` (CLI flags do override toml values — VERIFIED `--indent-width 2` beats `indent_width = 3`).

## stdout / stderr / exit code (VERIFIED)
| Case | exit | stdout | stderr |
|---|---|---|---|
| OK | 0 | formatted (LF by default even for CRLF input) | empty |
| Lua parse error | 2 | empty | `error: could not format from stdin: failed to format from stdin: error parsing: ...` |
| Bad enum value | 2 | empty | `error: "Bogus" isn't a valid value for '--quote-style <QUOTE_STYLE>'` + possible values |
| Bad number | 2 | empty | `error: Invalid value "abc" for '--column-width <COLUMN_WIDTH>': invalid digit found in string` |
| Bad/unknown key in toml | 2 | empty | `error: Config file not in correct format: TOML parse error at line 1, column 15 ... unknown variant` |
| `--config-path` missing file | 2 | empty | `error: Failed to read config file: The system cannot find the file specified. (os error 2)` |
`--config-path NUL` works on Windows as "empty config" (VERIFIED).

## Isolation / discovery gotchas (VERIFIED, stdin mode)
- `stylua.toml` AND `.stylua.toml` in the **cwd** are auto-loaded, even without `--stdin-filepath`.
- Parent directories are searched only with `-s` (then also `$XDG_CONFIG_HOME`).
- `--stdin-filepath some/dir/x.lua` moves the search root to that dir (finds `some/dir/stylua.toml`).
- `.editorconfig` is read from cwd **and parents** (no flag needed, no `--stdin-filepath` needed) whenever no stylua.toml was found, and beats CLI flags (see above).
- A broken `stylua.toml` in cwd makes every format fail (exit 2).
- The extension runs stylua with `workingDirectory = null` → DevToys' cwd → currently NOT isolated.
- `--config-path X` disables toml discovery and, because a config is "found", EditorConfig too (verified: ec dir + `--config-path empty.toml` → defaults). Add `--no-editorconfig` anyway as belt and braces.

## Options reference
| Key (toml) | Type | Tool default | Values | CLI form | Meaning | Headline? |
|---|---|---|---|---|---|---|
| column_width | uint | 120 | any | `--column-width N` | Soft wrap guide | Yes |
| line_endings | enum | Unix | Unix, Windows | `--line-endings X` | LF / CRLF | Yes |
| indent_type | enum | Tabs | Tabs, Spaces | `--indent-type X` | Indent char | Yes |
| indent_width | uint | 4 | any (0 accepted → no indent with Spaces) | `--indent-width N` | Spaces per level; with Tabs only used for width math | Yes |
| quote_style | enum | AutoPreferDouble | AutoPreferDouble, AutoPreferSingle, ForceDouble, ForceSingle | `--quote-style X` | Auto* minimises escapes | Yes |
| call_parentheses | enum | Always | Always, NoSingleString, NoSingleTable, None, Input | `--call-parentheses X` | Parens on single string/table arg calls | Yes |
| collapse_simple_statement | enum | Never | Never, FunctionOnly, ConditionalOnly, Always | `--collapse-simple-statement X` | `if x then return end` on one line | Yes |
| space_after_function_names | enum | Never | Never, Definitions, Calls, Always | `--space-after-function-names X` | `foo (x)` | Secondary |
| block_newline_gaps | enum | Never | Never, Preserve | `--preserve-block-newline-gaps X` (**CLI broken in 2.3.1**; toml works) | Keep blank lines at block start/end | Secondary |
| [sort_requires] enabled | bool | false | true/false | `--sort-requires` (enable only) | Sort contiguous `local x = require(...)` blocks | Secondary |
| syntax | enum | All | **this build: All, Lua51**; official: + Lua52, Lua53, Lua54, LuaJIT, Luau, CfxLua | `--syntax X` | Grammar disambiguation | Only after binary swap |
All 11 are settable by flag and by toml (modulo the two defects). Defaults DOCS + consistent with VERIFIED default output.

## Mismatches vs current `FormatterSettings.cs`
No Lua settings exist (`Language.Lua => baseArgs`) — nothing to mismatch; tool defaults above should be the UI defaults.

## Verified notes (input: two requires, `local function foo (x)`, `if x then return end`, `print 'hello "w"'`, `call{ 1, 2 }`)
- `--indent-type Spaces --indent-width 2` → 2-space indent. `--quote-style AutoPreferSingle` → `require('a')`. `--call-parentheses None` → `require "b"`, `call { 1, 2 }`; `NoSingleString` keeps `call({ 1, 2 })`; `Input` keeps source form.
- `--collapse-simple-statement Always` → `if x then return end`, `local t = function() return 1 end`.
- `--sort-requires` → `a` before `b`. `--space-after-function-names Always` → `foo (x)`, `print ('..')`, `function ()`.
- `--column-width 20` → hangs `local b =` / `require("b")`. `--line-endings Windows` → CRLF.
- Startup ≈ 0.08 s.
