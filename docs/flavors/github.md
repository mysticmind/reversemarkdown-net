# GitHub

GitHub Flavored Markdown (GFM) is CommonMark plus GFM extensions: pipe tables, task lists,
`~~` strikethrough, and autolinks.

## Two ways to target GitHub

There are two distinct GitHub-oriented options, and they behave differently:

### `Flavor = MarkdownFlavor.GitHub`

Selects the dedicated, CommonMark-based **GitHub writer**. It is round-trip-faithful and
**preserves raw HTML** for constructs it cannot represent as clean markdown.

snippet: sample_github_flavor

### `GithubFlavored = true`

A legacy switch that produces **clean GFM markdown on the default writer** - `<br>`, `<pre>` →
fenced code blocks, and task lists. Tables are always emitted as GFM regardless of this flag.

snippet: sample_github_flavored

::: tip Which should I use?
Use `GithubFlavored = true` if you want tidy GFM markdown (fenced code, pipe tables, task lists).
Use `Flavor = MarkdownFlavor.GitHub` if you want the round-trip-faithful writer that keeps raw
HTML for anything without a clean markdown form.
:::

## Task lists

Under GitHub-flavored conversion, a leading `<input type="checkbox">` in a list item becomes a
task-list marker:

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
