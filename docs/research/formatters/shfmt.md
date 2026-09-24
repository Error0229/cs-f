# shfmt (Shell)

Legend: **VERIFIED** = ran against `Binaries/shfmt.exe`; **DOCS** = upstream docs (URL cited).

## Bundled version
`shfmt.exe --version` → `v3.12.0` (VERIFIED). Upstream latest is 3.14.1; zsh dialect arrived in 3.13.0 (DOCS: https://github.com/mvdan/sh/blob/master/CHANGELOG.md).

## Full `--help` (VERIFIED, exit 0)
```
usage: shfmt [flags] [path ...]

  --version  show version and exit

  -l[=0], --list[=0]  list files whose formatting differs from shfmt
  -w,     --write     write result to file instead of stdout
  -d,     --diff      error with a diff when the formatting differs
  --apply-ignore      always apply EditorConfig ignore rules
  --filename str      provide a name for the standard input file

Parser options:
  -ln, --language-dialect str  bash/posix/mksh/bats, default "auto"
  -p,  --posix                 shorthand for -ln=posix
  -s,  --simplify              simplify the code

Printer options:
  -i,  --indent uint       0 for tabs (default), >0 for number of spaces
  -bn, --binary-next-line  binary ops like && and | may start a line
  -ci, --case-indent       switch cases will be indented
  -sr, --space-redirects   redirect operators will be followed by a space
  -kp, --keep-padding      keep column alignment paddings
  -fn, --func-next-line    function opening braces are placed on a separate line
  -mn, --minify            minify the code to reduce its size (implies -s)

Utilities: -f/--find, --to-json, --from-json
Formatting options can also be read from EditorConfig files; see 'man shfmt'
```

## Recommended invocation
stdin → stdout, pure CLI flags, no config file needed.
```
["--filename", "script.sh", "-ln", "<dialect>", "-i", "<N>", ("-bn")?, ("-ci")?, ("-sr")?, ("-kp")?, ("-fn")?, ("-s")?, ("-mn")?]
```
ALWAYS emit `-i N` (even `-i 0`) — it is what guarantees EditorConfig is off (see below). The current code already does that.

## stdout / stderr / exit code (VERIFIED)
| Case | exit | stdout | stderr |
|---|---|---|---|
| OK | 0 | formatted (LF; CRLF input → LF) | empty |
| Shell syntax error | 1 | empty | `script.sh:1:1: "if" must be followed by a statement list` |
| Dialect violation (`-ln posix` + arrays) | 1 | empty | `script.sh:1:3: arrays are a bash/mksh feature; tried parsing as posix` |
| Unknown flag | 2 | empty | `flag provided but not defined: -zz` + full usage |
| Bad value (`-i abc`, `-i -1`, `-ln zsh`) | 2 | empty | `invalid value "abc" for flag -i: parse error` / `invalid value "zsh" for flag -ln: unknown shell language variant: "zsh"` + full usage |

## Isolation / discovery gotchas (VERIFIED)
- EditorConfig IS used with stdin when `--filename` is given and NO parser/printer flag is given: `.editorconfig` is searched from cwd (relative filename) and parents. cwd `.editorconfig` with `indent_size=3, switch_case_indent=true` → 3-space indent + indented cases.
- Without `--filename`, stdin ignores EditorConfig.
- ANY parser or printer flag disables EditorConfig entirely — verified for `-i 0`, `-i=0`, `-sr`, `-ln bash`, `-s` (DOCS man page: https://github.com/mvdan/sh/blob/v3.12.0/cmd/shfmt/shfmt.1.scd). Since the extension always passes `-i`, it is isolated today. Do not "optimise" by omitting default-valued flags.
- `--filename` with an absolute path in a nonexistent dir also sidesteps cwd discovery.
- Dialect auto-detection uses filename extension then shebang, falling back to bash. With `--filename script.sh` and no shebang, bash arrays parse fine; with `#!/bin/sh` the same input fails as posix (`... (parsed as posix via -ln=auto)`). So "auto" results depend on the shebang — expected, but surface `-ln` in the UI.

## Options reference
| Key | Type | Tool default | Values | CLI form | EditorConfig key | Meaning | Headline? |
|---|---|---|---|---|---|---|---|
| indent | uint | **0 (tabs)** | 0 = tabs, >0 = spaces | `-i N` | indent_style / indent_size | Indentation | Yes |
| binaryNextLine | bool | **false** | flag | `-bn` | binary_next_line | `&&`, `\|` may start a line (`a \` newline `&& b`) | Yes |
| caseIndent | bool | **false** | flag | `-ci` | switch_case_indent | Indent `case` patterns | Yes |
| spaceRedirects | bool | false | flag | `-sr` | space_redirects | `>out` → `> out` | Yes |
| keepPadding | bool | false | flag | `-kp` | keep_padding | Keep column alignment padding. **DEPRECATED, "will be removed in the next major version"** (DOCS man page v3.12.0) | No (consider dropping) |
| funcNextLine | bool | false | flag | `-fn` | function_next_line | `foo()` newline `{` | Yes |
| languageDialect | enum | `auto` | `auto`, `bash`, `posix`, `mksh`, `bats` (NO `zsh` at 3.12.0 — exit 2) | `-ln X` (`-p` = `-ln posix`) | shell_variant | Parser dialect | Yes |
| simplify | bool | false | flag | `-s` | simplify | Simplify code (`[[ "$a" == "b" ]]` → `[[ $a == "b" ]]`) — changes tokens, not just layout | Secondary |
| minify | bool | false | flag | `-mn` | minify | Minify, implies `-s`, drops comments & indentation | Secondary |

Non-formatting: `-l`, `-w`, `-d`, `-f`, `--apply-ignore`, `--to-json`, `--from-json`, `--filename`.

## Mismatches vs current `FormatterSettings.cs` (wrapper default ≠ tool default)
| Setting | FormatterSettings.cs | shfmt | Note |
|---|---|---|---|
| indent | **2** | **0 (tabs)** | MISMATCH |
| binaryNextLine | **true** | **false** | MISMATCH |
| caseIndent | **true** | **false** | MISMATCH |
| spaceRedirects | false | false | ok |
| keepPadding | false | false | ok (deprecated upstream) |
| funcNextLine | false | false | ok |
| — | missing | `-ln`, `-s`, `-mn` | not exposed |
`indent` Max: 16 is an extension-side cap; shfmt accepts any uint. (The 2/true/true triple is Google-shell-style, not the tool default.)

## Verified notes
Input had `if [ -n "$a" ] &&` continuation, `echo hi >out.txt`, a `case`, aligned comments, `foo() {`.
- `-i 4`: tabs → 4 spaces. `-bn`: `[ -n "$a" ] \` / `&& [ -n "$b" ]; then`. `-ci`: `a) echo a ;;` indented one level. `-sr`: `> out.txt`. `-fn`: brace on own line. `-kp`: kept `a=1   # pad` spacing. `-s`: unquoted `$a` inside `[[ ]]`. `-mn`: `foo(){`, no indentation, comments removed.
- Default always rewrites backticks to `$(...)`.
