# ktlint — options research

Legend: **VERIFIED** = ran against the bundled exe on this machine (2026-09-19, OpenJDK 25 first on PATH). **DOCS** = upstream docs for tag 1.8.0:
- https://github.com/pinterest/ktlint/blob/1.8.0/documentation/release-latest/docs/rules/configuration-ktlint.md
- https://github.com/pinterest/ktlint/blob/1.8.0/documentation/release-latest/docs/rules/standard.md
(The `pinterest.github.io/ktlint/<version>/...` URLs returned 404 during research; the repo copies above are the same content.)

## Bundled version

`ktlint.exe --version` → `ktlint version 1.8.0`. VERIFIED.

`ktlint.exe` is a **launcher**, not a native binary: it extracts `ktlint.jar` (71 MB) to `%TEMP%\ktlint-launcher\ktlint.jar` and runs `java` **from PATH** with `--add-opens=...`. VERIFIED.
- Needs Java 11+. With Java 8 first on PATH: exit 1, stdout empty, stderr `Unrecognized option: --add-opens=java.base/java.lang=ALL-UNNAMED / Error: Could not create the Java Virtual Machine.` VERIFIED.
- The launcher **ignores `JAVA_HOME`** (VERIFIED: JAVA_HOME pointed at JDK 25, PATH had Java 8 → same failure). The extension must prepend `<jdk>\bin` to the child process PATH itself (e.g. derive from `JAVA_HOME` if set).
- Startup: 3.5–4.1 s per invocation warm (3 runs: 3513 / 3946 / 4077 ms), ~5.3 s first run. VERIFIED. UI must treat this as slow/async.
- JDK 25 prints 4 `WARNING:` lines (sun.misc.Unsafe / native access) to **stderr** on every run. Harmless; do not treat non-empty stderr as failure.

## Recommended invocation & config-passing mechanism

ktlint has no style flags. Everything is `.editorconfig`. `--code-style=` still appears in `--help` as "(deprecated)" but is **rejected**: exit 1, stderr `Parameter '--code-style' is no longer valid. The code style should be defined as '.editorconfig' property 'ktlint_code_style='`. VERIFIED.

Mechanism: write a `.editorconfig` with `root = true` into a private directory and point the *virtual path* of stdin into that directory with `--stdin-path`. Do **not** rely on `--editorconfig=`: it only supplies *defaults* and is overridden per property by any `.editorconfig` discovered from the working directory upward (VERIFIED, see gotchas).

```
privDir = %TEMP%\<ext>\ktlint\<settings-hash>\        # contains .editorconfig
args = [
  "--stdin",
  "--format",
  "--log-level=none",
  "--ignore-autocorrect-failures",
  "--stdin-path=" + privDir + "\\Snippet.kt"          # use Snippet.kts for Kotlin script
]
cwd  = privDir                                         # belt and braces
env  = PATH = <java11+>\bin;%PATH%
```

`Snippet.kt` need not exist; with `--format` + `--stdin-path` nothing is written to disk (VERIFIED: directory still only contains `.editorconfig`). Both `\` and `/` separators work. VERIFIED.

Minimal generated `.editorconfig`:

```ini
root = true

