# clang-format (C / C++ / ObjC / Java / JS / C# / Proto) — formatter options research

Legend: **VERIFIED** = executed against the bundled binary on Windows 11 (2026-09-19). **DOCS** = from documentation (URL cited).

## 1. Bundled version — READ THIS FIRST

`C:\Users\login\cs-f\Binaries\clang-format.exe --version` -> `clang-format version 12.0.0` (VERIFIED). 3.4 MB, embedded version string `12.0.0-6923b0a7`.

**It is NOT the 12.0.0 release. It is an early 12.0.0 development snapshot (git 6923b0a7), functionally "clang-format 11 + a few keys".** Evidence (VERIFIED): keys documented for release 12.0.0 are rejected as `unknown key`: `AttributeMacros`, `SpaceAroundPointerQualifiers`, `EmptyLineBeforeAccessModifier`, `IndentRequires`, `PenaltyIndentedWhitespace`, `BreakBeforeConceptDeclarations`, `SortJavaStaticImport`, `StatementAttributeLikeMacros`, `SpaceBeforeCaseColon`. The only post-11 key present is `BitFieldColonSpacing`. The accepted key set == the 11.0.0 docs key set + `BitFieldColonSpacing`.

Consequences:
- The binary is ~6 years old. The task brief's expectation of "~180 keys incl. SpaceBeforeParensOptions, AlignConsecutive* structs, InsertBraces, QualifierAlignment" does not hold: this binary has **114 top-level keys** (109 in `--dump-config` incl. `Language` + 4 accepted-but-not-dumped-when-empty + `BasedOnStyle`), and **one** nested struct (`BraceWrapping`, 18 sub-keys) plus two list-of-struct keys (`IncludeCategories`, `RawStringFormats`).
- Use the **11.0.0** docs as the closest reference: https://releases.llvm.org/11.0.0/tools/clang/docs/ClangFormatStyleOptions.html . Do NOT use the current docs page for enum values; several enums were bools at this version (see section 4.4).
- The only trustworthy source of truth is `--dump-config` of this binary + the empirical probes below. If modern options (InsertBraces, QualifierAlignment, etc.) are wanted in the UI, **upgrade the binary** (current LLVM is 20+); the mechanism in section 2 is unchanged in newer versions.

## 2. Recommended invocation and config-passing mechanism

Mechanism: **a single `--style={...}` argument containing inline YAML flow mapping with `BasedOnStyle` first**. No files. Works with stdin->stdout and `--assume-filename` (VERIFIED).

```text
clang-format.exe --assume-filename=file.cpp "--style={BasedOnStyle: LLVM, IndentWidth: 4, ...}"
```

Exact `ArgumentList` example (2 elements; the braces/spaces are inside ONE element, no extra quoting because no shell is involved):

```text
--assume-filename=file.cpp
--style={BasedOnStyle: Google, IndentWidth: 4, TabWidth: 4, UseTab: Never, ColumnLimit: 100, BreakBeforeBraces: Allman, PointerAlignment: Left, SortIncludes: false, BraceWrapping: {AfterFunction: true, BeforeElse: true}}
```

Rules (VERIFIED unless noted):

- **Always pass an explicit `--style`.** With no `--style`, the default is `--style=file`, which searches for `.clang-format` / `_clang-format` (see section 3). `--style=LLVM` or `--style={...}` performs no file lookup.
- **Always include `BasedOnStyle` in the inline map.** `--style={IndentWidth: 8}` without `BasedOnStyle` starts from LLVM defaults (VERIFIED: output identical to LLVM + IndentWidth 8), so it is deterministic, but being explicit is what the UI preset dropdown maps to.
- Nested struct: `BraceWrapping: {AfterFunction: true}` — only honoured when `BreakBeforeBraces: Custom` (DOCS). Lists: `ForEachMacros: [foreach, Q_FOREACH]`. Strings with regex chars should be quoted: `CommentPragmas: "^ IWYU"`.
- YAML needs the space after each colon (`IndentWidth: 4`, not `IndentWidth:4`).
- Values are case-sensitive for enum members (`Allman`), preset names are case-insensitive (`llvm`, `google` accepted).
- `--assume-filename` selects the language (section 5) and, **only when style=file**, the directory from which `.clang-format` search starts.
- Other useful flags present in this build (`--help`, VERIFIED): `--fallback-style`, `--sort-includes` (force include sorting on), `--lines=a:b`, `--offset/--length`, `--cursor`, `--dump-config`, `--dry-run/-n`, `--Werror`, `--output-replacements-xml`, `--verbose`. No `--qualifier-alignment`, no `--style=file:<path>` (added in 14; DOCS).

