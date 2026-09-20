# ruff (Python) — formatter options research

Legend: **VERIFIED** = executed against the bundled binary on Windows 11 (2026-09-19). **DOCS** = taken from documentation (URL cited). Option docs below marked "EMBEDDED" come from `ruff.exe config <key>` — i.e. the documentation compiled into this exact binary, so they are version-accurate.

## 1. Bundled version

`C:\Users\login\cs-f\Binaries\ruff.exe --version` -> `ruff 0.14.6` (VERIFIED). 39.8 MB, file date 2025-11-21.

## 2. Recommended invocation and config-passing mechanism

Mechanism: **`--isolated` + one `--config "<toml key> = <toml value>"` pair per option**. No files needed. Works with stdin->stdout (VERIFIED).

```text
ruff.exe format --isolated --stdin-filename <name>.py [--config KEY=VALUE]... -
```

Exact `ProcessStartInfo.ArgumentList` example (each line = one array element):

```text
format
--isolated
--stdin-filename
snippet.py
--config
line-length=100
--config
indent-width=2
--config
format.quote-style="single"
--config
format.indent-style="tab"
--config
format.skip-magic-trailing-comma=true
--config
format.line-ending="lf"
--config
format.docstring-code-format=true
--config
format.docstring-code-line-length=60
--config
target-version="py312"
--config
format.preview=true
-
```

Rules (all VERIFIED):

- The `--config` value must be valid TOML. **String/enum values need inner double quotes**: `format.quote-style="single"` works; `format.quote-style=single` fails with exit 2 (`string values must be quoted, expected literal string`). Bools and ints are bare (`true`, `100`). The existing `BuildRuffArgs` already does this correctly (`string s => "\"s\""`).
- `--config` can be repeated any number of times. An inline TOML table also works: `--config 'format = {quote-style = "single", skip-magic-trailing-comma = true}'` — but one pair per flag is simpler and gives per-option error messages.
- A uniform wrapper can use `--config` for *everything*; the dedicated flags are just sugar:
  - `--line-length N` == `--config line-length=N`
  - `--target-version py312` == `--config 'target-version="py312"'` (both verified to change output identically)
  - `--preview` == `--config format.preview=true` (or top-level `preview=true`)
  - There is NO dedicated flag for indent-width, quote-style, indent-style, skip-magic-trailing-comma, line-ending, docstring-code-*.
- The trailing `-` is optional when stdin is piped (`ruff format --isolated --stdin-filename x.py < file` works), but keep it.
- `--stdin-filename` picks the source type by extension: `.py` (default), `.pyi` (stub style — verified: `def f(a:int)->int:\n    ...` collapses to `def f(a: int) -> int: ...`), `.ipynb` (expects notebook JSON). **It also drives config discovery** (see section 3).
- Errors go to stderr, stdout is empty on failure (VERIFIED: 0 bytes on syntax error).

## 3. Isolation / discovery gotchas

| Finding | Status |
|---|---|
| Without `--isolated`, `ruff format -` discovers `ruff.toml` / `.ruff.toml` / `pyproject.toml` ([tool.ruff]) in the **process cwd or any parent**. Test: `ruff.toml` with `line-length = 20`, `quote-style = "single"` two directories above cwd changed stdin output. | VERIFIED |
| `pyproject.toml` with `[tool.ruff] indent-width = 2` in a parent of cwd changed stdin output. | VERIFIED |
| With `--stdin-filename some/dir/x.py`, discovery starts from **that file's directory** instead of cwd (config in `disc/` applied while cwd was elsewhere). So never pass a real user path unless you want project config. Use a bare name like `snippet.py`. | VERIFIED |
| `--isolated` disables all of the above; output returned to defaults. | VERIFIED |
| User-level config `%APPDATA%\ruff\ruff.toml` is also used as fallback when no project config found; `--isolated` ignores it too. | DOCS https://docs.astral.sh/ruff/configuration/#config-file-discovery |
| Without isolation, `target-version` may be inferred from a discovered `pyproject.toml` `requires-python`. | DOCS (embedded `ruff config target-version`) |
| `--config` overrides always beat any config file, so explicitly-set options are safe either way; only *unset* options leak from discovered files. | VERIFIED + `--help` text |
| No `.ruff_cache` directory is created when formatting stdin. | VERIFIED |

