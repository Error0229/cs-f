# dprint g-plane plugins: real configuration surface

Scope: malva v0.15.1, markup_fmt v0.25.1, pretty_yaml v0.5.1, pretty_graphql v0.2.3, run by bundled `Binaries/dprint.exe` (dprint 0.50.2, VERIFIED `--version`).

Claim markers: **VERIFIED** = executed with the bundled binary on this machine (2026-09-19). **DOCS** = read from a cited source, not executed.

## 0. Sources and how completeness was established

For every plugin, two version-matched sources were downloaded and diffed by script:

| Source | URL pattern |
|---|---|
| Published JSON schema | `https://plugins.dprint.dev/g-plane/<name>/v<ver>/schema.json` |
| Config resolver at the git tag | `https://raw.githubusercontent.com/g-plane/<repo>/v<ver>/dprint_plugin/src/config.rs` |
| Plugin info (config key, extensions) | `https://raw.githubusercontent.com/g-plane/<repo>/v<ver>/dprint_plugin/src/lib.rs` |
| Per-option docs at the tag | `https://github.com/g-plane/<repo>/tree/v<ver>/docs/src/config` (there is no `docs/config.md`; it is an mdBook, one file per option) |

Result (VERIFIED by script): the key set read by `config.rs` is **identical** to the schema `properties` key set for all four plugins. No hidden keys, no schema-only keys. `dprint_plugin/deployment/schema.json` at each tag is byte-size identical to the published schema.

| Plugin | Version | Config section key | Keys | File extensions matched |
|---|---|---|---|---|
| malva | 0.15.1 | `malva` | 35 | css, scss, sass, less |
| markup_fmt | 0.25.1 | `markup` | 46 | html, vue, svelte, astro, jinja, jinja2, j2, twig, njk, vto, component.html (Angular), mustache, hbs, handlebars, xml, svg, wsdl, xsd, xslt, xsl |
| pretty_yaml | 0.5.1 | `yaml` | 16 | yaml, yml |
| pretty_graphql | 0.2.3 | `graphql` | 48 | graphql, gql |

Config keys: DOCS (`lib.rs` `config_key`) and VERIFIED (diagnostics are prefixed `[dprint_plugin_malva]`, `[dprint_plugin_markup]`, `[dprint_plugin_yaml]`, `[dprint_plugin_graphql]`, and options under those section names took effect).

The doc sites (malva.netlify.app, markup-fmt.netlify.app, pretty-yaml.netlify.app, pretty-graphql.netlify.app, all `/config/` return 200) track the **latest** release, not the pinned one. They were not used as the source of truth; nothing in the tables below depends on them.

### Schema defaults that are wrong or misleading (source wins)

| Plugin | Key | Schema says | Resolver (`config.rs`) says |
|---|---|---|---|
| pretty_graphql | `parenSpacing` | default `null` | `false` |
| pretty_graphql | `braceSpacing` | default `null` | `true` (VERIFIED: default output `{ x: 1, y: 2 }`) |
| pretty_yaml | `dashSpacing` | no default | `"oneSpace"` |
| malva, pretty_yaml | `*.preferSingleLine` overrides | default `false` (via `$ref`) | nullable, unset = inherit base `preferSingleLine` |
| malva | `attrSelector.quotes` | default `alwaysDouble` (via `$ref`) | nullable, unset = inherit `quotes` |

A UI generated mechanically from the schema would show wrong defaults for these.

## 1. Test mechanism used (minimal; core mechanism is another agent's scope)

Working form (VERIFIED):

```
<tmp>/dprint.json:  {"plugins":["<url>", ...], "malva":{...}}
dprint.exe fmt --config <tmp>\dprint.json --stdin file.css      # source on stdin
```

Findings that matter for the implementation, all VERIFIED:

- `--stdin <bare filename>` works from any cwd with `--config <abs path>`.
- `--stdin <absolute path>` **fails if the file does not exist**: `Error canonicalizing path ...: The system cannot find the file specified. (os error 2)`, exit 1, empty stdout. It works if the file exists (even outside the config dir). Use a bare filename.
- If the stdin filename matches no loaded plugin (e.g. `f.xyz`, or a mangled path), dprint **echoes the input unchanged with exit 0 and no stderr**. Silent no-op; a wrong extension looks like success.
- Top-level (global) `lineWidth`, `indentWidth`, `useTabs`, `newLineKind` are inherited by all four plugins when the plugin-level key is absent (DOCS `config.rs` `global_config.*`; VERIFIED `{"indentWidth":8}` at top level gave 8-space CSS). pretty_yaml has no `useTabs`; global `useTabs:true` is silently ignored for YAML (VERIFIED).
- JSON `null` is accepted for nullable options (VERIFIED `"hexColorLength": null`).
- `--plugins a b c` without a config file also loads several plugins (VERIFIED), but cannot carry settings.

### Invalid configuration reporting (VERIFIED, identical shape for all four)

Input `"malva":{"tabWidth":4,"singleQuote":true,"hexCase":"bogus","indentWidth":"x"}`:

```
[dprint_plugin_malva]: invalid digit found in string (indentWidth)
[dprint_plugin_malva]: invalid value for config `hexCase` (hexCase)
[dprint_plugin_malva]: Unknown property in configuration (tabWidth)
[dprint_plugin_malva]: Unknown property in configuration (singleQuote)
[dprint_plugin_malva]: Error initializing from configuration file. Had 4 diagnostic(s).
Had 4 configuration errors.
```

- Exit code **1**, stdout **empty**, everything on **stderr**. Nothing is formatted; one bad key kills the whole run.
- Format per line: `[dprint_plugin_<key>]: <message> (<propertyName>)`. All diagnostics are reported, not just the first.
- Wrong bool type: `provided string was not \`true\` or \`false\` (useTabs)`. Bad enum: `invalid value for config \`comma\` (comma)`.
- Source syntax error: exit 1, stderr `syntax error at line 2, col 1: expect token \`}\`, but found \`<eof>\``, empty stdout.

## 2. malva v0.15.1 — section key `malva`

35 keys. Sources: schema `https://plugins.dprint.dev/g-plane/malva/v0.15.1/schema.json`, resolver `https://raw.githubusercontent.com/g-plane/malva/v0.15.1/dprint_plugin/src/config.rs`. All rows DOCS unless marked V (VERIFIED changes output).

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `printWidth` | int >= 0 | 80 (or global `lineWidth`) | | Soft line width limit | YES |
| `useTabs` | bool | false (or global) | | Indent with tabs | YES |
| `indentWidth` | int >= 0 | 2 (or global) | | Indent size (V) | YES |
| `lineBreak` | string | `lf` (global `newLineKind` crlf maps to `crlf`) | `lf`, `crlf` | Line ending | YES |
| `quotes` | string | `alwaysDouble` | `alwaysDouble`, `alwaysSingle`, `preferDouble`, `preferSingle` | String quote style (V) | YES |
| `attrSelector.quotes` | string or null | null = inherit `quotes` | same as `quotes` | Quote style inside `[attr="v"]` selectors only | override |
| `hexCase` | string | `lower` | `ignore`, `lower`, `upper` | Hex colour case (V) | YES |
| `hexColorLength` | string or null | null = keep | `short`, `long` | `#FFFFFF` to `#FFF` or the reverse (V) | YES |
| `operatorLinebreak` | string | `after` | `before`, `after` | Line break position around operators | |
| `blockSelectorLinebreak` | string | `consistent` | `always`, `consistent`, `wrap` | Line breaks after selector commas (V) | YES |
| `omitNumberLeadingZero` | bool | false | | `0.5` to `.5` (V) | YES |
| `trailingComma` | bool | false | | Trailing comma in multi-line Sass maps/params etc. (V) | |
| `formatComments` | bool | false | | Pad comment delimiters with whitespace | |
| `alignComments` | bool | true | | Re-indent multi-line comments | |
| `linebreakInPseudoParens` | bool | false | | Allow breaks inside `:is(...)` etc. | |
| `declarationOrder` | string or null | null = no sorting | `alphabetical`, `smacss`, `concentric` | Sort declarations (V) | YES |
| `declarationOrderGroupBy` | string | `nonDeclaration` | `nonDeclaration`, `nonDeclarationAndEmptyLine` | What delimits a sortable group | |
| `singleLineBlockThreshold` | int >= 0 or null | null | | Blocks with <= N statements stay on one line if they fit (V) | YES |
| `keyframeSelectorNotation` | string or null | null = keep | `keyword`, `percentage` | `from/to` vs `0%/100%` | |
| `attrValueQuotes` | string | `always` | `always`, `ignore` | Add quotes to unquoted attr selector values | |
| `preferSingleLine` | bool | false | | Collapse lists to one line when they fit (V) | YES |
| `selectors.preferSingleLine` | bool or null | null = inherit | | override | override |
| `functionArgs.preferSingleLine` | bool or null | null | | override | override |
| `sassContentAtRule.preferSingleLine` | bool or null | null | | override | override |
| `sassIncludeAtRule.preferSingleLine` | bool or null | null | | override | override |
| `sassMap.preferSingleLine` | bool or null | null | | override (V) | override |
| `sassModuleConfig.preferSingleLine` | bool or null | null | | override | override |
| `sassParams.preferSingleLine` | bool or null | null | | override | override |
| `lessImportOptions.preferSingleLine` | bool or null | null | | override | override |
| `lessMixinArgs.preferSingleLine` | bool or null | null | | override | override |
| `lessMixinParams.preferSingleLine` | bool or null | null | | override | override |
| `singleLineTopLevelDeclarations` | bool | false | | Force all top-level declarations on one line. Internal: markup_fmt sets it for `style="..."` attributes. Do not expose. | no |
| `selectorOverrideCommentDirective` | string | `malva-selector-override` | free text | Comment directive text | no |
| `ignoreCommentDirective` | string | `malva-ignore` | free text | Comment that skips next statement | no |
| `ignoreFileCommentDirective` | string | `dprint-ignore-file` | free text | Comment that skips the file | no |

