# Introduction

ReverseMarkdown is an HTML to Markdown converter library for C#/.NET. Conversion is reliable
because the [HtmlAgilityPack](https://html-agility-pack.net/) library is used to traverse the HTML
DOM.

::: tip This is v5
v5 is the HtmlAgilityPack-based line. The latest release, [v6](https://mysticmind.github.io/reversemarkdown-net/),
is a ground-up rewrite on AngleSharp with a Markdown DOM pipeline, additional flavors
(MultiMarkdown, Pandoc), and substantially better performance. New projects should prefer v6; this
site documents v5 for existing users.
:::

## Highlights

- **Broad tag support** - `h1`-`h6`, `p`, `em`, `strong`, `i`, `b`, `blockquote`, `code`, `img`,
  `a`, `hr`, `li`, `ol`, `ul`, `table`, `tr`, `th`, `td`, `br`, `pre`, `del`, `strike`, `sup`,
  `dl`, `dt`, `dd`, `div`, and `span`, including nested lists and tables.
- **Markdown flavors** - [GitHub Flavored Markdown](/flavors/github), [Slack](/flavors/slack),
  [Telegram MarkdownV2](/flavors/telegram), and a [CommonMark-focused mode](/flavors/commonmark).
- **Tables** - nested tables (emitted as HTML inside markdown), captions (rendered as a paragraph
  above the table), and configurable header handling.
- **Links and images** - smart href handling, URI scheme whitelisting, and base64 image handling
  (include as-is, skip, or save to disk).
- **Extensibility** - tag aliasing, unknown-tag strategies, and [custom converters](/extending).
- **Broad framework support** - see [Supported Frameworks](/guide/supported-frameworks).

## Quick example

```cs
var converter = new ReverseMarkdown.Converter();

string html = "This a sample <strong>paragraph</strong> from <a href=\"http://test.com\">my site</a>";

string result = converter.Convert(html);
// This a sample **paragraph** from [my site](http://test.com)
```

Continue to [Getting Started](/guide/getting-started).
