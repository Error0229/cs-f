# ormolu (Haskell) — option research

Tags: **VERIFIED** = ran against `Binaries/ormolu.exe` on this machine; **DOCS** = from upstream docs (URL given).

## Bundled version & what the exe is
- `ormolu 0.8.0.2` (commit 3331e79), `ghc-lib-parser 9.12.1.20250105`. VERIFIED (`--version`).
- Native statically linked GHC binary, 109 MB. No runtime extraction, no temp files.

## Startup time
- ~0.2–0.5 s per invocation for a small module (bash `time`, warm disk: 0.20 s; first run 0.5 s). `--unsafe` made no measurable difference on small input. VERIFIED. Fine for live formatting with debounce.

## Recommended invocation
```
["--no-cabal", "--no-dot-ormolu", "--color", "never"]            // + user options, stdin -> stdout
  + for each extension E:     ["-o", "-X" + E]
  + for each fixity F:        ["-f", "infixl 6 +++"]
  + optional:                 ["-t", "module|sig|auto"]
```
- With `--no-cabal`, `--stdin-input-file` is NOT needed (VERIFIED: exit 0). Without both, ormolu refuses: `The --stdin-input-file option is necessary when using input from stdin and accounting for .cabal files`, exit 9. VERIFIED.
- Current invocation (`--stdin-input-file stdin.hs`) is non-isolated: see gotchas.

