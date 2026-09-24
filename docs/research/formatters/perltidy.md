# perltidy (Perl) — option research

Tags: **VERIFIED** = ran against `Binaries/perltidy.exe`; **DOCS** = https://perltidy.github.io/perltidy/perltidy.html (same text on metacpan / sourceforge).
Raw dumps: `perltidy-options-raw.txt` (same dir) — `--dump-defaults`, `--dump-long-names`, `--dump-short-names`, `--dump-options`, `--dump-token-types`, `--dump-want-left-space`, `--dump-want-right-space`, `--dump-cuddled-block-list`, `--dump-integer-option-range`.

## Bundled version & what the exe is
- `perltidy v20250912` (Perl::Tidy). VERIFIED (`-v`).
- 12 MB **PAR::Packer 1.064** self-extracting bundle embedding Perl 5.42 (`perl542.dll`). VERIFIED (strings in exe).
- On first run extracts ~21 MB to `%TEMP%\par-<hex(username)>\cache-<sha1>\` (seen: `%TEMP%\par-6c6f67696e\cache-9e89a8...`); reused afterwards. If %TEMP% is cleaned the next run re-extracts (slower).

## Startup time
- **~1.8–2.4 s per invocation** with warm PAR cache, 6-line input (5 runs: 2386, 2076, 2075, 1796, 1908 ms). VERIFIED. This is the slowest stdin formatter in the bundle; live-as-you-type needs a long debounce + cancel of in-flight runs. No daemon mode exists.

## Recommended invocation
All options are plain CLI flags — no config file needed. stdin → stdout.
```
["-npro", "-st", "-se", "-nola"?, ...userFlags]
e.g. ["-npro","-st","-se","-i=2","-l=100","-ce","-pt=2","-ole=unix"]
```
- Form: `--long-name=value` or `-short=value`; booleans `--name` / `--noname` (short: `-x` / `-nx`). Values containing spaces are fine as ONE argv element: `"-wba=+ - / *"` (VERIFIED).
- Alternative: write a profile file and pass `-pro=<path>` (one option per line, `#` comments). Only worthwhile to avoid long command lines.
- Always pass `-npro` (see gotchas). `-npro` must be on the command line (it cannot come from a profile).

## stdout / stderr / exit codes (VERIFIED)
| Case | stdout | stderr | exit |
|---|---|---|---|
| OK | formatted | empty | 0 |
| Unknown option / non-integer for int option | empty | `Unknown option: bogus-thing` / `Value "abc" invalid for option indent-columns (integer number expected)` + `Error on command line; for help try 'perltidy -h'` | 1 |
| Integer out of range (`-pt=7`) | formatted with default | `--paren-tightness=7 but should be <= 2; using default 1` | 0 (warning only) |
| Conflicting options (`-t` with default `-ola`) | formatted, option ignored | `Conflict: -t (tabs) cannot be used with the -ola  option; ignoring -t; see -et.` | 0 |
| Source has unbalanced braces etc. | **input echoed unchanged** (or best-effort formatted) | `<stdin>: Begin Error Output Stream ...` details | 2 |
- DOCS: exit 0 = ok, 1 = abnormal termination, 2 = completed with warnings/errors in input.
- Without `-se`, errors would be written to a `perltidy.ERR` file in cwd — keep `-se`.
- Treat stderr non-empty + exit 0 as a warning to surface (bad option value silently replaced by default).

