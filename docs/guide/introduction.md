# Introduction

ReverseMarkdown is an HTML to Markdown converter library for C#/.NET.

Version 6 is a ground-up rewrite: HTML is parsed with [AngleSharp](https://anglesharp.io/)'s
HTML5-compliant parser into an intermediate **Markdown DOM**, which is then rendered through
per-flavor writers. The result is more correct, faster, and extensible, and it adds
MultiMarkdown and Pandoc output flavors.

## Highlights

- **Seven output flavors** — Default, GitHub, CommonMark, Slack, Telegram, MultiMarkdown, and
  Pandoc, chosen via the [`Flavor`](/configuration#flavor) enum.
- **Spec-compliant round-trips** — CommonMark (651/651) and GitHub Flavored Markdown (672/672)
  round-trip at 100% against canonical cmark-gfm; MultiMarkdown and Pandoc are verified against
  canonical `pandoc`.
- **Filterable, structured output** — `Parse()` returns a mutable Markdown DOM you can traverse
  and transform before `Render()`, plus HTML pre-filtering via CSS selectors and predicates.
- **Extensible** — plug in [custom readers](/extending#custom-readers), alias tags, or transform
  the DOM directly.
- **Broad framework support** — see [Supported Frameworks](/guide/supported-frameworks).

Using v5.x? See the [v5.x documentation](https://github.com/mysticmind/reversemarkdown-net/blob/5.x/README.md).
