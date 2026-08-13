# HTML Preprocessing

Real-world HTML rarely converts cleanly as-is. Pages carry navigation chrome, tracking markup,
styling wrappers and inline CSS that have no place in Markdown. `Config.Preprocess` is an ordered
pipeline of transformations applied to the **source HTML**, before it is read into the Markdown DOM,
so you can shape the input instead of post-processing the output.

```
source HTML  ->  [ Preprocess steps ]  ->  Html filters  ->  Markdown DOM  ->  writer  ->  Markdown
```

## Quick start

snippet: sample_preprocess

Every helper appends a step and returns the pipeline, so calls chain. Steps run **in the order they
were added**, each one seeing the result of the previous step: `Rename("h1", "h2")` followed by
`Remove("h2")` is not the same as the reverse.

## Removing content

| Method | Effect |
| ------ | ------ |
| `Remove(selector)` | Removes matching elements and their content. |
| `RemoveWhere(predicate)` | Removes elements for which the predicate returns true, for conditions a selector cannot express. |
| `KeepOnly(selector)` | Reduces the document to the matching elements, dropping everything else. If nothing matches, the document is left untouched, so a typo cannot silently empty the output. |
| `RemoveComments()` | Drops HTML comments from the source. |
| `RemoveEmptyElements(selector = "p, div, span")` | Removes matching elements with no text and no meaningful content. Nested wrappers collapse in one pass; elements holding images, tables or line breaks are kept. |

## Transforming elements and content

| Method | Effect |
| ------ | ------ |
| `Rename(selector, tagName)` | Changes the tag, keeping attributes and children. `Rename("h1", "h2")`, `Rename("b", "strong")`. |
| `Unwrap(selector)` | Removes the element but keeps its children. `Unwrap("span, font")` strips styling wrappers; `Unwrap("a")` keeps link text without the link. |
| `Wrap(selector, tagName)` | Wraps matching elements in a new element. `Wrap("table", "figure")`. |
| `ReplaceWith(selector, html)` | Replaces the element **and** its content with an HTML fragment. |
| `ReplaceWithText(selector, text)` | Replaces the element with a text node (inserted as content, so it is escaped like any other text). |
| `Transform(selector, action)` | Runs your action against every match. The escape hatch for rewriting attributes, moving nodes and anything else. |
| `ReplaceText(find, replacement)` | Ordinal text replacement across the document's text nodes. |
| `ReplaceText(regex, replacement)` | Regex text replacement. |

Both `ReplaceText` overloads skip text inside `<script>` and `<style>`, but not inside
`<pre>`/`<code>`.

## Stripping styles, scripts and attributes

| Method | Effect |
| ------ | ------ |
| `RemoveStyles()` | Everything styling: inline `style` attributes, `<style>` elements and stylesheet `<link>` elements. |
| `RemoveInlineStyles(selector = "*")` | Only inline `style` attributes. |
| `RemoveStyleSheets()` | Only `<style>` and stylesheet `<link>` elements. |
| `RemoveScripts()` | `<script>` and `<noscript>` elements plus inline `on*` event handler attributes. |
| `RemoveClasses(selector = "*")` | `class` attributes. |
| `RemoveAttributes(selector, params names)` | Named attributes. A name ending in `*` is a prefix match, so `RemoveAttributes("*", "data-*")` removes all data attributes. |

Steps run against the whole parsed document, so `RemoveStyles()` also reaches the `<style>` and
`<link>` elements the HTML5 parser hoists into `<head>`.

::: warning
Fenced code block languages are detected from classes such as `language-cs`. Use a selector like
`RemoveClasses(":not(pre):not(code)")` if you strip classes and want to keep them.
:::

## Working with styles

Formatting that only exists as CSS is lost in translation unless you recover it first. Word, Outlook
and Google Docs exports are the usual offenders: they emit `<span style="font-weight:700">` instead
of `<strong>`.

| Method | Effect |
| ------ | ------ |
| `ConvertInlineStylesToTags()` | Promotes inline formatting to semantic tags: a bold `font-weight` becomes `<strong>`, an italic `font-style` becomes `<em>`, a line-through `text-decoration` becomes `<del>`. |
| `RemoveHidden()` | Drops elements hidden by an inline `display: none` / `visibility: hidden`, or by the `hidden` attribute. Email preheader text is the classic case. |

snippet: sample_preprocess_styles

