# php-cs-fixer (PHP) — option research

Tags: **VERIFIED** = ran it (bundled `Binaries/php-cs-fixer.exe`, or the identical phar it extracts, run with its own `php.exe` for speed); **DOCS** = https://cs.symfony.com/ (`doc/usage.html`, `doc/config.html`, `doc/rules/index.html`, `doc/ruleSets/index.html`).
Raw dump: `php-cs-fixer-rules-raw.txt` (same dir): all 294 rules with every option/default/allowed values + `list-sets` output.

## Bundled version & what the exe is
- `PHP CS Fixer 3.92.0 (7b15cb5) "Exceptional Exception"`, `PHP runtime: 8.4.15`. VERIFIED.
- The 18 MB exe is a **self-extracting launcher**: on EVERY run it (re)writes `php.exe`, `php8.dll`, `libcrypto-3-x64.dll`, `libssl-3-x64.dll`, `php-cs-fixer.phar` (≈20 MB) into `%TEMP%\php-cs-fixer-launcher\` (file mtimes updated on each run — VERIFIED) and then runs `php.exe php-cs-fixer.phar <args>`. Help text shows that path. Args (including JSON with quotes) are forwarded intact (VERIFIED via .NET `ProcessStartInfo.ArgumentList`).

## Startup time
- **~2.9–3.6 s per invocation** through the launcher (5 runs: 3603, 3142, 3666, 2867, 3018 ms); the fix itself reports 0.03 s. VERIFIED.
- Running the extracted `php.exe php-cs-fixer.phar` directly: 1.2–1.6 s. So ~1.5–2 s is pure re-extraction overhead. Too slow for per-keystroke formatting; needs long debounce. (Possible optimisation, outside this research: ship php + phar unpacked and call `php.exe` directly. Concurrent launcher runs rewrite the same files — possible sharing violations if two formats overlap.)

## stdin support — NO (VERIFIED)
`fix -` reads stdin but "won't automatically fix anything": it only prints a diff (`--diff`) and exits 8. The fixed source is never emitted. The temp-file approach must stay.

## Recommended invocation
Private temp dir per invocation containing `input.php` + generated `config.php`; cwd = that dir.
```
["fix", "<dir>\\input.php", "--config=<dir>\\config.php", "--using-cache=no", "-n", "--no-ansi", "--quiet"]
```
`config.php` (generated; rules passed as JSON so C# never has to emit PHP array syntax):
```php
<?php
return (new PhpCsFixer\Config())
    ->setRiskyAllowed(false)            // true when user enables risky rules
    ->setUsingCache(false)
    ->setIndent("    ")                 // "\t" or N spaces
    ->setLineEnding("\n")               // "\n" or "\r\n"
    ->setRules(json_decode('{"@PSR12":true,"array_syntax":{"syntax":"short"}}', true));
```
(escape `'` and `\` in the JSON when embedding in a single-quoted PHP string.)

Alternative without a config file (works, but cannot set indent/line ending and leaves cwd discovery on):
```
["fix","<file>","--rules={\"@PSR12\":true,\"array_syntax\":{\"syntax\":\"short\"}}","--allow-risky=no","--using-cache=no","-n","--no-ansi","--quiet"]
```
- `--rules` accepts a comma list (`@PSR12,-braces_position,strict_comparison`; `-name` removes a rule) **or** a JSON object string. JSON as ONE argv element works on Windows with no shell (VERIFIED through the launcher with `ArgumentList`).
- **Indent and line ending are only settable via config file** (`setIndent()`, `setLineEnding()`); rules `indentation_type` and `line_ending` (both in @PSR12) have no options and read those values. VERIFIED: `setIndent("  ")` → 2-space, `setIndent("\t")` → tabs, `setLineEnding("\r\n")` → CRLF. DOCS: only `"\n"`/`"\r\n"` and `"\t"`/spaces are valid.
- **Always pass `-n` (`--no-interaction`).** See gotchas.

