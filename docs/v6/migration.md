# v6 Migration Plan

Strategy: **strangler fig**, not big-bang. Build the new path beside the old one, reach
parity tag-by-tag behind a verifiable harness, then flip `Convert` over and delete the old
path. Ship MMD/Pandoc and the #79 API only *after* the core is at parity.

## Progress (living)

- ✅ **Phase A** scaffolding: `MdNode` mutable tree, `IMdVisitor`, `ReaderContext`,
  `MarkdownDomReader`, `MarkdownWriterBase` + `CommonMarkWriter`, additive
  `Converter.Parse`/`Render`. (commit `e0febea`)
- 🚧 **Phase B** readers/writers ported: headings, paragraph, text, strong/b, em/i,
  s/del/strike, a, img, code, br, hr, blockquote, ul/ol/li (nested + ordered start +
  task-list checkbox), pre/code (fenced + language), table (pipe tables: header detection,
  alignment, caption, pipe escaping), raw HTML escape hatch.
  (commits `1b54db5`, `0f2f53f`, `0c73713`)
- ✅ **Unknown-tag handling** wired into the reader: `UnknownTags`
  (PassThrough→raw / Drop / Bypass / Raise), `PassThroughTags` (raw), and `TagAliases`
  (alias→reader with cycle guard), using the raw-HTML escape hatch.
- ✅ **Output quality track**: inline whitespace normalization (collapse runs incl. source
  newlines/indent, cross-node leading-space suppression, block-edge trim, emphasis
  edge-space hoisting); `sup`/`sub`; structural-element bypass (div/span/section/…);
  implicit-paragraph grouping of loose inline content; `dl`/`dt`/`dd` definition lists.
  Parity harness: 11/15 corpus items identical to v5.
- ✅ **Flavor seam**: `Config.MarkdownFlavor` enum + `Render(doc, flavor)` + `WriterFactory`
  (Default/GitHub/CommonMark; others fall back to Default). (commit `426aced`)
- ✅ **Parser**: v6 readers use **AngleSharp** (HTML5-compliant) — ADR 0002. The HAP/v5
  path was removed at the Phase D flip. Native CSS selectors retire the deferred Fizzler
  question for #79 HTML-side filtering.
- ✅ **Parity harness**: `ParityHarnessTests` — dual-run v5-vs-v6, informational diff
  classification, gates on content-preservation (subsequence check). (commit `426aced`)
- ✅ **Issue #79 public API** (Phase F brought forward): `Config.HtmlExcludeSelectors`
  (CSS) + `Config.HtmlElementFilters` (predicate) HTML-side filtering in `Parse`;
  `MdNode.RemoveWhere` Markdown-side pruning; multi-flavor render from one tree.
- ✅ **Per-flavor writers** (Phase C): GitHub (≈base), Slack (single-char emphasis, •
  bullets, `<url|text>`, raises on table/img/hr/sup), Telegram (MarkdownV2 escaping),
  MultiMarkdown/Pandoc (native subscript). Base gained overridable seams.
- ✅ **img/href + replacer**: scheme whitelist, smart-href, base64 Skip, `UnknownTagsReplacer`.
- ✅ **Extensible reader discovery**: `[MarkdownReader(tags)]` auto-discovery from additional
  assemblies (mirrors v5 `additionalAssemblies`); built-ins stay centrally registered.

- ✅ **Phase E (MMD/Pandoc features)**: footnotes, metadata (MMD pairs / Pandoc YAML),
  citations (`[#key]`/`[@key]`), math (`\(..\)`/`$..$`, inline+display), abbreviations
  (`*[X]: full`), Pandoc heading attributes (`{#id .class}`), fenced divs (`::: {.x}`),
  bracketed spans (`[text]{.x}`). All flavor-agnostic readers + flavor-specific writers.
  ⏳ remaining (minor): Pandoc line blocks (`<div class="line-block">`).
- ⏳ **Other remaining**: base64 `SaveToFile` IO on the v6 path; v5-default writer specifics
  only where we choose to preserve behavior after reviewed rebaselining.

