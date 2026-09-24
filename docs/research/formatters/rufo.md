# rufo (Ruby)

Legend: **VERIFIED** = ran against `Binaries/rufo.exe` (and read the Ruby source it unpacks); **DOCS** = https://github.com/ruby-formatter/rufo/blob/master/docs/settings.md

## Bundled version / what the exe is
- `rufo.exe --version` → `run_rufo 0.18.1` (VERIFIED). ("run_rufo" is the wrapper script name: `require 'rufo'; Rufo::Command.run(ARGV)`.)
- It is an **OCRAN-packed Ruby app** (strings: `Ocran stub`, `OCRAN_EXECUTABLE`): 2.2 MB exe that on EVERY run unpacks a ~7 MB Ruby **3.2.0** runtime + gem `rufo-0.18.1` into `%TEMP%\ocranXXXXXXXX\`, runs, then deletes it (VERIFIED: dir exists during the run, 0 leftovers after).
- **Startup ≈ 2.6 s per invocation** (`--version` 2.59 s, `--help` 2.63 s) vs < 0.1 s for the other tools. No system Ruby is required. Risk: AV scanning of the temp extraction; a killed process (30 s timeout) leaves the ocran dir behind.

## Full `--help` (VERIFIED, exit 0)
```
Usage: rufo files or dirs [options]
    -c, --check                      Only check formating changes
        --filename=value             Filename to use to lookup .rufo (useful for STDIN formatting)
    -x, --simple-exit                Return 1 in the case of failure, else 0
        --loglevel[=LEVEL]           error, warn, log (default), debug, silent
    -h, --help                       Show this help
```
**No formatting option is settable from the CLI.** Everything comes from a `.rufo` file.

## Recommended invocation — private dir with `.rufo`
Per invocation (or cached per settings hash) create a private directory containing `.rufo`, then:
```
["--filename=<privateDir>\\stdin.rb", "-x"]        // stdin → stdout
```
- `--filename` is only used as the start point for the `.rufo` lookup (VERIFIED in `command.rb`: `format(code, @filename_for_dot_rufo || Dir.getwd)`; `dot_file.rb` checks `<path>/.rufo` then each parent). A filename ending in `.erb` switches to the ERB formatter.
- Alternative: set the process working directory to the private dir and pass no `--filename`.
- ALWAYS write a `.rufo` (even empty) — it is the only way to stop the upward search (VERIFIED: empty `.rufo` in a subdir shadows a parent's `.rufo` and yields defaults).
- `-x` makes exit codes 0/1 (see below), removing the "3 = changed" special case.

`.rufo` format is NOT Ruby DSL evaluation; it is a line parser: `name value` where value is `:symbol`, `true`, `false`, or `[a,b]` (VERIFIED `dot_file.rb`). Example with every key at its default:
```
parens_in_def :yes
align_case_when false
align_chained_calls false
trailing_commas true
quote_style :double
```

## stdout / stderr / exit code (VERIFIED)
Constants in `command.rb`: `CODE_OK = 0`, `CODE_ERROR = 1`, `CODE_CHANGE = 3`.
| Case | exit (default) | exit with `-x` | stdout | stderr |
|---|---|---|---|---|
| Already formatted | 0 | 0 | code | empty |
| Reformatted | **3** | 0 | formatted code | empty |
| Ruby syntax error | 1 | 1 | empty | `STDIN is invalid code. Error on line:1 syntax error, unexpected end-of-input` |
| Unknown CLI flag | 1 | 1 | empty | Ruby backtrace: `invalid option: --bogus (OptionParser::InvalidOption)` |
| Invalid value / unknown key in `.rufo` | 3/0 (NOT an error) | — | formatted with the DEFAULT for that key | `Invalid value for quote_style: :bogus. Valid values are: :double, :single, :mixed` / `Invalid config option=nonsense` / `Unknown config value="maybe" for "trailing_commas"` |
So invalid config is only a stderr warning — the extension (which shows stderr only on failure) would silently ignore it; generate the file from validated enums.
- **Output uses CRLF on Windows** (Ruby text-mode stdout): LF input → CRLF output; CRLF input → CRLF (no `\r\r\n` doubling). UTF-8 passes through intact. No line-ending option exists; the extension must normalise if it wants LF.
- The extension's `ExitCode == 0 || stdout non-empty` rule happens to accept exit 3; `-x` makes it explicit.

## Isolation / discovery gotchas (VERIFIED)
- stdin without `--filename`: `.rufo` is searched from the **cwd up to the drive root**. Extension uses `workingDirectory = null` → depends on DevToys' cwd. A `.rufo` anywhere above changes output.
- `--filename=c1/whatever.rb` from another cwd picked up `c1/.rufo`.
- There is no "no config" flag; shadowing with an own `.rufo` is the only isolation.

## Options reference (complete for 0.18.1 — `lib/rufo/settings.rb`, first value = default)
| Key | Type | Tool default | Values | `.rufo` form | Meaning | Headline? |
|---|---|---|---|---|---|---|
| parens_in_def | enum | `:yes` | `:yes`, `:dynamic` | `parens_in_def :dynamic` | `:yes` forces `def foo(a, b)`; `:dynamic` keeps `def foo a, b` as written | Yes |
| align_case_when | bool | false | true/false | `align_case_when true` | Align `then` bodies of consecutive one-line `when`s | Yes |
| align_chained_calls | bool | false | true/false | `align_chained_calls true` | Align leading `.method` lines under the first dot | Yes |
| trailing_commas | bool | true | true/false | `trailing_commas false` | Add/remove trailing comma in multiline array/hash/args | Yes |
| quote_style | enum | `:double` | `:double`, `:single`, `:mixed` | `quote_style :single` | Preferred quotes for plain strings; `:mixed` leaves as written | Yes |
| includes | list | nil | globs | `includes [a,b]` | File discovery only — irrelevant for stdin | No |
| excludes | list | nil | globs | `excludes [a,b]` | File discovery only — irrelevant for stdin | No |
5 formatting options. Not configurable: indent (2 spaces), line width (rufo never wraps), hash syntax, line endings.

## Mismatches vs current `FormatterSettings.cs`
No Ruby settings exist (`Language.Ruby => baseArgs`). Nothing to mismatch.

## Verified notes
With `.rufo` = all non-defaults: `def foo a, b` kept (default → `def foo(a, b)`); `'hi'` kept (default → `"hi"`); `when 1   then 2` aligned; no trailing comma added after `2` in multiline array (default adds `2,`). `align_chained_calls true`: `  .baz(2)` → `   .baz(2)` aligned under `.bar`. `quote_style :mixed`: `'a'` and `"b"` both preserved. Always applied: `{:a=>1}` → `{ :a => 1 }`.