## stdout / stderr / exit codes (VERIFIED)
Result = the temp file rewritten in place. With `--quiet` both streams are empty on success.
| Case | exit | Output |
|---|---|---|
| Fixed or nothing to fix | 0 | (quiet) nothing; non-quiet: stdout `1) file` + summary, stderr banner/progress |
| **PHP syntax error in source** | **0** | file untouched; with `--quiet` NO signal at all. Non-quiet stdout: `Files that were not fixed due to errors reported during linting before fixing: 1) path`. `--format=json` gives `"files":[]` (indistinguishable from "already clean") |
| Unknown rule / invalid JSON / risky rule without allow-risky | 16 | stderr `The rules contain unknown fixers: "nope_rule".` / `Invalid JSON rules input: "Syntax error".` / `The rules contain risky fixers ("declare_strict_types"), but they are not allowed to run. Perhaps you forget to use --allow-risky=yes option?` (printed even with `--quiet`; boxed, ANSI unless `--no-ansi`) |
| Invalid rule option value | 32 | `[array_syntax] Invalid configuration: The option "syntax" with value "medium" is invalid. Accepted values are: "long", "short".` |
| dry-run/stdin: needs fixing | 8 | |
| Exit bit flags (help text) | | 0 OK, 1 general/PHP requirement, 4 invalid syntax (dry-run only), 8 needs fixing (dry-run only), 16 app config error, 32 fixer config error, 64 exception |
- To detect syntax errors: drop `--quiet`, add `--show-progress=none`, and look for `not fixed due to errors` in stdout (add `-v` for the lint message); or run a separate dry-run (exit bit 4) — doubles the 3 s cost.

## Isolation / discovery gotchas (all VERIFIED)
1. **Interactive config creation (new in recent 3.x).** With no config file found and neither `-n` nor `--quiet`, `fix` asks `Do you want to create the config file? [yes]`; with stdin closed/empty it takes the default **yes**, writes `.php-cs-fixer.dist.php` (rules `@auto`) **into the cwd**, prints "Config file created, re-run the command", **does not format**, and exits 0. With stdin piped it consumes the source as answers. The current invocation is safe only because `--quiet` implies non-interactive; add `-n` explicitly.
2. **Auto-discovery:** looks for `.php-cs-fixer.php` then `.php-cs-fixer.dist.php` in (a) the directory of the single file argument and (b) the **cwd**. It does NOT walk up to parents (config in grandparent ignored).
3. **`--rules` does not disable discovery.** A discovered config is still loaded ("Rules from configuration have been overridden by rules provided as command argument"), and its `setIndent`/`setLineEnding`/risky/finder still apply — observed tabs from a cwd config despite `--rules=@PSR12`. Only `--config=<file>` fully pins behaviour.
4. Warning on stderr every run outside a Composer project: `Unable to determine minimum PHP version supported by your project from composer.json` — harmless; only affects `@auto*` sets (avoid exposing `@auto`, `@autoPHPMigration`: they depend on composer.json in cwd).
5. `PHP_CS_FIXER_IGNORE_ENV=1` (DOCS) bypasses the PHP-version/extension requirement check; not needed with the bundled PHP 8.4.15 (no complaint seen). `--allow-unsupported-php-version=yes` is the CLI equivalent for too-new PHP. `PHP_CS_FIXER_FUTURE_MODE=1` switches to 4.0 defaults — do not set.
6. Cache: `--using-cache=no` / `setUsingCache(false)`, otherwise `.php-cs-fixer.cache` is written to cwd.
7. Default rules when nothing is given: `@PSR12` in 3.x (DOCS; help text: "overriding the default PSR-12"); `@auto` is what `init` writes.

