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
- ✅ **Parser**: v6 readers moved to **AngleSharp** (HTML5-compliant) — ADR 0002. HAP stays
  for the v5 path until the Phase D flip. Native CSS selectors retire the deferred Fizzler
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

- 🚧 **Phase E (MMD/Pandoc features)**: ✅ footnotes (ref/def collection, back-ref
  suppression, doc-end emission), ✅ metadata (MMD pairs / Pandoc YAML from `<head>`),
  ✅ citations (`<cite data-cite>` → MMD `[#key]` / Pandoc `[@key]` / default italic).
  ⏳ remaining: math (`code/span.math` → `\(..\)` / `$..$`), abbreviations
  (`<abbr title>` → `*[X]: full`), Pandoc fenced divs / bracketed spans / heading attrs /
  line blocks. All follow the established reader→DOM→writer pattern.
- ⏳ **Other remaining**: base64 `SaveToFile` IO on the v6 path; v5-default writer specifics
  if we choose to match (indented code, `* * *` HR); **Phase D flip** (`Convert` → v6 +
  remove HAP).

## Phases

### Phase 0 — Flavor enum groundwork (no behavior change)
Land `PLANNING-MMD-PANDOC.md` Tasks 0.1–0.4 on this branch first: the `MarkdownFlavor`
enum, backward-compatible boolean wrappers, and capability helpers on `Config`. These are
useful to both the v5 path (today) and v6 writers (later), and they're low-risk.
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
- Redefine `Convert(html)` ⇒ `Render(Parse(html), Config.Flavor)`.
- Delete the v5 converter classes and the `IConverter`/`TextWriter` path.
- **Re-baseline** the snapshots that changed (whitespace-only, reviewed). Tag **v6.0.0**.

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
1. **Dual-run harness.** A test mode that runs both the v5 path and the v6 path on every
   existing fixture and diffs them. Classify each diff:
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
| Long-lived branch drifts from `master` | Medium | Land Phase 0 + scaffolding fast; rebase regularly; keep v5 path intact until Phase D |

## Definition of done (v6.0.0)

- [ ] All existing fixtures pass via the v6 path (identical or reviewed re-baseline).
- [ ] `Convert`, `Parse`, `Render`, `Render(doc, flavor)` public and documented.
- [ ] Writers: Default, GFM, CommonMark, Slack, Telegram at parity.
- [ ] MMD + Pandoc writers with the node-model degradation matrix implemented.
- [ ] Issue #79: mutable DOM traversal + HTML-side and Markdown-side filtering, with tests.
- [ ] `rebaseline-log.md` complete; release notes list the breaking changes.
- [ ] v5 converter path deleted.
