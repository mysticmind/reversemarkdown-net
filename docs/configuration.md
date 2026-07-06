# Configuration

Options are organized into groups on `Config`. The former flat properties still work but are
**obsolete** and forward to the grouped members shown below.

## Flavor

- **`Flavor`** - the Markdown flavor to produce (`MarkdownFlavor` enum): `Default`, `GitHub`,
  `CommonMark`, `Slack`, `Telegram`, `MultiMarkdown`, `Pandoc`. The single, canonical flavor
  selector. See [Flavors](/flavors/).
- **`GithubFlavored`** - GitHub-style conversion (br, pre → fenced code, task lists) on the
  default writer. Tables are always GFM regardless. Distinct from `Flavor = MarkdownFlavor.GitHub`
  (see [GitHub](/flavors/github)). Default `false`.
- **`CommonMarkUseHtmlInlineTags`** - when the CommonMark flavor is selected, emit HTML for inline
  tags to avoid delimiter edge cases. Default `true`.
- **`CommonMarkIntrawordEmphasisSpacing`** - when the CommonMark flavor is selected, insert spaces
  to avoid intraword emphasis. Default `false`.

## `Formatting`

Output formatting.

- **`Formatting.CleanupSpaces`** - clean up unnecessary spaces in the output. Default `true`.
- **`Formatting.SuppressDivNewlines`** - remove prefixed newlines from `div` tags. Default `false`.
- **`Formatting.RemoveComments`** - remove comment tags with text. Default `false`.
- **`Formatting.PreAsHtml`** - treat `<pre>` (and `<pre><code>`) content as normal HTML instead of
  a code block. Default `false`.
- **`Formatting.EscapeLineStarts`** - escape markdown line starts (headings, lists, block markers)
  in plain-text output. Default `false`.
- **`Formatting.OutputLineEnding`** - output line endings. Default `Environment.NewLine`.
- **`Formatting.ListBulletChar`** - the unordered-list bullet character. Default `-` (some systems
  expect `*`). Ignored for the Slack flavor, which always uses `•`.
- **`Formatting.DefaultCodeBlockLanguage`** - default GFM code block language when class-based
  language markers are absent.

## `Links`

- **`Links.SmartHref`** - how to handle an `<a>` href.
  - `false` *(default)* - outputs `[{name}]({href}{title})` even if name and href are identical.
  - `true` - if name and href are equal, outputs just the `name` (with http/https and
    `tel:`/`mailto:` refinements). If the Uri is not well formed, markdown syntax is used anyway.
- **`Links.WhitelistedSchemes`** - schemes (without trailing colon) allowed for `<a>`/`<img>`.
  Others are bypassed. Empty (default) allows everything.

## `Tables`

- **`Tables.WithoutHeaderRow`** - handle a table without a header row.
  - `TableWithoutHeaderRowHandlingOption.Default` - first row is used as the header (default).
  - `TableWithoutHeaderRowHandlingOption.EmptyRow` - an empty header row is added.
- **`Tables.HeaderColumnSpans`** - handle table header columns with column spans. Default `true`.

## `Tags`

- **`Tags.Unknown`** - handle unknown tags.
  - `UnknownTagsOption.PassThrough` - include the tag plus text (default).
  - `UnknownTagsOption.Drop` - drop the tag and its content.
  - `UnknownTagsOption.Bypass` - ignore the tag but convert its content.
  - `UnknownTagsOption.Raise` - raise an error.
- **`Tags.Replacer`** - optional markdown wrappers for unknown tags. Key = tag name, value =
  prefix/suffix wrapper. Example: `{ ["u"] = "*" }`.
- **`Tags.Aliases`** - treat a tag as another tag. Example: `{ ["u"] = "em" }`.
- **`Tags.PassThrough`** - tags to pass through as-is without processing.

## `Images`

- **`Images.Base64Handling`** - how base64-encoded images (inline data URIs) are handled.
  - `Base64ImageHandling.Include` - include as-is (default).
  - `Base64ImageHandling.Skip` - ignore them entirely.
  - `Base64ImageHandling.SaveToFile` - save to disk and reference the path. Requires
    `Images.Base64Directory`.
- **`Images.Base64Directory`** - directory to save images to when `Base64Handling` is `SaveToFile`.
- **`Images.Base64FileName`** - `Func<int, string, string>` generating a filename (without
  extension) from the image index and MIME type. Defaults to `image_0`, `image_1`, …
- **`Images.LazySrcFallback`** - when enabled, an `<img>` whose `src` is empty or a `data:`
  placeholder falls back to the first usable URL in `Images.LazySourceAttributes`. Default `false`.
- **`Images.LazySourceAttributes`** - ordered attributes consulted (first usable wins). Defaults to
  `data-src`, `data-original`, `data-lazy-src`, `data-srcset`, `data-original-src`.

### Base64 image examples

```csharp
// Skip base64 images
var skip = new Config { Images = { Base64Handling = Config.Base64ImageHandling.Skip } };

// Save base64 images to disk
var save = new Config
{
    Images =
    {
        Base64Handling = Config.Base64ImageHandling.SaveToFile,
        Base64Directory = "/path/to/images",
        Base64FileName = (index, mime) => $"image_{index}",
    },
};
```

## `Html`

HTML pre-filtering (v6 Markdown DOM path).

- **`Html.ExcludeSelectors`** - CSS selectors whose matching elements are removed before conversion.
- **`Html.ElementFilters`** - predicate filters; an element for which any predicate returns true is
  removed.

```csharp
var config = new ReverseMarkdown.Config();
config.Html.ExcludeSelectors.Add("div.advertisement, aside.related");
config.Html.ElementFilters.Add(el => el.ClassList.Contains("tracking"));
```