## Rule inventory at 3.92.0 (VERIFIED — enumerated `FixerFactory::registerBuiltInFixers()` from the bundled phar)
- There is **no `list-rules` command**. Subcommands: `check, completion, describe, fix, help, init, list, list-files, list-sets, self-update`. Per-rule info: `describe <rule|@set> [--expand] [--format=txt|tree]`.
- **294 rules**: 75 risky, 133 configurable, 17 deprecated (e.g. `visibility_required` → `modifier_keywords`, `braces` family → `braces_position` etc.).
- **101 rule sets** from `list-sets` (many are alias pairs, `@PHP82Migration` = `@PHP8x2Migration`). Useful ones: `@PSR1`, `@PSR2`, `@PSR12`(+`:risky`), `@PER-CS` (= latest, 3.0), `@PER-CS1.0/2.0/3.0`(+`:risky`), `@Symfony`(+`:risky`), `@PhpCsFixer`(+`:risky`), `@PHP54…@PHP85Migration`(+some `:risky`), `@PHPUnit30…11x0Migration:risky`, `@DoctrineAnnotation`, `@auto*` (composer-dependent). `:risky` sets require allow-risky.

## Options reference
Global (not rules):
| Key | Type | Default | Values | CLI / config form | Meaning | Headline? |
|---|---|---|---|---|---|---|
| ruleset | enum(multi) | @PSR12 | see sets | `--rules=@X` / `setRules(['@X'=>true])` | Base preset | Yes |
| indent | string | 4 spaces | `"\t"`, 2/4/… spaces | config only: `setIndent()` | Indentation unit | Yes |
| lineEnding | string | `"\n"` | `"\n"`, `"\r\n"` | config only: `setLineEnding()` | EOL | Yes |
| allow-risky | bool | no | yes/no | `--allow-risky=yes` / `setRiskyAllowed(true)` | Permit behaviour-changing rules | Yes |
| using-cache | bool | yes | yes/no | `--using-cache=no` | fixed `no` | fixed |