**Override-key pattern:** `<nodeKind>.<baseOption>`. The key is a flat string containing a literal dot (`"sassMap.preferSingleLine": false`), NOT a nested object. Unset (or null) inherits the base option. 10 node kinds for `preferSingleLine`, 1 (`attrSelector`) for `quotes`.

**Verified:**

Input: `a,b{color:#FFFFFF;background:url(x.png);margin:0.5px;content:'x'}` + `@media (min-width:100px){.z{top:0;bottom:0}}`

- Default: `a, b {` / 2-space indent / `#ffffff` / `0.5px` / `"x"`.
- With `indentWidth:4, quotes:alwaysSingle, hexCase:upper, hexColorLength:short, omitNumberLeadingZero:true, declarationOrder:alphabetical, blockSelectorLinebreak:always`: `a,`newline`b {`, 4-space, `background` sorted before `color`, `#FFF`, `'x'`, `.5px`, `bottom` before `top`.
- `singleLineBlockThreshold:2`: `a{color:#FFF}` became `a { color: #FFF; }`.
- SCSS override: `preferSingleLine:true, sassMap.preferSingleLine:false, trailingComma:true` collapsed a multi-line `@include foo($a, $b);` while the Sass map stayed multi-line and gained a trailing comma.
- `.less` formats. `.sass` (indented syntax) is matched but came back unchanged; do not claim indented-Sass support.

## 3. markup_fmt v0.25.1 — section key `markup`

