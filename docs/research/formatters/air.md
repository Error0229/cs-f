# Air (R)

Legend: **VERIFIED** = ran against `Binaries/air.exe`; **DOCS** = https://posit-dev.github.io/air/configuration.html , https://posit-dev.github.io/air/cli.html , https://github.com/posit-dev/air/blob/main/CHANGELOG.md

## Bundled version
`air.exe --version` → `air 0.8.0` (VERIFIED). Upstream is ≥ 0.11.0.

## Full help (VERIFIED, exit 0)
```
Usage: air.exe [OPTIONS] <COMMAND>      Commands: format, language-server, help
Global options:
  --log-level <error|warn|info|debug|trace>   (default warn)
  --no-color        (or env NO_COLOR)

Usage: air.exe format [OPTIONS] [PATHS]...
  --check    do not write; non-zero exit if files would change
```
That is everything: **no formatting flags, no `--config`, no stdin** at 0.8.0.
- `air format -` → `ERROR Failed to format -: The system cannot find the file specified.` exit 255 (VERIFIED).
- `air format --stdin-file-path x.R` → `error: unexpected argument '--stdin-file-path' found`, exit 2 (VERIFIED).
- DOCS: `--stdin-file-path` (stdin → stdout) and `--force` were added in **0.9.0**; `assignment-style` in 0.10.0; user-level `%APPDATA%\air\air.toml` in 0.11.0; `--no-configuration` is in the unreleased dev version. Upgrading the binary to ≥ 0.9.0 would remove the temp-file hack (config would still come from an `air.toml` located via the `--stdin-file-path` directory).

## Recommended invocation (0.8.0) — private temp dir per invocation
```
<tmp>\CodeFormatter\<guid>\air.toml      ← generated from UI settings (always write it, even if all defaults)
<tmp>\CodeFormatter\<guid>\input.R       ← source
args: ["format", "--no-color", "<tmp>\\CodeFormatter\\<guid>\\input.R"]
```
then read `input.R` back and delete the directory. VERIFIED: `air.toml` next to the file is applied for every option below; cwd is irrelevant (cwd containing a different `air.toml` had no effect on a file elsewhere) — discovery starts at the **file's** directory and walks up. Always writing `air.toml` in the private dir is what shadows any `air.toml`/`.air.toml` in `%TEMP%`, `AppData`, the user's home, or `C:\`. The current flat `…\Temp\CodeFormatter\temp_<guid>.r` layout is NOT isolated (VERIFIED: a parent-dir `air.toml` is picked up).

Generated file (all 0.8.0 keys at their defaults):
```toml
[format]
line-width = 80
indent-width = 2
indent-style = "space"
line-ending = "auto"
persistent-line-breaks = true
exclude = []
default-exclude = true
skip = []
table = []
default-table = true
```
File-name notes (VERIFIED): `.air.toml` is also recognised (DOCS: `air.toml` wins if both); the extension of the source file is not checked for explicitly passed files (`t.txt` was formatted); `exclude` does NOT apply to an explicitly passed file at 0.8.0 (DOCS: 0.9.0 changed exclusion for directly supplied files and added `--force` — re-verify on upgrade).

## stdout / stderr / exit code (VERIFIED)
| Case | exit | stdout | stderr | file |
|---|---|---|---|---|
| OK (changed or not) | 0 | empty | empty | rewritten in place |
| R syntax error | **255** | empty | `ERROR Failed to format e9\t.r: Failed to parse due to syntax errors.` — contains ANSI colour codes even when redirected unless `--no-color` | **unchanged** |
| Unknown key in air.toml | 255 | empty | `air failed` / `Cause: Failed to parse <path>\air.toml: TOML parse error at line 2 ... unknown field 'bogus', expected one of 'line-width', 'indent-width', 'indent-style', 'line-ending', 'persistent-line-breaks', 'exclude', 'default-exclude', 'skip', 'table', 'default-table'` | unchanged |
| Bad enum / out of range | 255 | empty | `unknown variant 'bogus', expected 'tab' or 'space'` / `The line width must be a value between 1 and 320, not 0.` / `The indent width must be a value between 1 and 24, not 100.` | unchanged |
| Unknown CLI arg | 2 | empty | clap usage error | unchanged |

**Extension bug exposed by this contract:** `RunWithTempFileAsync` computes `success = ExitCode == 0 || !string.IsNullOrEmpty(formatted)`. On every air failure the temp file still holds the (non-empty) input, so failures are reported as success with unchanged text and the stderr message is dropped. For air, success must be `ExitCode == 0`.

## Isolation / discovery gotchas
- Discovery: file's dir → parents, `air.toml` / `.air.toml` (VERIFIED). Newer versions add a user-level file (DOCS) — a private-dir `air.toml` still takes priority (project-level wins).
- `line-ending = "auto"` (default) keeps the input's convention: CRLF in → CRLF out, LF in → LF out (VERIFIED). `File.WriteAllTextAsync` writes whatever the editor text contains, so this is input-dependent, not machine-dependent. `native` = CRLF on Windows (VERIFIED).
- `# fmt: skip` / `# fmt: skip file` comments in the source are honoured (VERIFIED `# fmt: skip`).

