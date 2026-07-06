# Slack

The Slack flavor produces Slack-compatible message formatting: `*` for bold, `_` for italic,
`~` for strikethrough, and `•` for list bullets.

```csharp
var config = new ReverseMarkdown.Config { Flavor = MarkdownFlavor.Slack };
var converter = new ReverseMarkdown.Converter(config);
```

The legacy `SlackFlavored = true` switch is an obsolete alias of `Flavor = MarkdownFlavor.Slack`.

::: warning
Slack has no table syntax - a `<table>` raises an unsupported-tag exception under this flavor.
The `Formatting.ListBulletChar` option is ignored for Slack, which always uses `•`.
:::