46 keys. Sources: schema `https://plugins.dprint.dev/g-plane/markup_fmt/v0.25.1/schema.json`, resolver `https://raw.githubusercontent.com/g-plane/markup_fmt/v0.25.1/dprint_plugin/src/config.rs`.

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `printWidth` | int | 80 (or global `lineWidth`) | | Soft line width | YES |
| `useTabs` | bool | false (or global) | | Tabs | YES |
| `indentWidth` | int | 2 (or global) | | Indent size (V) | YES |
| `lineBreak` | string | `lf` | `lf`, `crlf` | Line ending | YES |
| `quotes` | string | `double` | `double`, `single` | Attribute value quotes (V). Note: NOT the malva value set | YES |
| `formatComments` | bool | false | | Pad and indent comments | |
| `scriptIndent` | bool | false | | Indent code inside `<script>` (V) | YES |
| `html.scriptIndent` / `vue.scriptIndent` / `svelte.scriptIndent` / `astro.scriptIndent` | bool or null | null = inherit | | Per-language override (V `vue.scriptIndent`) | override |
| `styleIndent` | bool | false | | Indent code inside `<style>` (V) | YES |
| `html.styleIndent` / `vue.styleIndent` / `svelte.styleIndent` / `astro.styleIndent` | bool or null | null = inherit | | Per-language override | override |
| `closingBracketSameLine` | bool | false | | Put `>` of a multi-line tag on the last attribute line (V) | YES |
| `closingTagLineBreakForEmpty` | string | `fit` | `always`, `fit`, `never` | Break before closing tag of empty element | |
| `maxAttrsPerLine` | int or null | null | | Max attributes per line. Conflicts with `preferAttrsSingleLine` | YES |
| `preferAttrsSingleLine` | bool | false | | Keep attributes on one line when they fit. Conflicts with `maxAttrsPerLine` | YES |
| `singleAttrSameLine` | bool | true | | Keep a sole attribute on the tag line | |
| `html.normal.selfClosing` | bool or null | null = keep | | `<div></div>` vs `<div />` | |
| `html.void.selfClosing` | bool or null | null = keep | | `<br>` vs `<br />` (V) | YES |
| `component.selfClosing` | bool or null | null | | Vue/Svelte/Astro/Angular components (V) | YES (framework) |
| `svg.selfClosing` | bool or null | null | | SVG elements | |
| `mathml.selfClosing` | bool or null | null | | MathML elements | |
| `whitespaceSensitivity` | string | `css` | `css`, `strict`, `ignore` | How significant whitespace around children is. `ignore` gives the prettiest but semantically riskiest output | YES |
| `component.whitespaceSensitivity` | string or null | null = inherit | `css`, `strict`, `ignore` | Same, for components | override |
| `doctypeKeywordCase` | string | `upper` | `ignore`, `upper`, `lower` | `<!DOCTYPE>` case (V) | |
| `vBindStyle` | string or null | null = keep | `short`, `long` | Vue `:x` vs `v-bind:x` (V) | YES (Vue) |
| `vOnStyle` | string or null | null | `short`, `long` | Vue `@x` vs `v-on:x` (V) | YES (Vue) |
| `vForDelimiterStyle` | string or null | null | `in`, `of` | Vue `v-for` delimiter (V) | Vue |
| `vSlotStyle` | string or null | null | `short`, `long`, `vSlot` | Vue `#x` vs `v-slot:x` (V) | Vue |
| `component.vSlotStyle` / `default.vSlotStyle` / `named.vSlotStyle` | string or null | null = inherit | same | Per-context override | override |
| `vBindSameNameShortHand` | bool or null | null = keep | | Vue 3.4 `:foo="foo"` to `:foo` (V) | Vue |
| `vueComponentCase` | string | `ignore` | `ignore`, `pascalCase`, `kebabCase` | Component tag naming in templates (V) | Vue |
| `strictSvelteAttr` | bool | false | | Svelte attr values in strict (quoted) form (V) | Svelte |
| `svelteAttrShorthand` | bool or null | null = keep | | `value={value}` to `{value}` (V) | YES (Svelte) |
| `svelteDirectiveShorthand` | bool or null | null = keep | | `bind:name={name}` to `bind:name` (V) | Svelte |
| `astroAttrShorthand` | bool or null | null = keep | | `title={title}` to `{title}` (V) | Astro |
| `angularNextControlFlowSameLine` | bool | true | | Angular `} @else {` on same line | Angular |
| `scriptFormatter` | string or null | `dprint` (under dprint) | `dprint`, `biome` | Tells markup_fmt which key names to send to the script formatter. Keep at default; do not expose | no |
| `ignoreCommentDirective` | string | `markup-fmt-ignore` | free text | | no |
| `ignoreFileCommentDirective` | string | `dprint-ignore-file` | free text | | no |

Counting the grouped rows individually gives 46.

**Override-key patterns** (flat dotted string keys, null/unset = inherit):
- `<language>.<option>`: `html|vue|svelte|astro` x `scriptIndent|styleIndent` (8 keys).
- `<elementClass>.selfClosing`: `html.normal`, `html.void`, `component`, `svg`, `mathml`. These have NO base `selfClosing` key; each is independent and null means "leave as written".
- `component.whitespaceSensitivity`; `component|default|named.vSlotStyle`.

### Embedded languages (the special question)