Headline rules (JSON form `"rule": true | false | {options}`; defaults = rule's own default when enabled with `true`; "In @PSR12?" tells whether the default preset already turns it on):
| Key | Option: type = default (values) | Meaning | Risky | Headline? |
|---|---|---|---|---|
| array_syntax | syntax = "short" (long, short) | `[]` vs `array()` | | Yes |
| single_quote | strings_containing_single_quote_chars: bool = false | `"a"` → `'a'` | | Yes |
| binary_operator_spaces | default = "single_space" (align, align_by_scope, align_single_space, align_single_space_minimal, align_single_space_by_scope, align_single_space_minimal_by_scope, single_space, no_space, at_least_single_space, null); operators: map op→same values = {} | Spacing/alignment around `=`, `=>`, … | | Yes |
| concat_space | spacing = "none" (none, one) | `$a.$b` vs `$a . $b` | | Yes |
| braces_position | classes_opening_brace, functions_opening_brace = "next_line_unless_newline_at_signature_end"; control_structures_opening_brace, anonymous_functions_opening_brace, anonymous_classes_opening_brace = "same_line" (values: those two); allow_single_line_anonymous_functions, allow_single_line_empty_anonymous_classes: bool = true | Brace placement (replaces deprecated `braces`/`curly_braces_position`) | | Yes |
| control_structure_continuation_position | position = "same_line" (same_line, next_line) | `} else {` vs `}\nelse {` | | Yes |
| trailing_comma_in_multiline | elements = ["arrays"] (arguments, array_destructuring, arrays, match, parameters); after_heredoc = false | Trailing commas | | Yes |
| ordered_imports | sort_algorithm = "alpha" (alpha, length, none); imports_order = null (["class","function","const"] permutation); case_sensitive = false | Sort `use` | | Yes |
| no_unused_imports | – | Remove unused `use` | | Yes |
| global_namespace_import | import_classes = true, import_constants = null, import_functions = null (true/false/null) | Import vs FQ global symbols | | Maybe |
| blank_line_before_statement | statements = [break, continue, declare, return, throw, try] (23 allowed, see raw) | Blank line before statements | | Yes |
| no_extra_blank_lines | tokens = ["extra"] (attribute, break, case, comma, continue, curly_brace_block, default, extra, parenthesis_brace_block, return, square_brace_block, switch, throw, use, use_trait) | Remove surplus blank lines | | Yes |
| class_attributes_separation | elements = {const: one, method: one, property: one, trait_import: none, case: none} (none, one, only_if_meta) | Blank lines between class members | | Yes |
| yoda_style | equal = true, identical = true, less_and_greater = null (true/false/null); always_move_variable = false | `null === $a` vs `$a === null` | | Yes |
| cast_spaces | space = "single" (none, single) | `(int) $a` vs `(int)$a` | | Yes |
| not_operator_with_successor_space | – | `! $a` | | Maybe |
| increment_style | style = "pre" (pre, post) | `++$i` vs `$i++` | | Maybe |
| operator_linebreak | position = "beginning" (beginning, end); only_booleans = false | Operator at line start/end when wrapped | | Yes |
| method_argument_space | on_multiline = "ensure_fully_multiline" (ignore, ensure_single_line, ensure_fully_multiline); keep_multiple_spaces_after_comma = false; attribute_placement = "standalone" (ignore, same_line, standalone); after_heredoc = false | Argument list layout | | Yes |
| function_declaration | closure_function_spacing = "one", closure_fn_spacing = "one" (none, one); trailing_comma_single_line = false | `function ()` vs `function()` | | Maybe |
| return_type_declaration | space_before = "none" (none, one) | `): int` vs `) : int` | | Maybe |
| types_spaces | space = "none" (none, single); space_multiple_catch = null | `A|B` vs `A | B` | | Maybe |
| single_line_empty_body | – | `{}` on same line | | Maybe |
| modifier_keywords | elements = [const, method, property] | Require visibility, order modifiers (successor of `visibility_required`) | | Maybe |
| nullable_type_declaration_for_default_null_value | use_nullable_type_declaration = true | `?T $x = null` | | Maybe |
| phpdoc_align | align = "vertical" (left, vertical); spacing = 1; tags = [method, param, property, return, throws, type, var] | PHPDoc column alignment | | Yes |
| array_indentation | – | Re-indent multi-line arrays | | Yes |
| declare_strict_types | preserve_existing_declaration = false | Add `declare(strict_types=1);` | **risky** | Yes (gated by allow-risky) |
| strict_comparison | – | `==` → `===` | **risky** | Maybe |
| native_function_invocation | include = ["@compiler_optimized"], exclude = [], scope = "all" (all, namespaced), strict = true | `\strlen()` | **risky** | No |
| indentation_type / line_ending | – (driven by setIndent/setLineEnding) | | | via global |

Every other rule/option: see raw file (`name [flags]`, then `- option (type) default=… allowed: …`). A schema-driven UI could be generated directly from that dump. Docs per rule: `https://cs.symfony.com/doc/rules/<group>/<rule>.html`.

## Verified notes (input: class with `array(1,2,3)`, `"hello" . $b`, `(int)$a+A::z()`, unsorted/unused imports)
- `--rules=@PSR12`: braces moved, `public function`, `$a + A::z()`; array/quotes untouched.
- JSON `{"@PSR12":true,"array_syntax":{"syntax":"short"},"concat_space":{"spacing":"none"},"no_unused_imports":true,"ordered_imports":true,"single_quote":true,"yoda_style":true,"binary_operator_spaces":{"default":"align_single_space_minimal"},"trailing_comma_in_multiline":true}` → `[1,2,3]`, `'hello'.$b`, `null == $a`, imports sorted and `Unused\C` removed, `'a'   => 1,` aligned, trailing comma added.
  - Note `cast_spaces:{"space":"none"}` and array element spacing `[1,2,3]` unchanged: @PSR12 does not include `whitespace_after_comma_in_array`.
- `--rules=declare_strict_types` → exit 16 without `--allow-risky=yes`; with it: `<?php declare(strict_types=1);`.
- Config discovery matrix: config in file's dir → loaded; in cwd → loaded (even with `--rules`); in file's grandparent → not loaded; `--config=` → only that.
