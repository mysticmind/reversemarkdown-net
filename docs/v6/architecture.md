# v6 Architecture

## 1. The pipeline

```
                    ┌────────────────────────── Converter.Convert(html) ──────────────────────────┐
                    │                                                                              │
 html ──────────────────────► AngleSharp DOM ─► [HtmlFilter*] ──► Reader ──► [Transform*] ──► Writer ──► post ──► string
                              (HTML5 parser)     (optional)                    (optional)
                                  ▲                 ▲              │             ▲             │
                              HTML DOM         issue #79a     Markdown DOM   issue #79b   flavor output
```

Two stages are new (`Reader`, `Writer`); two are optional hooks (`HtmlFilter`,
`Transform`); the rest already exist in `Converter.cs` and are reused.

### Stage responsibilities

| Stage | Input → Output | Flavor-aware? | Notes |
|-------|----------------|---------------|-------|
| Parse | string → AngleSharp `IDocument` | no | HTML5-compliant parsing; reads from `document.Body`. |
| **HtmlFilter** | AngleSharp `IElement` → same | no | Optional. Prune by tag/class/id. Operates on HTML facts. |
| **Reader** | AngleSharp `IElement` → `MarkdownDocument` | **no** | One reader per tag. Builds the richest tree always. |
| **Transform** | `MarkdownDocument` → same | no | Optional. Visit / prune / reshape Markdown nodes. |
| **Writer** | `MarkdownDocument` → string | **yes** | One per flavor. Owns all degradation. |
| Post | string → string | minimal | Line-ending normalization (`ApplyOutputLineEndings`). |

The key invariant: **flavor knowledge exists only in writers.** Readers never look at
`Config.GithubFlavored` etc. This is the property that makes the architecture pay off.

## 2. The Markdown DOM

Two families of node, `MdBlock` and `MdInline`, both deriving from `MdNode`. Full
catalog and degradation rules in [node-model.md](node-model.md). Shape:

```csharp
public abstract class MdNode
{
    public MdNode? Parent { get; internal set; }
    public MdAttributes? Attributes { get; set; }   // id / classes / key-values (Pandoc, filtering)
    public string? SourceTag { get; init; }          // originating HTML tag, for filters/debug
    public abstract void Accept(IMdVisitor visitor);
}

public abstract class MdBlock : MdNode { }
public abstract class MdInline : MdNode { }

public sealed class MarkdownDocument : MdBlock
{
    public IList<MdBlock> Children { get; } = new List<MdBlock>();
    // collected side-channel data (footnote defs, abbreviations, metadata)
    public MdDocumentMeta Meta { get; } = new();
}
```

Design rules:

1. **Superset.** The tree can represent any construct any target flavor supports
   (footnotes, math, definition lists, Pandoc attributes…). Writers *degrade* what they
   can't emit. A reader never has to ask "is this flavor capable?"
2. **Raw escape hatch.** `MdHtmlBlock` / `MdRawInline` carry verbatim HTML. This is how
   unrepresentable input survives end-to-end (replaces v5 `PassThrough` / `ByPass`).
3. **Attributes preserved.** `id`, `class`, and `data-*` are kept on nodes so (a) the
   Pandoc writer can emit `{#id .class}` and (b) Markdown-side transforms can target them.
4. **Whitespace is semantic, not literal.** Text nodes hold canonical content; *block
   separation (blank lines) is decided by the writer*, not encoded as text. See §5.

## 3. Readers

```csharp
public interface IMdReader
{
    // Build zero or more Markdown nodes from an HTML node, using ctx to recurse.
    void Read(IElement element, ReaderContext ctx);
}
```

- Built-in readers are centrally registered by tag name. External readers can be discovered from
  additional assemblies with `[MarkdownReader(tags)]`.
- `ReaderContext` replaces the `AsyncLocal` `ConverterContext`. It is **passed explicitly**
  down the recursion and owns:
  - the current parent block/inline being built (a builder cursor),
  - the ancestor stack (`AncestorsAny`, `AncestorsCount` — ported from
    `ConverterContext.cs`),
  - document-level collectors (`Meta`: footnotes, abbreviations, metadata).
