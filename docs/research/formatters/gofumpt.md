# gofumpt (Go)

Legend: **VERIFIED** = ran against `Binaries/gofumpt.exe` on this machine; **DOCS** = from upstream docs (URL cited).

## Bundled version
`gofumpt.exe -version` → `v0.9.2 (go1.25.3)` (VERIFIED). Fork of gofmt as of Go 1.25.0 (DOCS: https://github.com/mvdan/gofumpt/blob/v0.9.2/README.md).

## Full `--help` (VERIFIED, exit 0)
```
usage: gofumpt [flags] [path ...]
	-version  show version and exit

	-d        display diffs instead of rewriting files
	-e        report all errors (not just the first 10 on different lines)
	-l        list files whose formatting differs from gofumpt's
	-w        write result to (source) file instead of stdout
	-extra    enable extra rules which should be vetted by a human

	-lang       str    target Go version in the form "go1.X" (default from go.mod)
	-modpath    str    Go module path containing the source file (default from go.mod)
```

## Recommended invocation
stdin → stdout, no config file exists for gofumpt. All options are CLI flags.
```
["-lang", "<go1.N>", "-modpath", "<path or _>", ("-extra")?]
```
To be deterministic ALWAYS pass both `-lang` and `-modpath` (see Isolation). Suggested neutral values: `-lang go1.25` (or UI-selected), `-modpath _` (a string that matches no import, so "no module").

## stdout / stderr / exit code (VERIFIED)
| Case | exit | stdout | stderr |
|---|---|---|---|
| OK (changed or not) | 0 | formatted code (LF; CRLF input is converted to LF) | empty |
| Go syntax error | 2 | empty | `<standard input>:1:20: expected 'IDENT', found '{'` |
| Unknown flag (`-bogus`) | 2 | empty | `flag provided but not defined: -bogus` + usage |
| Invalid `-lang` value (`bogus`, `1.20`) | 2 | empty | **Go panic + stack trace** (`panic: invalid Go version: "bogus"`) — validate in the UI |
| `-s` | 0 | formatted | `warning: -s is deprecated as it is always enabled` |
| `-r ...` | 2 | empty | `the rewrite flag is no longer available; use "gofmt -r" instead` |

## Isolation / discovery gotchas (VERIFIED)
- With stdin, gofumpt looks for `go.mod` in the **process working directory and its parents** and takes defaults for `-lang` (from the `go` directive) and `-modpath` (from `module`). The extension passes `workingDirectory = null` for gofumpt, so the result depends on DevToys' cwd.
  - cwd with `go.mod` `go 1.22`: `0777` → `0o777`; no go.mod: `0777` stays (lang falls back to `go1`, i.e. the oldest).
  - cwd with `module mymod`: import `"mymod/pkg"` is split into its own group after std imports; without go.mod it is sorted among std imports.
- Passing `-lang X` alone does NOT stop `-modpath` discovery (and vice versa). Pass both. `-lang=` (empty) = "not set".
- A malformed `go.mod` in cwd is silently ignored (exit 0).
- No env vars, no config file (DOCS README).

## Options reference
| Key | Type | Tool default | Values | CLI form | Meaning | Headline? |
|---|---|---|---|---|---|---|
| extra | bool | false | flag present/absent | `-extra` | Extra rules "which should be vetted by a human": group adjacent same-type params (`a int, b int` → `a, b int`); replace naked returns with explicit (`return` → `return n`) | Yes |
| lang | string | from go.mod; else `go1` | `go1`, `go1.N`, `go1.N.P` (must start with `go`; `1.20` panics; `go1.99` accepted) | `-lang go1.22` | Target Go version. Only version-gated rule: octal literals `0777` → `0o777` when ≥ go1.13 (DOCS README) | Yes (choice/ text) |
| modpath | string | from go.mod; else empty | any module path | `-modpath example.com/m` | Module path; imports with this prefix are treated as non-std and grouped separately from std imports | Minor |
| (simplify `-s`) | — | always on | — | hidden/deprecated | gofmt -s simplifications are always applied (`[]T{T{1}}` → `[]T{{1}}`, `s[1:len(s)]` → `s[1:]`) | No — not switchable |

Non-formatting flags (irrelevant for stdin use): `-d`, `-e` (report all errors, not just first 10 — could be useful for error display), `-l`, `-w`, `-version`.

**Plain gofmt behaviour:** NOT available from this binary. There is no flag to turn the gofumpt rules off; `-s` is forced on; `-r` is removed (VERIFIED + DOCS). Offering "gofmt" would require bundling gofmt.

## Mismatches vs current `FormatterSettings.cs`
- `extra` (bool, default false) — matches the tool.
- Missing: `lang`, `modpath`. More important than the missing UI: they are not pinned, so output depends on cwd's go.mod.

## Verified notes
- `-extra`: `func f(a int, b int, s []int)` → `func f(a, b int, s []int)`; naked `return` → `return n`.
- `-lang go1.12` keeps `0777`; `-lang go1.13` → `0o777`.
- `-modpath mymod` separates `"mymod/pkg"` into a second import group.
- `-lang go1.22 -modpath _` inside a module dir gives the same output as outside any module.
- Startup is instantaneous (native Go binary).