## stdout / stderr / exit codes (VERIFIED)
| Case | stdout | stderr | exit |
|---|---|---|---|
| OK | formatted source (always LF, even for CRLF input) | empty | 0 |
| Bad CLI option / bad `-f` syntax | – | message + usage | 1 |
| Parse error | empty | `<stdin>:2:5\n  The GHC parser (in Haddock mode) failed:\n  [GHC-58481] parse error on input ...` | 3 |
| Unknown `-o` GHC option | empty | `The following GHC options were not recognized:\n  -XNope` | 7 |
| stdin without `--stdin-input-file`/`--no-cabal` | empty | message | 9 |
Other documented codes (DOCS, from memory of https://github.com/tweag/ormolu#exit-codes — re-check before relying on exact numbers): 2 CPP unsupported, 4 formatted output fails to parse, 5 AST differs, 6 non-idempotent (`-c`), 8 cabal parse error, 10 missing field, 100 check-mode diff, 101 bad region, 102 `.ormolu` parse error. Treat any non-zero as failure and show stderr.
- `--color always` emits ANSI escapes in stderr; `auto` is safe when piped, `never` is safest. VERIFIED.

## Isolation / discovery gotchas (VERIFIED)
- With `--stdin-input-file stdin.hs` (relative), ormolu walks up **from the process cwd** looking for a `*.cabal` file and a `.ormolu` file.
  - `.cabal` found but not mentioning the file: only a stderr note (`Found .cabal file p.cabal, but it did not mention stdin.hs`), no extensions applied. If the pseudo-path matches a component's `hs-source-dirs`, `default-extensions`/`default-language`/dependencies are applied (`cfgDynOptions = [-XHaskell2010, -XBlockArguments, -XLambdaCase]` seen with `--stdin-input-file src/X.hs`).
  - `.ormolu` in cwd (or ancestors) IS applied even for stdin: fixity overrides changed operator layout.
- Switch off: `--no-cabal` and `--no-dot-ormolu`. Both are independent flags; pass both. No home-dir or env-var config exists.

## Options reference
ormolu has **no style options** (no indent width, no line length; by design). Everything below affects parsing or operator layout only.

| Key | Type | Default | Values | CLI form | Meaning | Headline? |
|---|---|---|---|---|---|---|
| ghc-opt | string list | none | any GHC dynamic flag, in practice `-X<Ext>` | `-o -XArrows` (repeat) | Enable language extensions needed to *parse*. Most are on by default; those that are not: see `--manual-exts` list below | **Yes** (multi-select of manual exts) |
| fixity | string list | built-in Hackage fixity DB (`base` only w/o cabal) | `infixl\|infixr\|infix N op[, op]` | `-f "infixl 6 +++"` (repeat) | Operator fixity override; changes multi-line operator chain indentation | Maybe (advanced text list) |
| reexport | string list | built-in (lens, optics, servant, hspec) | `module A exports B` / `module A exports "pkg" B` | `-r "module Foo exports Bar"` | Fixity import via re-exports | No |
| package | string list | `base` | package names | `-p lens` | Packages whose operator fixities are assumed | Maybe |
| source-type | enum | auto | module, sig, auto | `-t sig` | Parse as `.hs` module or Backpack `.hsig`; auto = by extension (stdin → module) | No |
| unsafe | bool | false | flag | `-u` | Skip AST-equality self-check (faster on large files; no gain on small) | No |
| check-idempotence | bool | false | flag | `-c` | Fail (exit 5) if format(format(x)) != format(x) | No |
| start-line / end-line | int | whole file | 1-based inclusive | `--start-line 5 --end-line 6` | Format only a region. NOTE: with region set, lines outside are passed through verbatim (VERIFIED: whole unformatted header preserved) | No (could power "format selection") |
| no-cabal | bool | false | flag | `--no-cabal` | Do not look for .cabal | always pass |
| no-dot-ormolu | bool | false | flag | `--no-dot-ormolu` | Do not look for `.ormolu` | always pass |
| stdin-input-file | path | – | path | `--stdin-input-file P` | Pseudo-path used for cabal/.ormolu lookup and source-type auto | omit when isolated |
| color | enum | auto | never, always, auto | `--color never` | Colour of diagnostics | fixed `never` |
| mode | enum | stdout | stdout, inplace, check | `-m check` / `-i` | Not relevant for stdin | No |
| debug | bool | false | flag | `-d` | Dumps config + fixity analysis to stderr | No |

`--respect-cpp` does not exist at 0.8.0.2 (not in `--help`); CPP is simply unsupported beyond trivial cases (DOCS: README "Limitations").

`--manual-exts` output (extensions NOT enabled by default; VERIFIED): AlternativeLayoutRule, AlternativeLayoutRuleTransitional, Arrows, BangPatterns, Cpp, ExtendedLiterals, ImportQualifiedPost, LexicalNegation, LinearTypes, MagicHash, MonadComprehensions, MultilineStrings, NegativeLiterals, OverloadedLabels, OverloadedRecordDot, OverloadedRecordUpdate, PatternSynonyms, RecursiveDo, StaticPointers, TemplateHaskell, TemplateHaskellQuotes, TransformListComp, UnboxedSums, UnboxedTuples, UnicodeSyntax. (`{-# LANGUAGE ... #-}` pragmas in the source also work.)

## What to expose
1. "Language extensions" multi-select (the 25 manual exts) → `-o -X<Ext>`. This is the only option ordinary users hit (e.g. `proc` notation fails to parse without Arrows).
2. Optionally an advanced "fixity overrides" text list → `-f`.
3. Nothing else. Users wanting indent width / line length need **Fourmolu** (configurable fork: indentation, column-limit, comma-style, etc. via `fourmolu.yaml`/CLI) — NOT bundled; note only.

## Verified notes
- `-f 'infixl 6 +++' -f 'infixl 7 ***'` changed `a +++ b *** c +++ d *** e` from one-operator-per-line to `+++ b *** c` grouping; swapping precedences changed it again.
- `-o -XArrows`: `proc y -> do` fails with exit 3 without it, formats with it.
- `.ormolu` in cwd with `infixl 1 +++ / infixl 2 ***` changed output; `--no-dot-ormolu` restored default.
- CRLF input → LF output. There is no line-ending option; the extension must re-apply CRLF if wanted.
- Docs: https://github.com/tweag/ormolu (README), https://github.com/fourmolu/fourmolu.
