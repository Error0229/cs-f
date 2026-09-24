# google-java-format — options research

Legend: **VERIFIED** = ran against the bundled exe on this machine (2026-09-19). **DOCS** = taken from upstream docs/source.

## Bundled version

`Binaries\google-java-format.exe --version` → `google-java-format: Version 1.33.0` (GraalVM native image, no JVM needed). VERIFIED.
Startup: ~150 ms for a trivial class. VERIFIED.

## Recommended invocation & config-passing mechanism

Flags only. There is **no config file** and no config discovery of any kind. Flags may come before or after `-` (VERIFIED: `- --aosp` works the same as `--aosp -`).

```
args = [ <zero or more option flags>, "-" ]
```

Examples:

```
["-"]                                              # Google style (2-space)
["--aosp", "-"]                                    # AOSP style (4-space)
["--aosp", "--skip-reflowing-long-strings", "--skip-javadoc-formatting", "-"]
["--fix-imports-only", "-"]
```

Optional: `--assume-filename Snippet.java` only changes the file name printed in diagnostics (`<stdin>` by default). VERIFIED.

`@<filename>` (read flags from a file) exists but is unnecessary for the extension.

## stdout / stderr / exit-code contract (VERIFIED)

| Situation | exit | stdout | stderr |
|---|---|---|---|
| Success | 0 | formatted source only | empty |
| Java syntax error | 1 | empty | `<stdin>:1:6: error: <identifier> expected` + source line + caret |
| Unknown flag (`--bogus`) | 2 | empty | `unexpected flag: --bogus` followed by the full usage text |

- stdout is never polluted. "Non-empty stdout == success" is safe here, but exit code is also reliable.
- Line endings are preserved: CRLF input → CRLF output (VERIFIED with `class A {}\r\n`).

## Isolation / discovery gotchas

None. No config files, no cwd dependence, no cache, no temp files. VERIFIED (nothing read from cwd).

## Options reference

Column limit (100), indent widths, brace style, import ordering scheme etc. are **not configurable** — by design (DOCS: https://github.com/google/google-java-format#readme, "the formatting algorithm is deliberately not configurable").

| Key | Type | Default | Values | CLI form | Meaning | Headline? |
|---|---|---|---|---|---|---|
| aosp | bool | false | — | `--aosp` (aliases `-aosp`, `-a`) | AOSP style: 4-space block indent, 8-space continuation (Google style: 2 / 4). Indent change VERIFIED. Also switches import ordering to the AOSP scheme (DOCS: `ImportOrderer` in upstream source; not verified here). | **Yes** |
| skipSortingImports | bool | false | — | `--skip-sorting-imports` | Do not reorder imports. Unused imports are still removed. | Yes |
| skipRemovingUnusedImports | bool | false | — | `--skip-removing-unused-imports` | Keep unused imports. Imports are still sorted. **Recommended ON for a snippet formatter**: a pasted fragment often has imports used by code that was not pasted. | **Yes** |
| skipReflowingLongStrings | bool | false | — | `--skip-reflowing-long-strings` | Do not split/re-join string literals that exceed column 100. | Yes |
| skipJavadocFormatting | bool | false | — | `--skip-javadoc-formatting` | Leave Javadoc comments untouched. | Yes |
| fixImportsOnly | bool | false | — | `--fix-imports-only` | Only sort imports + remove unused; no other formatting. Mode switch rather than style option. | Maybe (as a "mode") |
| assumeFilename | string | `<stdin>` | any | `--assume-filename <name>` | Name used in error diagnostics only. No effect on output. | No |
| lines | range list | all | `N:M`, 1-based, repeatable | `--lines 1:5` (aliases `-lines`, `--line`, `-line`) | Partial formatting: only reformat given line ranges. Works with `-`. Irrelevant for the extension (always whole document). | No |
| offset / length | int pairs | all | 0-based char offset + length; repeatable, must be paired | `--offset N --length M` | Partial formatting by character range. Irrelevant. | No |
| replace | bool | false | — | `-i` / `--replace` | In-place file write. With `-` result still goes to stdout. Irrelevant. | No |
| dryRun | bool | false | — | `--dry-run` / `-n` | List files that would change. Irrelevant (breaks stdout contract). **Do not expose.** | No |
| setExitIfChanged | bool | false | — | `--set-exit-if-changed` | Exit 1 if changes were made. **Do not expose** — would make successful formatting look like failure. | No |

Option count that affects output: **6** (5 style toggles + 1 mode); headline: `--aosp`, `--skip-removing-unused-imports`.

## Verified notes (input: class with unsorted + unused imports, messy Javadoc, a >100-col string literal)

1. `-` → 2-space indent, imports sorted, `java.io.File` (unused) removed, Javadoc reflowed (`Foo   bar.` → `Foo bar.`, blank line before `@param`), long string split into `"..." + " ..."`.
2. `--aosp -` → same but 4-space indent / 8-space continuation.
3. `--skip-sorting-imports -` → `List` stays before `ArrayList`; unused `File` still removed.
4. `--skip-removing-unused-imports -` → `import java.io.File;` kept (and sorted first).
5. `--skip-reflowing-long-strings -` → string literal kept on one 140-col line.
6. `--skip-javadoc-formatting -` → Javadoc left byte-identical.
7. `--fix-imports-only -` → only import block changed; body untouched (still unformatted).
8. `--lines 1:3 -` → works with stdin; imports fixed and only partially reformatted body (confirms irrelevance).
9. `--bogus -` → exit 2, stderr `unexpected flag: --bogus` + usage; stdout empty.
10. `echo "class {" | google-java-format -` → exit 1, stderr diagnostics, stdout empty.

All flags confirmed working with `-` stdin.