- Readers are **stateless and flavor-agnostic** → trivially unit-testable
  ("`<strong>` ⇒ `MdStrong{ Text("x") }`") with no Config and no threading concerns.

Example (illustrative):

```csharp
public sealed class StrongReader : IMdReader
{
    public void Read(IElement element, ReaderContext ctx)
    {
        var strong = new MdStrong { SourceTag = element.LocalName };
        using (ctx.Open(strong))          // push as current parent, also pushes ancestor
            ctx.ReadChildren(element);     // recurse; children attach to `strong`
        ctx.Emit(strong);                  // attach to whatever is currently open
    }
}
```

Note what's gone: no `Config.SlackFlavored ? "*" : "**"`. The reader just records "this is
strong." The marker choice is the writer's job.

## 4. Writers

```csharp
public interface IMarkdownWriter
{
    string Write(MarkdownDocument document);
}
```

Implemented as a visitor with a virtual method per node type:

```csharp
public abstract class MarkdownWriterBase : IMdVisitor, IMarkdownWriter
{
    protected MarkdownWriterBase(Config config) { Config = config; }

    // CommonMark-ish defaults live here.
    public virtual void Visit(MdStrong n) { /* emit ** ... ** */ }
    public virtual void Visit(MdTable n)  { /* emit pipe table */ }
    // ... one per node type ...

    // Degradation hook: called when a writer chooses not to support a node natively.
    protected virtual void Degrade(MdNode n) => WriteRawHtml(n);  // default: round-trip as HTML
}
```

Flavor writers override only what differs:

```csharp
public sealed class SlackWriter : MarkdownWriterBase
{
    public override void Visit(MdStrong n) => Wrap("*", n);          // single asterisk
    public override void Visit(MdStrikethrough n) => Wrap("~", n);   // single tilde
    public override void Visit(MdTable n) => Degrade(n);             // Slack has no tables
    public override void Visit(MdImage n) =>
        throw new SlackUnsupportedTagException("img");               // preserve v5 behavior
}
```

Planned writers: `DefaultWriter` (v5 default), `GithubWriter`, `SlackWriter`,
`TelegramWriter`, `CommonMarkWriter`, then `MultiMarkdownWriter`, `PandocWriter`.

### Degradation is explicit, per writer, per node

