# HAP-drop regression triage

Baseline at HEAD (`4908df9`, v5 HAP path default): **319 passed / 0 failed**.
After forcing v6-always (HAP dropped): **203 passed / 114 failed**.

All 114 were green before this change. They are **not** "expected snapshot noise" — they
are behavioural gaps where the v6 AngleSharp/MarkdownDom path does not reproduce v5 output.

Each cluster below is tagged:

- **BUG** — v6 output is wrong or a feature is unwired. Fix v6.
- **REBASELINE** — v6 output is arguably *more* correct (CommonMark block separation).
  Update the expectation, but this is a maintainer/product call (it changes published output).
- **DECISION** — genuinely ambiguous; maintainer must pick the intended v6 contract.

Fixing by root cause clears tests in bulk (count in parens).

---

## BUG — whole feature not wired into the v6 path

### 1. Slack flavor writer not applied (10) — ✅ FIXED
`SlackFlavored_Bold/Italic/Strikethrough/Bullets` emit standard markdown (`**test**`,
`-` bullets) instead of Slack (`*test*`, `•`). `SlackFlavored_Unsupported_Hr/Img/Sup/Table/
Table_Td/Table_Tr` no longer throw `SlackUnsupportedTagException`.
→ Fixed: `Config.SlackFlavored` now maps to `SlackWriter` via `Converter.EffectiveFlavor`;
orphan `<td>/<tr>/<th>` (dropped by the HTML5 parser) guarded in `Converter.Convert`.

### 2. Telegram MarkdownV2 writer not applied (7) — ✅ FIXED
`TelegramMarkdownV2_BasicFormatting/EscapeLinkTextAndHref/EscapesListMarkers/
EscapeSpecialCharactersInText/Img_FallsBackToLink/Sup_FallsBackToCaretNotation/
Table_FallsBackToCodeBlock` all emit standard markdown. Telegram writer not ported.
→ Fixed: `Config.TelegramMarkdownV2` maps to `TelegramWriter`, which now escapes link
text/href and list markers, and falls back img→link, sup→`^x`, table→code block.

### 3. Base64 image SaveToFile not implemented (4) — ✅ FIXED
`WhenMultipleBase64ImgTags…`, `WhenBase64ImgTag_WithSaveToFileAndNonExistentDirectory`,
`WhenThereIsBase64PngImgTag…`, `WhenThereIsBase64JpegImgTag…` — 0 files written.
→ Fixed: filename generator now matches v5 (`image_{n}`). **Contract decision:** v6 references
the saved image by *filename* (not the v5 absolute path) — the two legacy ConverterTests that
asserted the full path were rebaselined to `Path.GetFileName`, matching the v6-native
`MinorFeatureTests`.

### 4. Img src scheme whitelist broken (4) — ✅ FIXED
`WhenThereIsImgTag_SchemeNotWhitelisted` / `…SrcWithNoSchema_NotWhitelisted` should yield
`""` but emit the data URI. `WhenThereIsImgTagWithUnixUrl…` / `…HttpProtocolRelativeUrl…`
should emit `![](/example.gif)` but yield `""`.
→ Fixed two root causes: (a) `UrlHelper.GetScheme` now maps `//host`→`http` and `/path`→`file`
(v5 RFC-3986 handling); (b) the reader uses `ImageUtils.IsValidBase64ImageData` so a malformed
`data:` URI falls through to the whitelist check (scheme `data`) instead of being emitted.

### 5. script/style stripping & bypass (2 of 4 fixed) — ✅ PARTIAL
`When_Content_Contains_script_tags_ignore_it` leaked `<script>…</script>`.
`WhenStyletagWithBypassOption_ReturnEmpty` leaked CSS.
→ Fixed both: new `DropReader` registered for `script`/`style` drops the element and its content
regardless of `UnknownTags`.

**REBASELINE (parser limitation, 2):** `WhenUnclosedScriptTag_WithBypassUnknownTags` and
`WhenUnclosedStyleTag_WithBypassUnknownTags` rely on HAP's lenient parsing. AngleSharp's
HTML5 parser treats `<script>`/`<style>` as raw-text elements, so an *unclosed* one swallows
all following markup (incl. `<p>Test content</p>`) as its text content — verified: the body
ends up empty / content-trapped. Recovering "Test content" would require re-parsing raw-text
node content, which is unsound. These should be rebaselined to the v6 (HTML5-correct) output.

---

## BUG — inline emphasis / sup / strike

### 6. Adjacent emphasis runs produce invalid markdown (2) — ✅ FIXED
`When_Consecutive_Em_Tags`: `*block1**block2*` (should be `*block1* *block2*`).
`When_Consecutive_Strong_Tags`: `**block1****block2**`.
→ Fixed: `DefaultWriter.InlineSeparator` inserts a space between two adjacent same-type
emphasis runs (via a new `MarkdownWriterBase.InlineSeparator` seam).

