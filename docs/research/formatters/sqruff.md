# sqruff — options research

Legend: **VERIFIED** = ran against the bundled exe on this machine (2026-09-19). **DOCS** = upstream at tag v0.29.3:
- defaults / full key list: https://github.com/quarylabs/sqruff/blob/v0.29.3/crates/lib/src/core/default_config.cfg
- rules: https://github.com/quarylabs/sqruff/blob/v0.29.3/docs/rules.md
- README (dialects, noqa, config): https://github.com/quarylabs/sqruff/blob/v0.29.3/README.md

## Bundled version

`sqruff.exe --version` → `sqruff 0.29.3` (native Rust). VERIFIED. Startup ~180–240 ms. VERIFIED.

Subcommands: `lint`, `fix`, `lsp`, `info`, `rules`, `help`. There is **no `format` subcommand and no `dialects` subcommand**; `fix` is the formatter. VERIFIED.

## Recommended invocation & config-passing mechanism

Everything except the config path is config-file driven. `--dialect` does **not exist** (exit 2 `error: unexpected argument '--dialect' found`). VERIFIED.

```
cfg  = %TEMP%\<ext>\sqruff\<settings-hash>\config.sqruff     # any file name works
args = [ "fix", "--config", cfg, "-" ]                        # "--config" is also accepted before "fix"
cwd  = directory of cfg (any dir without a user .sqruff/.sqlfluff)
```

`--config` works with stdin `fix -` (VERIFIED) and accepts `\` or `/` separators.

Minimal generated config:

```ini
[sqruff]
dialect = ansi
rules = core
exclude_rules = CP02,AM06
max_line_length = 80

[sqruff:indentation]
indent_unit = space
tab_space_size = 4
indented_joins = False

[sqruff:layout:type:comma]
line_position = trailing

[sqruff:rules:capitalisation.keywords]
capitalisation_policy = upper

