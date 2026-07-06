# CommonMark

Enable CommonMark-focused output with `CommonMark`:

```cs
var config = new ReverseMarkdown.Config
{
    CommonMark = true
};

var converter = new ReverseMarkdown.Converter(config);
```

This mode applies CommonMark-focused rules and is useful when you need output that round-trips
predictably through a CommonMark parser. To keep tricky emphasis and link cases unambiguous, it may
emit inline HTML for those constructs.

## Related options

- **`CommonMarkUseHtmlInlineTags`** (default `true`) - when CommonMark is enabled, emit HTML for
  inline tags (`em`, `strong`, `a`, `img`) to avoid delimiter edge cases. Set to `false` to force
  pure markdown output.
- **`CommonMarkIntrawordEmphasisSpacing`** (default `false`) - insert spaces to avoid intraword
  emphasis, so `he<strong>ll</strong>o` becomes `he **ll** o`.

```cs
var config = new ReverseMarkdown.Config
{
    CommonMark = true,
    CommonMarkUseHtmlInlineTags = false,
    CommonMarkIntrawordEmphasisSpacing = true
};
```

::: warning Combine with care
CommonMark is best used on its own. Combining `CommonMark` with `GithubFlavored` can produce mixed
output; keep them separate unless you explicitly want that behavior.
:::

::: tip v6
v6 has a dedicated, round-trip-faithful CommonMark writer (651/651 spec examples). See the
[v6 CommonMark page](https://mysticmind.github.io/reversemarkdown-net/flavors/commonmark).
:::