Presets at this version (VERIFIED, from `--help` and by probing `--dump-config --style=X`): **LLVM, GNU, Google, Chromium, Microsoft, Mozilla, WebKit**, plus pseudo-style **`none`** (accepted as BasedOnStyle; disables formatting baseline). `InheritParentConfig` is rejected (added in 13). `--style=Nope` -> exit 1, stderr `Invalid value for -style`.

Dump of every option + default for a base style: `clang-format.exe --dump-config --style=LLVM` (raw output saved in `clang-format-dump-config-llvm.yaml`, 150 lines). `--dump-config "--style={BasedOnStyle: Google, IndentWidth: 4}"` shows the effective merged config — handy for debugging the UI.

## 3. Isolation / discovery gotchas

| Finding | Status |
|---|---|
| **Current extension invocation (`--assume-filename=file.c`, no `--style`) is cwd-dependent.** With a `.clang-format` (`IndentWidth: 8`) in the parent of the process cwd, stdin output used 8-space indents. | VERIFIED |
| `_clang-format` (underscore variant) in cwd is also picked up. | VERIFIED |
| With style=file, the search starts at the directory of `--assume-filename` if it has a path (absolute path into a dir with `.clang-format` applied that config while cwd was elsewhere; absolute path to `C:/Windows/Temp/file.c` from a cwd that had a config -> config NOT applied). Bare filename -> cwd and its parents. | VERIFIED |
| A discovered `.clang-format` containing a key this old binary does not know (e.g. `InsertBraces: true`, very common in modern repos) makes the run **fail**: exit 1, `error: unknown key 'InsertBraces'`, `Error reading <path>\.clang-format: invalid argument`, empty stdout. | VERIFIED |
| Passing `--style=LLVM` or `--style={...}` in the same directory ignores the file entirely (exit 0, LLVM output). | VERIFIED |
| `--fallback-style=<preset>` only matters with `--style=file` when no file is found (default fallback LLVM). `--fallback-style=none` + no file -> input echoed unformatted. Not needed when style is explicit. | VERIFIED |
| No env vars or user-level config are consulted. | DOCS |
| `.clang-format-ignore` does not exist at this version (added in 18). | DOCS |

**Recommendation:** always emit `--style={BasedOnStyle: <preset>, ...}` — even when the user changed nothing (`--style={BasedOnStyle: LLVM}` or plain `--style=LLVM`).

## 4. Options reference

### 4.1 Headline options (proposed UI set, all exist in this binary)

All are passed as `Key: Value` inside the single `--style={...}` argument.

