# pasfmt (Delphi / Pascal)

Legend: **VERIFIED** = ran against `Binaries/pasfmt.exe`; **DOCS** = https://github.com/integrated-application-development/pasfmt

## Bundled version
`pasfmt.exe --version` → `pasfmt 0.7.0+dev` (VERIFIED) — a dev build past 0.7.0, not a tagged release. `pasfmt -C help` is therefore the authoritative option list for this binary.

## `--help` essentials (VERIFIED, exit 0)
```
Usage: pasfmt [OPTIONS] [PATHS]...     (no paths → stdin is read, mode defaults to stdout)
  -f, --files-from <FILE>
      --config-file <CONFIG_FILE>  Override the configuration file. By default working directory
                                   will be traversed until a `pasfmt.toml` file is found
  -C <KEY=VALUE>                   Override one configuration option. Takes precedence over
                                   `--config-file`. To list available options, use `-C help`.
  -m, --mode <files|stdout|check>
      --cursor <CURSOR>...         prints `CURSOR=<list>` of moved byte offsets to stderr
  -v, --verbose...   -l, --log-level <OFF|ERROR|WARN|INFO|DEBUG|TRACE> [default: WARN]
```

## Recommended invocation — `--config-file` (isolation) + `-C` (values)
```
["--config-file", "<abs path to an extension-owned EMPTY pasfmt.toml>",
 "-C", "encoding=utf-8",
 "-C", "line_ending=lf",            // or the UI value
 "-C", "wrap_column=120", "-C", "begin_style=auto", "-C", "format_multiline_strings=true",
 "-C", "use_tabs=false", "-C", "tab_width=2", "-C", "continuation_indents=2"]
```
stdin → stdout. `-C` works with stdin (VERIFIED, every key), repeated `-C` allowed, `-Ckey=value` (no space) also accepted. Equivalent: generate a full toml and pass only `--config-file`:
```toml
wrap_column = 120
begin_style = "auto"
format_multiline_strings = true
encoding = "utf-8"
use_tabs = false
tab_width = 2
continuation_indents = 2
line_ending = "lf"
```
**`encoding=utf-8` is mandatory, not cosmetic**: the default `native` means the Windows ANSI code page and it applies to stdin too. The extension pipes UTF-8. On this machine (ACP 1252) UTF-8 bytes happen to round-trip, but VERIFIED with `-C encoding=big5` / `shift_jis` (what `native` means on zh-TW / ja-JP Windows): `ERROR failed to read from stdin ... File '<stdin>' has malformed sequences (in encoding 'Big5')`, exit 1, for any non-ASCII string/comment. A BOM on input overrides the encoding and is preserved on output.

## stdout / stderr / exit code (VERIFIED)
| Case | exit | stdout | stderr |
|---|---|---|---|
| OK | 0 | formatted | empty, or `WARN ...` lines (e.g. multiline-string whitespace warning) — stderr non-empty does not mean failure |
| Unparseable Pascal | **0** | best-effort output (pasfmt is token/line based, never rejects; `begin begin ((( end` is just re-indented) | empty |
| Unknown `-C` key | 1 | empty | `ERROR failed to construct configuration` / `unknown field 'bogus', expected one of 'wrap_column', 'begin_style', 'format_multiline_strings', 'encoding', 'use_tabs', 'tab_width', 'continuation_indents', 'line_ending'` |
| Bad value | 1 | empty | `invalid type: string "abc", expected an integer for key 'wrap_column'` / `enum BeginStyle does not have variant constructor x` / `invalid value: string "bogus", expected "native" or a valid encoding label` |
| `--config-file` missing (also `NUL`) | 1 | empty | `ERROR configuration file "nope.toml" not found` |
| Unknown key in toml | 1 | empty | same "unknown field" message |
| Undecodable stdin | 1 | empty | see encoding above |

## Isolation / discovery gotchas (VERIFIED)
- `pasfmt.toml` is discovered from the **cwd and all parents** in stdin mode (cwd and parent both verified with `tab_width = 6`). Extension uses `workingDirectory = null` → depends on DevToys' cwd.
- `-C` beats the discovered toml per key, but passing all 8 keys by `-C` is still not isolation: a **broken** `pasfmt.toml` in cwd/parents fails the run (exit 1) before overrides apply.
- `--config-file <file>` disables discovery completely (VERIFIED in a dir with a broken toml → exit 0). The file must exist; an empty file is valid; `NUL` is NOT accepted.
- **Default `line_ending = native` → CRLF on Windows** (VERIFIED `^M$` on default run). Pin it.

## Options reference (complete: output of `pasfmt -C help`, 8 keys)
| Key | Type | Tool default | Values | CLI / toml form | Meaning | Headline? |
|---|---|---|---|---|---|---|
| wrap_column | uint | 120 | integer | `-C wrap_column=100` / `wrap_column = 100` | Target line length before wrapping | Yes |
| begin_style | enum | auto | `auto`, `always_wrap` | `-C begin_style=always_wrap` | `always_wrap`: `begin` after `if/for/while…` always on its own line at statement indent | Yes |
| use_tabs | bool | false | true/false | `-C use_tabs=true` | Tabs for indentation | Yes |
| tab_width | uint | 2 | integer | `-C tab_width=4` | Spaces per indent level (ignored if use_tabs) | Yes |
| continuation_indents | uint | 2 | integer | `-C continuation_indents=1` | Indent of wrapped continuation lines, as a multiple of the indent width | Yes |
| line_ending | enum | native (CRLF on Windows) | `lf`, `crlf`, `native` | `-C line_ending=lf` | Line terminator | Yes |
| format_multiline_strings | bool | true | true/false | `-C format_multiline_strings=false` | Re-indent the inside of `'''` multiline strings to the opening quote, normalise their line endings | Secondary |
| encoding | string | native (ANSI code page on Windows) | `native` or any WHATWG encoding label (`utf-8`, `windows-1252`, `big5`…) | `-C encoding=utf-8` | I/O encoding incl. stdin/stdout. Not a UI option — hard-wire `utf-8` | No (pin) |

## Mismatches vs current `FormatterSettings.cs`
No Delphi settings exist (`Language.Delphi => baseArgs`). Behavioural issues with the current bare `pasfmt` call: `encoding=native` (breaks non-ASCII on DBCS locales), CRLF output, cwd `pasfmt.toml` discovery.

## Verified notes
Sample: `if A > B then begin` + a 90-col `WriteLn(...)`.
- `wrap_column=60` → arguments broken one per line at column 8 (base indent 4 + 2×2); adding `continuation_indents=1` → column 6 (4 + 1×2).
- `begin_style=always_wrap` → `if A > B then` / `begin`.
- `use_tabs=true` → `\t` indents; `tab_width=4` / `8` → 4 / 8 spaces.
- `format_multiline_strings=false` leaves inner lines at their original 8/10-space indent; `true` re-indents them to the opening `'''`.
- Startup ≈ 0.08 s.