Mechanism (DOCS, `dprint_plugin/src/lib.rs` at tag): for each embedded block markup_fmt calls back into the dprint host with a virtual path `<filename>#.<ext>` (e.g. `f.html#.js`, `#.ts`, `#.css`, `#.scss`, `#.json`). The host routes it to whatever loaded plugin matches that extension. If none matches, the host returns nothing and markup_fmt keeps the code verbatim. It passes an override config: `lineWidth` and `printWidth` (remaining width), `fileIndentLevel`, and for code inside attributes `quoteStyle` (opposite of the attribute quote) plus malva `singleLineTopLevelDeclarations:true` for `style="..."`.

VERIFIED with the bundled binary:

| Setup | `<style>` | `<script>` | `style="..."` attr | `<script type=application/json>` | Vue `{{ }}` / directive expressions, Svelte `{expr}`, Astro frontmatter |
|---|---|---|---|---|---|
| Only markup_fmt (what the extension does today) | verbatim, only re-indented | verbatim | verbatim | verbatim | verbatim |
| markup_fmt + typescript 0.95.13 + malva 0.15.1 | formatted | formatted | formatted (`color: red; top: 0`) | verbatim | formatted (`{{ n + 1 }}`, `{ a }`, `value > 1`, Astro frontmatter) |
| + json 0.21.0 | | | | formatted | |

- No error and no warning when the helper plugins are missing. Exit 0. **Today the extension's HTML/Vue/Svelte/Astro output leaves all JS/TS/CSS untouched** — for a `.vue` file that means only the `<template>` markup is formatted.
- Sub-plugin settings ARE respected: with `"typescript":{"quoteStyle":"preferSingle","semiColons":"asi"}` and `"malva":{"hexCase":"upper","omitNumberLeadingZero":true}` the embedded script came out `{ a: 1, b: 'two' }` without semicolons and the embedded CSS came out `#FFF` / `.5px`.
- `lang="ts"` and `lang="scss"` blocks in Vue were routed correctly to typescript and malva.
- Inline event handlers (`onclick="foo( 1,'a' )"`) were NOT formatted in `.html` at this version, even with typescript loaded.
- A syntax error in embedded code fails the whole file: exit 1, stderr `failed to format code with external formatter:` followed by the typescript plugin's error pointing at `file:///f.html#.js:3:12`.
- Also works with no config: `dprint fmt --stdin file.html --plugins <markup> <ts> <malva>` formatted embedded code. That is a zero-risk fix available before any settings work.
- **Indent mismatch hazard:** with `markup.indentWidth:4` and `typescript.indentWidth:3` plus `scriptIndent:true`, the script block came out ragged (first line at 12 spaces, following lines at 9). Setting indent once at the top level (`{"indentWidth":4, ...}`) gave clean output. Indent/width/tabs/line-ending must be written as global keys, or forced equal across markup, typescript and malva sections.

**Verified option changes (HTML):** `indentWidth:4`, `quotes:single` (`class='c'`), `closingBracketSameLine:true` (`style='...'>`), `html.void.selfClosing:true` (`<br />`, `<img ... />`), `doctypeKeywordCase:lower` (`<!doctype html>`), `scriptIndent`/`styleIndent:true`.

**Vue:** `vBindStyle:short, vOnStyle:short, vForDelimiterStyle:in, vSlotStyle:short, vBindSameNameShortHand:true, vueComponentCase:pascalCase, component.selfClosing:true, vue.scriptIndent:true` turned
`<my-comp v-bind:value="n" v-on:click="n++" v-for="i of list" :foo="foo">...<template v-slot:default=...>` / `<MyOther></MyOther>` into
`<MyComp :value="n" @click="n++" v-for="i in list" :key="i" :foo>` / `#default=` / `<MyOther />`, with the script block indented.

**Svelte:** `svelteAttrShorthand, svelteDirectiveShorthand, strictSvelteAttr, component.selfClosing` turned `<Input value={value} bind:name={name} title={"str"}></Input>` into `<Input {value} bind:name class="a {b}" title='{"str"}' />`.

**Astro:** `astroAttrShorthand:true` turned `<Layout title={title}>` into `<Layout {title}>`. Frontmatter and `{items.map(...)}` are formatted only when the typescript plugin is loaded.

## 4. pretty_yaml v0.5.1 — section key `yaml`