**Recommendation: always pass `--isolated`.** The extension currently does not (`Args = ["format", "-"]`), so results today depend on DevToys' working directory. If a future "respect project config" feature is wanted, make it an explicit toggle that drops `--isolated` and sets `--stdin-filename` to the real path.

## 4. Full options reference — every setting that affects `ruff format` at 0.14.6

The `[format]` table has exactly 8 keys at this version (VERIFIED: `ruff config format` lists `exclude, preview, indent-style, quote-style, skip-magic-trailing-comma, line-ending, docstring-code-format, docstring-code-line-length`; the unknown-field error message lists the same 8). Plus 4 top-level keys that the formatter reads. Types/defaults are EMBEDDED (`ruff config <key>`).

| Key | Type | Default | Values | CLI form | Meaning | Headline? |
|---|---|---|---|---|---|---|
| `line-length` | int | `88` | 1..320 | `--config line-length=N` or `--line-length N` | Preferred wrap width (soft limit; width counted in unicode width). | YES |
| `indent-width` | int (nonzero u8) | `4` | 1..255 | `--config indent-width=N` | Spaces per indent level; also the visual width of a tab for line-length computation. | YES |
| `format.indent-style` | enum | `"space"` | `"space"`, `"tab"` | `--config format.indent-style="tab"` | Indent with spaces or tabs. | YES |
| `format.quote-style` | enum | `"double"` | `"double"`, `"single"`, `"preserve"` | `--config format.quote-style="single"` | Preferred string quote. Docstrings/triple-quoted always double. Falls back to the other quote to avoid escapes. | YES |
| `format.skip-magic-trailing-comma` | bool | `false` | `true`/`false` | `--config format.skip-magic-trailing-comma=true` | If true, an existing trailing comma no longer forces one-item-per-line. | YES |
| `format.line-ending` | enum | `"auto"` | `"auto"`, `"lf"`, `"cr-lf"`, `"native"` | `--config format.line-ending="lf"` | Output newline. `auto` = detect from input (first line ending wins; `\n` if none). `native` = `\r\n` on Windows. | YES |
| `format.docstring-code-format` | bool | `false` | `true`/`false` | `--config format.docstring-code-format=true` | Reformat code examples inside docstrings (doctest, Markdown fences, reST literal/code blocks). | YES |
| `format.docstring-code-line-length` | int or enum | `"dynamic"` | `"dynamic"` or int 1..320 | `--config format.docstring-code-line-length=60` / `="dynamic"` | Line length for docstring code; only effective when `docstring-code-format=true`. `dynamic` = global line-length minus docstring indent. | yes (dependent) |
| `format.preview` | bool | `false` | `true`/`false` | `--config format.preview=true` or `--preview` / `--no-preview` | Enable unstable preview formatting style. | optional |
| `preview` (top-level) | bool | `false` | `true`/`false` | `--config preview=true` | Global preview (linter + formatter). Prefer `format.preview`. | no |
| `target-version` | enum | `"py310"` (EMBEDDED default when nothing to infer from) | `"py37"`, `"py38"`, `"py39"`, `"py310"`, `"py311"`, `"py312"`, `"py313"`, `"py314"` | `--config target-version="py312"` or `--target-version py312` | Minimum Python version. Affects formatting of version-gated syntax, e.g. parenthesized `with` items are only emitted for >= py39. Also affects which syntax is accepted without error. | optional |
| `format.exclude` | list[str] | `[]` | globs | n/a | File exclusion; irrelevant for stdin. | no |

