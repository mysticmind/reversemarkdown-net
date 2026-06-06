# ADR 0002 — AngleSharp as the v6 reader's HTML parser

- Status: **Accepted**
- Date: 2026-06-05
- Supersedes: HtmlAgilityPack
- Context branch: `feature/v6-markdown-dom`

## Context

v5 parsed HTML with HtmlAgilityPack (HAP) and exposed `HtmlNode` through `IConverter`.
The v6 reader stage is the *only* place the HTML parser is touched — everything downstream
(Markdown DOM, writers, the parity harness) is parser-agnostic. Since the readers are
being rewritten for v6 anyway, this is the cheapest moment to reconsider the parser; after
v6 stabilizes, the readers stop changing and a swap becomes expensive again.

Two pressures motivated the change:

1. **Malformed-HTML correctness.** HAP is a lenient, non-spec parser. The test suite
   carries scar tissue from its quirks (`When_DeeplyNestedParagraphs_WithMalformedHTML_…`,
   `When_ManySequentialUnclosedParagraphs`, `When_UnclosedParagraphsWithSpansAndTextNodes`).
2. **CSS selectors for issue #79.** We deferred a Fizzler dependency for HTML-side
   filtering (ADR/decision in docs/v6). A spec parser with native `QuerySelectorAll` removes
   the need for Fizzler entirely.

## Decision

Use **AngleSharp** (WHATWG HTML5-compliant parser) for the reader path.
`IMdReader.Read` takes AngleSharp's `IElement`; text/comment nodes are dispatched by
`MarkdownDomReader`. `Converter.Parse` parses with a reusable `HtmlParser` and reads
`document.Body`.

HtmlAgilityPack and the v5 `IConverter`/`HtmlNode` path were removed at the Phase D flip.

## Consequences

### Positive
- Spec-compliant tree construction (implied tags, table foster-parenting, formatting
  reconstruction) eliminates a class of malformed-HTML bugs at the parse layer.
- Entity decoding is handled by AngleSharp (`IText.Data`, `GetAttribute`), so readers drop
  the manual `WebUtility.HtmlDecode` calls — simpler readers.
- Native CSS selectors (`QuerySelector`) are available for #79 HTML-side filtering, retiring
  the deferred Fizzler question.
- Real DOM semantics + an actively maintained dependency.

### Negative / costs
- New runtime dependency (`AngleSharp`) on the main library.
- Public-API break for the v6 reader surface: `IMdReader` exposes `IElement`, not
  `HtmlNode`. Appropriate for a major version.
- The parity harness no longer has the old HAP path as a runtime oracle; v6 correctness is gated
  against canonical flavor renderers and reviewed fixture rebaselines.
- AngleSharp builds a richer DOM (somewhat heavier than HAP). Not a concern for 6.0.

### Notes
- `HtmlParser` is reused across `Parse` calls (it holds no per-parse mutable state), keeping
  `Converter` thread-safe.
- AngleSharp follows the HTML spec's "ignore a single leading newline in `<pre>`" rule,
  which HAP does not — a deliberate, spec-correct difference in code-block output.

## Validation

Swap landed with the v6 reader suite, canonical flavor gates, and benchmark evidence. The v5
`Convert` path has been removed for v6.
