# dprint: invocation, config passing, and plugin options

Research date: 2026-09-19. Platform: Windows 11, bundled binary `Binaries\dprint.exe`.
Legend: **VERIFIED** = executed against the bundled binary during this research. **DOCS** = taken from documentation/schema (URL cited), not executed.

Scope of this file: dprint core + the five official plugins pinned in `Services/ConfigManager.cs`
(typescript-0.95.13, json-0.21.0, markdown-0.20.0, toml-0.7.0, dockerfile-0.3.3).
The g-plane plugins (malva, markup_fmt, pretty_yaml, pretty_graphql) use the same mechanism but their option lists are out of scope here. Their config section keys are `malva`, `markup`, `yaml`, `graphql` (VERIFIED from `plugin-cache-manifest.json`).

## 1. Bundled version

| Item | Value | Status |
|---|---|---|
| `dprint --version` | `dprint 0.50.2` | VERIFIED |
| Latest upstream at research time | 0.57.4 (printed by `dprint help`) | VERIFIED |
| Wasm cache version | `6.1.0-rc.3` (compiled-plugin file suffix `-6.1.0-rc.3-x86_64-<hash>`) | VERIFIED |
| Plugin cache manifest schema | `schemaVersion: 8` | VERIFIED |

## 2. Invocation and config-passing mechanism

### 2.1 Summary

- There is **no inline/CLI config override**. `dprint fmt --help` and `dprint help` list only `--config`, `--config-discovery`, `--plugins`, `--log-level`, the includes/excludes flags, `--incremental`, `--stdin`, `--diff`, `--staged`, `--allow-no-files`, `--allow-node-modules`. No `--set`/`-o key=value` style flag, and no env var carries config values. VERIFIED.
- Env vars recognised (from `dprint help`): `DPRINT_CACHE_DIR`, `DPRINT_MAX_THREADS`, `DPRINT_CONFIG_DISCOVERY`, `DPRINT_CERT`, `DPRINT_TLS_CA_STORE`, `DPRINT_IGNORE_CERTS`, `HTTPS_PROXY`/`HTTP_PROXY`/`NO_PROXY`. None pass plugin options. VERIFIED.
- The only way to pass options is a JSON(C) config file given with `--config <path>`. VERIFIED.
- `--plugins <url>` **can be combined with `--config`**; the CLI list overrides (replaces) any `plugins` array in the file, and the file does not need a `plugins` array at all in that case. VERIFIED.

### 2.2 Recommended command line

```
dprint.exe fmt --stdin file.ts --config "<tempdir>\dprint-<guid>.json" --plugins https://plugins.dprint.dev/typescript-0.95.13.wasm
```

i.e. keep the existing base args from `CreateDefaultConfig` unchanged and insert `--config <abs path>`. Source on stdin (UTF-8), result on stdout, diagnostics on stderr.

When the user has **no** settings to apply, still isolate from stray configs with either an empty config (`{}`) via `--config`, or `--config-discovery=false` (see 2.4). Recommended: always pass `--config`, so there is a single code path.

