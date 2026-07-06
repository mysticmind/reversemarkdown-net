# Slack

Enable Slack mrkdwn output with `SlackFlavored`:

```cs
var config = new ReverseMarkdown.Config
{
    SlackFlavored = true
};

var converter = new ReverseMarkdown.Converter(config);
```

Slack's markup differs from standard Markdown. When enabled, ReverseMarkdown produces:

- `*bold*` (single asterisks) for `<strong>` / `<b>`
- `_italic_` for `<em>` / `<i>`
- `~strike~` for `<del>` / `<strike>`
- `•` for unordered list bullets

::: warning
Because Slack uses `•` for bullets, the [`ListBulletChar`](/configuration#listbulletchar) option is
ignored when `SlackFlavored` is enabled.
:::