| Key | Type | LLVM default | Values (VERIFIED accepted by this binary) | Meaning |
|---|---|---|---|---|
| `BasedOnStyle` | enum | (LLVM) | LLVM, Google, Chromium, Mozilla, WebKit, Microsoft, GNU, none | Preset everything else overrides. |
| `IndentWidth` | uint | 2 | int | Columns per indent level. |
| `TabWidth` | uint | 8 | int | Columns per tab stop. |
| `UseTab` | enum | Never | Never, ForIndentation, ForContinuationAndIndentation, AlignWithSpaces, Always (legacy true/false) | Tab usage. |
| `ColumnLimit` | uint | 80 | int, 0 = no limit (-1 rejected) | Wrap column. |
| `ContinuationIndentWidth` | uint | 4 | int | Indent for continuation lines. |
| `BreakBeforeBraces` | enum | Attach | Attach, Linux, Mozilla, Stroustrup, Allman, Whitesmiths, GNU, WebKit, Custom | Brace style. `Custom` -> uses `BraceWrapping`. |
| `PointerAlignment` | enum | Right | Left, Right, Middle | `int* p` / `int *p` / `int * p`. |
| `DerivePointerAlignment` | bool | false | true/false | Infer pointer alignment from the input (overrides PointerAlignment; Google preset = true!). |
| `AccessModifierOffset` | int | -2 | int (negative ok) | Offset of `public:` etc. |
| `NamespaceIndentation` | enum | None | None, Inner, All | Indent namespace bodies. |
| `IndentCaseLabels` | bool | false | true/false | Indent `case` inside `switch`. |
| `IndentPPDirectives` | enum | None | None, AfterHash, BeforeHash | Preprocessor indent. |
| `AllowShortFunctionsOnASingleLine` | enum | All | None, InlineOnly, Empty, Inline, All | `int f() { return 0; }` on one line. |
| `AllowShortIfStatementsOnASingleLine` | enum | Never | Never, WithoutElse, Always (OnlyFirstIf/AllIfsAndElse REJECTED — 13+) | `if (a) return;` |
| `AllowShortLoopsOnASingleLine` | bool | false | true/false | `while (x) y();` |
| `AllowShortBlocksOnASingleLine` | enum | Never | Never, Empty, Always | `while (x) {}` |
| `AllowShortCaseLabelsOnASingleLine` | bool | false | true/false | `case 1: x = 1; break;` |
| `AllowShortLambdasOnASingleLine` | enum | All | None, Empty, Inline, All | Lambdas. |
| `AllowShortEnumsOnASingleLine` | bool | true | true/false | `enum { A, B } e;` |
| `SpaceBeforeParens` | enum | ControlStatements | Never, ControlStatements, ControlStatementsExceptForEachMacros, NonEmptyParentheses, Always (`Custom`, `ControlStatementsExceptControlMacros` REJECTED) | Space before `(`. |
| `SortIncludes` | **bool** | true | true/false ONLY (`Never`/`CaseSensitive`/`CaseInsensitive` REJECTED: "invalid boolean") | Sort `#include` blocks. |
| `AlignConsecutiveAssignments` | **bool** | false | true/false ONLY (`Consecutive`, `AcrossEmptyLines`... REJECTED) | Align `=` on consecutive lines. |
| `AlignConsecutiveDeclarations` | **bool** | false | true/false | Align declaration names. |
| `AlignTrailingComments` | **bool** | true | true/false | Align `//` comments. |
| `BinPackArguments` | bool | true | true/false | false = one argument per line when wrapping. |
| `BinPackParameters` | bool | true | true/false | Same for declarations. |
| `Cpp11BracedListStyle` | bool | true | true/false | `{1, 2}` vs `{ 1, 2 }`. |
| `MaxEmptyLinesToKeep` | uint | 1 | int | Collapse blank lines. |
| `ReflowComments` | bool | true | true/false | Re-wrap long comments. |
| `FixNamespaceComments` | bool | true | true/false | Add `// namespace x`. |
| `Standard` | enum | Latest | c++03, c++11, c++14, c++17, c++20, Latest, Auto (aliases Cpp03, Cpp11, C++11; `c++23` REJECTED) | Parsing/`>>` style. |
| `UseCRLF` + `DeriveLineEnding` | bool, bool | false, true | true/false | Line endings: derive from input by default; set `DeriveLineEnding: false, UseCRLF: true|false` to force. (Replaced by `LineEnding` in 16 — REJECTED here.) |