## Isolation / discovery gotchas
- Profile search order (DOCS; first two VERIFIED): `.perltidyrc` in **cwd** → file named by env `PERLTIDY` → `$HOME/.perltidyrc`; on Windows also `perltidy.ini` variants and `%USERPROFILE%`, `%ALLUSERSPROFILE%`, `C:\`. `perltidy -dpro` prints the search trace.
  - VERIFIED: `.perltidyrc` with `-i=8` in cwd changed indent; `PERLTIDY=<file>` with `-i=6` changed indent; `-npro` suppressed both.
- `-npro`/`--noprofile` disables all of it. Pass it always.
- Line endings: CRLF stdin → LF stdout by default; `-ple` (preserve) had **no effect on stdin** (VERIFIED). Use `-ole=win|unix|dos|mac` explicitly (`-ole=win` VERIFIED → `\r\n`).
- `-t` (tabs) is silently ignored unless `-nola` (no outdent labels) is also given (VERIFIED). `-et=N` works without it.
- `--timeout-in-seconds=10` default only applies to interactive stdin wait; harmless.

## Option inventory (VERIFIED from `--dump-long-names`)
- **390** long option names: 202 negatable booleans (`!`), 73 integers (`=i`/`:i`), 103 strings (`=s`), 12 plain flags.
- Not formatting-relevant: ~80 HTML/pod2html (`html-*`, `pod*`, `frames`, `title`…), 21 `dump-*`, 16 `warn-*`, ~25 I/O / run control (`outfile`, `backup-*`, `logfile`, `standard-output`, `profile`, `quiet`, `assert-*`…).
- **Formatting-relevant: ~245.** 140 options have non-off defaults (`--dump-defaults`). 72 integer options with ranges in `--dump-integer-option-range`.
- 673 short-name/abbreviation entries (`--dump-short-names`), including preset expansions.

Categories (DOCS section names, approximate counts of long names):
| Category | ~Count | Examples |
|---|---|---|
| Basic / indentation | 20 | indent-columns, continuation-indentation, tabs, entab-leading-whitespace, default-tabsize, line-up-parentheses, extended-line-up-parentheses, closing-token-indentation, indent-closing-brace, outdent-* |
| Line length / breaking control | 35 | maximum-line-length, variable-maximum-line-length, want-break-before/after, break-before/after-all-operators, break-at-old-*-breakpoints, ignore-old-breakpoints, keep-old-breakpoints-*, comma-arrow-breakpoints, weld-nested-containers |
| Whitespace | 35 | paren/brace/square-bracket/block-brace-tightness, space-for-semicolon, space-function-paren, space-keyword-paren, want-left/right-space, nowant-*, add-whitespace, delete-old-whitespace, tight-secret-operators, trim-qw |
| Braces / block layout | 30 | opening-brace-on-new-line, opening-sub-brace-on-new-line, brace-left-and-indent, cuddled-else, cuddled-block-list, *-vertical-tightness(-closing), stack-opening/closing-*, one-line-block-* |
| Comments | 30 | indent-block-comments, static-block-comments, closing-side-comments(+8 csc-*), hanging-side-comments, minimum-space-to-comment, fixed-position-side-comment, delete-*-comments |
| Blank lines | 20 | blanks-before-blocks/comments, blank-lines-before-subs/packages, maximum-consecutive-blank-lines, keep-old-blank-lines, keyword-group-blanks-* (8) |
| Commas / semicolons / syntax edits | 20 | add/delete-semicolons, add/delete-trailing-commas, want-trailing-commas, add/delete-interbracket-arrows, add-missing-else |
| Vertical alignment | 10 | valign-code, valign-block-comments, valign-side-comments, valign-exclusion-list, valign-signed-numbers |
| Skipping / encoding / misc | 25 | format-skipping(-begin/-end), code-skipping, character-encoding, output-line-ending, iterations, use-feature |

Presets (VERIFIED expansions from `--dump-short-names`):
- `-pbp` / `--perl-best-practices` = `l=78 i=4 ci=4 st se vt=2 cti=0 pt=1 bt=1 sbt=1 bbt=1 nsfs nolq wbb="% + - * / x != == >= <= =~ !~ < > | & = **= += *= &= <<= &&= -= /= |= >>= ||= //= .= %= ^= x="`
- `-gnu` / `--gnu-style` = `lp bl noll pt=2 bt=2 sbt=2 cpi=1 csbi=1 cbi=1`
- `--mangle`, `--extrude` (stress-test/minify presets), `-conv` = `it=4`, `-kgb` = `kgbb=2 kgbi kgba=2`, `-bbs` = `blbs=1 blbp=1`, `-vt=N` = `pvt=N bvt=N sbvt=N`, `-vtc=N` = `pvtc bvtc sbvtc`, `-cti=N` = `cpi cbi csbi`, `-act=N` = `pt sbt bt bbt`.
- Later flags override earlier ones, so `["-pbp","-l=100"]` works as preset + override.

## Options reference — headline set
| Key (long) | Short | Type | Default | Values | CLI form | Meaning | Headline? |
|---|---|---|---|---|---|---|---|
| (preset) | -pbp / -gnu | enum | none | none, pbp, gnu | `-pbp` | Style preset (expansions above) | Yes |
| indent-columns | -i | int | 4 | 0.. | `-i=2` | Spaces per indent level | Yes |
| continuation-indentation | -ci | int | 2 | 0.. | `-ci=4` | Extra indent for continued statements | Yes |
| maximum-line-length | -l | int | 80 | 0.. (0 = unlimited) | `-l=100` | Line width | Yes |
| entab-leading-whitespace | -et | int | 0 (off) | 0.. | `-et=4` | Convert each N leading spaces to a tab | Yes ("use tabs") |
| tabs | -t | bool | off | | `-t -nola` | One tab per indent level; requires `-nola` | Yes (alt.) |
| opening-brace-on-new-line | -bl | bool | off | | `-bl` | Block `{` on its own line | Yes |
| opening-sub-brace-on-new-line | -sbl | bool | follows -bl | | `-sbl` | Sub `{` on its own line | Yes |
| brace-left-and-indent | -bli | bool | off | | `-bli` | Whitesmiths-style: brace on new line and indented | Yes |
| cuddled-else | -ce | bool | off | | `-ce` | `} else {` on one line | Yes |
| paren-tightness | -pt | int | 1 | 0,1,2 | `-pt=2` | Space inside `( )`: 0 always, 2 never | Yes |
| square-bracket-tightness | -sbt | int | 1 | 0,1,2 | `-sbt=2` | Same for `[ ]` | Yes |
| brace-tightness | -bt | int | 1 | 0,1,2 | `-bt=2` | Same for hash/anon `{ }` | Yes |
| block-brace-tightness | -bbt | int | 0 | 0,1,2 | `-bbt=1` | Same for code-block `{ }` | Yes |
| space-for-semicolon | -sfs | bool | on | | `-nsfs` | Space before `;` in `for ( ; ; )` | Yes |
| space-function-paren / space-keyword-paren | -sfp / -skp | bool | off | | `-sfp` | `f (` / `if  (` spacing | Maybe |
| outdent-long-quotes | -olq | bool | on | | `-nolq` | Outdent over-long quoted strings | Yes |
| outdent-long-comments | -olc | bool | on | | `-nolc` | Same for comments | Maybe |
| vertical-tightness | -vt | int | 0 | 0,1,2 | `-vt=2` | Keep opening token with next line | Yes |
| vertical-tightness-closing | -vtc | int | 0 | 0..3 | `-vtc=2` | Keep closing token with previous line | Yes |
| closing-token-indentation | -cti | int | 0 | 0..3 | `-cti=1` | Indentation of lone `)`, `]`, `}` | Maybe |
| line-up-parentheses | -lp | bool | off | | `-lp` | Align continuation under opening paren | Yes |
| want-break-before | -wbb | string | (`. : ? && \|\| and or` etc.) | token list | `"-wbb=+ - * /"` | Break line BEFORE these operators | Yes |
| want-break-after | -wba | string | | token list | `"-wba=. ,"` | Break AFTER these operators | Yes |
| break-before-all-operators / break-after-all-operators | -bbao / -baao | bool | off | | `-bbao` | Shortcut for the above | Yes |
| blanks-before-blocks | -bbb | bool | on | | `-nbbb` | Blank line before `if/for/while…` blocks | Yes |
| blanks-before-comments | -bbc | bool | on | | `-nbbc` | Blank before full-line comments | Maybe |
| blank-lines-before-subs | -blbs | int | 1 | 0.. | `-blbs=2` | Blank lines before `sub` | Yes |
| blank-lines-before-packages | -blbp | int | 1 | 0.. | `-blbp=2` | Blank lines before `package` | Maybe |
| maximum-consecutive-blank-lines | -mbl | int | 1 | 0.. | `-mbl=2` | Cap on consecutive blank lines | Yes |
| keep-old-blank-lines | -kbl | int | 1 | 0,1,2 | `-kbl=0` | 0 drop, 1 keep (subject to mbl), 2 keep all | Yes |
| keyword-group-blanks | -kgb | preset | off | | `-kgb` | Blank lines around runs of `my/our/use` (≥5 lines) | Maybe |
| delete-semicolons | -dsm | bool | on | | `-ndsm` | Remove redundant `;` | Yes |
| add-semicolons | -asc | bool | on | | `-nasc` | Add `;` before closing `}` of multi-line block | Yes |
| add-trailing-commas / delete-trailing-commas / want-trailing-commas | -atc / -dtc / -wtc | bool/bool/string | off/off/– | wtc: `0,1,*,m,b,h,i` | `-atc -wtc=m` | Trailing comma policy in lists | Maybe |
| closing-side-comments | -csc | bool | off | | `-csc` (+ `-csci=N`, `-cscp=s`) | Add `## end sub foo` comments on long blocks | Maybe |
| weld-nested-containers | -wn | bool | off | | `-wn` | `({` … `})` welded on one line | Maybe |
| ignore-old-breakpoints | -iob | bool | off | | `-iob` | Do not honour existing line breaks | Maybe |
| freeze-newlines / freeze-whitespace | -fnl / -fws | bool | off | | `-fnl` | Leave line breaks / whitespace untouched | No |
| output-line-ending | -ole | enum | (LF on stdout) | dos, win, mac, unix | `-ole=win` | Line endings | Yes (or derive from editor) |
| iterations / converge | -it / -conv | int | 1 | 0.. | `-conv` | Re-run until stable (slower) | No |
| character-encoding | -enc | string | guess | none, utf8, guess | `-enc=utf8` | stdin decoding; recommend fixed `utf8` | fixed |
| valign-code / valign-side-comments / valign-block-comments | -vc / -vsc / -vbc | bool | on | | `-nvc` | Vertical alignment of `=`/`=>`/comments | Maybe |

Full list with types: section `--dump-long-names` of the raw file; defaults: `--dump-defaults`; int ranges: `--dump-integer-option-range`. A generic "extra arguments" text box covers the long tail since every option is a CLI flag.

## Verified notes (input `s.pl`: sub with if/else, C-style for, hash, long expression)
- `-i=2 -ce -bl` → 2-space indent, `sub foo` + newline + `{`.
- `-pt=2 -bt=2 -sbt=2 -nsfs` → `my ($a, $b) = @_;`, `[1, 2, 3]`, `{x => 1}`, `for (my $i = 0; $i < 10; $i++)`.
- `-l=50 "-wba=+ - / *"` → breaks after each arithmetic operator; `-pbp` → breaks before (`= $some_value` / `+ $another…`), ci=4.
- `-gnu` → brace on new line, tight parens.
- `-t -nola` and `-et=4` → leading `\t`; `-t` alone → ignored with Conflict message.
- `-ole=win` → CRLF. `-vt=2 -vtc=2` → `b => 2, );`. `-blbs=0 -kbl=0` removed blank lines before subs. `-ndsm` kept stray `;`.
- Unbalanced `{`: exit 2, stdout = original text, stderr diagnostic with line/caret.