[sqruff:rules:capitalisation.functions]
extended_capitalisation_policy = consistent
```

Only emit keys with valid values: several invalid values **panic** the process (see contract). Section prefix `sqruff` is canonical; a discovered `.sqlfluff` file with `[sqlfluff:…]` headers is also understood (VERIFIED).

Post-processing: `fix -` prints the fixed SQL **plus one extra `\n`** (`SELECT a FROM foo\n` → `SELECT a FROM foo\n\n`). Strip exactly one trailing newline. VERIFIED. CRLF input is emitted as **LF** (VERIFIED) — re-apply the editor's EOL if needed.

## stdout / stderr / exit-code contract (`fix -`, VERIFIED)

| Situation | exit | stdout | stderr |
|---|---|---|---|
| Already clean | 0 | SQL + extra `\n` | empty |
| Violations found, **all fixed** | 0 | fixed SQL + `\n` | human report of the *pre-fix* violations: `== [<string>] FAIL` / `L:   1 | P:  10 | LT01 | Expected single whitespace …` |
| Violations found, **some unfixable** (e.g. AM06, LT05 on unbreakable line) | **1** | fixed SQL + `\n` (complete, usable) | same report (lists all violations, fixed and unfixed alike) |
| **Parse error** (`SELECT FROM WHERE (((`) | 1 | **original SQL unchanged** + `\n` | `L:   1 | P:  21 | ???? | Couldn't find closing bracket for opening bracket.` |
| Unknown flag | 2 | empty | clap error + usage |
| `--config` file missing | 1 | empty | `The specified config file '…' does not exist.` |
| Unknown dialect (`dialect = klingon`) | **101** | empty | `thread 'main' panicked at crates\lib\src\core\config.rs:114:68: called Result::unwrap() on an Err value: VariantNotFound` |
| Bad value (`tab_space_size = abc`, `rules = ZZ99`) | **101** | empty | panic at `utils\reflow\config.rs:235` |
| Rule crash mid-run (seen: `rules = all` + `preferred_not_equal_style = c_style` on `… and b is not null` → panic in `rules\convention\cv05.rs:131`) | 1 | fixed SQL still printed | report + panic text |

Key facts:
- **The lint report is NOT mixed into stdout** — stdout is always pure SQL; the report goes to stderr. The extension's "non-empty stdout = success" rule therefore yields correct text in every non-crash case. Do **not** switch to "exit code 0 = success": exit 1 is normal whenever an unfixable rule fires.
- Parse errors are indistinguishable by stdout (input echoed back). Detect via stderr containing `| ???? |` if the UI should show "could not parse".
- Silencing the report: `-f json` leaves stderr **empty** and stdout unchanged in fix mode (VERIFIED) → `["fix", "-f", "json", "--config", cfg, "-"]` is a usable quiet mode, at the cost of losing parse-error text. `-f github-annotation-native` prints `::error …` lines to stderr. There is no `--quiet`.
- `--parsing-errors` made no visible difference for stdin fix.

## Isolation / discovery gotchas (VERIFIED)

1. Without `--config`, sqruff loads `.sqruff` **or `.sqlfluff` from the current working directory only** — parent directories are *not* searched (config in parent of cwd: ignored, in both stdin and file mode; config in cwd: applied). So today's `sqruff fix -` output depends on the host process' cwd.
2. `--config <file>` **replaces** cwd discovery (cwd `.sqruff` set keywords upper; `--config` file without that key → keywords left alone). No merge.
3. No home-directory config observed (`~/.sqruff` absent; not probed further).
4. No cache / temp files.
5. Default `rules = core` includes **CP02 (capitalisation.identifiers)** which rewrote table `Foo` → `foo` in the baseline test. For case-sensitive identifiers that is a semantic change. Recommend the extension default to `exclude_rules = CP02` or set `[sqruff:rules:capitalisation.identifiers] extended_capitalisation_policy` explicitly and expose it.
6. Inline `-- noqa` / `-- noqa: disable=…` comments in user SQL suppress rules (DOCS).

## Dialects

No CLI listing. Probed every sqlfluff dialect name through `dialect = X` (exit 0 vs panic 101). VERIFIED accepted at 0.29.3 (14):

`ansi` (default) · `athena` · `bigquery` · `clickhouse` · `databricks` · `duckdb` · `mysql` · `postgres` · `redshift` · `snowflake` · `sparksql` · `sqlite` · `trino` · `tsql`

Rejected (panic): db2, exasol, greenplum, hive, impala, mariadb, materialize, oracle, soql, starrocks, teradata, vertica. README lists the same set minus `tsql`.

## Options reference

Full key list is the sqlfluff-derived `default_config.cfg` (DOCS). Keys below are the formatting-relevant ones; section names given with the `sqruff` prefix.

### `[sqruff]`

| Key | Type | Default | Values | Config form | Meaning | Headline? |
|---|---|---|---|---|---|---|
| dialect | enum | `ansi` | 14 values above | `dialect = postgres` | Parser dialect. Invalid → panic 101. VERIFIED | **Yes** |
| rules | list | `core` | rule codes (`LT01`), names (`layout.spacing`), groups (`all`, `core`, `layout`, `capitalisation`, `aliasing`, `ambiguous`, `convention`, `references`, `structure`) | `rules = layout,capitalisation` | Allow-list. `rules = layout` = "pure formatter" mode (VERIFIED: whitespace/indent only, identifiers untouched, select targets one-per-line via LT09). `rules = all` adds LT03/LT04/LT09/LT13/LT15, CV*, ST* etc. (VERIFIED: ST01 removed `else null`). Unknown code → panic. | **Yes** (as preset: core / layout-only / all) |
| exclude_rules | list | None | same tokens | `exclude_rules = CP02,AM06` | Deny-list. VERIFIED | Yes |
| max_line_length | int | 80 | int; ≤0 disables | `max_line_length = 120` | LT05 wrap threshold. VERIFIED (200 → query stays on one line; 80 → clause per line; 30 → WHERE conditions split further) | **Yes** |
| templater | enum | `raw` | `raw`, `placeholder`; `jinja`/`dbt`/`python` need the Python build | `templater = raw` | Leave at `raw`. DOCS (docs/templaters.md) | No |
| runaway_limit | int | 10 | | | Max fix passes. DOCS | No |
| large_file_skip_byte_limit | int | 20000 | 0 = off | | sqlfluff semantics: skip files above limit. **Not enforced for stdin at 0.29.3** — VERIFIED: 28.9 KB / 1200-statement input was fully fixed (2.4 s). No need to set. | No |
| verbose, nocolor, output_line_length, ignore, warnings, warn_unused_ignores, ignore_templated_areas, encoding, disable_noqa, sql_file_exts, fix_even_unparsable, processes | | | | | Inherited from sqlfluff; not output-relevant / not necessarily implemented. DOCS | No |

### `[sqruff:indentation]`

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| indent_unit | enum | `space` | `space`, `tab` | Indent character. VERIFIED (`tab`) | **Yes** |
| tab_space_size | int | 4 | positive int | Spaces per indent. Non-int → panic. VERIFIED (2) | **Yes** |
| indented_joins | bool | False | True/False (case-insensitive VERIFIED) | Indent `JOIN` under `FROM`. VERIFIED | Yes |
| indented_ctes | bool | False | | Indent CTE bodies' `WITH` siblings. DOCS | Maybe |
| indented_using_on | bool | True | | Indent `ON`/`USING` under join. DOCS | Maybe |
| indented_on_contents | bool | True | | DOCS | No |
| indented_then | bool | True | | `THEN` indentation in CASE. DOCS | No |
| indented_then_contents | bool | True | | DOCS | No |
| allow_implicit_indents | bool | False | | Allow `WHERE a = 1` + indented `AND …` instead of forcing break after `WHERE`. VERIFIED | Yes |
| template_blocks_indent | bool | True | | Templating only | No |
| skip_indentation_in | list | `script_content` | | DOCS | No |
| trailing_comments | enum | `before` | `before`, `after` | Where LT05 moves trailing comments. DOCS | No |

### `[sqruff:layout:type:<segment>]`

Per segment type: `spacing_before`, `spacing_after`, `spacing_within` ∈ `single` | `touch` | `any` (+ `:inline` modifier, `single:inline`, `touch:inline`), and `line_position` ∈ `leading` | `trailing` | `alone` | `alone:strict`. ~45 segment types in default config (comma, binary_operator, comparison_operator, assignment_operator, set_operator, statement_terminator, start/end_bracket, casting_operator, select_clause, from_clause, where_clause, join_clause, groupby_clause, orderby_clause, having_clause, limit_clause, common_table_expression, function_name, comment, …). DOCS.

| Key | Default | Values | Meaning | Headline? |
|---|---|---|---|---|
| `[sqruff:layout:type:comma] line_position` | `trailing` | `trailing`, `leading` | Leading vs trailing commas. **Only enforced when LT04 is enabled — LT04 is not in `core`** (VERIFIED: no effect with default rules; works with `rules = all` or `rules = core,LT04`). | **Yes** |
| `[sqruff:layout:type:binary_operator] line_position` | `leading` | `leading`, `trailing` | `AND`/`OR`/`+` at line start or end (LT03, not in core). DOCS | Yes |
| `[sqruff:layout:type:comparison_operator] line_position` | `leading` | same | DOCS | No |
| `[sqruff:layout:type:set_operator] line_position` | `alone:strict` | | UNION on own line (LT11). DOCS | No |
| `[sqruff:layout:type:<x>_clause] line_position` | `alone` (orderby: `leading`) | `alone`, `alone:strict`, `leading` | `alone:strict` forces a line break before every clause even on short queries. DOCS | Maybe |

### `[sqruff:rules]` and `[sqruff:rules:<rule.name>]`

| Section · Key | Default | Values | Meaning | Headline? |
|---|---|---|---|---|
| `capitalisation.keywords` · `capitalisation_policy` | `consistent` | `consistent`, `upper`, `lower`, `capitalise` | CP01 keyword case. VERIFIED (`upper`, `capitalise`) | **Yes** |
| `capitalisation.identifiers` · `extended_capitalisation_policy` | `consistent` | `consistent`, `upper`, `lower`, `capitalise`, `pascal` (sqlfluff also: `snake`, `camel`) | CP02 unquoted identifier case. VERIFIED (`upper`). **Changes identifiers** — see gotcha 5 | **Yes** |
| `capitalisation.functions` · `extended_capitalisation_policy` | `consistent` | same | CP03 function-name case. VERIFIED (`lower`: `Count(*)` → `count(*)`) | **Yes** |
| `capitalisation.literals` · `capitalisation_policy` | `consistent` | `consistent`, `upper`, `lower`, `capitalise` | CP04 `NULL`/`TRUE`/`FALSE` case. DOCS | Yes |
| `capitalisation.types` · `extended_capitalisation_policy` | `consistent` | as CP02 | CP05 datatype case. DOCS | Yes |
| each capitalisation.* · `ignore_words`, `ignore_words_regex` | None | list / regex | Exemptions. DOCS | No |
| `[sqruff:rules]` · `allow_scalar`, `single_table_references`, `unquoted_identifiers_policy` | True, consistent, all | | Shared rule settings. DOCS | No |
| `aliasing.table` / `aliasing.column` · `aliasing` | `explicit` | `explicit`, `implicit` | AL01/AL02: add or remove `AS`. DOCS | Yes |
| `aliasing.length` · `min_alias_length`, `max_alias_length` | None | int | Lint only | No |
| `aliasing.forbid` · `force_enable` | False | | | No |
| `ambiguous.join` · `fully_qualify_join_types` | `inner` | `inner`, `outer`, `both` | AM05 (`JOIN` → `INNER JOIN`); not in core. DOCS | Maybe |
| `ambiguous.column_references` · `group_by_and_order_by_style` | `consistent` | `consistent`, `explicit`, `implicit` | AM06 lint only (unfixable → exit 1) | No |
| `convention.not_equal` · `preferred_not_equal_style` | `consistent` | `consistent`, `c_style` (`!=`), `ansi` (`<>`) | CV01; not in core. VERIFIED (`c_style`: `<>` → `!=`), but triggered a CV05 panic in the same run | Maybe |
| `convention.select_trailing_comma` · `select_clause_trailing_comma` | `forbid` | `forbid`, `require` | CV03. DOCS | No |
| `convention.count_rows` · `prefer_count_1`, `prefer_count_0` | False | bool | CV04. DOCS | No |
| `convention.terminator` · `multiline_newline`, `require_final_semicolon` | False, False | bool | CV06 (not in core): add final `;`. DOCS | Maybe |
| `convention.quoted_literals` · `preferred_quoted_literal_style`, `force_enable` | `consistent` | `consistent`, `single_quotes`, `double_quotes` | CV10. DOCS | No |
| `convention.casting_style` · `preferred_type_casting_style` | `consistent` | `consistent`, `shorthand` (`::`), `cast`, `convert` | CV11. DOCS | No |
| `convention.blocked_words` · `blocked_words`, `blocked_regex`, `match_source` | None | | Lint only | No |
| `references.*` · `force_enable`, `unquoted_identifiers_policy`, `quoted_identifiers_policy`, `prefer_quoted_identifiers`, `prefer_quoted_keywords`, `allow_space_in_identifier`, `additional_allowed_characters`, `ignore_words*` | see default_config.cfg | | Mostly lint only; RF06 can remove needless quotes. DOCS | No |
| `layout.long_lines` · `ignore_comment_lines`, `ignore_comment_clauses` | False | bool | LT05 exemptions. DOCS | No |
| `layout.select_targets` · `wildcard_policy` | `single` | `single`, `multiple` | LT09. DOCS | No |
| `layout.newlines` · `maximum_empty_lines_between_statements`, `maximum_empty_lines_inside_statements` | 2, 1 | int | LT15 (not in core). DOCS | Maybe |
| `structure.subquery` · `forbid_subquery_in` | `join` | `join`, `from`, `both` | ST05. DOCS | No |
| `structure.join_condition_order` · `preferred_first_table_in_join_clause` | `earlier` | `earlier`, `later` | ST09. DOCS | No |

Counts: ~25 top-level/indentation keys, ~45 layout segment sections (≤4 keys each), ~45 rule keys. Realistically **~12 worth exposing**; headline 9: dialect, rule preset, max_line_length, indent_unit, tab_space_size, keyword case, identifier case (or CP02 off), function case, comma position.

## Rules (`sqruff rules`, VERIFIED list; "fixable" per docs/rules.md — DOCS, spot checks show it is not fully accurate, e.g. ST01 *was* auto-fixed)

62 rules. `core` group (default): AL02–AL06, AL08, AL09, AM01, AM02, AM06, CP01–CP05, CV03–CV05, LT01, LT02, LT05–LT08, LT10–LT12, RF01, ST03, ST08.
Formatting-relevant and auto-fixing:
- Layout (all fixable except LT07): **LT01** spacing, **LT02** indent, LT03 operator position*, LT04 comma position*, **LT05** long lines, LT06 function paren, LT08 CTE newline, LT09 select targets one per line*, LT10, LT11, LT12 EOF newline, LT13 leading blank lines*, LT15 blank-line runs*. (* = not in `core`, needs `rules = all`/`layout`/explicit.)
- Capitalisation: CP01–CP05 (all fixable, all core).
- Semantics-touching fixers to keep opt-in: AL01/AL02 (AS), AL05 (drops unused alias), AL07, AM02/AM03/AM05, CV01, CV06, CV07, CV10, CV11, RF03, RF06, ST01, ST04–ST07.
- Lint-only rules that commonly cause **exit 1** on ordinary snippets: AM06, AL03, RF01, LT05 (unbreakable long line), CV03–CV05.

## Verified notes

Input A: `select a,b, Count(*) as c from Foo f join bar b on f.id=b.id where a=1 and b in (select x from y) group by a,b order by 1`
1. `fix -` (defaults): clause-per-line, spacing fixed, `Foo`→`foo`; exit 1 (AM06); report on stderr; stdout pure SQL + extra newline.
2. `--config one.sqruff` (postgres, keywords upper, functions lower, exclude AM06+CP02): `SELECT a, b, count(*) AS c / FROM Foo f / …`, exit 0.
3. `max_line_length = 200` → whole query stays on one line; `= 30` → `where` block split into indented `a = 1` / `and b in (…)`.

Input B (8-line select with CASE, join, two WHERE conditions):
4. `indent_unit = tab`, `indented_joins = True` → `\t` indents, `inner join` indented under `from`.
5. `tab_space_size = 2`, `allow_implicit_indents = true` → 2-space; `where a <> 1` kept on the `where` line.
6. `capitalisation_policy = capitalise` + identifiers `upper` → `Select A, … From FOO Inner Join BAR`.
7. `rules = LT01` → only spacing fixed, indentation untouched. `exclude_rules = ST01,LT02` likewise honoured.
8. comma `line_position = leading`: no effect under `rules = core`; with `rules = core,LT04` → `    a\n    , b\n    , case …`.