## Options reference (complete for 0.8.0 = the "expected one of" list printed by the binary)
| Key (`[format]`) | Type | Tool default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| line-width | int | 80 | 1–320 (VERIFIED bounds) | Preferred max line length | Yes |
| indent-width | int | 2 | 1–24 (VERIFIED bounds) | Spaces per indent level | Yes |
| indent-style | enum | "space" | "space", "tab" | Indent character | Yes |
| line-ending | enum | "auto" | "auto", "lf", "crlf", "native" | Line terminator; auto = detect from input | Yes |
| persistent-line-breaks | bool | true | true/false | true: a user-placed line break after `(`/in a call keeps it expanded; false: collapse whenever it fits | Yes |
| skip | string[] | [] | function names | Calls to these functions are left unformatted | Secondary (text list) |
| table | string[] | [] | function names | Calls formatted as aligned tables (added 0.8.0) | Secondary |
| default-table | bool | true | true/false | Built-in table functions (`tribble()`, `fcase()`) get table formatting | Secondary |
| exclude | string[] | [] | gitignore-style globs | Directory-walk exclusion; no effect on the single explicit file | No |
| default-exclude | bool | true | true/false | Built-in excludes (e.g. `renv/`, `cpp11.R`); no effect here | No |
| assignment-style | — | — | — | **NOT in 0.8.0** (added 0.10.0: "arrow"/"equal"/"preserve"). Note 0.8.0 preserves `=` assignments as written (VERIFIED `x = c(...)` kept). | after upgrade |
All config is toml-only; CLI form: none. Defaults DOCS; defaults for line-width/indent/persistent-line-breaks consistent with VERIFIED default output.

## Mismatches vs current `FormatterSettings.cs`
No R settings exist (`Language.R => baseArgs`). Issues are in the mechanism: shared flat temp dir (not isolated, and no place to put a per-call `air.toml`), failure detection (above), missing `--no-color`.

## Verified notes
Sample had a long `print(...)` inside `if/else` and `list(` newline `a = 1, b = 2)`.
- `line-width = 200` → `print(...)` stays on one line (default 80 breaks it one-arg-per-line).
- `indent-width = 4`, `indent-style = "tab"` → 4 spaces / `\t`.
- `line-ending = "crlf"` and `"native"` → `\r\n`; `"lf"` on CRLF input → `\n`.
- `persistent-line-breaks = false` → `list(a = 1, b = 2)` collapsed (default keeps it expanded).
- `skip = ["mymat"]` leaves `mymat(1,0,` / ` 0,1)` untouched; `table = ["mymat"]` → aligned `1 , 0 ,` rows; `default-table = false` → `tribble()` formatted as an ordinary call, one arg per line.
- Default always applied: `if (a > b) x else y` gets braces, `f<-function` → `f <- function`.
- Startup ≈ 0.09 s.
