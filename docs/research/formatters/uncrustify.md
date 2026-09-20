# uncrustify (Objective-C; also C, C++, C#, D, Java, Pawn, Vala) — formatter options research

Legend: **VERIFIED** = executed against the bundled binary on Windows 11 (2026-09-19). **DOCS** = from documentation (URL cited). **EMBEDDED** = text from the binary's own `--show-config` dump (version-exact).

## 1. Bundled version

`C:\Users\login\cs-f\Binaries\uncrustify.exe --version` -> `Uncrustify-0.82.0_f` (VERIFIED). 2.7 MB, file date 2025-12-13. `--count-options` -> **857 options** (VERIFIED; parsed count from the dump also 857).

## 2. Recommended invocation and config-passing mechanism

Mechanism: **`-c -` (built-in defaults, no file) + one `--set option=value` pair per option**. Works with stdin->stdout, no files needed (VERIFIED).

```text
uncrustify.exe -l OC -c - -q [--set NAME=VALUE]...
```

Exact `ArgumentList` example (each line = one element):

```text
-l
OC
-c
-
-q
--set
indent_columns=4
--set
indent_with_tabs=0
--set
sp_before_sparen=force
--set
sp_arith=force
--set
nl_oc_mdef_brace=force
--set
code_width=100
```

Rules (all VERIFIED):