Minimal generated config (one plugin section; key = plugin's config key):

```json
{
  "typescript": {
    "lineWidth": 100,
    "indentWidth": 4,
    "quoteStyle": "preferSingle",
    "semiColons": "asi"
  }
}
```

Verified run (cwd = empty scratch dir):

```
$ printf 'const a = "x"\nfunction f( a,b ) { return a+b }\n' | dprint fmt --stdin file.ts --config ../cfg/ts-noplug.json --plugins https://plugins.dprint.dev/typescript-0.95.13.wasm
const a = 'x'
function f(a, b) {
  return a + b
}
exit=0
```

Config section keys: `typescript` (also used for .js/.jsx/.tsx/.mjs/.cjs), `json`, `markdown`, `toml`, `dockerfile`. VERIFIED via `output-resolved-config`.

A useful helper for development/tests: `dprint output-resolved-config --config <cfg> --plugins <url>` prints the fully-expanded effective config as JSON (exit 0), or the diagnostics if the config is invalid. VERIFIED. It can be used to validate a generated config without formatting anything.

### 2.3 `--config` + `--plugins` combinations (all VERIFIED)

| Config file has `plugins`? | `--plugins` on CLI? | Result |
|---|---|---|
| yes | no | Works; plugin options applied. |
| no | yes | Works; plugin options applied. **Recommended** (keeps URL pinning in `ConfigManager.cs`). |
| yes | yes | Works; CLI list wins (help text: "This overrides what is specified in the config file"). |
| no | no | Fails: `No formatting plugins found. Ensure at least one is specified in the 'plugins' array of the configuration file.` exit **13**. |

- `--config` accepts relative paths, Windows absolute paths (`C:\...\x.json`), and (per help text) URLs. Relative and absolute VERIFIED; URL is DOCS (`dprint help`).
- Config file is parsed as JSONC: comments and trailing commas are accepted. VERIFIED.
- **A UTF-8 BOM in the config file breaks parsing**: `Error deserializing. Unexpected token on line 1 column 1`, exit **11**. VERIFIED. In C#, write with `new UTF8Encoding(false)` (or `File.WriteAllText(path, text)` without an encoding argument); do NOT use `Encoding.UTF8`.
- A section for a plugin that is not loaded (e.g. a `json` section while only the typescript plugin is in `--plugins`) is silently ignored, exit 0. VERIFIED. So one shared config file containing all sections is viable, but a per-invocation file with just the relevant section is simpler and avoids races when settings change.

### 2.4 Config discovery (the current code is exposed to this) - all VERIFIED

- Without `--config`, dprint looks for `dprint.json`, `dprint.jsonc`, `.dprint.json`, `.dprint.jsonc` in the cwd and then in **every ancestor directory up to the drive root** (seen with `-L debug`: it probed cwd, each parent, `C:\Users\login\`, `C:\Users\`, `C:\`).
- A discovered config IS applied to `--stdin` formatting even when `--plugins` is on the CLI. Test: `disc/dprint.json` = `{ "typescript": { "quoteStyle": "preferSingle" } }`; running today's exact extension command from `disc/child/grand/` produced `const a = 'x';` instead of `const a = "x";`. A `.dprint.jsonc` in a parent gave the same result.
- Consequence: **today's invocation (no `--config`) can be silently altered by a stray dprint config in the process cwd or any ancestor** (e.g. the user's home directory or a repo root, depending on DevToys' working directory). A discovered config with an unknown key would make every format fail with exit 1.
- Three verified ways to prevent it:
  1. `--config <explicit path>`: discovery is skipped, only that file is used (tested from the same `grand/` dir: the ancestor config was ignored).
  2. `--config-discovery=false` (flag; must use `=`): output back to defaults.
  3. Env var `DPRINT_CONFIG_DISCOVERY=false` (also `0` per help): same result.
- Also worth setting the process working directory to a neutral directory (e.g. the temp dir) as defence in depth.

### 2.5 `--stdin <arg>` semantics - all VERIFIED unless noted

Help text: "Provide an absolute file path to apply the inclusion and exclusion rules or an extension or file name to always format the text."

| `--stdin` argument | Behaviour |
|---|---|
| `file.ts` (bare file name) | Always formatted. Config `includes`/`excludes` are NOT applied (tested with `"includes": ["src/**/*.js"], "excludes": ["**/*.ts"]`: still formatted). |
| `ts` (bare extension) | Always formatted. |
| `sub/dir/file.ts` (relative path, nonexistent) | Formatted; `excludes: ["**/*.ts"]` was NOT applied. |
| Absolute path that does not exist | **Error**: `Error canonicalizing path ...: The system cannot find the file specified. (os error 2)`, exit 1. |
| Absolute path that exists and matches `excludes` | Input echoed back **unformatted**, exit 0, empty stderr. |
| Absolute path that exists, outside the config's directory | Formatted with the config's options. |
| Unknown extension (`file.xyz`) | Input echoed back **unformatted**, exit 0, empty stderr (silent no-op). |

Recommendation: keep using a bare fake file name (`file.ts`, `file.json`, `Dockerfile`, ...). It does not need to be absolute or under the config directory, and it makes includes/excludes irrelevant. Never pass an absolute path.

The file name still matters for plugin selection and for file-name-sensitive plugin behaviour:
- TOML `cargo.applyConventions` only triggers when the name is `Cargo.toml` (VERIFIED: `--stdin Cargo.toml` sorted `[package]`/`[dependencies]` keys; `--stdin file.toml` did not). With the extension's `file.toml` this option is a no-op.
- JSON `trailingCommas: "jsonc"` adds trailing commas only for `.jsonc` names (and names listed in `jsonTrailingCommaFiles`). DOCS (schema description); `.jsonc` + `"always"` VERIFIED.
- Dockerfile plugin matched both `Dockerfile` and `my.dockerfile`. VERIFIED.
- A plugin-level `"associations": ["**/*.foo"]` key exists to map extra file patterns to a plugin; VERIFIED with `--stdin file.foo` being formatted by the typescript plugin. Not needed by the extension.

### 2.6 Global keys vs per-plugin keys - all VERIFIED

- Top-level (global) keys: `lineWidth`, `indentWidth`, `useTabs`, `newLineKind`. They are handed to every plugin as defaults. (Other top-level keys: `plugins`, `includes`, `excludes`, `extends`, `incremental` - DOCS https://dprint.dev/config/.)
- Precedence: **plugin section beats global**. `{ "indentWidth": 8, "typescript": { "indentWidth": 3 } }` gave 3-space indent; `{ "indentWidth": 8 }` alone gave 8.
- A global key that a plugin does not support is silently dropped: global `indentWidth`/`useTabs` with the dockerfile plugin is fine, and only `lineWidth` reached it. But the same key inside the plugin section is an error (`"dockerfile": {"indentWidth": 4}` -> `Unknown property in configuration (indentWidth)`).
- Unknown top-level keys (`"bogusGlobal": 1`) are silently ignored, exit 0.
- Within the typescript plugin, a specific key beats its umbrella key: `"trailingCommas": "never", "arguments.trailingCommas": "always"` resolved to `arguments=always`, `parameters=never`.
- Recommendation for the extension: write everything inside the plugin section (thin wrapper, one namespace, and typos are reported instead of being silently ignored).

### 2.7 Error reporting - all VERIFIED

On any plugin config diagnostic, stdout is empty and exit code is **1**. stderr examples (exact text):

| Case | stderr | Exit |
|---|---|---|
| Unknown key in plugin section | `[dprint-plugin-typescript]: Unknown property in configuration (bogusKey)` / `[dprint-plugin-typescript]: Error initializing from configuration file. Had 1 diagnostic(s).` / `Had 1 configuration errors.` | 1 |
| Invalid enum value | `[dprint-plugin-typescript]: Found invalid value 'nope'. (quoteStyle)` + same two trailer lines | 1 |
| Wrong type (`"lineWidth": "abc"`) | `[dprint-plugin-typescript]: invalid digit found in string (lineWidth)` + trailer | 1 |
| Malformed JSON | `Error deserializing. Unterminated object on line 1 column 17` / `    at <config path>` | 11 |
| Config file with UTF-8 BOM | `Error deserializing. Unexpected token on line 1 column 1` | 11 |
| Config path does not exist | `Error canonicalizing path ...: The system cannot find the file specified. (os error 2)` | 11 |
| No plugins anywhere | `No formatting plugins found. ...` | 13 |
| Plugin download fails (offline, uncached) | `Error resolving plugin <url>: Error downloading <url> - Error: ...: Connection Failed: ...` | 12 |
| Source syntax error | `Unexpected token \`=\`. Expected yield, an identifier, [ or { at file:///file.ts:1:7` + code frame | 1 |
| Unknown top-level key / section for an unloaded plugin | (nothing) | 0 |
| Unknown file extension or excluded absolute path | (nothing; input echoed unchanged) | 0 |

Format of plugin diagnostics is stable enough to parse: `[<plugin-name>]: <message> (<key>)`.

Note `"quoteStyle": "single"` / `"double"` are invalid (`Found invalid value 'single'`); the real values are `alwaysDouble|alwaysSingle|preferDouble|preferSingle`. The current `Models/FormatterSettings.cs` TypeScript choices `["double","single",...]` and default `"double"` would produce exit 1 once wired. Likewise its JSON `trailingCommas` choices omit `maintain`, and its `newLineKind` default is right (`lf`) but see 2.9.

### 2.8 Plugin cache, offline behaviour, startup cost - all VERIFIED

- Default cache: `%LOCALAPPDATA%\dprint\cache\` (`plugin-cache-manifest.json`, `plugins\<plugin-name>\<ver>-<wasmCacheVer>-x86_64-<hash>`, `locks\`). Override with `DPRINT_CACHE_DIR`. The cached artefact is the *compiled* module (typescript = 17 MB, markdown 7.5 MB, dockerfile 6.3 MB, toml 3.9 MB, json 2.1 MB).
- First use of a URL downloads and compiles: typescript plugin cold run = **~2.4 s** (prints `Compiling https://plugins.dprint.dev/typescript-0.95.13.wasm` on **stderr**; suppressed by `-L error`/`-L silent` or by ignoring stderr on success).
- Warm run: **~0.25 s** wall for the typescript plugin, with or without `--config` (0.253 s vs 0.263 s, i.e. no measurable cost for `--config`). Skipping discovery also avoids ~50 file-existence probes.
- Offline after first download works: with `HTTPS_PROXY=http://127.0.0.1:9` (dead proxy) a cached plugin formatted normally; no network access is attempted for cached URLs (cache key = `remote:<url>`).
- Offline with an uncached plugin fails with exit 12 (text above). This is the first-run risk for users without network/behind a proxy (`HTTPS_PROXY`, `DPRINT_CERT`, `DPRINT_TLS_CA_STORE` exist for corporate setups).
- `--plugins` also accepts a **local .wasm file path** (`--plugins C:\path\json-0.21.0.wasm`): compiled once ("Compiling C:\...wasm" on stderr), then cached; worked with the network blocked. This is the route to fully-offline operation if the wasm files were bundled next to `dprint.exe` (raw wasm sizes are much smaller than the compiled cache).
- `fmt --stdin` did not print the "Latest version" upgrade notice (only `dprint help` did), so no update check appears to run during formatting.
- The help text warns the cache directory "may be periodically deleted by the CLI".

### 2.9 Newlines and encoding - all VERIFIED

- Default `newLineKind` is `"lf"` in every official plugin: CRLF input comes out as LF. `"auto"` preserved CRLF input; `"system"` produced CRLF on Windows; `"crlf"` forces CRLF. Since a DevToys text box on Windows usually carries CRLF, the extension should decide deliberately (e.g. default the UI to `auto`, or normalise after the fact).
- A UTF-8 BOM at the start of **stdin input** is stripped from the output; non-ASCII content round-trips as UTF-8.
- A BOM in the **config file** is fatal (see 2.3).

### 2.10 Implementation checklist for `BuildDprintArgs`

1. Serialise `settings` to `{ "<pluginKey>": { ...settings } }` using the real key names/values from the tables below (dotted keys are literal JSON property names, e.g. `"arrowFunction.useParentheses"`, not nested objects - VERIFIED).
2. Write to a unique temp file, UTF-8 **without BOM**; delete after the process exits.
3. Insert `--config <abs path>` into the args (any position after `fmt` works; tested before and after `--plugins`/`--stdin`).
4. Always pass `--config` (or `--config-discovery=false`) even with no settings.
5. Treat non-zero exit as failure and surface stderr; lines matching `[plugin]: message (key)` identify the offending setting.
6. Numbers must be JSON numbers and booleans JSON booleans (`"lineWidth": "100"` happens to parse, `"abc"` fails; do not rely on string coercion).
7. Only emit keys the user changed from default, to keep forward/backward compatibility if plugin versions are bumped (unknown keys are fatal).
8. Verified coercions: string-typed numbers/booleans are accepted (`"lineWidth": "100"` -> 100, `"useTabs": "true"` -> true), non-integers are fatal (`100.5` -> `Error deserializing. invalid digit found in string in object property 'typescript -> lineWidth'`), and nested objects are NOT an alternative to dotted keys (`"arrowFunction": {"useParentheses": ...}` -> `Unknown property in configuration (arrowFunction)`). `--config` may also be placed before the `fmt` subcommand.

## 3. How the option tables were built

For each plugin two version-pinned sources were combined:

1. **Schema (DOCS)**: the `configSchemaUrl` that the plugin itself reports (recorded in `plugin-cache-manifest.json`), downloaded at the pinned version: `https://plugins.dprint.dev/dprint/dprint-plugin-<name>/<version>/schema.json`. Types, allowed values and meanings come from there.
2. **Resolved config (VERIFIED)**: `dprint output-resolved-config --config empty.json --plugins <pinned url>` run on the bundled binary. All **defaults** in the tables come from this output, so every default is VERIFIED for the pinned version. Keys that exist in the schema but not in the resolved output are "umbrella"/meta keys (they fan out to specific keys); their default shown is the schema's.

Keys found in one source but not the other were individually probed for acceptance (VERIFIED; noted in the tables). `locked` and `deno` appear in the official schemas: `locked` is config-inheritance plumbing (irrelevant to the extension); `deno: true` switches the plugin to Deno's preset.

All keys go inside the plugin's section. Every key name is a flat literal string, including the dots. No option in these tables could only be confirmed for a different version; the two unpinned doc pages cited were used only as secondary confirmation.

## 4. typescript-0.95.13 (config key `typescript`; JS/TS/JSX/TSX)

Sources: https://plugins.dprint.dev/dprint/dprint-plugin-typescript/0.95.13/schema.json (DOCS, pinned), `output-resolved-config` (VERIFIED), docs page https://dprint.dev/plugins/typescript/config/ (DOCS, unpinned/latest).

**Count: 186 keys** = 184 in the pinned schema (incl. `locked`, `deno`) + 2 accepted-but-unlisted (`conditionalExpression.linePerExpression`, `fileIndentLevel`). 173 are concrete resolved keys; the other 13 are umbrella/meta keys.

### 4.1 Headline options (suggested UI surface, 19)

`lineWidth`, `indentWidth`, `useTabs`, `newLineKind`, `semiColons`, `quoteStyle`, `jsx.quoteStyle`, `quoteProps`, `trailingCommas`, `bracePosition`, `nextControlFlowPosition`, `singleBodyPosition`, `useBraces`, `operatorPosition`, `preferSingleLine`, `preferHanging`, `arrowFunction.useParentheses`, `spaceSurroundingProperties`, `module.sortImportDeclarations`.

Defaults that may surprise Prettier users (VERIFIED): `lineWidth` = 120, `quoteStyle` = `alwaysDouble`, `arrowFunction.useParentheses` = `maintain`, `useBraces` = `whenNotSingleLine` (so `if (x) { y() }` becomes `if (x) y();`), and import/export declarations plus named imports/exports are **sorted** case-insensitively by default.

### 4.2 Umbrella keys and what they expand to (VERIFIED by diffing `output-resolved-config`)

| Umbrella key | Sets these specific keys |
|---|---|
| `useBraces` | `forInStatement.`, `forOfStatement.`, `forStatement.`, `ifStatement.`, `whileStatement.useBraces` (5) |
| `bracePosition` | all 22 `*.bracePosition` keys |
| `singleBodyPosition` | `forInStatement.`, `forOfStatement.`, `forStatement.`, `ifStatement.`, `whileStatement.singleBodyPosition` (5) |
| `nextControlFlowPosition` | `doWhileStatement.`, `ifStatement.`, `tryStatement.nextControlFlowPosition` (3) |
| `trailingCommas` | all 12 `*.trailingCommas` keys |
| `operatorPosition` | `binaryExpression.`, `conditionalExpression.`, `conditionalType.operatorPosition` (3) |
| `preferHanging` (boolean) | all 24 `*.preferHanging` keys; for the enum-typed ones (`arguments`, `parameters`, `arrayExpression`, `tupleType`, `typeParameters`) `true` maps to `"always"` |
| `preferSingleLine` | 22 `*.preferSingleLine` keys (NOT `importDeclaration.`/`exportDeclaration.preferSingleLine`, which default to `true` and are unaffected) |
| `spaceAround` | all 14 `*.spaceAround` keys |
| `spaceSurroundingProperties` | `objectExpression.`, `objectPattern.`, `typeLiteral.spaceSurroundingProperties` (also echoed as itself in resolved config) |
| `jsx.bracketPosition` | `jsxOpeningElement.bracketPosition`, `jsxSelfClosingElement.bracketPosition` |
| `typeLiteral.separatorKind` | `typeLiteral.separatorKind.singleLine`, `.multiLine` |
| `quoteStyle` | also sets `jsx.quoteStyle` when that is not given (`alwaysSingle`/`preferSingle` -> `preferSingle`) |
| `deno` | preset: `lineWidth` 80, all `bracePosition` -> `sameLine`, `arrowFunction.useParentheses` -> `force`, `binaryExpression.operatorPosition` -> `sameLine`, `quoteStyle` -> `preferDouble`, module sort -> `maintain`, `commentLine.forceSpaceAfterSlashes` -> false, `conditionalExpression.preferSingleLine` -> true, `*.spaceAfterNewKeyword` and `functionExpression.spaceAfterFunctionKeyword` -> true, ignore comments -> `deno-fmt-ignore[-file]` |

There are NO umbrella keys for `memberSpacing`, `sortNamedImports`, `linePerExpression`, `spaceBeforeParentheses`, `forceMultiLine` (each probed: `Unknown property in configuration`). A specific key always beats its umbrella.

### 4.3 Full option tables

Grouping is by key suffix; the "JSX" group holds every `jsx*`-prefixed key (including their preferHanging/preferSingleLine/space variants, which the umbrella keys also cover). Defaults are VERIFIED (resolved config); type/values/meaning are DOCS (pinned schema).

#### General / top-level (includes umbrella keys that fan out to the per-node keys below) (22)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `locked` | boolean | - | true, false | Whether the configuration is not allowed to be overridden or extended. |  |
| `lineWidth` | number | `120` | integer | The width of a line the printer will try to stay under. Note that the printer may exceed this width in certain cases. | YES |
| `indentWidth` | number | `2` | integer | The number of columns for an indent. | YES |
| `useTabs` | boolean | `false` | true, false | Whether to use tabs (true) or spaces (false). | YES |
| `semiColons` | string | `"prefer"` | always, prefer, asi | How semi-colons should be used. | YES |
| `quoteStyle` | string | `"alwaysDouble"` | alwaysDouble, alwaysSingle, preferDouble, preferSingle | How to use single or double quotes. | YES |
| `quoteProps` | string | `"preserve"` | asNeeded, consistent, preserve | Change when properties in objects are quoted. | YES |
| `newLineKind` | string | `"lf"` | auto, crlf, lf, system | The kind of newline to use. | YES |
| `useBraces` | string | `"whenNotSingleLine"` | maintain, whenNotSingleLine, always, preferNone | If braces should be used or not. | YES |
| `bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. | YES |
| `singleBodyPosition` | string | `"maintain"` | maintain, sameLine, nextLine | Where to place the expression of a statement that could possibly be on one line (ex. `if (true) console.log(5);`). | YES |
| `nextControlFlowPosition` | string | `"sameLine"` | maintain, sameLine, nextLine | Where to place the next control flow within a control flow statement. | YES |
| `trailingCommas` | string | `"onlyMultiLine"` | never, always, onlyMultiLine | If trailing commas should be used. | YES |
| `operatorPosition` | string | `"nextLine"` | maintain, sameLine, nextLine | Where to place the operator for expressions that span multiple lines. | YES |
| `preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. | YES |
| `preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. | YES |
| `deno` | boolean | `false` | true, false | Top level configuration that sets the configuration to what is used in Deno. |  |
| `spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `spaceSurroundingProperties` | boolean | `true` | true, false | Whether to add a space surrounding the properties of single line object-like nodes. | YES |
| `ignoreNodeCommentText` | string | `"dprint-ignore"` | any string | The text to use for an ignore comment (ex. `// dprint-ignore`). |  |
| `ignoreFileCommentText` | string | `"dprint-ignore-file"` | any string | The text to use for a file ignore comment (ex. `// dprint-ignore-file`). |  |
| `fileIndentLevel` | number | `0` | integer | Base indent level applied to the whole file (used by host plugins for embedded code). NOT in the schema; accepted by the plugin. Not useful for a UI. |  |

#### useParentheses (1)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `arrowFunction.useParentheses` | string | `"maintain"` | force, maintain, preferNone | Whether to use parentheses around a single parameter in an arrow function. | YES |

#### linePerExpression (3)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `binaryExpression.linePerExpression` | boolean | `false` | true, false | Whether to force a line per expression when spanning multiple lines. |  |
| `memberExpression.linePerExpression` | boolean | `false` | true, false | Whether to force a line per expression when spanning multiple lines. |  |
| `conditionalExpression.linePerExpression` | boolean | `true` | true, false | Whether to force each part of a multi-line conditional (ternary) onto its own line. NOT in the 0.95.13 schema, but accepted by the plugin and present in resolved config. |  |

#### JSX (11)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `jsx.bracketPosition` | string | `"nextLine"` | maintain, sameLine, nextLine | If the end angle bracket of a jsx open element or self closing element should be on the same or next line when the attributes span multiple lines. |  |
| `jsxOpeningElement.bracketPosition` | string | `"nextLine"` | maintain, sameLine, nextLine | If the end angle bracket of a jsx open element or self closing element should be on the same or next line when the attributes span multiple lines. |  |
| `jsxSelfClosingElement.bracketPosition` | string | `"nextLine"` | maintain, sameLine, nextLine | If the end angle bracket of a jsx open element or self closing element should be on the same or next line when the attributes span multiple lines. |  |
| `jsx.forceNewLinesSurroundingContent` | boolean | `false` | true, false | Forces newlines surrounding the content of JSX elements. |  |
| `jsx.quoteStyle` | string | `"preferDouble"` | preferDouble, preferSingle | How to use single or double quotes in JSX attributes. | YES |
| `jsx.multiLineParens` | string | `"prefer"` | never, prefer, always | Surrounds the top-most JSX element or fragment in parentheses when it spans multiple lines. |  |
| `jsxSelfClosingElement.spaceBeforeSlash` | boolean | `true` | true, false | Whether to add a space before a JSX element's slash when self closing. |  |
| `jsxExpressionContainer.spaceSurroundingExpression` | boolean | `false` | true, false | Whether to add a space surrounding the expression of a JSX container. |  |
| `jsxAttributes.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `jsxAttributes.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `jsxElement.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |

#### typeLiteral.separatorKind (3)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `typeLiteral.separatorKind` | string | `"semiColon"` | semiColon, comma | The kind of separator to use in type literals. |  |
| `typeLiteral.separatorKind.singleLine` | string | `"semiColon"` | semiColon, comma | The kind of separator to use in type literals. |  |
| `typeLiteral.separatorKind.multiLine` | string | `"semiColon"` | semiColon, comma | The kind of separator to use in type literals. |  |

#### memberSpacing (1)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `enumDeclaration.memberSpacing` | string | `"maintain"` | newLine, blankLine, maintain | How to space the members of an enum. |  |

#### Spacing (space*) (39)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `arguments.spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `arrayExpression.spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `arrayPattern.spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `catchClause.spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `doWhileStatement.spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `forInStatement.spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `forOfStatement.spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `forStatement.spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `ifStatement.spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `parameters.spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `parenExpression.spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `switchStatement.spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `tupleType.spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `whileStatement.spaceAround` | boolean | `false` | true, false | Whether to place spaces around enclosed expressions. |  |
| `objectExpression.spaceSurroundingProperties` | boolean | `true` | true, false | Whether to add a space surrounding the properties of a single line object expression. |  |
| `objectPattern.spaceSurroundingProperties` | boolean | `true` | true, false | Whether to add a space surrounding the properties of a single line object pattern. |  |
| `typeLiteral.spaceSurroundingProperties` | boolean | `true` | true, false | Whether to add a space surrounding the properties of a single line type literal. |  |
| `binaryExpression.spaceSurroundingBitwiseAndArithmeticOperator` | boolean | `true` | true, false | Whether to surround the operator in a binary expression with spaces. |  |
| `constructor.spaceBeforeParentheses` | boolean | `false` | true, false | Whether to add a space before the parentheses of a constructor. |  |
| `constructorType.spaceAfterNewKeyword` | boolean | `false` | true, false | Whether to add a space after the `new` keyword in a constructor type. |  |
| `constructSignature.spaceAfterNewKeyword` | boolean | `false` | true, false | Whether to add a space after the `new` keyword in a construct signature. |  |
| `doWhileStatement.spaceAfterWhileKeyword` | boolean | `true` | true, false | Whether to add a space after the `while` keyword in a do while statement. |  |
| `exportDeclaration.spaceSurroundingNamedExports` | boolean | `true` | true, false | Whether to add spaces around named exports in an export declaration. |  |
| `forInStatement.spaceAfterForKeyword` | boolean | `true` | true, false | Whether to add a space after the `for` keyword in a "for in" statement. |  |
| `forOfStatement.spaceAfterForKeyword` | boolean | `true` | true, false | Whether to add a space after the `for` keyword in a "for of" statement. |  |
| `forStatement.spaceAfterForKeyword` | boolean | `true` | true, false | Whether to add a space after the `for` keyword in a "for" statement. |  |
| `forStatement.spaceAfterSemiColons` | boolean | `true` | true, false | Whether to add a space after the semi-colons in a "for" statement. |  |
| `functionDeclaration.spaceBeforeParentheses` | boolean | `false` | true, false | Whether to add a space before the parentheses of a function declaration. |  |
| `functionExpression.spaceBeforeParentheses` | boolean | `false` | true, false | Whether to add a space before the parentheses of a function expression. |  |
| `functionExpression.spaceAfterFunctionKeyword` | boolean | `false` | true, false | Whether to add a space after the function keyword of a function expression. |  |
| `getAccessor.spaceBeforeParentheses` | boolean | `false` | true, false | Whether to add a space before the parentheses of a get accessor. |  |
| `ifStatement.spaceAfterIfKeyword` | boolean | `true` | true, false | Whether to add a space after the `if` keyword in an "if" statement. |  |
| `importDeclaration.spaceSurroundingNamedImports` | boolean | `true` | true, false | Whether to add spaces around named imports in an import declaration. |  |
| `method.spaceBeforeParentheses` | boolean | `false` | true, false | Whether to add a space before the parentheses of a method. |  |
| `setAccessor.spaceBeforeParentheses` | boolean | `false` | true, false | Whether to add a space before the parentheses of a set accessor. |  |
| `taggedTemplate.spaceBeforeLiteral` | boolean | `false` | true, false | Whether to add a space before the literal in a tagged template. |  |
| `typeAnnotation.spaceBeforeColon` | boolean | `false` | true, false | Whether to add a space before the colon of a type annotation. |  |
| `typeAssertion.spaceBeforeExpression` | boolean | `true` | true, false | Whether to add a space before the expression in a type assertion. |  |
| `whileStatement.spaceAfterWhileKeyword` | boolean | `true` | true, false | Whether to add a space after the `while` keyword in a while statement. |  |

#### forceSpaceAfterSlashes (1)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `commentLine.forceSpaceAfterSlashes` | boolean | `true` | true, false | Forces a space after the double slash in a comment line. |  |

#### Imports/exports: sorting and forceSingleLine/forceMultiLine (10)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `module.sortImportDeclarations` | string | `"caseInsensitive"` | maintain, caseSensitive, caseInsensitive | The kind of sort ordering to use. | YES |
| `module.sortExportDeclarations` | string | `"caseInsensitive"` | maintain, caseSensitive, caseInsensitive | The kind of sort ordering to use. |  |
| `exportDeclaration.sortNamedExports` | string | `"caseInsensitive"` | maintain, caseSensitive, caseInsensitive | The kind of sort ordering to use. |  |
| `exportDeclaration.sortTypeOnlyExports` | string | `"none"` | first, last, none | The kind of sort ordering to use for typed imports and exports. |  |
| `importDeclaration.sortNamedImports` | string | `"caseInsensitive"` | maintain, caseSensitive, caseInsensitive | The kind of sort ordering to use. |  |
| `importDeclaration.sortTypeOnlyImports` | string | `"none"` | first, last, none | The kind of sort ordering to use for typed imports and exports. |  |
| `exportDeclaration.forceSingleLine` | boolean | `false` | true, false | If code should be forced to be on a single line if able. |  |
| `importDeclaration.forceSingleLine` | boolean | `false` | true, false | If code should be forced to be on a single line if able. |  |
| `exportDeclaration.forceMultiLine` | string | `"never"` | always, never, whenMultiple | If code import/export specifiers should be forced to be on multiple lines. |  |
| `importDeclaration.forceMultiLine` | string | `"never"` | always, never, whenMultiple | If code import/export specifiers should be forced to be on multiple lines. |  |

#### useBraces (5)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `forInStatement.useBraces` | string | `"whenNotSingleLine"` | maintain, whenNotSingleLine, always, preferNone | If braces should be used or not. |  |
| `forOfStatement.useBraces` | string | `"whenNotSingleLine"` | maintain, whenNotSingleLine, always, preferNone | If braces should be used or not. |  |
| `forStatement.useBraces` | string | `"whenNotSingleLine"` | maintain, whenNotSingleLine, always, preferNone | If braces should be used or not. |  |
| `ifStatement.useBraces` | string | `"whenNotSingleLine"` | maintain, whenNotSingleLine, always, preferNone | If braces should be used or not. |  |
| `whileStatement.useBraces` | string | `"whenNotSingleLine"` | maintain, whenNotSingleLine, always, preferNone | If braces should be used or not. |  |

#### bracePosition (22)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `arrowFunction.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `classDeclaration.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `classExpression.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `constructor.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `doWhileStatement.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `enumDeclaration.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `forInStatement.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `forOfStatement.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `forStatement.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `functionDeclaration.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `functionExpression.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `getAccessor.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `ifStatement.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `interfaceDeclaration.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `moduleDeclaration.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `method.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `setAccessor.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `staticBlock.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `switchStatement.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `switchCase.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `tryStatement.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |
| `whileStatement.bracePosition` | string | `"sameLineUnlessHanging"` | maintain, sameLine, nextLine, sameLineUnlessHanging | Where to place the opening brace. |  |

#### singleBodyPosition (5)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `forInStatement.singleBodyPosition` | string | `"maintain"` | maintain, sameLine, nextLine | Where to place the expression of a statement that could possibly be on one line (ex. `if (true) console.log(5);`). |  |
| `forOfStatement.singleBodyPosition` | string | `"maintain"` | maintain, sameLine, nextLine | Where to place the expression of a statement that could possibly be on one line (ex. `if (true) console.log(5);`). |  |
| `forStatement.singleBodyPosition` | string | `"maintain"` | maintain, sameLine, nextLine | Where to place the expression of a statement that could possibly be on one line (ex. `if (true) console.log(5);`). |  |
| `ifStatement.singleBodyPosition` | string | `"maintain"` | maintain, sameLine, nextLine | Where to place the expression of a statement that could possibly be on one line (ex. `if (true) console.log(5);`). |  |
| `whileStatement.singleBodyPosition` | string | `"maintain"` | maintain, sameLine, nextLine | Where to place the expression of a statement that could possibly be on one line (ex. `if (true) console.log(5);`). |  |

#### nextControlFlowPosition (3)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `ifStatement.nextControlFlowPosition` | string | `"sameLine"` | maintain, sameLine, nextLine | Where to place the next control flow within a control flow statement. |  |
| `tryStatement.nextControlFlowPosition` | string | `"sameLine"` | maintain, sameLine, nextLine | Where to place the next control flow within a control flow statement. |  |
| `doWhileStatement.nextControlFlowPosition` | string | `"sameLine"` | maintain, sameLine, nextLine | Where to place the next control flow within a control flow statement. |  |

#### trailingCommas (12)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `arguments.trailingCommas` | string | `"onlyMultiLine"` | never, always, onlyMultiLine | If trailing commas should be used. |  |
| `parameters.trailingCommas` | string | `"onlyMultiLine"` | never, always, onlyMultiLine | If trailing commas should be used. |  |
| `arrayExpression.trailingCommas` | string | `"onlyMultiLine"` | never, always, onlyMultiLine | If trailing commas should be used. |  |
| `arrayPattern.trailingCommas` | string | `"onlyMultiLine"` | never, always, onlyMultiLine | If trailing commas should be used. |  |
| `enumDeclaration.trailingCommas` | string | `"onlyMultiLine"` | never, always, onlyMultiLine | If trailing commas should be used. |  |
| `exportDeclaration.trailingCommas` | string | `"onlyMultiLine"` | never, always, onlyMultiLine | If trailing commas should be used. |  |
| `importDeclaration.trailingCommas` | string | `"onlyMultiLine"` | never, always, onlyMultiLine | If trailing commas should be used. |  |
| `objectExpression.trailingCommas` | string | `"onlyMultiLine"` | never, always, onlyMultiLine | If trailing commas should be used. |  |
| `objectPattern.trailingCommas` | string | `"onlyMultiLine"` | never, always, onlyMultiLine | If trailing commas should be used. |  |
| `tupleType.trailingCommas` | string | `"onlyMultiLine"` | never, always, onlyMultiLine | If trailing commas should be used. |  |
| `typeLiteral.trailingCommas` | string | `"onlyMultiLine"` | never, always, onlyMultiLine | If trailing commas should be used. |  |
| `typeParameters.trailingCommas` | string | `"onlyMultiLine"` | never, always, onlyMultiLine | If trailing commas should be used. |  |

#### operatorPosition (3)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `binaryExpression.operatorPosition` | string | `"nextLine"` | maintain, sameLine, nextLine | Where to place the operator for expressions that span multiple lines. |  |
| `conditionalExpression.operatorPosition` | string | `"nextLine"` | maintain, sameLine, nextLine | Where to place the operator for expressions that span multiple lines. |  |
| `conditionalType.operatorPosition` | string | `"nextLine"` | maintain, sameLine, nextLine | Where to place the operator for expressions that span multiple lines. |  |

#### preferHanging (23)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `arguments.preferHanging` | string | `"never"` | always, onlySingleItem, never | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `arrayExpression.preferHanging` | string | `"never"` | always, onlySingleItem, never | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `arrayPattern.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `doWhileStatement.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `exportDeclaration.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `extendsClause.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `forInStatement.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `forOfStatement.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `forStatement.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `ifStatement.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `implementsClause.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `importDeclaration.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `objectExpression.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `objectPattern.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `parameters.preferHanging` | string | `"never"` | always, onlySingleItem, never | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `sequenceExpression.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `switchStatement.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `tupleType.preferHanging` | string | `"never"` | always, onlySingleItem, never | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `typeLiteral.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `typeParameters.preferHanging` | string | `"never"` | always, onlySingleItem, never | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `unionAndIntersectionType.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `variableStatement.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |
| `whileStatement.preferHanging` | boolean | `false` | true, false | Set to prefer hanging indentation when exceeding the line width instead of making code split up on multiple lines. |  |

#### preferSingleLine (22)

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `arrayExpression.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `arrayPattern.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `arguments.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `binaryExpression.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `computed.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `conditionalExpression.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `conditionalType.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `decorators.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `exportDeclaration.preferSingleLine` | boolean | `true` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `forStatement.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `importDeclaration.preferSingleLine` | boolean | `true` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `mappedType.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `memberExpression.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `objectExpression.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `objectPattern.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `parameters.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `parentheses.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `tupleType.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `typeLiteral.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `typeParameters.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `unionAndIntersectionType.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |
| `variableStatement.preferSingleLine` | boolean | `false` | true, false | If code should revert back from being on multiple lines to being on a single line when able. |  |

### 4.4 Verified (input -> output)

Input (`in.txt`), command `dprint fmt --stdin file.ts --config v.json --plugins https://plugins.dprint.dev/typescript-0.95.13.wasm < in.txt`:

```ts
import {b, a} from "m"
const o = {a:1, b:[1,2,3]}
if (x) { y() } else { z() }
const f = x => x*2
function g(aaaa: string, bbbb: number) { return aaaa + bbbb }
```

`{}` (defaults):

```ts
import { a, b } from "m";
const o = { a: 1, b: [1, 2, 3] };
if (x) y();
else z();
const f = x => x * 2;
function g(aaaa: string, bbbb: number) {
  return aaaa + bbbb;
}
```

`{"typescript":{"quoteStyle":"preferSingle","semiColons":"asi","lineWidth":40,"indentWidth":4}}`:

```ts
import { a, b } from 'm'
const o = { a: 1, b: [1, 2, 3] }
if (x) y()
else z()
const f = x => x * 2
function g(aaaa: string, bbbb: number) {
    return aaaa + bbbb
}
```

`{"typescript":{"bracePosition":"nextLine","nextControlFlowPosition":"nextLine","arrowFunction.useParentheses":"force","trailingCommas":"always","useTabs":true}}`:

```ts
import { a, b, } from "m";
const o = { a: 1, b: [1, 2, 3,], };
if (x) y();
else z();
const f = (x) => x * 2;
function g(aaaa: string, bbbb: number,)
{
	return aaaa + bbbb;
}
```

`{"typescript":{"objectExpression.spaceSurroundingProperties":false,"module.sortImportDeclarations":"maintain","importDeclaration.sortNamedImports":"maintain","functionDeclaration.spaceBeforeParentheses":true,"lineWidth":40,"parameters.preferHanging":"always"}}`:

```ts
import { b, a } from "m";
const o = {a: 1, b: [1, 2, 3]};
if (x) y();
else z();
const f = x => x * 2;
function g (aaaa: string,
  bbbb: number)
{
  return aaaa + bbbb;
}
```

Options verified to change output: `quoteStyle`, `semiColons`, `indentWidth`, `useTabs`, `bracePosition`, `arrowFunction.useParentheses`, `trailingCommas`, `objectExpression.spaceSurroundingProperties`, `importDeclaration.sortNamedImports`, `functionDeclaration.spaceBeforeParentheses`, `lineWidth` + `parameters.preferHanging`, `newLineKind` (lf/crlf/auto/system, section 2.9). (`nextControlFlowPosition` had no visible effect on this input because default `useBraces` removed the braces and already put `else` on its own line.)

## 5. json-0.21.0 (config key `json`; .json/.jsonc)

Sources: https://plugins.dprint.dev/dprint/dprint-plugin-json/0.21.0/schema.json (DOCS, pinned); `output-resolved-config` (VERIFIED). **Count: 13 keys** (10 concrete + `preferSingleLine` umbrella + `locked` + `deno`).

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `locked` | boolean | - | true, false | Whether the configuration is not allowed to be overriden or extended. |  |
| `lineWidth` | number | `120` | integer | The width of a line the printer will try to stay under. Note that the printer may exceed this width in certain cases. | YES |
| `indentWidth` | number | `2` | integer | The number of characters for an indent. | YES |
| `useTabs` | boolean | `false` | true, false | Whether to use tabs (true) or spaces (false). | YES |
| `newLineKind` | string | `"lf"` | auto, crlf, lf, system | The kind of newline to use. | YES |
| `commentLine.forceSpaceAfterSlashes` | boolean | `true` | true, false | Forces a space after slashes.  For example: `// comment` instead of `//comment` | YES |
| `preferSingleLine` | boolean | `false` | true, false | If arrays and objects should collapse to a single line if it would be below the line width. | YES |
| `array.preferSingleLine` | boolean | `false` | true, false | If arrays and objects should collapse to a single line if it would be below the line width. |  |
| `object.preferSingleLine` | boolean | `false` | true, false | If arrays and objects should collapse to a single line if it would be below the line width. |  |
| `trailingCommas` | string | `"jsonc"` | always, jsonc, maintain, never | Whether to use trailing commas. | YES |
| `jsonTrailingCommaFiles` | array | `[]` | any string | When `trailingCommas` is `jsonc`, treat these files as JSONC and use trailing commas (ex. `["tsconfig.json", ".vscode/settings.json"]`). |  |
| `deno` | boolean | `false` | true, false | Top level configuration that sets the configuration to what is used in Deno. |  |
| `ignoreNodeCommentText` | string | `"dprint-ignore"` | any string | The text to use for an ignore comment (ex. `// dprint-ignore`). |  |

Notes: `preferSingleLine` expands to `array.preferSingleLine` + `object.preferSingleLine` (VERIFIED). `trailingCommas` values are `always | jsonc | maintain | never` (the extension's current choice list lacks `maintain`). `jsonTrailingCommaFiles` is an array of strings (default `[]`), only meaningful with real file names.

### Verified

Command: `dprint fmt --stdin file.json --config v.json --plugins https://plugins.dprint.dev/json-0.21.0.wasm`.

- Input `{"b":[\n1,2],"c":{\n"d":null}}`: defaults keep both containers multi-line (9 lines); `{"json":{"preferSingleLine":true}}` -> `{ "b": [1, 2], "c": { "d": null } }`.
- Input `{"a":1,"b":[1,2,\n3],//c\n"c":{"d":null}}`: defaults -> 2-space indent and `// c`; `{"json":{"indentWidth":4,"commentLine.forceSpaceAfterSlashes":false}}` -> 4-space indent and `//c`.
- `--stdin file.jsonc` with `{"json":{"trailingCommas":"always","useTabs":true}}` -> tab indentation and `"c": { "d": null },` (trailing comma added).
- `{"json":{"lineWidth":10}}` -> the inline array/object were broken one element per line.

## 6. markdown-0.20.0 (config key `markdown`; .md)

Sources: https://plugins.dprint.dev/dprint/dprint-plugin-markdown/0.20.0/schema.json (DOCS, pinned); `output-resolved-config` (VERIFIED). **Count: 12 keys** = 11 in schema + `unorderedListKind`, which is missing from the pinned schema but accepted and resolved by the plugin (VERIFIED: default `dashes`; `asterisks` accepted and changes output; `zzz` -> `Found invalid value 'zzz'. (unorderedListKind)`). Its value set `dashes | asterisks` is otherwise DOCS from https://dprint.dev/plugins/markdown/config/ (unpinned).

No `indentWidth`/`useTabs` keys exist for this plugin. A `tags` key is rejected at 0.20.0: `Unknown property in configuration: tags (tags)` (VERIFIED). Code blocks are only formatted by other plugins if those plugins are also loaded (DOCS: https://dprint.dev/plugins/markdown/); with the extension's single `--plugins` URL, embedded code is left as-is.

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `locked` | boolean | - | true, false | Whether the configuration is not allowed to be overridden or extended. |  |
| `lineWidth` | number | `80` | integer | The width of a line the printer will try to stay under. Note that the printer may exceed this width in certain cases. | YES |
| `newLineKind` | string | `"lf"` | auto, crlf, lf, system | The kind of newline to use. | YES |
| `textWrap` | string | `"maintain"` | always, maintain, never | Text wrapping possibilities. | YES |
| `emphasisKind` | string | `"underscores"` | asterisks, underscores | The character to use for emphasis/italics. | YES |
| `strongKind` | string | `"asterisks"` | asterisks, underscores | The character to use for strong emphasis/bold. | YES |
| `deno` | boolean | `false` | true, false | Top level configuration that sets the configuration to what is used in Deno. |  |
| `ignoreDirective` | string | `"dprint-ignore"` | any string | The text to use for an ignore directive (ex. `<!-- dprint-ignore -->`). |  |
| `ignoreFileDirective` | string | `"dprint-ignore-file"` | any string | The text to use for an ignore file directive (ex. `<!-- dprint-ignore-file -->`). |  |
| `ignoreStartDirective` | string | `"dprint-ignore-start"` | any string | The text to use for an ignore start directive (ex. `<!-- dprint-ignore-start -->`). |  |
| `ignoreEndDirective` | string | `"dprint-ignore-end"` | any string | The text to use for an ignore end directive (ex. `<!-- dprint-ignore-end -->`). |  |
| `unorderedListKind` | string | `"dashes"` | dashes, asterisks | Bullet character for unordered lists (`-` or `*`). NOT in the 0.20.0 schema, but accepted by the plugin and present in resolved config. | YES |

`textWrap` value meanings (schema): `always` = wrap at `lineWidth`; `maintain` = keep line breaks as-is; `never` = unwrap paragraphs to one line.

### Verified

Input: `# T\n\n* one *emph* and __strong__ text that is fairly long so that it will need wrapping at some point ok\n* two\n`, command `dprint fmt --stdin file.md --config v.json --plugins https://plugins.dprint.dev/markdown-0.20.0.wasm`.

- `{}` -> `- one _emph_ and **strong** text ... ok` on one line (bullets -> `-`, emphasis -> `_`, strong -> `**`).
- `{"markdown":{"textWrap":"always","lineWidth":40}}` -> list item wrapped onto 3 lines with 2-space continuation indent.
- `{"markdown":{"emphasisKind":"asterisks","strongKind":"underscores","unorderedListKind":"asterisks"}}` -> `* one *emph* and __strong__ text ...`.

## 7. toml-0.7.0 (config key `toml`; .toml)

Sources: https://plugins.dprint.dev/dprint/dprint-plugin-toml/0.7.0/schema.json (DOCS, pinned); `output-resolved-config` (VERIFIED). **Count: 7 keys** (incl. `locked`).

Naming trap (VERIFIED): `output-resolved-config` prints `cargoApplyConventions` and `commentForceLeadingSpace`, but those spellings are **rejected** as input (`Unknown property in configuration (cargoApplyConventions)`). The accepted input keys are the schema's dotted names `cargo.applyConventions` and `comment.forceLeadingSpace`.

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `locked` | boolean | - | true, false | Whether the configuration is not allowed to be overriden or extended. |  |
| `lineWidth` | number | `120` | integer | The width of a line the printer will try to stay under. Note that the printer may exceed this width in certain cases. | YES |
| `indentWidth` | number | `2` | integer | The number of characters for an indent. | YES |
| `useTabs` | boolean | `false` | true, false | Whether to use tabs (true) or spaces (false). | YES |
| `newLineKind` | string | `"lf"` | auto, crlf, lf, system | The kind of newline to use. | YES |
| `comment.forceLeadingSpace` | boolean | `true` | true, false | Whether to force a leading space in a comment. | YES |
| `cargo.applyConventions` | boolean | `true` | true, false | Whether to apply sorting to a Cargo.toml file. | YES |

### Verified

Input: `[package]\nversion="1"\nname="x"\n#comment\narr=["aaaaaaaaaaaa","bbbbbbbbbbbbbb","cccccccccccc"]\n[dependencies]\nb="1"\na="1"\n`.

- `--stdin Cargo.toml`, `{}` -> `name` sorted before `version`, dependencies sorted `a`,`b`, and `# comment`. With `{"toml":{"cargo.applyConventions":false,"comment.forceLeadingSpace":false}}` -> original key order kept and `#comment` kept.
- `--stdin file.toml` (what the extension uses): no Cargo sorting regardless of `cargo.applyConventions`, so that option is a no-op for the extension unless the fake file name is changed.
- `{"toml":{"lineWidth":30,"indentWidth":4}}` -> array broken one element per line with 4-space indent and trailing comma; `{"toml":{"lineWidth":30,"useTabs":true}}` -> same with tab indent.

## 8. dockerfile-0.3.3 (config key `dockerfile`; `Dockerfile`, `*.dockerfile`)

Sources: https://plugins.dprint.dev/dprint/dprint-plugin-dockerfile/0.3.3/schema.json (DOCS, pinned); `output-resolved-config` (VERIFIED). **Count: 3 keys** (`lineWidth`, `newLineKind`, `locked`). `indentWidth` and `useTabs` are rejected inside the section (`Unknown property in configuration (indentWidth)`), though harmless as global keys (VERIFIED).

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `locked` | boolean | - | true, false | Whether the configuration is not allowed to be overridden or extended. |  |
| `lineWidth` | number | `120` | integer | The width of a line the printer will try to stay under. Note that the printer may exceed this width in certain cases. | YES |
| `newLineKind` | string | `"lf"` | auto, crlf, lf, system | The kind of newline to use. | YES |

### Verified

Command: `dprint fmt --stdin Dockerfile --config v.json --plugins https://plugins.dprint.dev/dockerfile-0.3.3.wasm`.

- Defaults: `FROM   node:18` -> `FROM node:18`; `FROM node:18 AS   build` -> single spaces; `CMD ["node","server.js"]` -> `CMD ["node", "server.js"]`; `ENV A=1    B=2` -> `ENV A=1 B=2`.
- `{"dockerfile":{"lineWidth":20}}`: `LABEL a="1" b="2" cccccccccccc="..." dddddddddd="..."` was split into one `key=value \` per line, aligned under the first. Long `RUN ... && ...` lines and the JSON-array `CMD` were NOT wrapped at any width, so `lineWidth` has a very narrow effect.
- `{"dockerfile":{"newLineKind":"crlf"}}` -> CRLF line endings in output.
- Only 2 user-facing options exist, so "3 options" could not be verified for this plugin; both were.

## 9. Mismatches between current `Models/FormatterSettings.cs` and reality (for the wiring work)

| Setting in code | Problem (VERIFIED) |
|---|---|
| TypeScript `quoteStyle` choices `double`, `single`; default `double` | Invalid values -> exit 1. Real: `alwaysDouble` (default), `alwaysSingle`, `preferDouble`, `preferSingle`. |
| TypeScript `semiColons` choices `prefer`, `asi` | Missing `always`. |
| JSON `trailingCommas` choices `never`, `jsonc`, `always` | Missing `maintain`. |
| Markdown | Missing `newLineKind`, `emphasisKind`, `strongKind`, `unorderedListKind`. |
| TOML | Missing `newLineKind`, `comment.forceLeadingSpace`; `cargo.applyConventions` is inert with `file.toml`. |
| Dockerfile | Only `lineWidth` and `newLineKind` are legal. |
| Line-ending default | Plugins default to `lf`, which rewrites CRLF text coming from the DevToys editor. |
