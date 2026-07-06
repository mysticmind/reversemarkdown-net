# Flavors

Select the Markdown flavor to produce with the `Flavor` property (a `MarkdownFlavor` enum). This
is the single, canonical flavor selector:

```csharp
var config = new ReverseMarkdown.Config { Flavor = MarkdownFlavor.CommonMark };
var converter = new ReverseMarkdown.Converter(config);
```

| Flavor | What it produces |
| --- | --- |
| `Default` | Clean, general-purpose Markdown. |
| [`GitHub`](/flavors/github) | CommonMark + GFM extensions (pipe tables, task lists, `~~` strikethrough, autolinks). |
| [`CommonMark`](/flavors/commonmark) | Round-trip-faithful CommonMark. |
| [`Slack`](/flavors/slack) | Slack's `*bold*` / `_italic_` / `~strike~` and `•` bullets. |
| [`Telegram`](/flavors/telegram) | Telegram MarkdownV2 with its escaping rules. |
| [`MultiMarkdown`](/flavors/multimarkdown) | MultiMarkdown with footnotes, metadata, definition lists, etc. |
| [`Pandoc`](/flavors/pandoc) | Pandoc Markdown with fenced divs, bracketed spans, math, citations, etc. |

## Legacy switches

The boolean switches `SlackFlavored`, `TelegramMarkdownV2`, and `CommonMark` still work but are
**obsolete aliases** of `Flavor` - prefer the enum.

`GithubFlavored` is the exception: it is a distinct switch (GFM-style conversion on the default
writer) and is **not** the same as `Flavor = MarkdownFlavor.GitHub`. See the
[GitHub flavor page](/flavors/github) for the difference.