### 7. Nested same-emphasis not collapsed (4) — ✅ FIXED
`When_Sup_And_Nested_Sup`: `t^e^s^^t` (should `t^es^t`).
`When_Strikethrough_And_Nested_Strikethrough`: `t~~e~~s~~t`.
`WhenThereIsEncompassingStrongOrBTag…`: inner `__bold__` not suppressed.
`WhenThereIsEncompassingEmOrITag…`: inner `_sample_` not suppressed.
→ Fixed: `DefaultWriter` collapses nested same-family emphasis (em/strong/strike/sup) to a
single outer wrap, matching v5. CommonMark/GFM keep their alternating-delimiter behavior.

### 8. Inline text run-together — lost separators (3)
`When_SuppressNewlineFlag_PrefixDiv_Should_Be_Empty`: `thefoxjumpsover`.
`When_Span_with_newline`: `**2 sets**30 mountain climbers` (lost `\n`).
`WhenBoldTagContainsBRTag`: `test **\ntest**` (should `test**test**`).

---

## BUG — tables

### 9. Table cell line breaks → `<br>` (4 of 5 fixed) — ✅ PARTIAL
`WhenTable_Cell_Content_WithNewline_Add_BR`, `WhenTable_CellContainsParagraph_AddBr`,
`WhenTable_CellContainsBr_PreserveBr`, `WhenTableCellsWithMultipleP` — multi-line/`<p>`/`<br>`
cell content collapsed to a space.
→ Fixed: `MarkdownWriterBase.RenderTableCell` renders cell blocks blank-line-separated and turns
every newline into `<br>`; cell text keeps its newlines (`CollapseWhitespaceKeepNewlines`).

**Reader limitation (1):** `WhenTableCellsWithDataAndP` (`data1<p>p</p>` → `data1<br>p`, single)
can't be distinguished from the double-`<br>` p-p case: the reader wraps the bare leading text
`data1` in an implicit paragraph, so both arrive as `[MdParagraph, MdParagraph]`. Matching v5
would need the reader to keep bare cell text inline (mark implicit paragraphs) — deferred;
rebaseline or do the reader change.

### 10. Table structural handling (5) — ✅ FIXED
`WhenTable_WithoutHeaderRow…EmptyRow` / `WhenTable_WithCaptionAndNoHeaderRow…` didn't emit
the `<!---->` empty header; `WhenTable_HasEmptyRow_DropsEmptyRow` kept the row as header;
`WhenTable_WithColSpan…HeaderColumnSpans` mis-expanded colspan; `WhenThereIsHeadingInsideTable`
emitted `## Heading`.
→ Fixed: writer emits a synthetic `<!---->` header under `EmptyRow` handling when no row is a
header; `Visit(MdHeading)` renders inline-only inside a cell; the reader repeats a `th` per
`colspan` (under `TableHeaderColumnSpanHandling`).