[*.{kt,kts}]
ktlint_code_style = ktlint_official
indent_style = space
indent_size = 4
max_line_length = 140
ij_kotlin_allow_trailing_comma = true
ij_kotlin_allow_trailing_comma_on_call_site = true
# required when --stdin-path is used, otherwise standard:filename fires (see gotchas)
ktlint_standard_filename = disabled
# optional: user-disabled rules
# ktlint_standard_no-wildcard-imports = disabled
# ktlint_experimental = enabled
```

Note: no spaces inside the glob braces (`[*.{kt,kts}]`, not `{kt, kts}`) or the section is silently ignored (DOCS).

## stdout / stderr / exit-code contract (VERIFIED)

| Situation | exit | stdout | stderr |
|---|---|---|---|
| Formatted, no remaining violations | 0 | formatted code | (JDK warnings only) |
| Formatted, **unfixable violations remain** (e.g. wildcard import, max-line-length) | **1** | formatted code (complete, usable) | `<stdin>:2:1: Wildcard import (cannot be auto-corrected) (standard:no-wildcard-imports)` lines (each printed twice) + `Summary error count ...` |
| Same, with `--ignore-autocorrect-failures` | 0 | formatted code | clean |
| **Kotlin syntax error** | **0** | **EMPTY** | `<stdin>:1:4: Not a valid Kotlin file (1:4 expecting function name or receiver type) ()` + summary |
| Default log level (no `--log-level`) | — | **POLLUTED**: first stdout line is `23:26:46.940 [main] INFO com.pinterest.ktlint.cli.internal.KtlintCommandLine -- Enable default patterns [**/*.kt, **/*.kts]` followed by the code | — |
| Unknown flag | 1 | empty | `Error: no such option --bogus` + usage line |
| Invalid editorconfig value (`indent_size = abc`) | 1 | empty | Java stack trace `RuntimeException: Property 'indent_size' expects an integer...` |
| `--editorconfig=` / config path that does not exist | as normal | normal (defaults used) | nothing — **silently ignored** |
| Unknown editorconfig key | as normal | normal | nothing — silently ignored |

Consequences for the extension:
- The **current invocation `--stdin --format` writes an slf4j INFO line into stdout** ahead of the code. `--log-level=none` (or `error`) removes it. VERIFIED. Lint violations always go to stderr (the `plain` reporter is redirected to stderr in stdin mode), so no `--reporter` tweak is needed.
- Exit code 1 does **not** mean formatting failed. Rule: success ⇔ stdout non-empty; failure ⇔ stdout empty (then show stderr minus `WARNING:` lines). `--ignore-autocorrect-failures` makes exit code meaningful again (0 unless crash/bad flag) — but a syntax error still gives exit 0 + empty stdout.
- Line endings: CRLF input → CRLF output (preserved). VERIFIED. `end_of_line` is listed by `generateEditorConfig` but made no difference in the test.

## Isolation / discovery gotchas (VERIFIED)

1. **ktlint auto-discovers `.editorconfig` from the working directory upward** for stdin input (virtual path defaults to `<cwd>/<stdin>`). Test: cwd `disc\sub`, `disc\.editorconfig` has `indent_size = 2` → output used 2-space indent with no flag given. Since DevToys' cwd is arbitrary, results would depend on where the host was started.
2. **`--editorconfig=<file>` loses to discovered files.** Test: cwd config `indent_size = 2`, `--editorconfig` file `indent_size = 8` → output used **2**. It is a defaults file only (matches `--help` text). It *does* work together with `--stdin --format` when nothing is discovered (VERIFIED: tabs/android_studio applied from an `--editorconfig` file in an empty cwd).
3. **`--stdin-path=<privDir>\Snippet.kt` fully isolates**: discovery starts at `privDir`; `root = true` stops the upward walk. Test: same cwd as (2), `--stdin-path` into dir with `indent_size = 8` → output used **8**.
4. **`--stdin-path` activates `standard:filename`**: `File name 'snippet.kt' should conform PascalCase (cannot be auto-corrected)` → exit 1. With a PascalCase name the rule still demands the file name match a single top-level class. Always emit `ktlint_standard_filename = disabled`. (Without `--stdin-path` the rule is not triggered.)
5. Extension of the virtual path selects script mode (`.kts`). Plain stdin without a path accepted top-level script statements in the test as well, so `.kt` is fine as default.
6. Shared temp artefact `%TEMP%\ktlint-launcher\ktlint.jar` — created by the launcher, outside the extension's control.

## Options reference (.editorconfig properties, section `[*.{kt,kts}]`)

Authoritative list = output of `ktlint generateEditorConfig --code-style=<style>` on the bundled exe (VERIFIED, 23 properties). Defaults shown as `ktlint_official / intellij_idea / android_studio`.

| Key | Type | Default (official / idea / android) | Values | Config form | Meaning (rule) | Headline? |
|---|---|---|---|---|---|---|
| ktlint_code_style | enum | ktlint_official | `ktlint_official`, `intellij_idea`, `android_studio` | `ktlint_code_style = android_studio` | Base style; changes defaults below and enables official-only rules (chain-method-continuation, if-else-bracing, blank-line-before-declaration, no-blank-line-in-list, no-consecutive-comments, no-empty-first-line-in-class-body …). VERIFIED | **Yes** |
| indent_style | enum | space | `space`, `tab` | `indent_style = tab` | Indent char (indent). VERIFIED | **Yes** |
| indent_size | int / `unset` | 4 | positive int | `indent_size = 2` | Indent width (indent + all wrapping rules). Non-int → crash exit 1. VERIFIED | **Yes** |
| max_line_length | int / `off` | 140 / off / 100 | int, `off` (`unset`) | `max_line_length = 100` | Wrap threshold for function-signature, class-signature, binary-expression-wrapping, chain-method-continuation, function-literal, argument/parameter-list-wrapping etc., plus max-line-length lint. VERIFIED (60 → binary expression re-wrapped; leftovers reported as unfixable) | **Yes** |
| ij_kotlin_allow_trailing_comma | bool | true / true / false | `true`, `false` | `ij_kotlin_allow_trailing_comma = false` | Add (true) or remove (false) trailing commas at declaration site (trailing-comma-on-declaration-site). VERIFIED | **Yes** |
| ij_kotlin_allow_trailing_comma_on_call_site | bool | true / true / false | `true`, `false` | `… = false` | Same at call site (trailing-comma-on-call-site). VERIFIED | **Yes** |
| insert_final_newline | bool | true | `true`, `false` | `insert_final_newline = true` | final-newline | Maybe |
| end_of_line | enum | lf | `lf`, `crlf`, `cr` | `end_of_line = lf` | Listed by generator; in test output EOL followed the input, not this property | No |
| ij_kotlin_imports_layout | string list | `*,java.**,javax.**,kotlin.**,^` / same / `*` | comma list of `*`, `pkg.**`, `^` (alias imports), `|` (blank line) | `ij_kotlin_imports_layout = *` | Import order (import-ordering). DOCS | Yes |
| ij_kotlin_packages_to_use_import_on_demand | string list | unset / `java.util.*,kotlinx.android.synthetic.**` / same | comma list of packages, `unset` | `… = java.util.*` | Packages where wildcard imports are allowed (no-wildcard-imports). DOCS | No |
| ij_kotlin_indent_before_arrow_on_new_line | bool | false | bool | | Indent `->` of a `when` entry when on new line (indent). DOCS | No |
| ij_kotlin_line_break_after_multiline_when_entry | bool | true | bool | | Blank line after multiline `when` entry (blank-line-between-when-conditions). DOCS | No |
| ktlint_function_signature_rule_force_multiline_when_parameter_count_greater_or_equal_than | int / `unset` | 2 / unset / unset | int ≥1, `unset` | `… = unset` | Force one-parameter-per-line signatures at N params (function-signature). VERIFIED (`unset` → `fun foo(a: Int, b: String): String {` kept on one line) | **Yes** |
| ktlint_function_signature_body_expression_wrapping | enum | multiline / default / default | `default`, `multiline`, `always` | | When to wrap expression bodies onto next line (function-signature). DOCS | Yes |
| ktlint_class_signature_rule_force_multiline_when_parameter_count_greater_or_equal_than | int / `unset` | 1 / unset / unset | int, `unset` | | Same for class primary constructors (class-signature). DOCS | Yes |
| ktlint_chain_method_rule_force_multiline_when_chain_operator_count_greater_or_equal_than | int / `unset` | 4 | int, `unset` | | Break `.a().b().c()` chains at N operators (chain-method-continuation; official style only). DOCS | Yes |
| ktlint_argument_list_wrapping_ignore_when_parameter_count_greater_or_equal_than | int / `unset` | unset / 8 / 8 | int, `unset` | | Skip argument-list-wrapping for big arg lists. DOCS | No |
| ktlint_annotation_handle_annotations_with_parameters_same_as_annotations_without_parameters | annotation list | unset | comma list, `*`, `unset` | | annotation rule wrapping exception. DOCS | No |
| ktlint_ignore_back_ticked_identifier | bool | false | bool | | Exclude back-ticked names from max-line-length. Lint only. | No |
| ktlint_enum_entry_name_casing | enum | upper_or_camel_cases | `upper_cases`, `camel_cases`, `upper_or_camel_cases` | | Lint only (enum-entry-name-case), no formatting effect | No |
| ktlint_property_naming_constant_naming | enum | screaming_snake_case | `screaming_snake_case`, `pascal_case` | | Lint only | No |
| ktlint_function_naming_ignore_when_annotated_with | list | unset | annotation names | | Lint only | No |
| ktlint_standard | enum | enabled | `enabled`, `disabled` | `ktlint_standard = disabled` | Toggle whole standard rule set; combine with per-rule `= enabled` for allow-list mode. VERIFIED (`ktlint_standard = disabled` + `ktlint_standard_indent = enabled` → only indentation fixed) | No (advanced) |
| ktlint_experimental | enum | disabled | `enabled`, `disabled` | `ktlint_experimental = enabled` | Run experimental rules. DOCS (accepted without error: VERIFIED) | Maybe |
| ktlint_standard_&lt;rule-id&gt; | enum | enabled (except `no-unused-imports`: disabled since 1.7) | `enabled`, `disabled` | `ktlint_standard_no-wildcard-imports = disabled` | Per-rule toggle, overrides rule-set toggle. VERIFIED (wildcard-import violation disappeared). Rule ids: see standard.md (≈90 rules: annotation, argument-list-wrapping, binary-expression-wrapping, blank-line-before-declaration, chain-method-continuation, class-signature, function-signature, if-else-bracing, import-ordering, indent, max-line-length, multiline-if-else, no-semi, no-unused-imports, no-wildcard-imports, parameter-list-wrapping, string-template, trailing-comma-on-call-site, trailing-comma-on-declaration-site, wrapping, *-spacing …) | Multi-select (advanced) |

CLI flags with output relevance:

| Flag | Meaning | Use |
|---|---|---|
| `--stdin` | read stdin | always |
| `-F` / `--format` | autocorrect; prints result to stdout in stdin mode | always |
| `--stdin-path=<path>` | virtual location → editorconfig lookup + `.kt`/`.kts` | always (isolation) |
| `--log-level=none` | suppress slf4j output (which goes to **stdout**) | always |
| `--ignore-autocorrect-failures` | unfixable violations not reported, exit 0 | recommended |
| `--editorconfig=<path>` | defaults-only editorconfig; loses to discovered ones | not recommended |
| `--code-style=` | removed; errors out | never |
| `-R/--ruleset`, `--reporter`, `--baseline`, `--limit`, `--relative`, `--color*`, `--patterns-from-stdin` | not relevant to formatting | never |

Counts: 23 generated properties + 3 toggle families; ~13 affect formatting output; **7 headline** (code style, indent_style, indent_size, max_line_length, 2× trailing comma, function-signature multiline threshold).

## Verified notes (input `a.kt`: wildcard import, `fun  foo(a:Int,b:String):String{`, badly indented `listOf(`, 170-col string concatenation)

1. Defaults: signature exploded one-param-per-line with trailing commas, `val list =\n listOf(` wrapping, binary expression wrapped at 140; exit 1 because of `no-wildcard-imports`.
2. `indent_size=2, max_line_length=60, trailing commas false, no-wildcard-imports disabled` → 2-space, no trailing commas, each `+` operand on own line; exit 1 with `Exceeded max line length (60) (cannot be auto-corrected)`.
3. `ktlint_code_style=android_studio, indent_style=tab` → single-line signature, tab indents, no trailing commas, no blank line after imports, exit 0 (wildcard `java.util.*` is allowed on-demand in that style).
4. `--log-level=error` has the same effect on stdout as `none`.
5. `generateEditorConfig` requires `--code-style=` (the only place that flag still works).
