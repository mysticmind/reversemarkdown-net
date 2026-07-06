# GitHub

Enable GitHub Flavored Markdown conversion with `GithubFlavored`:

```cs
var config = new ReverseMarkdown.Config
{
    GithubFlavored = true
};

var converter = new ReverseMarkdown.Converter(config);
```

When enabled, this affects `br`, `pre`, and task lists:

- `<br>` is preserved as a hard line break.
- `<pre>` (and `<pre><code>`) becomes a fenced code block. Use
  [`DefaultCodeBlockLanguage`](/configuration#defaultcodeblocklanguage) to set a default language
  when class-based language markers are not available.
- A leading `<input type="checkbox">` in a list item becomes a task-list marker.

## Task lists

```html
<ul>
  <li><input type="checkbox" checked> Done</li>
  <li><input type="checkbox"> Todo</li>
</ul>
```

```txt
- [x] Done
- [ ] Todo
```

## Tables

Tables are always emitted as GitHub-flavored (pipe) tables, regardless of the `GithubFlavored`
flag. See [Configuration](/configuration#tables) for header-row and column-span handling.

::: tip
`GithubFlavored` is a boolean switch in v5. In v6 this became `Flavor = MarkdownFlavor.GitHub`
(plus a separate `GithubFlavored` compatibility switch). See the
[v6 GitHub flavor page](https://mysticmind.github.io/reversemarkdown-net/flavors/github).
:::