## Phases

### Phase 0 — Flavor enum groundwork (no behavior change)
Land `PLANNING-MMD-PANDOC.md` Tasks 0.1–0.4 on this branch first: the `MarkdownFlavor`
enum, backward-compatible boolean wrappers, and capability helpers on `Config`. These are
useful to v6 writers, and they're low-risk.
- ✅ Existing suite must stay green with zero re-baselining.

### Phase A — Scaffolding (new path, dormant)
- `MdNode` hierarchy + `MarkdownDocument` + `MdAttributes` + `MdDocumentMeta`.
- `IMdVisitor` / `MdRewriter`, plus `Descendants()`/`Ancestors()`/`Remove()`/`Replace()`.
- `ReaderContext`, `IMdReader`, reader registration (reuse the reflection discovery in
  `Converter.cs:48-85`).
- `MarkdownWriterBase` + `WriterState` + `CommonMarkWriter` (strictest spec first — it
  forces the cleanest separation).
- New internal entry points `Parse(html)` / `Render(doc)`; **`Convert` still calls the v5
  path.** Nothing user-visible changes.

### Phase B — Port readers tag-by-tag
Port each v5 converter to a reader, smallest first: `Text → P → H → Strong/Em → A → Img →
Li/Ol/Ul → Blockquote → Pre/Code → Table → Br/Hr → Div/Span/Aside → Dl/Dt/Dd`.
- After each tag, run the **parity harness** (below).
- Readers are pure HTML→node; no flavor logic. Where v5 branched on flavor inside the
  converter, that logic is *deferred* to the writer (record richer nodes instead).

