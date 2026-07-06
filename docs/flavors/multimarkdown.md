# MultiMarkdown

The MultiMarkdown (MMD) flavor produces [MultiMarkdown](https://fletcherpenney.net/multimarkdown/)
output, including its extended constructs.

```csharp
var config = new ReverseMarkdown.Config { Flavor = MarkdownFlavor.MultiMarkdown };
var converter = new ReverseMarkdown.Converter(config);
```

## Extended constructs

- **Document metadata** — title and `<meta name=.. content=..>` become MMD metadata pairs.
- **Footnotes** — `<sup>`/anchor footnote references and definitions.
- **Definition lists** — `<dl>`/`<dt>`/`<dd>` render with `:` definition syntax.
- **Math** and **abbreviations**.
- **Tables** — MMD-style tables (including captions).

MultiMarkdown preserves inline raw HTML for tags it lacks a clean markdown form for. Round-trip
fidelity is measured against canonical MultiMarkdown output.
