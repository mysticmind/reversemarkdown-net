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

At a glance: [`Remove`](#remove) | [`RemoveWhere`](#removewhere) | [`KeepOnly`](#keeponly) |
[`RemoveComments`](#removecomments) | [`RemoveEmptyElements`](#removeemptyelements)

### Remove

`Remove(selector)` removes matching elements and their content.

snippet: sample_step_remove

### RemoveWhere

`RemoveWhere(predicate)` removes elements for which the predicate returns true, for conditions a
CSS selector cannot express.

snippet: sample_step_removewhere

### KeepOnly

`KeepOnly(selector)` reduces the document to the matching elements, dropping everything else. Nested
matches are kept via their outermost match rather than duplicated. If nothing matches, the document
is left untouched, so a typo cannot silently empty the output.

snippet: sample_step_keeponly

### RemoveComments

`RemoveComments()` drops HTML comments from the source. Unlike `Formatting.RemoveComments`, which
works on the conversion output, this removes them before anything else runs.

snippet: sample_step_removecomments

### RemoveEmptyElements

`RemoveEmptyElements(selector = "p, div, span")` removes matching elements with no text and no
meaningful content. Nested wrappers collapse in one pass; elements holding images, tables or line
breaks are kept.

snippet: sample_step_removeempty

## Transforming elements and content

At a glance: [`Rename`](#rename) | [`Unwrap`](#unwrap) | [`Wrap`](#wrap) |
[`ReplaceWith`](#replacewith) | [`ReplaceWithText`](#replacewithtext) | [`Transform`](#transform) |
[`ReplaceText`](#replacetext)

### Rename

`Rename(selector, tagName)` changes the tag, keeping attributes and children.

snippet: sample_step_rename

### Unwrap

`Unwrap(selector)` removes the element but keeps its children. `Unwrap("span, font")` strips styling
wrappers; `Unwrap("a")` keeps link text without the link.

snippet: sample_step_unwrap

### Wrap

`Wrap(selector, tagName)` wraps matching elements in a new element.

snippet: sample_step_wrap

### ReplaceWith

`ReplaceWith(selector, html)` replaces the element **and** its content with an HTML fragment.

snippet: sample_step_replacewith

### ReplaceWithText

`ReplaceWithText(selector, text)` replaces the element with a text node. The text is inserted as
content, not markup, so it is escaped like any other text.

snippet: sample_step_replacewithtext

### Transform

`Transform(selector, action)` runs your action against every match: the escape hatch for rewriting
attributes, moving nodes and anything else.

snippet: sample_step_transform

### ReplaceText

`ReplaceText(find, replacement)` does an ordinal replacement across the document's text nodes.

snippet: sample_step_replacetext

`ReplaceText(regex, replacement)` takes a `Regex` instead.

snippet: sample_step_replacetext_regex

Both overloads skip text inside `<script>` and `<style>`, but not inside `<pre>`/`<code>`.

## Stripping styles, scripts and attributes

At a glance: [`RemoveStyles`](#removestyles) | [`RemoveInlineStyles`](#removeinlinestyles) |
[`RemoveStyleSheets`](#removestylesheets) | [`RemoveScripts`](#removescripts) |
[`RemoveClasses`](#removeclasses) | [`RemoveAttributes`](#removeattributes)

Steps run against the whole parsed document, so these also reach the `<style>` and `<link>` elements
the HTML5 parser hoists into `<head>`.

### RemoveStyles

`RemoveStyles()` removes everything styling: inline `style` attributes, `<style>` elements and
stylesheet `<link>` elements.

snippet: sample_step_removestyles

### RemoveInlineStyles

`RemoveInlineStyles(selector = "*")` removes only inline `style` attributes.

snippet: sample_step_removeinlinestyles

### RemoveStyleSheets

`RemoveStyleSheets()` removes only `<style>` and stylesheet `<link>` elements.

snippet: sample_step_removestylesheets

### RemoveScripts

`RemoveScripts()` removes `<script>` and `<noscript>` elements plus inline `on*` event handler
attributes.

snippet: sample_step_removescripts

### RemoveClasses

`RemoveClasses(selector = "*")` removes `class` attributes.

snippet: sample_step_removeclasses

::: warning
Fenced code block languages are detected from classes such as `language-cs`, so pass a selector like
`":not(pre):not(code)"` (as above) if you strip classes and want to keep them.
:::

### RemoveAttributes

`RemoveAttributes(selector, params names)` removes named attributes. A name ending in `*` is a prefix
match, so `"data-*"` removes all data attributes.

snippet: sample_step_removeattributes

## Table cells

A nested table or list inside a `<td>`/`<th>` has no Markdown representation - GitHub Flavored
Markdown tables hold simple inline content only, and emitting a real list inside a cell would break
the table. ReverseMarkdown therefore keeps those elements as HTML. `Tables.CellListHandling` decides
what that HTML looks like.

### `RawHtml` (default)

The source markup is copied verbatim, which is faithful but brings every `class`, inline `style` and
editor wrapper with it. Output from CKEditor, SharePoint or Word can leave a cell looking like this:

```html
<ol class="customList"><li><p class="noSpacing" data-text-type="noSpacing">
<span style="font-size:17px" data-fontsize="17px">First point</span></p></li></ol>
```

### `CleanHtml`

Keeps the list as a real list, with the presentational markup stripped: `class`, `style` and
`data-*` attributes, `<span>`/`<font>` wrappers, and a `<p>` that is a list item's only child. Those
attributes reference a stylesheet the Markdown does not carry, and every other conversion path
already drops them. A nested `<table>` is cleaned the same way.

snippet: sample_cell_list_cleanhtml

::: tip
This is usually what you want, and it is a candidate for becoming the default in the next major
version. It is opt-in for now because it changes existing output.
:::

### `InlineText`

Renders the list as inline text instead: one item per line separated by `<br>`, each prefixed with
its bullet or number, with the item content converted to Markdown.

snippet: sample_cell_list_handling

This leaves no HTML in the output at all, which suits Markdown that is read rather than rendered -
RAG indexing, LLM prompts, plain-text diffing. It honours an `<ol start="n">` and
`Formatting.ListBulletChar`.

It is lossy by design: the list stops being a list, and nested lists are flattened to one level.
Nested `<table>` elements are unaffected and stay HTML, since flattening a table to text would lose
its shape entirely.

### Shaping it yourself

`SimplifyTableCellHtml()` applies the same cleanup as `CleanHtml`, but as a preprocessing step over
the source document. Reach for it when you want the cleanup to reach content the option does not
touch, or to combine it with other steps.

snippet: sample_step_simplifytablecellhtml

For a different trade-off, scope any general helper to cells with a descendant selector:

snippet: sample_step_tablecell_scoped

**Which to pick.** `CleanHtml` if the Markdown gets rendered: the list still renders as a list,
without the noise. `InlineText` if it gets read, for Markdown with no HTML in it. Both compose with
preprocessing, which runs first.

## Working with styles

Formatting that only exists as CSS is lost in translation unless you recover it first. Word, Outlook
and Google Docs exports are the usual offenders: they emit `<span style="font-weight:700">` instead
of `<strong>`.

At a glance: [`ConvertInlineStylesToTags`](#convertinlinestylestotags) | [`RemoveHidden`](#removehidden) |
[`InlineStyle`](#the-inlinestyle-helper)

### ConvertInlineStylesToTags

`ConvertInlineStylesToTags()` promotes inline formatting to semantic tags: a bold `font-weight`
becomes `<strong>`, an italic `font-style` becomes `<em>`, and a line-through `text-decoration`
becomes `<del>`.

snippet: sample_step_convertinlinestyles

It strips the declarations it consumes, so it is idempotent and leaves unrelated ones (`color`,
`margin`) untouched. Pair it with `Unwrap` to shed the wrappers once they have done their job:

snippet: sample_preprocess_styles

### RemoveHidden

`RemoveHidden()` drops elements hidden by an inline `display: none` / `visibility: hidden`, or by the
`hidden` attribute. Email preheader text is the classic case.

snippet: sample_step_removehidden

### The InlineStyle helper

`InlineStyle` exposes the same reading primitives for your own steps: `Get`, `Has`, `IsBold`,
`IsItalic`, `IsStruckThrough` and `IsHidden`. It parses the `style` attribute directly, and correctly
ignores separators inside values such as `url(data:image/png;base64,...)`.

snippet: sample_step_inlinestyle_helper

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

snippet: sample_step_resolverelativeurls

A fuller extraction pipeline, converting just the article body of a scraped page:

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