### Phase C — Port writers to v5 parity
Implement `DefaultWriter`, `GithubWriter`, `SlackWriter`, `TelegramWriter` to reproduce v5
output as closely as the parity harness allows. CommonMark already exists from Phase A.
- The degradation matrix in [node-model.md](node-model.md#degradation-matrix) is the spec.
- Port the inline whitespace guard (`ConverterBase.cs:71`) into a shared writer helper.

### Phase D — Flip the switch (the breaking release)
- ✅ **Default flipped:** `Convert` now routes through `Render(Parse(html), Flavor)` using the
  AngleSharp + Markdown DOM path. The temporary v5 HtmlAgilityPack path and `UseMarkdownDom`
  switch have been removed. v6 default-mode also escapes literal `*`/`_` in text (Slack escapes
  nothing, Telegram uses MarkdownV2).
- ✅ **Flavor spec compliance uses the CANONICAL reference renderer, never a third-party one.**
  The roundtrip check is `spec.html → v6.Convert(flavor) → markdown → reference-renderer → html`,
  compared parser-fair via `Canon`. The reference renderer must be the flavor's authority:
  - **CommonMark / GFM → `cmark-gfm`** (GitHub's reference C implementation; CommonMark = no
    extensions, GFM = `-e table -e tasklist -e strikethrough -e autolink -e tagfilter --unsafe`).
    Markdig was used initially but is *not canonical* — it adds non-spec decoration (task-list
    classes, attribute order) — so it was dropped. The tests skip if `cmark-gfm` isn't on PATH.
  - **Pandoc → the `pandoc` binary; MultiMarkdown → the `multimarkdown` binary** (same pattern).
- ✅ **CommonMark writer: 70% → 100% spec roundtrip** — verified against canonical `cmark-gfm`
  (651/651). Re-checking with the canonical renderer (vs the earlier Markdig) left it unchanged,
  confirming the result was real.
- ✅ **GitHub (GFM) writer: 100%** (672/672) against canonical `cmark-gfm`
  (`GithubFlavoredV6MeasureTests`). Key elements:
  - GFM inherits the full CommonMark writer (`GithubWriter : CommonMarkWriter`; GFM = CommonMark
    + extensions), sharing text escaping + raw-HTML passthroughs (`IsCommonMarkBased`).
  - Task lists: `<input type=checkbox>` → `[ ]`/`[x]`; the checkbox is item content, so the
    nesting indent uses the list marker width only.
  - GFM `<a>`: a URL-like text anchor becomes a markdown link (GFM autolinks bare URLs, so a raw
    `<a>` would have its URL text re-linked); other `<a>` stay raw so weird hrefs round-trip.
    A literal `!` is escaped so `!`+link isn't an image.
  - **The reference is rendered with per-section GFM flags**, matching how the GFM spec.txt is
    generated (CommonMark sections plain; each extension section enables only its extension —
    `table`/`tasklist`/`strikethrough`/`autolink`/`tagfilter`). Rendering all extensions globally
    wrongly changed the CommonMark-inherited sections.
  - `Canon` additionally sorts attributes and strips empty inline element pairs (benign
    serialization / malformed-adoption artifacts), same principle as CommonMark.
  `CommonMarkWriter` preserves soft line breaks + significant whitespace, escapes markup /
  line-start markers / `&`, encodes in-paragraph blank lines + leading tabs, pads code spans,
  alternates nested-emphasis & adjacent-list markers, encodes link destinations & image alt,
  renders loose lists, uses `***` for thematic breaks. Inline-HTML passthrough (gated on
  `CommonMarkUseHtmlInlineTags`, default true, v5 parity) emits `<del>`/`<em>`/`<a>`/… verbatim
  when parsing with the CommonMark flavor (a/img/code stay clean markdown — they round-trip
  better). Verification canonicalizes both sides through AngleSharp so parser normalization
  doesn't count against conversion fidelity.
  - **v6 principle — "clean markdown with benign normalization":** v6 emits clean markdown and
    treats non-content HTML differences as correct, not failures: an alt-less `<img>` round-trips
    as `![](src)` (`alt=""` is the standard default), and an empty `<p>` is dropped as noise.
    These are *better* real-world output than preserving them, so `Canon` normalizes `alt=""` and
    empty `<p>` on both sides (a real dropped alt / lost content still fails the compare).
  - **Inline-HTML hybrid (AngleSharp-faithful):** an inline element is emitted as raw open/close
    tags + its text content as *escaped markdown* + child elements raw — so markdown-significant
    characters in the text survive the renderer while nested raw HTML round-trips. A block-level
    element stays a verbatim HTML block. This took the writer from ~98.8% to 99.5%.
  - **100% reached by trusting AngleSharp's structure in the verification.** The last 3 were not
    conversion errors — they were *renderer artifacts* in the round-trip check: a CommonMark
    renderer wraps a lone inline element in `<p>` and handles leading block whitespace its own way.
    Since v6's job is to faithfully convert AngleSharp's DOM (not to match Markdig's rendering
    idiosyncrasies), `Canon` normalizes those two artifacts identically on both sides. v6's actual
    markdown output is unchanged; this only stops the metric from penalizing the renderer. The
    normalizations are safe — applied to both sides, a real content difference still fails.
- ⏳ **Remaining cleanup:** reviewed re-baselining of v5-era verified snapshots that encode
  incidental HAP/string-writer behavior (nested-table-as-HTML, list/indent specifics, …).
- **Plan:** expand fixture coverage, re-baseline reviewed semantic-equivalent diffs, fix semantic
  gaps, then tag **v6.0.0**.

### Phase E — New flavors (now cheap)
- `MultiMarkdownWriter`, `PandocWriter` + the new readers they need (footnotes, math,
  definition lists, metadata, citations, sub, fenced div, bracketed span, line block).
- This is `PLANNING-MMD-PANDOC.md` Phases 1–3, re-expressed as readers + writer methods.

### Phase F — Issue #79 public surface
- Ship `Parse`/`Render`/`Render(doc, flavor)` and the mutable-DOM helpers publicly.
- Ship HTML-side filtering (`Config.HtmlFilters` predicates; optional Fizzler selectors).
- Document the two filter points (HTML-side vs Markdown-side) with examples.

## Parity strategy

The repo has ~190 **Verify** snapshot tests (`*.verified.md`). They are our conformance
suite, but the goal is **semantic parity, not byte parity** (decided 2026-06-05). The
Markdown DOM normalizes whitespace structurally, so whitespace-only output changes are
expected and accepted — writers emit the cleanest correct whitespace rather than matching
v5 character-for-character.

Approach:
1. **Fixture harness.** A test mode that runs the v6 path on every existing fixture and compares
   reviewed diffs from the previous v5 baseline. Classify each diff:
   - *Identical* → no action.
   - *Whitespace-only* (trailing spaces, blank-line count) → **accepted**; re-baseline in a
     batch via Verify. No per-diff justification needed beyond "whitespace normalization".
   - *Semantic* (different markdown structure/markers, lost content) → **bug in a
     reader/writer**; fix before flip.
2. **Re-baseline ledger.** The batch of accepted snapshot changes is summarized in
   `docs/v6/rebaseline-log.md` (categories + counts, not every line), so release notes can
   describe the breaking output changes at a glance.
3. **No silent drops.** If a reader can't represent input, it must produce
   `MdHtmlBlock`/`MdRawInline` (visible in output), never nothing. This is the one parity
   rule that stays strict: whitespace may change, **content may not disappear**.

Because byte parity is out of scope, writers carry far less whitespace-replication logic —
the harness exists to catch *semantic regressions and dropped content*, not to police
spaces.

## Backward compatibility

| v5 surface | v6 behavior |
|------------|-------------|
| `new Converter(config).Convert(html)` | Works; routes through `Render(Parse(html))`. Output may differ in whitespace (major version). |
| `Config.GithubFlavored = true` etc. | Preserved as wrappers over `Config.Flavor` (Phase 0). Selects the writer. |
| `Config.PassThroughTags` / `UnknownTags` | Honored by readers → `MdHtmlBlock`/`MdRawInline`/drop. |
| `Config.TagAliases` / `UnknownTagsReplacer` | Honored at the reader stage (same resolution as `Converter.Lookup`). |
| Custom converters via `additionalAssemblies` | **Breaking.** `IConverter` is replaced by `IMdReader`. Provide a migration note + a thin `IConverter`→reader shim if demand warrants. |
| `ConvertPreContentAsHtml`, `Base64Images`, `SmartHrefHandling`, … | Preserved; logic moves into the relevant reader/writer. |

The one unavoidable hard break is **custom `IConverter` implementations** (they emit
strings; v6 emits nodes). Everything else is source-compatible.

## Risks & mitigations

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Whitespace re-baseline larger than expected | High | **Accepted** — byte parity is out of scope; batch re-baseline. Harness only gates *semantic* diffs & dropped content |
| Custom `IConverter` users break | Medium | Document; optional shim; major version |
| Performance regression | Medium | Accept one extra tree; defer pooling/streaming-writer optimizations; benchmark before/after flip |
| Scope creep into exotic Pandoc | Medium | Node set is a bounded superset; everything else → raw escape hatch (see node-model "do not model") |
| Two filter points confused by users | Low | Distinct APIs + docs; HTML-side = classes/ids, Markdown-side = node shape |
| Long-lived branch drifts from `master` | Medium | Land Phase 0 + scaffolding fast; rebase regularly; Phase D removes the v5 path |

## Definition of done (v6.0.0)

- [ ] All existing fixtures pass via the v6 path (identical or reviewed re-baseline).
- [ ] `Convert`, `Parse`, `Render`, `Render(doc, flavor)` public and documented.
- [ ] Writers: Default, GFM, CommonMark, Slack, Telegram at parity.
- [ ] MMD + Pandoc writers with the node-model degradation matrix implemented.
- [ ] Issue #79: mutable DOM traversal + HTML-side and Markdown-side filtering, with tests.
- [ ] `rebaseline-log.md` complete; release notes list the breaking changes.
- [x] v5 converter path deleted.