Not format options but relevant CLI flags (from `ruff format --help`, VERIFIED present): `--stdin-filename`, `--range <start>-<end>` (format a line/col range; single file only), `--check`, `--diff`, `--exit-non-zero-on-format`, `--extension ext:lang` (`python|ipynb|pyi`), `-q/--quiet`, `-s/--silent`, `--no-cache`, `--cache-dir`. Nothing else influences formatting style: ruff's formatter is intentionally near-zero-config (Black-compatible) — **12 keys is the whole surface, 7-8 worth exposing.**

Suggested UI set (8): line-length, indent-width, indent-style, quote-style, skip-magic-trailing-comma, line-ending, docstring-code-format, docstring-code-line-length; optional advanced: target-version, preview.

Note for the existing code: `BuildRuffArgs` prefixes every non-`line-length` key with `format.`. That is wrong for `indent-width` and `target-version` (top-level). `--config format.indent-width=2` would fail with exit 2 (`unknown field`). The wrapper table should carry the full dotted key.

## 5. Verified notes

Input used (`in.py`):

```python
def f(a,b):
    """Doc.

    >>> x = {  'a':1 }
    """
    x = {'a':1,'b':[1,2,],}
    if a:
        return "it's" + 'q'
    return  x
```

| # | Command (after `ruff.exe format --isolated`) | Observed effect |
|---|---|---|
| 1 | `--line-length=20 -` | `return "it's" + "q"` became a parenthesized 3-line split. |
| 2 | `--config line-length=20 --config indent-width=2 -` | 2-space indentation everywhere; wrapping at 20. |
| 3 | `--config format.quote-style="single" --config format.indent-style="tab" -` | `'a': 1`, `'q'`; `"it's"` kept double (escape avoidance); docstring kept `"""`; tab indents. |
| 4 | `--config format.skip-magic-trailing-comma=true --config format.docstring-code-format=true -` | dict collapsed to `x = {"a": 1, "b": [1, 2]}`; doctest line became `>>> x = {"a": 1}`. |
| 5 | `--config format.docstring-code-format=true --config format.docstring-code-line-length=20 -` | Doctest call exploded across `...` continuation lines. |
| 6 | `--config format.line-ending="lf"` / `"cr-lf"` / `"native"` / `"auto"` on LF input | `od -c`: lf->`\n`, cr-lf->`\r\n`, native->`\r\n` (Windows), auto->`\n`. CRLF input + auto -> `\r\n`. |
| 7 | `--target-version py38` vs `py39` on a long two-item `with` | py38: `with open(...) as a, open(\n ...\n) as b:`; py39: parenthesized `with (\n open(...) as a,\n open(...) as b,\n):`. `--config target-version="py38"` identical to the flag. |
| 8 | `--preview` and `--config format.preview=true` | Both accepted (exit 0); no difference on the small sample. |

Error reporting (VERIFIED) — all failures: **exit code 2, message on stderr, stdout empty**:

| Case | stderr (abridged) |
|---|---|
| Unknown key `--config format.bogus=1` | `error: invalid value 'format.bogus=1' for '--config <CONFIG_OPTION>'` ... `unknown field 'bogus', expected one of 'exclude', 'preview', 'indent-style', ...` |
| Bad enum `format.quote-style="triple"` | `unknown variant 'triple', expected one of 'single', 'double', 'preserve'` |
| Unquoted string `format.quote-style=single` | `The supplied argument is not valid TOML ... string values must be quoted` |
| Out of range `--line-length=500` | `The line width must be a value between 1 and 320.` |
| `indent-width=0` | `invalid value: integer '0', expected a nonzero u8` |
| Python syntax error in input | `error: Failed to parse at 1:5: Expected an identifier` (exit 2) |

Exit codes: 0 = formatted OK (also when unchanged); 2 = any error (bad args, bad config, unparsable input). (1 is only used with `--check`/`--diff`/`--exit-non-zero-on-format`.)

Docs: https://docs.astral.sh/ruff/settings/#format , https://docs.astral.sh/ruff/formatter/ , https://docs.astral.sh/ruff/configuration/
