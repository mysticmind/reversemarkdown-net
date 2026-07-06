# ADR 0001 - Intermediate Markdown DOM for v6

- Status: **Proposed**
- Date: 2026-06-05
- Deciders: maintainers
- Context branch: `feature/v6-markdown-dom`

## Context

ReverseMarkdown v5 converts HTML to Markdown in a single streaming pass: each tag
converter reads an `HtmlNode` and writes Markdown text directly to a `TextWriter`
(`IConverter.Convert(TextWriter, HtmlNode)`). Output flavor is selected by boolean flags on
`Config` (`GithubFlavored`, `SlackFlavored`, `TelegramMarkdownV2`, `CommonMark`), and the
flavor branching is duplicated inside ~21 converters across ~63 sites.

Two forces expose the limits of this design:

1. **Flavor scaling.** Adding MultiMarkdown and Pandoc requires editing ~21 converters.
   Each new flavor multiplies the branching - an N×M problem.
2. **Issue #79.** Users want structured, *filterable* output ("return an object I can pick
   from", "filter what I don't want") rather than only a final string. The streaming design
   has no intermediate object to expose.

The library already has the HTML half of an AST (HtmlAgilityPack's `HtmlNode` tree) but no
Markdown AST - converters jump straight from HTML node to string.

## Decision

Introduce an **intermediate Markdown DOM** (an `mdast`-equivalent typed document tree) and
split conversion into:

- **Readers**: `HtmlNode` → Markdown DOM. One per tag, **flavor-agnostic**, always building
  the richest (superset) tree.
- **Writers**: Markdown DOM → string. One per flavor, owning **all** flavor-specific output
  and **all** degradation decisions (what to do with a node the flavor can't represent).

`Convert(html)` becomes `Render(Parse(html))`. `Parse` and `Render` are public, and the DOM
is mutable and traversable, directly serving issue #79.

This is the architecture used by Pandoc (one document model, N readers / M writers) and
unified.js (`hast` → `mdast` → stringify).

## Consequences

### Positive
- N×M → N+M. New flavor = one writer; new HTML feature = one reader benefiting all flavors.
- Flavor branching leaves the converters and consolidates in writers.
- Issue #79 is satisfied by the substrate, not a bolt-on: query/prune/reshape the tree,
  then render - optionally to multiple flavors from one parse.
- Block separation/whitespace becomes a structural writer decision instead of scattered
  string patching and a global newline-collapse regex.
- Readers and writers are stateless and individually unit-testable; the `AsyncLocal`
  context is replaced by explicitly-passed `ReaderContext`/`WriterState`.

### Negative / costs
- **Major rewrite.** Every converter is reshaped from "write string" to "return node", plus
  4–6 writers. v6 is a major version.
- **Not byte-identical to v5.** Structural whitespace normalization re-baselines some of the
  ~190 Verify snapshots. We commit to *semantic* parity + a reviewed re-baseline ledger.
- **Breaking for custom `IConverter` implementations** (string-emitting). Mitigated by docs
  and an optional shim.
- Extra allocations from building one more tree (HtmlAgilityPack already builds one).
  Accepted; optimization deferred.

### Mitigations
- Strangler-fig migration (build beside v5, flip at the end) with a dual-run parity harness.
- Bounded superset node set + raw-HTML escape hatch for everything not modeled.
- Two clearly separated filter points: HTML-side (class/id) and Markdown-side (node shape).

## Alternatives considered

1. **Keep v5; add MMD/Pandoc via more flags.**
   Lower immediate effort, but compounds the N×M branching and does **not** address #79.
2. **HTML-side filtering only.** Solves #79's class-based filtering cheaply, but yields no
   Markdown-level structured output and no flavor-scaling benefit.
3. **Adopt Markdig's AST as the intermediate model.** Reuses an existing tree, but couples
   the library to Markdig's block/inline model (built for Markdown→X, not X→Markdown) and a
   large dependency. A purpose-built, minimal node set is lighter and fits HTML→MD better.

Decision: option in this ADR (purpose-built Markdown DOM).