Requested in the brief but **NOT available in this binary** (VERIFIED `unknown key`, exit 1): `InsertBraces` (15), `QualifierAlignment` (14), `SpaceBeforeParensOptions` (14), `PackConstructorInitializers` (14), `ReferenceAlignment` (13), `SeparateDefinitionBlocks` (14), `InsertNewlineAtEOF` (16), `LineEnding` (16), `ShortNamespaceLines` (13), `IndentAccessModifiers` (13), `EmptyLineAfterAccessModifier` (13), `EmptyLineBeforeAccessModifier` (12), `LambdaBodyIndentation` (13), `SpacesInLineCommentPrefix` (13), `IfMacros` (13), `AlignArrayOfStructures` (13), `AttributeMacros` (12), `SpaceAroundPointerQualifiers` (12), `SpaceBeforeCaseColon` (12), `IndentRequires` (12), `PenaltyIndentedWhitespace` (12), `BreakBeforeConceptDeclarations` (12), `SortJavaStaticImport` (12), `StatementAttributeLikeMacros` (12). Version numbers = release that introduced the key (DOCS: https://clang.llvm.org/docs/ClangFormatStyleOptions.html "clang-format N" badges).

### 4.2 ALL top-level keys in this binary (from `--dump-config --style=LLVM`, VERIFIED) — 109 dumped + 5 accepted-but-not-dumped (114 total)

Types: b=bool, u=unsigned, i=int, e=enum, s=string, [s]=string list, struct, [struct]. Enum values listed were each probed against the binary (accepted = listed). Legacy `true`/`false` are additionally accepted by most formerly-bool enums (AlignAfterOpenBracket, AlignEscapedNewlines, AlignOperands, AllowShortBlocks/Functions/If/Lambdas, AlwaysBreakAfterDefinitionReturnType, AlwaysBreakTemplateDeclarations, BreakBeforeBinaryOperators, IndentExternBlock, SpaceBeforeParens, UseTab, BraceWrapping.AfterControlStatement). H = headline.

| Key | Type | LLVM default | Values | Meaning | H |
|---|---|---|---|---|---|
| Language | e | Cpp | Must match detected language (see 5); any other value -> `Error parsing -style: Unsuitable`. Do not set. | Language this config section applies to | |
| BasedOnStyle | e | — | LLVM, Google, Chromium, Mozilla, WebKit, Microsoft, GNU, none | Base preset (not dumped; shown as comment) | H |
| AccessModifierOffset | i | -2 | | Indent offset of access modifiers | H |
| AlignAfterOpenBracket | e | Align | Align, DontAlign, AlwaysBreak (BlockIndent rejected) | Align args after `(` | |
| AlignConsecutiveMacros | b | false | true/false only | Align consecutive `#define` values | |
| AlignConsecutiveAssignments | b | false | true/false only | Align `=` | H |
| AlignConsecutiveBitFields | b | false | true/false | Align bitfield `:` | |
| AlignConsecutiveDeclarations | b | false | true/false | Align declared names | H |
| AlignEscapedNewlines | e | Right | DontAlign, Left, Right | Align `\` in macros | |
| AlignOperands | e | Align | DontAlign, Align, AlignAfterOperator | Align operands of wrapped binary expr | |
| AlignTrailingComments | b | true | true/false only | Align trailing comments | H |
| AllowAllArgumentsOnNextLine | b | true | | Allow all call args on next line | |
| AllowAllConstructorInitializersOnNextLine | b | true | | (deprecated in 14) | |
| AllowAllParametersOfDeclarationOnNextLine | b | true | | Allow all decl params on next line | |
| AllowShortEnumsOnASingleLine | b | true | | | H |
| AllowShortBlocksOnASingleLine | e | Never | Never, Empty, Always | | H |
| AllowShortCaseLabelsOnASingleLine | b | false | | | H |
| AllowShortFunctionsOnASingleLine | e | All | None, InlineOnly, Empty, Inline, All | | H |
| AllowShortLambdasOnASingleLine | e | All | None, Empty, Inline, All | | H |
| AllowShortIfStatementsOnASingleLine | e | Never | Never, WithoutElse, Always | | H |
| AllowShortLoopsOnASingleLine | b | false | | | H |
| AlwaysBreakAfterDefinitionReturnType | e | None | None, All, TopLevel | Deprecated alias of next | |
| AlwaysBreakAfterReturnType | e | None | None, All, TopLevel, AllDefinitions, TopLevelDefinitions | Return type on own line | |
| AlwaysBreakBeforeMultilineStrings | b | false | | | |
| AlwaysBreakTemplateDeclarations | e | MultiLine | No, MultiLine, Yes | Break after `template<...>` | |
| BinPackArguments | b | true | | | H |
| BinPackParameters | b | true | | | H |
| BraceWrapping | struct | see 4.3 | | Per-construct brace placement (needs BreakBeforeBraces: Custom) | |
| BreakBeforeBinaryOperators | e | None | None, NonAssignment, All | Wrap before/after binary ops | |
| BreakBeforeBraces | e | Attach | Attach, Linux, Mozilla, Stroustrup, Allman, Whitesmiths, GNU, WebKit, Custom | | H |
| BreakBeforeInheritanceComma | b | false | | Legacy; use BreakInheritanceList | |
| BreakInheritanceList | e | BeforeColon | BeforeColon, BeforeComma, AfterColon (AfterComma rejected) | | |
| BreakBeforeTernaryOperators | b | true | | | |
| BreakConstructorInitializersBeforeComma | b | false | | Legacy | |
| BreakConstructorInitializers | e | BeforeColon | BeforeColon, BeforeComma, AfterColon | | |
| BreakAfterJavaFieldAnnotations | b | false | | Java | |
| BreakStringLiterals | b | true | | Split long string literals | |
| ColumnLimit | u | 80 | 0 = unlimited | | H |
| CommentPragmas | s (regex) | `^ IWYU pragma:` | | Comments never to touch | |
| CompactNamespaces | b | false | | `namespace a { namespace b {` on one line | |
| ConstructorInitializerAllOnOneLineOrOnePerLine | b | false | | | |
| ConstructorInitializerIndentWidth | u | 4 | | | |
| ContinuationIndentWidth | u | 4 | | | H |
| Cpp11BracedListStyle | b | true | | | H |
| DeriveLineEnding | b | true | | Detect CRLF/LF from input | |
| DerivePointerAlignment | b | false | | | H |
| DisableFormat | b | false | | Turn formatting off | |
| ExperimentalAutoDetectBinPacking | b | false | | Do not expose | |
| FixNamespaceComments | b | true | | | H |
| ForEachMacros | [s] | foreach, Q_FOREACH, BOOST_FOREACH | | Macros parsed as loops | |
| IncludeBlocks | e | Preserve | Preserve, Merge, Regroup | How include blocks are regrouped | |
| IncludeCategories | [struct {Regex s, Priority i, SortPriority i}] | 3 LLVM entries (see dump) | | Include ordering categories | |
| IncludeIsMainRegex | s | `(Test)?$` | | | |
| IncludeIsMainSourceRegex | s | `''` | | | |
| IndentCaseLabels | b | false | | | H |
| IndentCaseBlocks | b | false | | Indent `{}` blocks under case | |
| IndentGotoLabels | b | true | | | |
| IndentPPDirectives | e | None | None, AfterHash, BeforeHash | | H |
| IndentExternBlock | e | AfterExternBlock | AfterExternBlock, NoIndent, Indent | | |
| IndentWidth | u | 2 | | | H |
| IndentWrappedFunctionNames | b | false | | | |
| InsertTrailingCommas | e | None | None, Wrapped (JS only; `Wrapped` requires `BinPackArguments: false` else "trailing comma insertion cannot be used with bin packing") | | |
| JavaScriptQuotes | e | Leave | Leave, Single, Double | JS | |
| JavaScriptWrapImports | b | true | | JS | |
| KeepEmptyLinesAtTheStartOfBlocks | b | true | | | |
| MacroBlockBegin | s (regex) | `''` | | | |
| MacroBlockEnd | s (regex) | `''` | | | |
| MaxEmptyLinesToKeep | u | 1 | | | H |
| NamespaceIndentation | e | None | None, Inner, All | | H |
| ObjCBinPackProtocolList | e | Auto | Auto, Always, Never | ObjC | |
| ObjCBlockIndentWidth | u | 2 | | ObjC | |
| ObjCBreakBeforeNestedBlockParam | b | true | | ObjC | |
| ObjCSpaceAfterProperty | b | false | | ObjC `@property (x)` | |
| ObjCSpaceBeforeProtocolList | b | true | | ObjC `Foo <Proto>` | |
| PenaltyBreakAssignment | u | 2 | | Penalties: advanced, do not expose | |
| PenaltyBreakBeforeFirstCallParameter | u | 19 | | | |
| PenaltyBreakComment | u | 300 | | | |
| PenaltyBreakFirstLessLess | u | 120 | | | |
| PenaltyBreakString | u | 1000 | | | |
| PenaltyBreakTemplateDeclaration | u | 10 | | | |
| PenaltyExcessCharacter | u | 1000000 | | | |
| PenaltyReturnTypeOnItsOwnLine | u | 60 | | | |
| PointerAlignment | e | Right | Left, Right, Middle | | H |
| ReflowComments | b | true | | | H |
| SortIncludes | b | true | true/false only | | H |
| SortUsingDeclarations | b | true | | | |
| SpaceAfterCStyleCast | b | false | | `(int) x` | |
| SpaceAfterLogicalNot | b | false | | `! x` | |
| SpaceAfterTemplateKeyword | b | true | | `template <` | |
| SpaceBeforeAssignmentOperators | b | true | | | |
| SpaceBeforeCpp11BracedList | b | false | | `Foo {` | |
| SpaceBeforeCtorInitializerColon | b | true | | | |
| SpaceBeforeInheritanceColon | b | true | | | |
| SpaceBeforeParens | e | ControlStatements | Never, ControlStatements, ControlStatementsExceptForEachMacros, NonEmptyParentheses, Always | | H |
| SpaceBeforeRangeBasedForLoopColon | b | true | | | |
| SpaceInEmptyBlock | b | false | | `{ }` | |
| SpaceInEmptyParentheses | b | false | | `( )` | |
| SpacesBeforeTrailingComments | u | 1 | | | |
| SpacesInAngles | b | false | true/false only (Never/Always/Leave rejected) | `< int >` | |
| SpacesInConditionalStatement | b | false | | `if ( a )` | |
| SpacesInContainerLiterals | b | true | | ObjC/JS literals | |
| SpacesInCStyleCastParentheses | b | false | | | |
| SpacesInParentheses | b | false | | `f( a )` | |
| SpacesInSquareBrackets | b | false | | | |
| SpaceBeforeSquareBrackets | b | false | | | |
| BitFieldColonSpacing | e | Both | Both, None, Before, After | | |
| Standard | e | Latest | c++03, c++11, c++14, c++17, c++20, Latest, Auto | | H |
| StatementMacros | [s] | Q_UNUSED, QT_REQUIRE_VERSION | | | |
| TabWidth | u | 8 | | | H |
| UseCRLF | b | false | | Only when DeriveLineEnding false / undetectable | H |
| UseTab | e | Never | Never, ForIndentation, ForContinuationAndIndentation, AlignWithSpaces, Always | | H |
| WhitespaceSensitiveMacros | [s] | STRINGIZE, PP_STRINGIZE, BOOST_PP_STRINGIZE | | | |
| TypenameMacros | [s] | (empty; not dumped) | accepted VERIFIED | | |
| NamespaceMacros | [s] | (empty; not dumped) | accepted VERIFIED | | |
| JavaImportGroups | [s] | (empty; not dumped) | accepted VERIFIED | Java | |
| RawStringFormats | [struct {Language, Delimiters, EnclosingFunctions, CanonicalDelimiter, BasedOnStyle}] | empty for LLVM (Google/Chromium define 2) | accepted VERIFIED | | |

### 4.3 Nested struct `BraceWrapping` (only effective with `BreakBeforeBraces: Custom`)

| Sub-key | Type | LLVM default | Notes |
|---|---|---|---|
| AfterCaseLabel | b | false | |
| AfterClass | b | false | |
| AfterControlStatement | e | Never | Never, MultiLine, Always (true/false accepted) — VERIFIED |
| AfterEnum | b | false | |
| AfterFunction | b | false | |
| AfterNamespace | b | false | |
| AfterObjCDeclaration | b | false | |
| AfterStruct | b | false | |
| AfterUnion | b | false | |
| AfterExternBlock | b | false | |
| BeforeCatch | b | false | |
| BeforeElse | b | false | |
| BeforeLambdaBody | b | false | |
| BeforeWhile | b | false | |
| IndentBraces | b | false | |
| SplitEmptyFunction | b | true | |
| SplitEmptyRecord | b | true | |
| SplitEmptyNamespace | b | true | |

CLI form: `--style={BasedOnStyle: LLVM, BreakBeforeBraces: Custom, BraceWrapping: {AfterFunction: true, BeforeElse: true}}`.

### 4.4 Docs-vs-binary traps (VERIFIED)

Keys that are enums/structs in current docs but **plain bools here**: `AlignConsecutiveMacros/Assignments/BitFields/Declarations`, `AlignTrailingComments`, `SortIncludes`, `SpacesInAngles`. A UI built from current docs would produce `invalid boolean` errors.

### 4.5 Preset differences from LLVM (VERIFIED via `--dump-config` diffs; scalar keys only)

- **Google**: AccessModifierOffset -1; AlignEscapedNewlines Left; AllowShortIfStatementsOnASingleLine WithoutElse; AllowShortLoopsOnASingleLine true; AlwaysBreakBeforeMultilineStrings true; AlwaysBreakTemplateDeclarations Yes; ConstructorInitializerAllOnOneLineOrOnePerLine true; **DerivePointerAlignment true**; IncludeBlocks Regroup; IndentCaseLabels true; KeepEmptyLinesAtTheStartOfBlocks false; ObjCBinPackProtocolList Never; PointerAlignment Left; SpacesBeforeTrailingComments 2; Standard Auto; own IncludeCategories + RawStringFormats.
- **Chromium**: Google plus AllowAllParametersOfDeclarationOnNextLine false; AllowShortFunctionsOnASingleLine Inline; BinPackParameters false; no short ifs/loops; DerivePointerAlignment false; IncludeBlocks Preserve.
- **Mozilla**: BreakBeforeBraces Mozilla; AlwaysBreakAfterReturnType TopLevel; BinPackArguments/Parameters false; BreakConstructorInitializers/BreakInheritanceList BeforeComma; ConstructorInitializerIndentWidth 2; ContinuationIndentWidth 2; Cpp11BracedListStyle false; FixNamespaceComments false; IndentCaseLabels true; PointerAlignment Left; SpaceAfterTemplateKeyword false.
- **WebKit**: IndentWidth 4; ColumnLimit 0; BreakBeforeBraces WebKit; AccessModifierOffset -4; AlignAfterOpenBracket DontAlign; AlignOperands DontAlign; AlignTrailingComments false; AllowShortBlocksOnASingleLine Empty; BreakBeforeBinaryOperators All; BreakConstructorInitializers BeforeComma; Cpp11BracedListStyle false; FixNamespaceComments false; NamespaceIndentation Inner; PointerAlignment Left; SpaceBeforeCpp11BracedList true; SpaceInEmptyBlock true.
- **Microsoft**: IndentWidth 4; TabWidth 4; ColumnLimit 120; BreakBeforeBraces Custom (Allman-like BraceWrapping); AllowShortFunctionsOnASingleLine None; AllowShortEnumsOnASingleLine false.
- **GNU**: BreakBeforeBraces GNU; ColumnLimit 79; AlwaysBreakAfterReturnType AllDefinitions; BreakBeforeBinaryOperators All; SpaceBeforeParens Always; Cpp11BracedListStyle false; FixNamespaceComments false; Standard c++03.

UI implication: a UI control must show the *preset's* default, not the LLVM default, when the user has not overridden a key. Cheapest correct approach: run `--dump-config --style=<preset>` once per preset at startup (takes ms) and read defaults from it.

## 5. Other languages through the same binary (`--assume-filename`, VERIFIED with `--style=LLVM`)

| Filename | Detected `Language` (from `--dump-config`) | Result |
|---|---|---|
| `a.c`, `a.cpp`, `a.h` | Cpp | OK |
| `a.m`, `a.mm` | ObjC | OK — `@interface A : NSObject`, `- (void)foo:(int)x bar:(int)y;`, message sends formatted. **Viable alternative to uncrustify for Objective-C with far better defaults.** `.h` with ObjC content formatted fine (language sniffing for headers is DOCS, not separately verified). |
| `A.java` | Java | OK |
| `a.js`, `a.ts` | JavaScript | OK (use `BasedOnStyle: Google` for idiomatic JS; LLVM gives `a : 1`) |
| `a.cs` | CSharp | OK (basic; C# support was young in 11/12) |
| `a.proto` | Proto | OK |
| `a.textproto` | TextProto | OK |
| `a.td` | (TableGen) | OK |
| `a.json` | **Cpp (not supported)** — JSON language added in 13 | Output is C++-style garbage (`{ "a" : 1, "b" : [ 1, 2 ] }`). Do not use. |
| `a.v` / `a.sv` | **Cpp (not supported)** — Verilog added in 17 | Do not use. |

`Language:` inside an inline `--style` must equal the detected language or the run fails (`Error parsing -style: Unsuitable`, exit 1). Just omit it.

## 6. Verified notes

Input `in.cpp`:

```cpp
#include <vector>
#include <algorithm>
namespace n { class A { public: int* p; int x=1; int longer = 2;
void f(int a,int b){ if(a) return; for(int i=0;i<b;i++) g(i);
switch(a){case 1: break;} } }; }
```

| # | Args | Observed |
|---|---|---|
| 1 | `--assume-filename=file.cpp` (+LLVM) | includes sorted (algorithm before vector), 2-space indent, `int *p;`, attached braces, `} // namespace n`. |
| 2 | `--style={BasedOnStyle: LLVM, IndentWidth: 4, BreakBeforeBraces: Allman, PointerAlignment: Left, SortIncludes: false, IndentCaseLabels: true, NamespaceIndentation: All, AlignConsecutiveAssignments: true, AccessModifierOffset: -4}` | include order preserved; all braces on own line; `int* p;`; `int x      = 1;` aligned with `int longer = 2;`; class indented inside namespace; `case` indented; `public:` flush with `class`. (8 options each visibly effective.) |
| 3 | `--style={BasedOnStyle: Google, ColumnLimit: 40, UseTab: Always, TabWidth: 4, IndentWidth: 4, AllowShortIfStatementsOnASingleLine: Never, SpaceBeforeParens: Always}` | tab indentation; `void f (int a, int b)`, `g (i);`; `}  // namespace n` (Google 2 spaces). |
| 4 | `--style={BasedOnStyle: LLVM, ColumnLimit: 30, ReflowComments: false, BinPackArguments: false, MaxEmptyLinesToKeep: 0, Cpp11BracedListStyle: false}` | long comment left over-width; call args one per line; 3 blank lines removed; `v{ 1, 2 }`. |
| 5 | `--style={BasedOnStyle: LLVM, DeriveLineEnding: false, UseCRLF: true}` on LF input | CRLF output (`od -c`). CRLF input + plain LLVM -> CRLF preserved (DeriveLineEnding). |

Error reporting (VERIFIED) — **exit code 1, stdout empty (0 bytes), diagnostics on stderr**:

| Case | stderr |
|---|---|
| Unknown key | `YAML:1:29: error: unknown key 'Bogus'` + echo of the style string with caret + `Error parsing -style: invalid argument` |
| Bad enum value | `YAML:1:21: error: unknown enumerated scalar` ... |
| Bad number | `error: invalid number` |
| Enum given to a bool key | `error: invalid boolean` |
| Bad preset name | `Invalid value for -style` |
| Semantic conflict | `Error parsing -style: trailing comma insertion cannot be used with bin packing`; `Error parsing -style: Unsuitable` (Language mismatch) |
| Unparsable source code | **No error**: exit 0, best-effort output. clang-format never reports syntax errors. |

One unknown key fails the whole run, so the UI's option table must be exactly matched to the bundled binary version (section 4.2), or probed at startup via `--dump-config`.
