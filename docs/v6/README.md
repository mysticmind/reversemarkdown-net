# ReverseMarkdown v6 — Markdown DOM Architecture

> Status: **Planning** · Branch: `feature/v6-markdown-dom` · Target: major version (v6.0.0)

## TL;DR

v5 converts HTML to Markdown in **one streaming pass**: each tag converter reads an
`HtmlNode` and writes Markdown **text** straight to a `TextWriter`. Flavor logic
(`CommonMark` / `GithubFlavored` / `SlackFlavored` / `TelegramMarkdownV2`) is duplicated
inside ~21 converters across ~63 branch sites.

v6 introduces an **intermediate Markdown DOM** (a typed document tree, the `mdast`
equivalent) and splits the engine into:

```
HTML string
  └─ HtmlAgilityPack parse ───────────► HTML DOM        (HtmlNode tree — already exists)
       └─ [optional] HTML-side filter ─► HTML DOM'       (prune by tag/class/id — issue #79a)
            └─ READER ─────────────────► Markdown DOM     (MdNode tree — NEW)
                 └─ [optional] transform► Markdown DOM'    (visit / prune / reshape — issue #79b)
                      └─ WRITER ────────► Markdown string  (flavor-specific — NEW)
                           └─ post ─────► normalized output
```

- **Readers** (HTML DOM → Markdown DOM) are **flavor-agnostic**. One per tag, ~the
  current converter set. They always build the *richest* tree (footnotes, math, etc.).
- **Writers** (Markdown DOM → string) are **one per flavor**. All flavor branching and
  all "this flavor can't represent X, degrade it" logic lives here.

This turns an **N×M** problem (every converter knows every flavor) into **N+M**
(N readers + M writers). It is the architecture Pandoc and unified.js (`hast`→`mdast`)
already use.

## Why

1. **Flavor scaling.** Adding MultiMarkdown + Pandoc under v5 means editing ~21
   converters (see `../../PLANNING-MMD-PANDOC.md`). Under v6, a new flavor is **one new
   writer**; a new HTML feature is **one new reader** that every flavor benefits from.
2. **Issue #79 — structured / filterable output.** A typed, traversable tree is the
   natural home for "return an object I can pick from" and "filter what I don't want."
3. **Correctness.** Whitespace/blank-line handling becomes a structural property of the
   writer instead of ad-hoc string patching scattered across converters.

## Goals

- A public, traversable, **mutable** Markdown DOM.
- Flavor-agnostic readers; flavor-specific writers with explicit degradation rules.
- Public API: `Parse(html) → MarkdownDocument`, `Render(doc) → string`,
  `Convert(html)` == `Render(Parse(html))`.
- Two distinct, documented filter points: **HTML-side** (by class/id) and
  **Markdown-side** (by node type/shape).
- Backward-compatible `Convert(string)` surface and flavor flags (mapped to writers).

## Non-goals

- **Byte-for-byte identical output to v5.** v6 is a major version. The Markdown DOM
  normalizes whitespace structurally, so some snapshots will be re-baselined. We commit
  to *semantic* parity + an explicit, reviewed diff — not character parity. See
  [migration.md](migration.md#parity-strategy).
- A general Markdown **parser** (string → DOM). v6 only builds the DOM from HTML. (A
  reader from Markdown could come later; out of scope now.)
- Modeling every exotic Pandoc construct. The node set is a *pragmatic superset*, with a
  raw-HTML escape hatch for everything else.

## Documents

| Doc | Contents |
|-----|----------|
| [architecture.md](architecture.md) | Pipeline, reader/writer contracts, context/threading, public API, the whitespace problem |
| [node-model.md](node-model.md) | Full node catalog + per-flavor degradation matrix |
| [migration.md](migration.md) | Strangler-fig phases, parity harness, compatibility, risks |
| [../adr/0001-markdown-dom-architecture.md](../adr/0001-markdown-dom-architecture.md) | The decision record |

## Decided

- **Byte-for-byte parity with v5 is NOT a goal** (2026-06-05). v6 targets *semantic*
  parity only. Writers render whitespace/blank-lines structurally and do not replicate v5's
  incidental string-level whitespace quirks. Snapshots re-baseline freely, reviewed in a
  batch. This removes the largest source of writer complexity. → migration.md
- **The Markdown DOM is mutable** (2026-06-05). Nodes expose in-place
  `Remove()`/`ReplaceWith()`/child mutation, and `Parent` is maintained by the tree.
  Rationale: issue #79's "pick what I want / don't want" is fundamentally an edit-in-place
  operation; a mutable tree is the most direct, least-ceremony API for it. Safety is handled
  by an `MdRewriter` base for systematic transforms rather than by immutability. → architecture.md
- **HTML-side filtering ships predicate-only; Fizzler/CSS deferred** (2026-06-05). v6.0
  exposes `Config.HtmlFilters` (`Func<HtmlNode, FilterAction>`) and takes **no new
  dependency**. A Fizzler-backed `AddExcludeSelector(string css)` convenience can be added
  later as a non-breaking overload that compiles to a predicate. → architecture.md

## Open questions

_None blocking Phase B. All architectural decisions resolved._
