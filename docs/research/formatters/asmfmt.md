# asmfmt (Go assembly)

Legend: **VERIFIED** = ran against `Binaries/asmfmt.exe`; **DOCS** = https://github.com/klauspost/asmfmt

## Bundled version
No version flag exists (`-version` → `flag provided but not defined`, exit 2). From embedded Go build info (`go version -m asmfmt.exe`, VERIFIED): module `github.com/klauspost/asmfmt v1.3.2`, built with go1.25.5 (locally built, not an upstream release artifact).

## Full help (VERIFIED; note `--help` exits **2**, text on stderr)
```
usage: asmfmt [flags] [path ...]
  -cpuprofile string   write cpu profile to this file
  -d   display diffs instead of rewriting files
  -e   report all errors (not just the first 10 on different lines)
  -l   list files whose formatting differs from asmfmt's
  -w   write result to (source) file instead of stdout
```

## Recommended invocation
```
[]            // stdin → stdout, exactly as today
```
No config mechanism: **zero formatting options** — no flags, no config file, no env vars, no discovery (VERIFIED from help; DOCS README describes a fixed gofmt-like style). Nothing to expose in the UI.

## stdout / stderr / exit code (VERIFIED)
| Case | exit | stdout | stderr |
|---|---|---|---|
| OK | 0 | formatted (LF; CRLF input → LF) | empty |
| NUL byte in input | 2 | empty | `zero (0) byte in input. file is unlikely an assembler file` |
| Unknown flag / `--help` | 2 | empty | message + usage |
| Garbage / unbalanced text | **0** | best-effort output | empty — asmfmt is line-based and has no real parser, so it almost never rejects input |

## Isolation / discovery gotchas
None known. There is no config file or env var in help or README (DOCS), so output should be independent of cwd (not separately traced).

## Options reference
| Key | Type | Default | Values | CLI form | Meaning | Headline? |
|---|---|---|---|---|---|---|
| — | — | — | — | — | No formatting options exist | — |

Fixed behaviour (VERIFIED on sample): tab indent for instructions, labels/`TEXT`/`#include` at column 0, `, ` after operand commas (`(SB),NOSPLIT,$0` → `(SB), NOSPLIT, $0`), single space before trailing `//` comments (DOCS: comments in a block are aligned), trailing `;` removed, blank line inserted before labels, Plan 9 (Go) assembler syntax only — not NASM/GAS/MASM, which matters because the extension labels this language generically as "assembly" (`["assembly"]` in ConfigManager.cs).

## Mismatches vs current `FormatterSettings.cs`
None (no settings defined, none possible).
