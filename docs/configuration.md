# Configuration

All options live on `ReverseMarkdown.Config` and are passed to the converter:

```cs
var config = new ReverseMarkdown.Config
{
    GithubFlavored = true,
    SmartHrefHandling = true,
    RemoveComments = true
};

var converter = new ReverseMarkdown.Converter(config);
```

## Flavors

These flags select the output style. See [Flavors](/flavors/) for details.

- **`GithubFlavored`** - GitHub-style markdown for `br`, `pre`, and tables. Default `false`. Tables
  are always converted to GitHub-flavored markdown regardless of this flag.
- **`SlackFlavored`** - Slack mrkdwn. Uses `*` for bold, `_` for italic, `~` for strikethrough, and
  `•` for list bullets. Default `false`.
- **`TelegramMarkdownV2`** - Telegram MarkdownV2 formatting and escaping rules. Falls back to
  readable text for unsupported constructs (`<img>` to link label, `<table>` to preformatted block,
  `<sup>` to caret notation). Default `false`.
- **`CommonMark`** - CommonMark-focused output rules. Default `false`.
- **`CommonMarkUseHtmlInlineTags`** - When CommonMark is enabled, emit HTML for inline tags (`em`,
  `strong`, `a`, `img`) to avoid delimiter edge cases. Default `true`.
- **`CommonMarkIntrawordEmphasisSpacing`** - When CommonMark is enabled, insert spaces to avoid
  intraword emphasis. Default `false`.

## Links {#links}

- **`SmartHrefHandling`** - How to handle an `<a>` tag's `href`.
  - `false` (default) - outputs `[{name}]({href}{title})` even when the name and href are identical.
  - `true` - when the name and href are equal, outputs just the `name`. If the URI is not
    well-formed per [`Uri.IsWellFormedUriString`](https://learn.microsoft.com/dotnet/api/system.uri.iswellformeduristring),
    markdown link syntax is used anyway. When `href` contains an `http`/`https` protocol and `name`
    does not but they are otherwise the same, the `href` is output. For a `tel:` or `mailto:`
    scheme that is otherwise identical to the name, the `name` is output.
- **`WhitelistUriSchemes`** - Schemes (without trailing colon) allowed for `<a>` and `<img>`.
  Others are bypassed (text or nothing). A `HashSet<string>`; use `.Add()` to add schemes. Allows
  everything by default. If `string.Empty` is provided and a scheme cannot be determined, it is
  whitelisted. The scheme is determined by the `Uri` class, except when a url begins with `/`
  (file scheme) or `//` (http scheme).

```cs
var config = new ReverseMarkdown.Config();
config.WhitelistUriSchemes.Add("http");
config.WhitelistUriSchemes.Add("https");
config.WhitelistUriSchemes.Add("mailto");
```

## Tables {#tables}

- **`TableWithoutHeaderRowHandling`** - How a table without a header row is handled.
  - `TableWithoutHeaderRowHandlingOption.Default` - the first row becomes the header row (default).
  - `TableWithoutHeaderRowHandlingOption.EmptyRow` - an empty header row is added.
- **`TableHeaderColumnSpanHandling`** - Handle table header columns that use column spans. Default
  `true`.

## Images {#images}

- **`Base64Images`** - How base64-encoded images (inline data URIs) are handled.
  - `Base64ImageHandling.Include` - include base64 images as-is (default).
  - `Base64ImageHandling.Skip` - skip base64 images entirely.
  - `Base64ImageHandling.SaveToFile` - save base64 images to disk and reference the saved path.
    Requires `Base64ImageSaveDirectory`.
- **`Base64ImageSaveDirectory`** - Directory to save images to when `Base64Images` is `SaveToFile`.
- **`Base64ImageFileNameGenerator`** - `Func<int, string, string>` receiving the image index and
  MIME type, returning a filename without extension. Defaults to `image_0`, `image_1`, and so on.
- **`LazyImageSrcFallback`** - When enabled, an `<img>` whose `src` is empty or a `data:`
  placeholder (as used by JavaScript lazy-loading libraries) falls back to the first usable URL in
  `LazyImageSourceAttributes`. Default `false`.
- **`LazyImageSourceAttributes`** - Ordered list of attributes consulted (first usable wins) when
  `LazyImageSrcFallback` is enabled. Defaults to `data-src`, `data-original`, `data-lazy-src`,
  `data-srcset`, `data-original-src` (`srcset`-style values use their first URL).

Supported image formats when saving: PNG, JPEG, GIF, BMP, TIFF, WebP, and SVG.

```cs
var config = new ReverseMarkdown.Config
{
    Base64Images = Config.Base64ImageHandling.SaveToFile,
    Base64ImageSaveDirectory = "/path/to/images",
    Base64ImageFileNameGenerator = (index, mimeType) => $"converted_{index}"
};
```

## Tags {#tags}

- **`UnknownTags`** - How unknown (unsupported) tags are handled.
  - `UnknownTagsOption.PassThrough` - include the unknown tag and its text in the output (default).
  - `UnknownTagsOption.Drop` - drop the unknown tag and its content.
  - `UnknownTagsOption.Bypass` - ignore the tag but convert its content.
  - `UnknownTagsOption.Raise` - raise an error.
- **`UnknownTagsReplacer`** - Optional markdown wrappers for unknown tags. Key is the tag name;
  value is the wrapper used as prefix and suffix around converted content, for example
  `{ ["u"] = "*" }`.
- **`TagAliases`** - Optional alias map that treats a tag as another tag during conversion, for
  example `{ ["u"] = "em" }`.
- **`PassThroughTags`** - A `HashSet<string>` of tags to pass through as-is without processing.

## Formatting {#formatting}

- **`DefaultCodeBlockLanguage`** {#defaultcodeblocklanguage} - Default code block language for
  GitHub-style markdown when class-based language markers are absent.
- **`ListBulletChar`** {#listbulletchar} - Bullet character for unordered lists. Default `-`. Some
  systems expect `*`. Ignored when `SlackFlavored` is enabled (Slack always uses `•`).
- **`OutputLineEnding`** - Output line endings for the generated markdown. Defaults to
  `Environment.NewLine`.
- **`CleanupUnnecessarySpaces`** - Clean up unnecessary spaces in the output. Default `true`.
- **`SuppressDivNewlines`** - Remove the prefixed newlines a `div` would otherwise introduce.
  Default `false`.
- **`ConvertPreContentAsHtml`** - Treat `<pre>` (and `<pre><code>`) content as normal HTML instead
  of a code block. Default `false`.
- **`EscapeMarkdownLineStarts`** - Escape markdown line starts (headings, lists, block markers) in
  plain-text output, so markdown-like text is preserved literally. Default `false`.
- **`RemoveComments`** - Remove HTML comments (and their text) from the output. Default `false`.
