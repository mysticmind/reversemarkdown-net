# Telegram

The Telegram flavor produces [Telegram MarkdownV2](https://core.telegram.org/bots/api#markdownv2-style)
with its formatting and escaping rules.

snippet: sample_telegram

The legacy `TelegramMarkdownV2 = true` switch is an obsolete alias of
`Flavor = MarkdownFlavor.Telegram`.

## Behavior

- Text and link labels escape Telegram-reserved characters.
- Ordered and unordered list markers are escaped (`1\.` and `\-`).
- `<img>` falls back to a link label, for example `[Image: alt](url)`.
- `<table>` falls back to a preformatted code block representation.
- `<sup>` falls back to caret notation, for example `x^2`.
