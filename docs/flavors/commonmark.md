# CommonMark

The CommonMark flavor produces round-trip-faithful [CommonMark](https://commonmark.org/): soft
line breaks are preserved, markup-significant characters are escaped, and line-start markers are
escaped so literal text is not reinterpreted.

```csharp
var config = new ReverseMarkdown.Config { Flavor = MarkdownFlavor.CommonMark };
var converter = new ReverseMarkdown.Converter(config);
```

The legacy `CommonMark = true` switch is an obsolete alias of `Flavor = MarkdownFlavor.CommonMark`.

## Options

These options apply when the CommonMark flavor is selected:

- **`CommonMarkUseHtmlInlineTags`** *(default `true`)* - emit HTML for inline tags
  (`em`/`strong`/`a`/`img`) to avoid delimiter edge cases.
- **`CommonMarkIntrawordEmphasisSpacing`** *(default `false`)* - insert spaces to avoid intraword
  emphasis, so `he<strong>ll</strong>o` becomes `he **ll** o`.

```csharp
var config = new ReverseMarkdown.Config
{
    Flavor = MarkdownFlavor.CommonMark,
    CommonMarkIntrawordEmphasisSpacing = true,
    CommonMarkUseHtmlInlineTags = false,
};
```

## Fidelity

The CommonMark flavor round-trips all 651 examples in the CommonMark spec (HTML → Markdown → HTML)
against canonical cmark-gfm.