16 keys. Sources: schema `https://plugins.dprint.dev/g-plane/pretty_yaml/v0.5.1/schema.json`, resolver `https://raw.githubusercontent.com/g-plane/pretty_yaml/v0.5.1/dprint_plugin/src/config.rs`.

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `printWidth` | int | 80 (or global `lineWidth`) | | Soft line width | YES |
| `indentWidth` | int | 2 (or global) | | Indent size (V) | YES |
| `lineBreak` | string | `lf` | `lf`, `crlf` | Line ending | YES |
| `quotes` | string | `preferDouble` | `preferDouble`, `preferSingle`, `forceDouble`, `forceSingle` | Quote style (V) | YES |
| `trailingComma` | bool | true | | Trailing comma in multi-line flow collections | |
| `formatComments` | bool | false | | `#c` to `# c` (V) | YES |
| `indentBlockSequenceInMap` | bool | true | | Indent `- item` under a map key (V) | YES |
| `braceSpacing` | bool | true | | `{ a: 1 }` vs `{a: 1}` (V) | YES |
| `bracketSpacing` | bool | false | | `[ 1, 2 ]` vs `[1, 2]` (V) | YES |
| `dashSpacing` | string | `oneSpace` (schema omits default) | `oneSpace`, `indent` | Spaces after `-`; only effective when `indentWidth` > 2 (V) | |
| `preferSingleLine` | bool | false | | Collapse flow collections when they fit | YES |
| `flowSequence.preferSingleLine` | bool or null | null = inherit | | override for `[...]` | override |
| `flowMap.preferSingleLine` | bool or null | null = inherit | | override for `{...}` | override |
| `trimTrailingWhitespaces` | bool | true | | Trim trailing whitespace | |
| `trimTrailingZero` | bool | false | | `2.50` to `2.5` (V) | |
| `ignoreCommentDirective` | string | `pretty-yaml-ignore` | free text | | no |

There is **no `useTabs`** (YAML forbids tabs) and no `ignoreFileCommentDirective`.

**Override pattern:** `flowSequence.` / `flowMap.` + `preferSingleLine`.

**Verified:** input `#comment` / `a:   'x'` / unindented `- one`, `- k: 1`, `j: 2.50` / `flow: {a: 1, b: [1,2,\n 3]}`.
- Default: `a: "x"`, sequence indented 2, `{ a: 1, b: [1, 2, 3] }`, comment untouched.
- With `indentWidth:4, quotes:forceSingle, formatComments:true, indentBlockSequenceInMap:false, braceSpacing:false, bracketSpacing:true, dashSpacing:indent, trimTrailingZero:true`: `# comment`, `a: 'x'`, `-   one` at column 0, `j: 2.5`, `{a: 1, b: [ 1, 2, 3 ]}`.

## 5. pretty_graphql v0.2.3 — section key `graphql`

48 keys. Sources: schema `https://plugins.dprint.dev/g-plane/pretty_graphql/v0.2.3/schema.json`, resolver `https://raw.githubusercontent.com/g-plane/pretty_graphql/v0.2.3/dprint_plugin/src/config.rs`.

Base options (9 + 2):

| Key | Type | Default | Values | Meaning | Headline? |
|---|---|---|---|---|---|
| `printWidth` | int | 80 (or global `lineWidth`) | | Soft line width | YES |
| `useTabs` | bool | false (or global) | | Tabs | YES |
| `indentWidth` | int | 2 (or global) | | Indent size (V) | YES |
| `lineBreak` | string | `lf` | `lf`, `crlf` | Line ending | YES |
| `comma` | string | `onlySingleLine` | `always`, `never`, `noTrailing`, `onlySingleLine` (`inherit` is accepted but meaningless on the base key) | Commas between list items (V) | YES |
| `singleLine` | string | `smart` | `prefer`, `smart`, `never` | Collapse or force-expand lists | YES |
| `parenSpacing` | bool | false (schema wrongly says null) | | `( a: 1 )` (V) | YES |
| `bracketSpacing` | bool | false | | `[ 1, 2 ]` (V) | YES |
| `braceSpacing` | bool | true (schema wrongly says null) | | `{ x: 1 }` vs `{x: 1}` (V) | YES |
| `formatComments` | bool | false | | `#c` to `# c` (V) | YES |
| `ignoreCommentDirective` | string | `dprint-ignore` | free text | | no |

Overrides (37 keys), pattern `<nodeKind>.<baseOption>`:

| Base option | Override type | Node kinds and their defaults |
|---|---|---|
| `comma` (12) | same enum plus `inherit` | default `inherit`: `arguments`, `argumentsDefinition`, `listValue`, `objectValue`, `variableDefinitions`. Default **`never`**: `directives`, `enumValuesDefinition`, `fieldsDefinition`, `inputFieldsDefinition`, `schemaDefinition`, `schemaExtension`, `selectionSet` |
| `singleLine` (14) | same enum plus `inherit` | default `inherit`: `arguments`, `argumentsDefinition`, `directiveLocations`, `directives`, `implementsInterfaces`, `listValue`, `objectValue`, `unionMemberTypes`, `variableDefinitions`. Default **`never`**: `enumValuesDefinition`, `fieldsDefinition`, `inputFieldsDefinition`, `schemaDefinition`, `schemaExtension`, `selectionSet` |
| `parenSpacing` (3) | bool or null (null = inherit) | `arguments`, `argumentsDefinition`, `variableDefinitions` |
| `braceSpacing` (7) | bool or null (null = inherit) | `enumValuesDefinition`, `fieldsDefinition`, `inputFieldsDefinition`, `objectValue`, `schemaDefinition`, `schemaExtension`, `selectionSet` |

Difference from malva/yaml: here the enum overrides inherit via an explicit string value `"inherit"`, and many overrides default to a concrete value (`never`), not to inherit. Consequence (VERIFIED): setting base `comma` or `singleLine` alone does NOT affect selection sets, field definitions or enum values. With `comma:never, selectionSet.comma:always` the selection set got commas (`id,` / `name,`) while arguments lost them; `enumValuesDefinition.singleLine:prefer` gave `enum E {A B}`; `arguments.singleLine:never` forced arguments onto separate lines.

**Verified:** default vs `indentWidth:4, parenSpacing:true, bracketSpacing:true, braceSpacing:false, formatComments:true`: `query Q( $a: Int = 1 $b: [Int] = [ 1 2 ] )`, `opts: {x: 1 y: 2}`, `# c`, 4-space indent.

## 6. Wrong keys in `Models/FormatterSettings.cs`

VERIFIED by feeding them to the plugin ("Unknown property in configuration", exit 1):

| Array | Key in code | Status | Correct |
|---|---|---|---|
| `CssSettings` | `printWidth` | OK | |
| `CssSettings` | `tabWidth` | **WRONG** (Prettier name) | `indentWidth` |
| `CssSettings` | `useTabs` | OK | |
| `CssSettings` | `singleQuote` | **WRONG** (Prettier name, bool) | `quotes` enum: `alwaysDouble` / `alwaysSingle` / `preferDouble` / `preferSingle` |
| `HtmlSettings` | `printWidth` | OK | |
| `HtmlSettings` | `tabWidth` | **WRONG** | `indentWidth` |
| `HtmlSettings` | `useTabs` | OK | |
| `HtmlSettings` | `closingBracketSameLine` | OK (default false matches) | |
| `YamlSettings` | `lineWidth` | **WRONG** as a plugin key | `printWidth` |
| `YamlSettings` | `indentWidth` | OK | |
| `YamlSettings` | `quotes` | OK, choices and default all correct | |
| `GraphQLSettings` | `lineWidth` | **WRONG** as a plugin key | `printWidth` |
| `GraphQLSettings` | `indentWidth`, `useTabs` | OK | |

5 wrong keys of 14. `lineWidth` is valid only as a top-level global dprint key; inside a g-plane plugin section it is rejected. Since one unknown key aborts the run with exit 1, wiring the current definitions as-is would break CSS, HTML, YAML and GraphQL formatting outright.

## 7. Risks

1. Unknown stdin extension = silent passthrough, exit 0.
2. Any invalid key or value = hard failure for that language; the UI must only emit keys from these tables and must version them with the plugin pin.
3. Absolute `--stdin` path to a non-existent file fails; use a bare filename.
4. Schema defaults are wrong for 3 keys and misleading for all nullable overrides (section 0).
5. Embedded formatting needs typescript + malva (+ json) in `plugins`, and consistent indent settings across sections.
6. Conflicting pair: `maxAttrsPerLine` vs `preferAttrsSingleLine` (DOCS, schema description).
7. Doc sites describe latest, not the pinned versions.