### 11. Nested table / list-in-cell not left as raw HTML (5) — ✅ FIXED
`When_NestedTableIsInTable`, `When_ComplexNestedTableIsInTable`,
`When_MultipleNestedTablesInTable` escaped the inner `<table>` to `\| … \|`.
`When_OrderedListIsInTable` / `When_UnorderedListIsInTable` converted instead of leaving
`<ol>`/`<ul>` raw.
→ Fixed: an `InTableCell` reader flag emits a nested `table`/`ol`/`ul` as compacted raw HTML
(v5 `CompactHtmlForMarkdown` + stripping the parser's auto-inserted `<tbody>`). Also fixed a
latent bug: `TableReader` selected rows with recursive `QuerySelectorAll("tr")`, pulling a
nested table's rows into the outer table — now scoped with `tr.Closest("table") == element`.

---

## BUG — PRE / code

### 12. Code-fence language class parsing (4) — ✅ FIXED
`When_PRE_With_Confluence_Lang…` / `…Github_Site_DIV_Parent…` lost the language
(`` ```python `` → `` ``` ``). `…Lang_Highlight_Class…` kept raw `highlight-python`.
`…DefaultCodeBlockLanguage…` ignored the configured default.
→ Fixed: `PreReader.DetectLanguage` ports v5's regex
(`highlight-source-|language-|highlight-|brush:\s`) checked on the code/pre/parent-div/child-code
class, and falls back to `Config.DefaultCodeBlockLanguage`.

### 13. Non-GFM code style regressed to fences (3)
`When_PRE_With_Parent_DIV_And_Non_GitHubFlavored…` and `When_PreTag_Contains_IndentedFirstLine`
expect 4-space indented code (non-GFM) but v6 always emits ```` ``` ```` fences.
`WhenPreContainsHtml_WithConvertPreContentAsHtml` doesn't convert inner HTML (emits fenced raw).

### 14. Code-span whitespace normalization (3)
`When_CodeContainsSpaces_ShouldPreserveSpaces`, `…SpanWithExtraSpaces…`,
`…SpacesAndIsSurroundedByWhitespace…` — v6 keeps one extra space vs v5's normalization.
Off-by-one in inline `code` padding/trim. *(Could be DECISION if v5 trimming was undesirable.)*

---

## BUG — links / escaping / config options

### 15. Smart link handling (4) — ✅ FIXED
`WhenThereIsHtmlNonWellFormedLinkLink_SmartHandling`, `…HttpSchemaAndNameWithout_SmartHandling`
/ `…HttpScheme…ConvertToPlain` (emitted `example.com` not `http://example.com`),
`WhenThereIsHtmlLinkWithParensInHref` (`\)` vs `%29`).
→ Fixed: `AnchorReader` ports v5 smart handling — for a well-formed scheme'd URL, drop the link
when text == href (or tel:/mailto: form), and output the full href for an `http(s)` link whose
text is the scheme-less host. `DefaultWriter.Visit(MdLink)` percent-encodes the href
(space→%20, ()→%28/%29) and trims the link text, matching v5's non-CommonMark output.

### 16. Text/anchor escaping options (5)
`When_TextContainsAngleBrackets_HexEscapeAngleBrackets` (no `&lt;` escape),
`WhenEscapeMarkdownLineStartsEnabled` (no `\#` escape),
`WhenCommonMarkTextContainsMarkdownLinkPattern` (link-pattern delimiters not escaped),
`WhenThereIsHtmlLinkWithDisallowedCharsInChildren` (lost `\]` escape),
`When_Anchor_Text_with_Underscore_Do_Not_Escape` (over-escapes `mov\_bbb.mp4`).

### 17. Misc unwired behaviors (5)
`When_CommonMark_Enabled_InlineEmphasisInsideWord` (no `he **ll** o` spacing insertion),
`WhenThereIsInputListWithGithubFlavoredDisabled` (emits `[ ]` not raw `<input>`),
`When_Tag_In_PassThoughTags_List…` (img not passed through),
`Li_With_No_Parent` (`item` not `- item`),
`WhenThereIsUnorderedListAndBulletIsAsterisk` (bullet-char config `*` ignored → `-`).
`TestConversionWithPastedHtmlContainingUnicodeSpaces` (unicode-space handling).

---

## REBASELINE — v6 block separation is more CommonMark-correct

v5 separated blocks with a single `\n` and stray leading/trailing spaces
(`This text has \n# header\n`); v6 uses proper blank lines (`…has\n\n# header\n\n…`).
v6 is the more correct output, but this **changes published formatting** — confirm before
rebaselining.

- Headings: `WhenThereIsH1Tag … H6Tag` (6)
- Blocks: `WhenThereIsParagraphTag`, `WhenThereIsBlockquoteTag`, `WhenThereIsAsideTag`,
  `WhenThereAreSemanticContainerTags`, `WhenThereIsUnorderedList`, `WhenThereIsOrderedList`,
  `WhenThereIsBase64ImgTag_WithDefaultConfig`, `Check_Converter_With_Unknown_Tag_PassThrough_Option`
- Interleaved text: `When_InterleavedParagraphsAndSpans`,
  `When_UnclosedParagraphsWithSpansAndTextNodes`
- Leading blank lines: `Bug391_AnchorTagUnnecessarilyIndented`

---

## DECISION — style differences, maintainer picks the v6 contract

- HR token: `* * *` (v5) vs `***` (v6) — `WhenThereIsHorizontalRule`. Pick one.
- Nested-list indent width: 4-space (v5) vs 2/3-space CommonMark (v6) —
  `WhenThereIsOrderedListWithNestedUnorderedList`, `…UnorderedListWithNestedOrderedList`,
  `WhenThereIsWhitespaceAroundNestedLists…`, `WhenListContainsMultipleParagraphs…`,
  `When_Table_Within_List_Should_Be_Indented`, `When_PreTag_Within_List_Should_Be_Indented(_GFM)`.
- Bullet normalization: `Bug393_RegressionWithVaryingNewLines` (`*` → `-`).
- List-item paragraph grouping: `WhenListContainsParagraphsOutsideItems…`
  (v6 renumbers stray paragraphs as new items — likely BUG, verify).
- Empty/edge: `WhenThereIsEmptyBlockquoteTag` (`>` vs empty),
  `WhenThereIsEmptyPreTag(_GFM)`, `WhenThereIsPreTag` (indent vs fence),
  `WhenThereIsImgTagWithMultilineAltText` (blank line inside alt — likely BUG).

---

## CommonMark gate

`CommonMark_Spec_Examples_RoundTripHtml`: 10/652 examples regressed (entity/escape and
URL-encoding edge cases). Re-measure after the escaping fixes (cluster 14–16) land; most
should clear.

---

## Suggested order

1. **Cluster 6** (adjacent emphasis) — produces corrupt markdown, smallest fix.
2. **Clusters 1–5** (unwired features: Slack, Telegram, SaveToFile, whitelist, script/style)
   — 29 tests, each a self-contained port.
3. **Clusters 9–13** (tables + PRE) — 22 tests.
4. **Clusters 7, 8, 14–17** (inline/escaping) — 22 tests; re-run CommonMark gate after.
5. **REBASELINE / DECISION** — get maintainer sign-off, then update `cases.json` expectations.