`ConvertInlineStylesToTags` strips the declarations it consumes, so it is idempotent and leaves
unrelated ones (`color`, `margin`) untouched. The `InlineStyle` static helper exposes the same
reading primitives for your own steps: `Get`, `Has`, `IsBold`, `IsItalic`, `IsStruckThrough` and
`IsHidden`. It parses the `style` attribute directly, and correctly ignores separators inside values
such as `url(data:image/png;base64,...)`.

snippet: sample_preprocess_style_predicate

### Styles from stylesheets (AngleSharp.Css)

The helpers above read the **`style` attribute only**. A rule in a `<style>` block or an external
sheet needs the CSS cascade, which AngleSharp keeps in the separate `AngleSharp.Css` package.
ReverseMarkdown deliberately does not depend on it: it adds no selector power (AngleSharp's core
already handles `:has()`, `:is()`, `:not()`, `:nth-child()` and friends), and it introduces
`Activator.CreateInstance` trim warnings that would end this library's Native AOT guarantee.

You can opt in yourself. Pass a browsing context configured with `.WithCss()` to the converter and
your steps get `ComputeCurrentStyle()`:

snippet: sample_preprocess_css

::: warning
`<head>` computes to `display: none`, and it holds the stylesheets. A predicate like
`RemoveWhere(e => e.ComputeCurrentStyle().GetPropertyValue("display") == "none")` would therefore
target `<head>` and destroy the cascade for every later step. The built-in removal steps never
detach `<html>`, `<head>` or `<body>`, so this is safe, but read computed styles **before** you
remove hidden content.

Two more caveats: AngleSharp fetches external resources if your configuration registers a requester,
and a shared browsing context is not guaranteed safe for concurrent parsing.
:::

The safe ordering, reading the cascade before removing anything:

snippet: sample_preprocess_css_hidden

## Rewriting URLs

`ResolveRelativeUrls(baseUrl)` turns relative `href`, `src` and `poster` values into absolute URLs,
so links and images survive out of their original page. Absolute URLs (including `data:` and
`mailto:`) and in-page `#anchor` references are left alone; protocol-relative `//host/path` values
pick up the base scheme.

snippet: sample_preprocess_extract

## Custom steps

Nothing here is a closed set. In increasing order of power: `Transform` for per-element work,
`Add(name, step)` for a whole-document step that receives the `<html>` element, and `AddText` for a
rewrite of the raw markup before it is parsed.

snippet: sample_preprocess_custom

### Text steps (before parsing)

A DOM step cannot fix markup the parser has already reinterpreted. `AddText` runs against the raw
HTML instead, with line endings normalized to `\n`. **All text steps run before all DOM steps**,
regardless of the order you add them.

snippet: sample_preprocess_text

Prefer DOM steps for anything structural: they are safer and say what they mean. Reach for a text
step only when the transformation genuinely has to happen before parsing.

For something reusable, implement `IHtmlPreprocessStep` and pass it to `Add`:

snippet: sample_preprocess_step_class

snippet: sample_preprocess_step_class_usage

The pipeline itself is inspectable and resettable: `Steps`, `TextSteps`, `Count` and `Clear()`.

Custom steps get the live AngleSharp DOM with nothing held back, so there is no transformation the
pipeline can refuse. The one rule the converter enforces: leave `<html>`, `<head>` and `<body>` in
the document (remove their contents instead). The built-in steps already honour this; a custom step
that detaches them fails with an explanation rather than a `NullReferenceException`.

## Inspecting the preprocessed HTML

`Converter.Preprocess(html)` returns the transformed HTML without converting it, which is handy for
debugging a pipeline or caching the cleaned markup. It returns the input unchanged when no steps are
configured.

snippet: sample_preprocess_inspect

## Notes

- Preprocessing applies to both `Convert` and `Parse`, so the Markdown DOM you get back is built
  from the transformed HTML.
- Steps run **before** the [`Html` filters](/configuration#html) (`Html.ExcludeSelectors` and
  `Html.ElementFilters`), which stay supported and are effectively a narrower version of `Remove` and
  `RemoveWhere`.
- The CommonMark and GitHub flavors pass raw HTML blocks through verbatim, and Slack rejects
  unsupported table markup. Both checks see the preprocessed markup, so a step that removes an
  offending element takes effect.
- Configure the pipeline before converting. Custom steps and predicates must be thread-safe if the
  converter is shared across threads.
- No reflection is involved, so preprocessing publishes cleanly under trimming and Native AOT.