This is the design's load-bearing detail (it's what people underestimate). Every writer
must answer, for every superset node it doesn't natively support: **inline it? emit raw
HTML? drop it? throw?** Defaults in the base cover the common case (raw HTML); writers
override where v5 had specific behavior (e.g. Slack throws on `img`/`table`). The
degradation choices are enumerated in [node-model.md](node-model.md#degradation-matrix).

## 5. The whitespace problem (read this before estimating)

v5 spends a lot of code on **string-level whitespace micromanagement**:

- `TreatEmphasizeContentWhitespaceGuard` (`ConverterBase.cs:71`) preserves leading/trailing
  spaces around `*`/`**` and chomps interiors.
- Strong adds a trailing space when the next sibling is also `<strong>` (`Strong.cs:48`).
- CommonMark intraword-emphasis spacing (`Strong.cs:30-46`).
- List indentation math (`Li.cs`, `IndentationFor`).
- Global "collapse 2+ blank lines" regex (`Converter.cs:143`) and `FixMultipleNewlines`.

A tree + naive serializer will **not** reproduce these byte-for-byte. The v6 stance:

1. **Block separation becomes structural.** The writer decides blank lines between blocks
   from node types (e.g. always one blank line between top-level blocks, tight vs loose
   lists from `MdList.Tight`). This *replaces* the global newline-collapse regex and is
   more predictable.
2. **Inline whitespace-guarding is ported into the writer**, once, as a shared helper
   (the same algorithm as `TreatEmphasizeContentWhitespaceGuard`), but only where it
   affects *correctness* (e.g. not gluing emphasis markers to adjacent words). We do **not**
   replicate v5's incidental whitespace quirks.
3. **Byte parity is explicitly out of scope** (decided 2026-06-05). Snapshots that change in
   whitespace-only ways are re-baselined in a reviewed batch; writers are free to emit the
   cleanest correct whitespace rather than matching v5 character-for-character. This is the
   single biggest simplification in the writer layer. See
   [migration.md](migration.md#parity-strategy).

## 6. Public API

```csharp
var converter = new Converter(config);

// One-shot conversion through AngleSharp + Markdown DOM:
string md = converter.Convert(html);                 // == Render(Parse(html))

// v6 structured API:
MarkdownDocument doc = converter.Parse(html);         // HTML → Markdown DOM
doc.Descendants().OfType<MdImage>().ToList()           // query
   .ForEach(img => img.Remove());                      // mutate / prune  (issue #79b)
string md2 = converter.Render(doc);                    // Markdown DOM → string (current flavor)
string slack = converter.Render(doc, MarkdownFlavor.Slack); // same tree, different writer
```

- The DOM is **mutable** (decided 2026-06-05). The mutation surface:
  - `MdNode.Remove()` — detach from parent.
  - `MdNode.ReplaceWith(params MdNode[])` — swap a node for zero or more siblings.
  - `MdNode.InsertBefore/InsertAfter(MdNode)`, and `IList<MdBlock>/IList<MdInline>` child
    collections that maintain `Parent` on add/remove (mutating a child list directly is the
    one place to be careful — child collections own the `Parent` back-pointer).
  - Read helpers: `Descendants()`, `DescendantsAndSelf()`, `Ancestors()`.
  - `MdRewriter` — a visitor base that walks the tree and lets you return a replacement (or
    null to drop) per node, for *systematic* transforms without manual parent juggling. This
    is the recommended path for non-trivial reshaping; raw `Remove()`/`ReplaceWith()` is for
    one-off edits.
  - Invariant: every node has at most one parent; helpers reparent rather than alias. A node
    moved into two places is a bug the child-list setters guard against.
- Flavor selection: keep the `MarkdownFlavor` enum from `PLANNING-MMD-PANDOC.md` Task 0.1
  and the backward-compatible boolean wrappers (Task 0.2). `Config.Flavor` chooses the
  default writer.

### HTML-side filtering (issue #79a) is a *separate* hook

Class/id-based selection is an **HTML DOM** concern — `class`/`id` mostly don't survive
into Markdown. So it runs *before* the reader:

```csharp
config.HtmlElementFilters.Add(element =>
    (element.GetAttribute("class") ?? string.Empty).Split(' ').Contains("ad"));
```

**Updated after ADR 0002:** v6.0 ships both predicate filters
(`Config.HtmlElementFilters`, `Func<IElement, bool>`) and native CSS selector filters
(`Config.HtmlExcludeSelectors`) because AngleSharp provides `QuerySelectorAll` directly.

Do **not** fold this into Markdown-side transforms — keeping the two filter points
distinct (HTML facts vs Markdown shape) is a documented design rule, not an accident.

## 7. Threading & state

v5 uses `AsyncLocal<ConverterContext>` (`Converter.cs:25`) so a single `Converter`
instance is thread-safe across concurrent `Convert` calls. v6 improves on this:

- **Readers** carry state in an explicitly-passed `ReaderContext` (one per `Parse` call) —
  no `AsyncLocal`, no shared mutable converter state. Naturally thread-safe and testable.
- **Writers** carry per-call state in a `WriterState` (output buffer, indent stack) passed
  down the visit, so a single writer instance is reentrant.
- `Converter` stays the thread-safe public entry point; `Parse`/`Render`/`Convert` hold no
  cross-call mutable state.

## 8. Performance

Building a tree adds allocations versus pure streaming. Accept it: AngleSharp already
materializes a full HTML tree, so v6 adds *one* more tree of comparable size — not a new
order of magnitude. Correctness first. Later levers if needed: pooled `StringBuilder`
(already TODO'd at `Converter.cs:212`), struct/readonly nodes for hot inline types, and a
streaming writer that never builds the full output string. None are required for v6.0.