- `--set` is repeatable; when the same option is given twice the **last one wins**.
- Form must be exactly `name=value` with **no spaces around `=`** inside the element: `"indent_columns = 2"` fails (`Unknown option 'indent_columns ' to override.`, exit 1). `--set=indent_columns=2` (single element) also works. `--set indent_columns` (no `=`) -> exit 64.
- Values: bool `true`/`false`; IARF `ignore`/`add`/`remove`/`force`; numbers bare; `newlines` `lf`/`crlf`/`cr`/`auto`; strings bare (no quotes needed in argv).
- `-l` is mandatory for stdin unless `--assume <filename>` is given (without either: exit 1, `If reading from stdin, you should specify the language using -l ...`). `-l` values: `C, CPP, D, CS, JAVA, PAWN, OC, OC+, VALA` (from `--help`; `OC+` = Objective-C++). `--assume x.m` detected OC correctly.
- `-q` suppresses the informational stderr line (`Parsing: N bytes ... from stdin as language OC`); **errors are still printed with `-q`**.
- `--frag` exists for code fragments (treat first line's indent as correct) — useful for a snippet formatter.
- Dump of every option with type, default and documentation: `uncrustify.exe --show-config` (identical content to `-c - --update-config-with-doc`; VERIFIED byte-identical after line-ending normalisation). Saved as `uncrustify-options-raw.txt` (3744 lines). Format per option: comment block (doc text, optional `# Default: X` line when default is not ignore/false/0), then `name = default  # type-or-values`. This file is machine-parsable and is the right source for generating the UI table.
- `--update-config` with `--set` prints the effective config (debug aid). `--universalindent` emits an INI with categories/descriptions/value types for every option — another machine-readable source (DOCS: `--help`).

### The important surprise: built-in defaults do almost nothing

With `-c -` and no `--set`, 405 of the 857 options are `ignore` and most bools are `false`. The only visible effect on the test input was re-indentation with **tabs, 8 columns** (`indent_columns=8`, `indent_with_tabs=1`). Spacing, brace placement, operators: untouched (`if(a>1){` stays `if(a>1){`). The current extension invocation (`-l OC -c - -q`) is therefore an indenter, not a formatter. A useful Objective-C result requires the extension to ship an opinionated base set of `--set` values (or a bundled .cfg passed via `-c <path>`; `--set` still overrides a cfg file — DOCS https://github.com/uncrustify/uncrustify#configuring-the-program ). Only 80 options have a default other than ignore/false/0.

## 3. Isolation / discovery gotchas

| Finding | Status |
|---|---|
| uncrustify never searches cwd or parent directories for a config. | VERIFIED (behaviour) / DOCS |
| If `-c` is **omitted**, it uses env var `UNCRUSTIFY_CONFIG`; verified: with the env var pointing at a cfg (`indent_columns=3`), `uncrustify -l OC -q` applied it. | VERIFIED |
| If `-c` is omitted and no env var: exit 74, `Specify the config file with '-c file' or set UNCRUSTIFY_CONFIG`. (On Unix it would also try `~/.uncrustify.cfg` / `~/uncrustify.cfg`; none present here.) | VERIFIED / DOCS |
| **With `-c -` the `UNCRUSTIFY_CONFIG` env var is ignored** (verified: env var set, output still built-in defaults). | VERIFIED |
| A config file may `include` other files and set file-extension/type/macro definitions; irrelevant with `-c -`. | DOCS |
| Output does not depend on cwd. `--assume FN` influences language detection and include sorting only. | DOCS (`--help`) |

**Recommendation:** keep `-c -` always; it is the isolation switch. Nothing else needed.

## 4. Options reference

### 4.1 All 857 options by category (VERIFIED, parsed from the dump in dump order)

| # | Category (section header in dump) | Count |
|---|---|---|
| 1 | General options | 15 |
| 2 | Spacing options | 269 |
| 3 | Indenting options | 117 |
| 4 | Newline adding and removing options | 188 |
| 5 | Blank line options | 52 |
| 6 | Positioning options | 12 |
| 7 | Line splitting options | 4 |
| 8 | Code alignment options (not left column spaces/tabs) | 74 |
| 9 | Comment modification options | 29 |
| 10 | Code modifying options (non-whitespace) | 57 |
| 11 | Preprocessor options | 21 |
| 12 | Sort includes options | 3 |
| 13 | Use or Do not Use options | 6 |
| 14 | Warn levels / debug | 10 |
| | **Total** | **857** |

By value type (trailing `# ...` annotation in the dump):

| Type | Count | Values |
|---|---|---|
| IARF | 405 | `ignore` / `add` / `remove` / `force` |
| bool | 251 | `true` / `false` |
| unsigned number | 139 | >= 0; some have a max (e.g. `indent_with_tabs` max 2 — enforced) |
| number | 38 | signed |
| token position | 12 | `ignore/break/force/lead/trail/join/lead_break/lead_force/trail_break/trail_force` (the `pos_*` options) |
| string | 11 | |
| line-ending | 1 | `lf/crlf/cr/auto` (`newlines`) |

By name prefix: `sp_` 268, `nl_` 237, `indent_` 117, `align_` 74, `mod_` 57, `cmt_` 29, `pp_` 21, `pos_` 12, `debug_` 8, `use_` 5, other 29. 61 options are Objective-C specific (doc text prefixed `(OC)`; VERIFIED count).

Because every option has a uniform `name = value # type` shape, the full table is not reproduced here — it IS `uncrustify-options-raw.txt`. A generic UI ("advanced: any option") can be generated from that file with four editor kinds: IARF dropdown, bool toggle, number box, string box (+ the 12 `pos_*` dropdowns and `newlines`).

### 4.2 Headline options (proposed UI set). Type/default/meaning are EMBEDDED; CLI form is always `--set <key>=<value>`.

| Key | Type | Default | Values | Meaning | Verified? |
|---|---|---|---|---|---|
| `indent_columns` | unum | 8 | | Columns per indent level | VERIFIED |
| `indent_with_tabs` | unum | 1 | 0 spaces only; 1 tabs to brace level, spaces for alignment; 2 tabs for indent and align | Tab usage | VERIFIED (0; 7 rejected) |
| `input_tab_size` / `output_tab_size` | unum | 8 / 8 | | Tab width in input / output | |
| `newlines` | enum | auto | lf, crlf, cr, auto | Line endings (auto = keep input's) | VERIFIED |
| `code_width` | unum | 0 | 0 = off | Try to limit code width to N columns | VERIFIED |
| `cmt_width` | unum | 0 | | Wrap comments at N columns | |
| `indent_switch_case` | unum | 0 | | Spaces to indent `case` from `switch` | VERIFIED |
| `indent_continue` | num | 0 | | Continuation indent | |
| `indent_class` / `indent_namespace` | bool | false | | Indent class / namespace body (C++) | |
| `nl_if_brace` | IARF | ignore | | Newline between `if` and `{` | VERIFIED |
| `nl_brace_else` / `nl_else_brace` | IARF | ignore | | Newline between `}` and `else` / `else` and `{` | VERIFIED |
| `nl_for_brace`, `nl_while_brace`, `nl_switch_brace`, `nl_do_brace` | IARF | ignore | | Same for other control statements | |
| `nl_fdef_brace` | IARF | ignore | | Newline between C function signature and `{` (does NOT affect ObjC methods — verified) | VERIFIED |
| `nl_oc_mdef_brace` | IARF | ignore | | (OC) Newline between method declaration and `{` | VERIFIED |
| `nl_oc_block_brace` | IARF | ignore | | (OC) Newline between block signature and `{` | |
| `nl_struct_brace`, `nl_enum_brace`, `nl_class_brace` | IARF | ignore | | Newline before `{` of struct/enum/class | |
| `nl_max` | unum | 0 | | Max consecutive newlines (3 = 2 blank lines) | VERIFIED |
| `nl_end_of_file` + `nl_end_of_file_min` | IARF + unum | ignore, 0 | | Trailing newline at EOF | VERIFIED (accepted) |
| `nl_after_func_body` | unum | 0 | | Newlines after a function body | |
| `nl_oc_msg_args` | bool | false | | (OC) Each message parameter on its own line | |
| `sp_arith` | IARF | ignore | | Space around `+ - * / %` ... | VERIFIED |
| `sp_assign` | IARF | ignore | | Space around `=`, `+=` ... | VERIFIED |
| `sp_compare` | IARF | ignore | | Space around `< > ==` ... | VERIFIED |
| `sp_bool` | IARF | ignore | | Space around `&&`, `\|\|` | |
| `sp_after_comma` | IARF | ignore | | `a,b` vs `a, b` | VERIFIED |
| `sp_before_sparen` | IARF | ignore | | Space before `(` of if/for/while/switch | VERIFIED |
| `sp_inside_sparen` / `sp_inside_paren` | IARF | ignore | | Space inside control / any parens | |
| `sp_sparen_brace` / `sp_fparen_brace` | IARF | ignore | | Space between `)` and `{` | VERIFIED (sparen) |
| `sp_else_brace` / `sp_brace_else` | IARF | ignore | | Space around `else` | |
| `sp_before_ptr_star` / `sp_after_ptr_star` | IARF | ignore | | Pointer star placement (`NSString *name`) | VERIFIED |
| `sp_after_cast` | IARF | ignore | | `(int)a` vs `(int) a` | VERIFIED |
| `sp_func_call_paren`, `sp_func_proto_paren`, `sp_func_def_paren` | IARF | ignore | | Space between function name and `(` | |
| `sp_cmt_cpp_start` | IARF | ignore | | Space after `//` | |
| `sp_after_oc_scope` | IARF | ignore | | (OC) `-(void)` vs `- (void)` | VERIFIED |
| `sp_after_oc_return_type` | IARF | ignore | | (OC) `-(int) f:` vs `-(int)f:` | VERIFIED |
| `sp_after_oc_type` | IARF | ignore | | (OC) `(int) x` vs `(int)x` in message specs | |
| `sp_after_oc_colon` / `sp_before_oc_colon` | IARF | ignore | | (OC) space around `:` in method declarations | VERIFIED (accepted) |
| `sp_after_send_oc_colon` / `sp_before_send_oc_colon` | IARF | ignore | | (OC) space around `:` in message sends | |
| `sp_after_oc_property` | IARF | ignore | | (OC) `@property (...)` | |
| `align_assign_span` | unum | 0 | 0 = off | Align `=` over N lines | VERIFIED |
| `align_var_def_span` + `align_var_def_star_style` | unum | 0, 0 | star style 0/1/2 | Align variable definitions | |
| `align_right_cmt_span` | unum | 0 | | Align trailing comments | |
| `align_oc_msg_colon_span` | unum | 0 | | (OC) Align message-send args on `:` (Xcode style) | |
| `align_oc_decl_colon` | bool | false | | (OC) Align method declaration params on `:` | |
| `align_nl_cont` | unum | 0 | 0..n | Align macro backslashes | |
| `align_with_tabs` / `align_keep_tabs` | bool | false | | Tabs in alignment | |
| `mod_full_brace_if` / `_for` / `_while` / `_do` | IARF | ignore | | Add/remove braces on single-statement bodies | VERIFIED (if=add) |
| `mod_paren_on_return` | IARF | ignore | | Parens on `return` | |
| `mod_remove_extra_semicolon` | bool | false | | Remove superfluous `;` | |
| `mod_sort_include` | bool | false | | Sort `#include`/`#import` (can break code) | |
| `mod_sort_oc_properties` | bool | false | | (OC) Reorder @property attributes | |
| `cmt_star_cont` | bool | false | | Star on continued comment lines | |
| `cmt_reflow_mode` | unum | 0 | 0,1,2 | Comment reflow | |
| `pp_indent` / `pp_space_after` | IARF | ignore | | Preprocessor indentation | |
| `utf8_bom` | IARF | ignore | | Add/remove BOM | |

Suggested minimal "~25" UI subset: indent_columns, indent_with_tabs, output_tab_size, newlines, code_width, cmt_width, indent_switch_case, nl_if_brace, nl_brace_else, nl_else_brace, nl_fdef_brace, nl_oc_mdef_brace, nl_max, nl_end_of_file, sp_arith, sp_assign, sp_compare, sp_bool, sp_after_comma, sp_before_sparen, sp_sparen_brace, sp_before_ptr_star, sp_after_ptr_star, sp_after_cast, sp_after_oc_scope, sp_after_oc_return_type, sp_after_send_oc_colon, align_assign_span, align_oc_msg_colon_span, mod_full_brace_if.

Usability note: brace style in uncrustify is not one knob; "Allman" = `nl_if_brace, nl_else_brace, nl_brace_else, nl_for_brace, nl_while_brace, nl_switch_brace, nl_do_brace, nl_fdef_brace, nl_oc_mdef_brace, nl_struct_brace, nl_enum_brace, nl_class_brace = force` (and `remove` for K&R). A thin wrapper can still offer a composite "brace style" control that expands into these `--set` pairs.

## 5. Verified notes

Input `in.m`:

```objc
@interface Foo : NSObject
@property (nonatomic,strong) NSString* name;
-(void)doThing:(int)a with:(NSString*)b;
@end
@implementation Foo
-(void)doThing:(int)a with:(NSString*)b {
if(a>1){
int x=a+1;
int longer=2;
[self doThing:x with:b];
} else {
NSLog(@"%d",a);
}
}
@end
```

1. `-l OC -c - -q` -> only change: body indented with TABs; `if(a>1){`, `int x=a+1;`, `NSString* name` all untouched.
2. `-l OC -c - -q --set indent_columns=4 --set indent_with_tabs=0 --set sp_before_sparen=force --set sp_arith=force --set sp_assign=force --set sp_compare=force --set nl_fdef_brace=force --set nl_if_brace=force --set nl_brace_else=force --set nl_else_brace=force --set sp_after_oc_scope=force --set sp_after_comma=force --set align_assign_span=1 --set sp_before_ptr_star=force --set sp_after_ptr_star=remove` ->

```objc
@property (nonatomic, strong) NSString *name;
- (void)doThing:(int)a with:(NSString *)b;
...
- (void)doThing:(int)a with:(NSString *)b {      <- nl_fdef_brace has no effect on OC methods
    if (a > 1)
    {
        int x      = a + 1;
        int longer = 2;
        [self doThing:x with:b];
    }
    else
    {
        NSLog(@"%d", a);
    }
}
```

3. Second input, `--set indent_columns=2 --set code_width=60 --set nl_oc_mdef_brace=force --set indent_switch_case=2 --set mod_full_brace_if=add --set nl_max=2 --set sp_after_cast=force --set sp_sparen_brace=force --set sp_after_oc_return_type=force ...` -> method `{` moved to own line; `case` indented 2; `if(a) return;` became `if (a) { return;}`; 3 blank lines collapsed to 1; `(int*) q`; long message send wrapped at 60 columns (wrap quality is poor without `align_oc_msg_colon_span`/`nl_oc_msg_args`).
4. `--set newlines=crlf` on LF input -> `\r\n` (od -c). CRLF input with default `auto` -> CRLF preserved.
5. `--set indent_columns=2 --set indent_columns=6` -> 6 (last wins).

Error reporting (VERIFIED) — stdout empty (0 bytes), message on stderr even with `-q`:

| Case | Exit | stderr |
|---|---|---|
| Unknown option `--set bogus_option=1` | 1 | `Unknown option 'bogus_option' to override.` |
| Bad IARF value `sp_arith=maybe` | 1 | `Option<IARF>: at :0: Expected one of 'ignore', 'add', 'remove', 'force', for 'sp_arith'; got 'maybe'` |
| Bad number `indent_columns=abc` | 1 | `Option<UNUM>: at :0: Expected unsigned number , for 'indent_columns'; got 'abc'` |
| Out of range `indent_with_tabs=7` | 1 | `requested value 7 for option 'indent_with_tabs' is greater than the maximum value 2` |
| `--set` without `=` | 64 | `Error while parsing --set` |
| No `-c` and no env var | 74 | `Specify the config file with '-c file' or set UNCRUSTIFY_CONFIG` |
| stdin without `-l`/`--assume` | 1 | `If reading from stdin, you should specify the language using -l ...` |

Other languages (DOCS `--help`, `-l` list VERIFIED present; C verified via `-l C`): C, CPP, D, CS, JAVA, PAWN, OC, OC+, VALA. Same `--set` mechanism for all.

Docs: https://github.com/uncrustify/uncrustify (README, `documentation/htdocs/`), option reference generated from the same source as `--show-config`: https://github.com/uncrustify/uncrustify/blob/master/documentation/htdocs/default.cfg
