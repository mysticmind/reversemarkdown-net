# Telegram

Enable [Telegram MarkdownV2](https://core.telegram.org/bots/api#markdownv2-style) output with
`TelegramMarkdownV2`:

```cs
var converter = new ReverseMarkdown.Converter(new ReverseMarkdown.Config
{
    TelegramMarkdownV2 = true
});

var html = "This is <strong>bold</strong>, <em>italic</em>, <del>strikethrough</del> and " +
           "<a href=\"https://example.com/path_(one)?q=1)2\">a_b[c]</a>";

var result = converter.Convert(html);
// This is *bold*, _italic_, ~strikethrough~ and [a\_b\[c\]](https://example.com/path_(one\)?q=1\)2)
```

When enabled, ReverseMarkdown applies Telegram-compatible formatting and escaping:

- Text and link labels escape Telegram-reserved characters.
- Ordered and unordered list markers are escaped (`1\.` and `\-`).
- `<img>` falls back to a link label, for example `[Image: alt](url)`.
- `<table>` falls back to a preformatted code block representation.
- `<sup>` falls back to caret notation, for example `x^2`.
