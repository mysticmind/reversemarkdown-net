# Pandoc

The Pandoc flavor produces [Pandoc Markdown](https://pandoc.org/MANUAL.html#pandocs-markdown),
including its native constructs.

snippet: sample_pandoc

## Native constructs

- **Document metadata** - emitted as a YAML metadata block.
- **Fenced divs** - `<div>` with attributes becomes a fenced div.
- **Bracketed spans** - a classed/id'd `<span>` becomes a bracketed span.
- **Definition lists** - `<dl>`/`<dt>`/`<dd>` render with `:` definition syntax.
- **Math** (`$..$` / `$$..$$`), **citations** (`<cite data-cite>`), **abbreviations**, and
  **line blocks**.
- **Heading attributes** and **footnotes**.

Round-trip fidelity is measured against canonical `pandoc`.
