# v6 Markdown DOM — Node Catalog & Degradation

The node set is a **pragmatic superset** of the features across all target flavors
(Default, GFM, Slack, Telegram, CommonMark, MultiMarkdown, Pandoc). Readers always build
the richest applicable node; writers degrade what they cannot emit.

## Block nodes (`MdBlock`)

| Node | From HTML | Key fields | Notes |
|------|-----------|-----------|-------|
| `MarkdownDocument` | root / `<body>` | `Children`, `Meta` | Document root + side-channel collectors |
| `MdHeading` | `h1`–`h6` | `Level`, inline children | Pandoc may attach `{#id .class}` from `Attributes` |
| `MdParagraph` | `p`, loose text | inline children | |
| `MdThematicBreak` | `hr` | — | |
| `MdBlockquote` | `blockquote`, `aside` | block children | |
| `MdList` | `ul`, `ol` | `Ordered`, `Start`, `Tight`, `BulletChar?` | `Tight`/loose drives blank lines (writer) |
| `MdListItem` | `li` | block children, `Checked?` | `Checked` set when `<input type=checkbox>` present (task lists) |
| `MdCodeBlock` | `pre`, `pre>code` | `Language?`, `Literal`, `PreferFenced` | Indented vs fenced chosen by writer |
| `MdTable` | `table` | `Alignments`, `Caption?` | rows/cells below |
| `MdTableRow` | `tr` | `IsHeader`, cells | |
| `MdTableCell` | `td`, `th` | inline children, `Align` | |
| `MdDefinitionList` | `dl` | items (term/definition) | MMD/Pandoc native; others degrade to a list |
| `MdHtmlBlock` | unknown / passthrough | `RawHtml` | **Escape hatch.** Replaces v5 `PassThrough` |
| `MdFootnoteDefinition` | `<li id=fn…>` in footnote section | `Id`, block children | Collected into `Meta`; emitted at doc end |
| `MdMetadataBlock` | `<meta>` / `<head>` | key-values | MMD: `Key: Value`; Pandoc: YAML frontmatter |
| `MdFencedDiv` | `div[class]` | `Attributes`, block children | Pandoc `::: {.x}`; others bypass to children |
| `MdLineBlock` | `div.line-block` | lines | Pandoc `\| line`; others degrade to paragraph+breaks |

## Inline nodes (`MdInline`)

| Node | From HTML | Key fields | Notes |
|------|-----------|-----------|-------|
| `MdText` | text node | `Value` | Escaping is the writer's job |
| `MdStrong` | `strong`, `b` | inline children | `**` / `*` (Slack,TG) per writer |
| `MdEmphasis` | `em`, `i` | inline children | `*` / `_` per writer |
| `MdStrikethrough` | `s`, `del`, `strike` | inline children | `~~` / `~` (Slack,TG); CommonMark → raw HTML |
| `MdInlineCode` | `code` | `Literal` | backtick fencing chosen by writer |
| `MdLink` | `a` | `Url`, `Title?`, inline children | smart-href handling moves to writer |
| `MdImage` | `img` | `Url`, `Title?`, `Alt` | base64 handling stays a reader/Config concern |
| `MdLineBreak` | `br` | `Hard` | two-space / `\n` / `\` per writer |
| `MdSuperscript` | `sup` | inline children | `^x^`; CommonMark → raw HTML |
| `MdSubscript` | `sub` | inline children | `~x~`; only MMD/Pandoc native |
| `MdMath` | `code.math`, `span.math` | `Literal`, `Display` | MMD `\(..\)` / Pandoc `$..$` |
| `MdFootnoteReference` | `<sup><a href=#fn>` | `Id` | MMD/Pandoc `[^id]`; others degrade |
| `MdCitation` | `cite[data-cite]` | `Key` | MMD `[#key]` / Pandoc `[@key]` |
| `MdBracketedSpan` | `span[class]` | `Attributes`, inline children | Pandoc `[txt]{.x}`; others bypass |
| `MdRawInline` | unknown inline | `RawHtml` | Inline escape hatch |

## Side-channel data (`MarkdownDocument.Meta`)

Some constructs are collected during reading and emitted by the writer at fixed document
positions, not inline:

- `Footnotes` — `Dictionary<string, MdFootnoteDefinition>` → end of document.
- `Abbreviations` — `Dictionary<string,string>` → MMD `*[ABBR]: full` at end.
- `Metadata` — ordered key-values → top of document (MMD pairs / Pandoc YAML).

This mirrors `PLANNING-MMD-PANDOC.md` Tasks 1.4 / 1.7 but centralizes collection in the
reader context instead of mutating `ConverterContext` from inside converters.

## Degradation matrix

What each writer does with a node it does **not** support natively. `native` = first-class
syntax; `raw` = emit verbatim HTML; `bypass` = emit children only (drop wrapper);
`text` = inline plain text; `throw` = `*UnsupportedTagException` (preserves v5);
`list` = render as ordinary list.

| Node | Default | GFM | CommonMark | Slack | Telegram | MMD | Pandoc |
|------|---------|-----|------------|-------|----------|-----|--------|
| Strong / Em | native | native | native | native(`*`/`_`) | native | native | native |
| Strikethrough | native | native | raw | native(`~`) | native | native | native |
| Table | native | native | raw | throw | native(code) | native | native |
| CodeBlock | indent | fenced | fenced | indent | fenced | fenced | fenced |
| Image | native | native | raw | throw | bypass | native | native |
| Superscript | native(`^`) | native | raw | throw | native | native | native |
| Subscript | raw | raw | raw | throw | raw | native(`~`) | native(`~`) |
| Footnote ref/def | text | text | text | text | text | native | native |
| Definition list | list | list | list | list | list | native | native |
| Math | raw | raw | raw | raw | raw | native(`\(`) | native(`$`) |
| Citation | text | text | text | text | text | native(`[#]`) | native(`[@]`) |
| Metadata | drop | drop | drop | drop | drop | native(pairs) | native(YAML) |
| Abbreviation | text | text | text | text | text | native+defs | text |
| Fenced div | bypass | bypass | bypass | bypass | bypass | bypass | native(`:::`) |
| Bracketed span | bypass | bypass | bypass | bypass | bypass | bypass | native(`{}`) |
| Heading attrs | drop | drop | drop | drop | drop | drop | native(`{#}`) |
| Line block | para | para | para | para | para | para | native(`\|`) |

The cells marked `throw`/`native(code)`/`bypass` for Slack/Telegram/CommonMark exactly
reproduce v5 behavior — see `S.cs`, `Img.cs`, `Table.cs`, `Hr.cs` for the current rules to
port. The MMD/Pandoc columns implement `PLANNING-MMD-PANDOC.md` Phases 1–3, but now as
writer methods rather than scattered `if (Config.X)` branches.

## What we deliberately do **not** model in v6.0

To bound scope, these stay as raw HTML (`MdHtmlBlock`/`MdRawInline`) for now:

- Pandoc `Div`/`Span` with complex nested attribute filters beyond `id`/`class`.
- Grid tables, multiline tables (pipe tables only).
- Inline/reference link *style* preference (always inline links in 6.0).
- Custom Pandoc raw blocks (`{=html}`, `{=latex}`).

Adding any of these later is **one node + one-or-two writer methods** — the whole point of
the architecture.
