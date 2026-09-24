# mh_style / MISS_HIT (MATLAB, Octave) — option research

Tags: **VERIFIED** = ran against `Binaries/mh_style.exe`; **DOCS** = https://florianschanda.github.io/miss_hit/style_checker.html and …/configuration.html.

## Bundled version & what the exe is
- `MISS_HIT 0.9.44`. VERIFIED (`--version`).
- 9.4 MB **PyInstaller one-file** bundle (strings `PyInstaller`, `pyi-runtime-tmpdir`): unpacks an embedded Python to `%TEMP%\_MEIxxxxx` on every start and deletes it on exit.

## Startup time
- **~1.6–1.9 s per invocation** on a 12-line file (1708, 1841, 1633 ms; `--version` 1.9 s). VERIFIED. Almost all of it is PyInstaller unpacking. Needs debounce for live formatting.

## No stdin
File-in-place only (`--fix` rewrites the file). Temp file stays.

## Recommended invocation
Private temp dir per invocation: `<dir>\miss_hit.cfg` (generated) + `<dir>\input.m`.
```
["--single", "--fix", "--brief", "<dir>\\input.m"]          env: PYTHONIOENCODING=UTF-8
```
`miss_hit.cfg`:
```
project_root
octave: "latest"            # only when Octave mode is on (or matlab: "2022a")
line_length: 100
tab_width: 4
newline_style: "lf"
indent_function_file_body: true
align_round_brackets: true
align_other_brackets: true
suppress_rule: "copyright_notice"
suppress_rule: "naming_functions"
suppress_rule: "line_length"
```
- `project_root` stops the upward config walk, so nothing above the temp dir can leak in. VERIFIED (parent cfg with `tab_width: 8` ignored once child cfg had `project_root`).
- `--single` only disables multi-threading; it does NOT ignore config (VERIFIED: config in the file's dir and in ancestors applied with `--single`).
- Only 4 rule parameters have CLI flags: `--line_length N`, `--tab_width N`, `--file_length N`, `--copyright-entity STR…`; plus `--octave [VER]` / `--matlab [VER]`. CLI overrides config (VERIFIED: `--tab_width 2` beat cfg `tab_width: 3`). Everything else (`suppress_rule`, `newline_style`, `align_*`, `indent_function_file_body`, `regex_*`) is **config-file only** → config file is the mechanism.
- A minimal no-config alternative: `["--single","--fix","--brief","--ignore-config","--tab_width","2","<file>"]` — but then rules cannot be suppressed and output has CRLF (below).
- `--octave` takes an OPTIONAL value: `--octave file.m` swallows the file name (`error: Octave version must be MAJOR.MINOR`; exit 2). Use `--octave latest` / put it in config / place it after the file. VERIFIED.

## stdout / stderr / exit codes (VERIFIED)
- **Everything goes to stdout** (messages, errors, summary); stderr only for argparse errors.
- First lines of stdout are a `WARNING: ... python will encode to cp1252 on stdout` banner unless env `PYTHONIOENCODING=UTF-8` is set (VERIFIED).
| Case | exit | Notes |
|---|---|---|
| All clean or everything fixed and nothing left | 0 | `MISS_HIT Style Summary: 1 file(s) analysed, everything seems fine` |
| Fixed, but non-fixable style issues remain (default: `naming_functions`, `copyright_notice` fire on almost any snippet) | **1** | file IS correctly rewritten |
| Parse / lex error | 1 | `x.m: error: file is not auto-fixed because it contains parse errors` + `x.m:1:11: error: expected IDENTIFIER, found NEWLINE instead`; file untouched |
| Config file error | 1 | `miss_hit.cfg:1:11: error: expected NUMBER, found STRING instead`, `...: error: expected valid style rule name (did you mean …?)`; file untouched |
| Unknown CLI option | 2 | argparse usage on stderr |
- Exit code cannot distinguish "formatted with leftover lint" from "failed". Contract: success ⇔ stdout contains no `: error:` / `lex error:` line. With `--brief`, messages are one-line `file:line:col: style|error: text [rule]`, fixed ones tagged `[fixed]`. `--json FILE` writes a machine-readable report if preferred.

## Isolation / discovery gotchas (VERIFIED)
- Config files `miss_hit.cfg` and `.miss_hit` are looked up in the file's directory **and every ancestor up to the drive root** (until one declares `project_root`); cwd is irrelevant. A temp file in `%TEMP%` therefore inherits e.g. `C:\Users\<user>\miss_hit.cfg` or `C:\miss_hit.cfg`. Current invocation is exposed to this.
- Switch off: `--ignore-config`, or own cfg with `project_root` (recommended since config is needed anyway).
- A broken ancestor config aborts the run ("cannot find project root because the config file contains errors").
- **Line endings:** default `newline_style` is `native` → on Windows the file is rewritten with **CRLF** regardless of input (VERIFIED). Set `newline_style: "lf"` (VERIFIED → LF) or `"crlf"`.
- **Encoding:** reads as cp1252 by default (`--input-encoding`). UTF-8 bytes round-tripped intact in test, but bytes undefined in cp1252 (0x81, 0x8D, 0x8F, 0x90, 0x9D) may fail; pass `--input-encoding utf-8` if the temp file is written as UTF-8 (VERIFIED works; write without BOM). Non-ASCII triggers the `unicode` style message (suppress it).
- `--fix` never wraps long lines; `line_length`/`file_length` are report-only.

## Options reference
### Parameters
| Key | Type | Default | Values | CLI / config form | Meaning | Headline? |
|---|---|---|---|---|---|---|
| tab_width | int | 4 | ≥0 | `--tab_width 2` / `tab_width: 2` | Indent width (spaces; tabs always removed) | **Yes** |
| line_length | int | 80 | | `--line_length 100` / `line_length: 100` | Limit for the (report-only) line_length rule; also affects nothing else | Low (lint only) |
| file_length | int | 1000 | | `--file_length N` / `file_length: N` | Report-only | No |
| newline_style | enum | native | native, lf, crlf, cr | cfg only `newline_style: "lf"` | EOL of rewritten file | **Yes** (or derive) |
| indent_function_file_body | bool | true | | cfg only | Indent body of function files | **Yes** |
| align_round_brackets | bool | true | | cfg only | Align continuation lines under `(` | **Yes** |
| align_other_brackets | bool | true | | cfg only | Same for `[` `{` | **Yes** |
| octave | string | off | "latest", "4.4" … "7.2" (bool `true` also accepted) | `--octave latest` / `octave: "latest"` | Octave dialect (`#` comments, `!=`, …; `endfunction` still a parse error at 0.9.44) | **Yes** |
| matlab | string | latest | "latest", "2017b" … "2022a" | `--matlab 2020b` / `matlab: "2020b"` | MATLAB dialect version | Maybe |
| suppress_rule / enable_rule | string (repeat) | all rules on | rule names below | cfg only `suppress_rule: "x"` | Turn optional rules off/on | **Yes** (checkbox per rule) |
| copyright_entity, copyright_primary_entity, copyright_3rd_party_entity, copyright_location, copyright_in_embedded_code, copyright_regex | string/bool | – | | `--copyright-entity` / cfg | Only for copyright_notice rule | No (suppress the rule) |
| regex_class_name, regex_function_name, regex_nested_name, regex_method_name, regex_attribute_name, regex_script_name, regex_parameter_name, regex_enumeration_name | regex string | Ada-ish scheme | | cfg only | Naming lint | No |
| enforce_encoding, enforce_encoding_comments | string/bool | "ascii"/true | | cfg only | unicode rule | No |
| --input-encoding | string | cp1252 | python codec | CLI only | Source decoding | fixed `utf-8` |
| --ignore-pragmas | flag | off | | CLI | Ignore `% mh:ignore_style` pragmas | No |
| --process-slx | flag | off | | CLI | Simulink models; irrelevant | No |
| --brief | flag | off | | CLI | One-line messages | fixed on |
| --single | flag | off | | CLI | No multiprocessing (faster for 1 file) | fixed on |
| enable, exclude_dir, ignore_dir, metric, library/entrypoint blocks | | | | cfg only | Project-level, irrelevant for snippets | No |

### Style rules
`mh_style --help` does NOT print a rules section at 0.9.44; list is from DOCS, and every name below was validated by putting `suppress_rule: "<name>"` in a config (invalid names give `error: expected valid style rule name`). VERIFIED.

Mandatory (cannot be suppressed — names rejected by `suppress_rule`; all autofix): trailing newline at EOF, consecutive blank lines (max 1), no tabs (uses tab_width), trailing whitespace, newlines (newline_style).

| Rule | Autofix | Meaning |
|---|---|---|
| no_starting_newline | yes | File must not start with blank line |
| whitespace_comma | yes | No space before, one after `,` |
| whitespace_semicolon | yes | Same for `;` |
| whitespace_colon | yes | No whitespace around `:` |
| whitespace_assignment | yes | Spaces around `=` |
| whitespace_brackets | yes | No space after `(`/before `)` |
| whitespace_keywords | yes | Space after `if`, `properties`, … |
| whitespace_comments | yes | Space between `%` and text; space before trailing comment |
| whitespace_continuation | yes | Space before `...` |
| whitespace_around_functions | yes | Blank line around function definitions |
| useless_continuation | yes | Remove pointless `...` |
| dangerous_continuation | yes (DOCS) | Continuations that change meaning in matrices |
| operator_after_continuation | yes | Operator must precede `...`, not start the next line |
| operator_whitespace | yes | Spaces around binary operators (not `^`), none after unary |
| end_of_statements | yes | Terminate with `;`/newline consistently; split `if a, b; else` one-liners |
| indentation | yes | Indent by tab_width; align continuations (align_* params) |
| redundant_brackets | yes | Remove useless `( )` |
| spurious_row_comma | yes | Trailing `,` in matrix/cell rows |
| spurious_row_semicolon | yes | Trailing `;` in matrix/cell |
| annotation_whitespace | yes | Space after `%|` annotations |
| implicit_shortcircuit | yes | `&`/`|` in `if`/`while` → `&&`/`||` |
| force_newlines (name accepted by 0.9.44; undocumented) | ? | – |
| builtin_shadow | no | Assigning to builtin names |
| copyright_notice | no | Requires copyright header — **suppress for snippets** |
| naming_classes / naming_functions / naming_scripts / naming_parameters / naming_enumerations | no | Regex naming lint — **suppress** |
| unicode | no | Non-ASCII characters |
| file_length | no | > file_length lines |
| line_length | no | > line_length chars |

Counts: 5 mandatory + 32 suppressible rule names (~21 autofix, ~10 report-only, 1 undocumented; autofix flags for operator_after_continuation, implicit_shortcircuit, builtin_shadow are from memory of DOCS, not re-verified). For a formatter UI: expose tab_width, newline_style, 3 indentation booleans, octave mode, and optionally checkboxes for the ~21 autofix rules; suppress all report-only rules by default so exit code 0 means success.

## Verified notes
- Default run on messy input: fixed operator/comma/assignment whitespace, split `if a>b,r=a+b;else`, 2→4-space indent, tab removed, `;;`→`;`, added `;` after `disp(z)`, collapsed double blank lines; output CRLF; exit 1 due to naming/copyright.
- `--tab_width 2` → 2-space indent. cfg `tab_width: 8` in grandparent dir applied (8-space indent) until `--ignore-config` / child `project_root`.
- cfg with `suppress_rule: "end_of_statements"`, `"whitespace_comma"` → those fixes not applied (`[1,2 ,3]; ;` left alone).
- cfg `newline_style: "lf"`, `indent_function_file_body: false`, `align_round_brackets: false` → LF file, body unindented, continuation `   b);` untouched; exit 0 "everything seems fine" once copyright/naming suppressed.
- `octave: "latest"`: `#` comment and `!=` accepted (lex error without); `endfunction` → parse error.
- `--line_length 30` → only reports `line exceeds 30 characters [line_length]`, no rewrap.
