# Meet ReverseMarkdown

[![Build status](https://github.com/mysticmind/reversemarkdown-net/actions/workflows/ci.yaml/badge.svg)](https://github.com/mysticmind/reversemarkdown-net/actions/workflows/ci.yaml) [![NuGet Version](https://badgen.net/nuget/v/reversemarkdown)](https://www.nuget.org/packages/ReverseMarkdown/)

ReverseMarkdown is a HTML to Markdown converter library in C#. v6 uses AngleSharp's HTML5-compliant parser and a Markdown DOM pipeline for reliable, performant conversion.

If you have used and benefitted from this library. Please feel free to sponsor me!<br>
<a href="https://github.com/sponsors/mysticmind" target="_blank"><img height="30" style="border:0px;height:36px;" src="https://img.shields.io/static/v1?label=GitHub Sponsor&message=%E2%9D%A4&logo=GitHub" border="0" alt="GitHub Sponsor" /></a>

## Features

**Core conversion**
- Supports common HTML tags like h1-h6, p, em, strong, i, b, blockquote, code, img, a, hr, li, ol, ul, table, tr, th, td, br, pre, del, strike, sup, dl, dt, dd, div, and span
- Supports nested lists
- Improved performance with optimized text writer approach and O(1) ancestor lookups

**Markdown flavors**

Select a flavor with `Flavor` (a `MarkdownFlavor` enum): `Default`, `GitHub`, `CommonMark`, `Slack`, `Telegram`, `MultiMarkdown`, or `Pandoc`.

- Slack. `var config = new ReverseMarkdown.Config { Flavor = MarkdownFlavor.Slack };`
- Telegram MarkdownV2. `var config = new ReverseMarkdown.Config { Flavor = MarkdownFlavor.Telegram };`
- CommonMark-focused output. `var config = new ReverseMarkdown.Config { Flavor = MarkdownFlavor.CommonMark };` It may emit inline HTML for tricky emphasis/link cases unless you disable `CommonMarkUseHtmlInlineTags`.
- GitHub Flavoured Markdown conversion for br, pre, tasklists, and table. Use `var config = new ReverseMarkdown.Config { GithubFlavored = true };`. By default the table is always converted to GitHub flavored markdown regardless of this flag. (`GithubFlavored` produces clean GFM markdown on the default writer; `Flavor = MarkdownFlavor.GitHub` selects the CommonMark-based GitHub writer, which preserves raw HTML — they are different.)

The legacy `SlackFlavored`, `TelegramMarkdownV2`, and `CommonMark` boolean switches still work but are obsolete aliases of `Flavor`.

**Tables**
- Support for nested tables (converted as HTML inside markdown)
- Support for table captions (rendered as paragraph above table)
- Configurable table header handling

**Links and images**
- Smart link handling and URI scheme whitelisting for links and images
- Base64-encoded image handling with options to include as-is, skip, or save to disk

**Extensibility and safety**
- Tag aliasing and unknown tag replacement options for custom conversion behavior
- Pass-through, bypass, drop, or raise strategies for unknown tags
- Pre-tidy handling for malformed unclosed script/style tags

**Formatting controls**
- Configurable list bullets and default code block language
- Comment removal and optional whitespace cleanup

## Supported frameworks

ReverseMarkdown targets `netstandard2.0`, `net8.0`, `net9.0`, and `net10.0`. The `netstandard2.0` target means it also runs on .NET Framework 4.6.1+, .NET Core 2.0+, Mono, and Unity, in addition to modern .NET.

## Usage

Install the package from NuGet using `Install-Package ReverseMarkdown` or clone the repository and build it yourself.

<!-- snippet: Usage -->
<a id='snippet-Usage'></a>
```cs
var converter = new ReverseMarkdown.Converter();

string html = "This a sample <strong>paragraph</strong> from <a href=\"http://test.com\">my site</a>";

string result = converter.Convert(html);
```
<sup><a href='/src/ReverseMarkdown.Test/Snippets.cs#L12-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-Usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Will result in:

<!-- snippet: Snippets.Usage.verified.txt -->
<a id='snippet-Snippets.Usage.verified.txt'></a>
```txt
This a sample **paragraph** from [my site](http://test.com)
```
<sup><a href='/src/ReverseMarkdown.Test/Snippets.Usage.verified.txt#L1-L1' title='Snippet source file'>snippet source</a> | <a href='#snippet-Snippets.Usage.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The conversion can also be customized:

<!-- snippet: UsageWithConfig -->
<a id='snippet-UsageWithConfig'></a>
```cs
var config = new ReverseMarkdown.Config
{
    // generate GitHub flavoured markdown, supported for BR, PRE and table tags
    GithubFlavored = true,
    // Include the unknown tag completely in the result (default as well)
    Tags = { Unknown = Config.UnknownTagsOption.PassThrough },
    // will ignore all comments
    Formatting = { RemoveComments = true },
    // remove markdown output for links where appropriate
    Links = { SmartHref = true },
};

var converter = new ReverseMarkdown.Converter(config);
```
<sup><a href='/src/ReverseMarkdown.Test/Snippets.cs#L28-L44' title='Snippet source file'>snippet source</a> | <a href='#snippet-UsageWithConfig' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

To treat `<pre>` (and `<pre><code>`) content as normal HTML instead of code blocks:

```cs
var config = new ReverseMarkdown.Config
{
    Formatting = { PreAsHtml = true }
};

var converter = new ReverseMarkdown.Converter(config);
```

If you need to preserve markdown-like text as literal content (for example `# Heading` or `- Item`), either enable `Formatting.EscapeLineStarts` or use the CommonMark flavor:

```cs
var config = new ReverseMarkdown.Config
{
    Formatting = { EscapeLineStarts = true },
    // or Flavor = MarkdownFlavor.CommonMark
};

var converter = new ReverseMarkdown.Converter(config);
```

### Telegram MarkdownV2 mode

When the Telegram flavor is enabled, ReverseMarkdown applies Telegram-compatible formatting and escaping rules:

```cs
var converter = new ReverseMarkdown.Converter(new ReverseMarkdown.Config
{
    Flavor = MarkdownFlavor.Telegram
});

var html = "This is <strong>bold</strong>, <em>italic</em>, <del>strikethrough</del> and <a href=\"https://example.com/path_(one)?q=1)2\">a_b[c]</a>";
var result = converter.Convert(html);
// This is *bold*, _italic_, ~strikethrough~ and [a\_b\[c\]](https://example.com/path_(one\)?q=1\)2)
```

Notes:

- Text and link labels escape Telegram-reserved characters.
- Ordered and unordered list markers are escaped (`1\.` and `\-`).
- `<img>` falls back to a link label (for example `[Image: alt](url)`).
- `<table>` falls back to a preformatted code block representation.
- `<sup>` falls back to caret notation (for example `x^2`).

## Configuration options

Options are organized into groups on `Config`. The former flat properties still work but are obsolete and forward to the grouped members shown below.

**Flavor**

* `Flavor` - the Markdown flavor to produce (`MarkdownFlavor` enum): `Default`, `GitHub`, `CommonMark`, `Slack`, `Telegram`, `MultiMarkdown`, `Pandoc`. This is the single, canonical flavor selector; `SlackFlavored`/`TelegramMarkdownV2`/`CommonMark` are obsolete aliases.
* `GithubFlavored` - GitHub-style conversion (br, pre → fenced code, task lists) on the default writer. Tables are always GFM regardless. Distinct from `Flavor = MarkdownFlavor.GitHub`, which selects the CommonMark-based GitHub writer (preserves raw HTML). Default is false
* `CommonMarkUseHtmlInlineTags` - when the CommonMark flavor is selected, emit HTML for inline tags (`em`, `strong`, `a`, `img`) to avoid delimiter edge cases. Default is true
* `CommonMarkIntrawordEmphasisSpacing` - when the CommonMark flavor is selected, insert spaces to avoid intraword emphasis. Default is false

**`Formatting`** - output formatting

* `Formatting.CleanupSpaces` - clean up unnecessary spaces in the output. Default is true
* `Formatting.SuppressDivNewlines` - remove prefixed newlines from `div` tags. Default is false
* `Formatting.RemoveComments` - remove comment tags with text. Default is false
* `Formatting.PreAsHtml` - treat `<pre>` (and `<pre><code>`) content as normal HTML instead of a code block. Default is false
* `Formatting.EscapeLineStarts` - escape markdown line starts (headings, lists, block markers) in plain text output. Default is false
* `Formatting.OutputLineEnding` - output line endings used in generated markdown. Default is `Environment.NewLine`
* `Formatting.ListBulletChar` - the unordered-list bullet character. Default is `-` (some systems expect `*`). Ignored for the Slack flavor, which always uses `•`
* `Formatting.DefaultCodeBlockLanguage` - default GFM code block language if class-based language markers are not available

**`Links`** - link handling

* `Links.SmartHref` - how to handle an `<a>` href
  * `false` (default) - outputs `[{name}]({href}{title})` even if name and href are identical
  * `true` - if name and href are equal, outputs just the `name` (with http/https and tel:/mailto: refinements). If the Uri is not well formed per [`Uri.IsWellFormedUriString`](https://docs.microsoft.com/en-us/dotnet/api/system.uri.iswellformeduristring), markdown syntax is used anyway
* `Links.WhitelistedSchemes` - schemes (without trailing colon) allowed for `<a>`/`<img>`. Others are bypassed. Empty (default) allows everything

**`Tables`** - table handling

* `Tables.WithoutHeaderRow` - handle a table without a header row
  * `TableWithoutHeaderRowHandlingOption.Default` - first row is used as the header row (default)
  * `TableWithoutHeaderRowHandlingOption.EmptyRow` - an empty row is added as the header row
* `Tables.HeaderColumnSpans` - handle table header columns with column spans. Default is true

**`Tags`** - tag handling

* `Tags.Unknown` - handle unknown tags
  * `UnknownTagsOption.PassThrough` - include the unknown tag completely (tag plus text). Default
  * `UnknownTagsOption.Drop` - drop the unknown tag and its content
  * `UnknownTagsOption.Bypass` - ignore the unknown tag but convert its content
  * `UnknownTagsOption.Raise` - raise an error
* `Tags.Replacer` - optional markdown wrappers for unknown tags. Key is the tag name, value is the wrapper used as prefix/suffix (example: `{ ["u"] = "*" }`)
* `Tags.Aliases` - optional alias map to treat a tag as another tag (example: `{ ["u"] = "em" }`)
* `Tags.PassThrough` - tags to pass through as-is without any processing

**`Images`** - image handling

* `Images.Base64Handling` - how base64-encoded images (inline data URIs) are handled
  * `Base64ImageHandling.Include` - include them as-is (default)
  * `Base64ImageHandling.Skip` - skip/ignore them entirely
  * `Base64ImageHandling.SaveToFile` - save to disk and reference the saved path. Requires `Images.Base64Directory`
* `Images.Base64Directory` - directory to save images to when `Base64Handling` is `SaveToFile`
* `Images.Base64FileName` - function generating a filename (without extension) from the image index (int) and MIME type (string). Defaults to `image_0`, `image_1`, …
* `Images.LazySrcFallback` - when enabled, an `<img>` whose `src` is empty or a `data:` placeholder falls back to the first usable URL in `Images.LazySourceAttributes`. Default is false
* `Images.LazySourceAttributes` - ordered attributes consulted (first usable wins) when `LazySrcFallback` is enabled. Defaults to `data-src`, `data-original`, `data-lazy-src`, `data-srcset`, `data-original-src`

**`Html`** - pre-filtering (v6 Markdown DOM path)

* `Html.ExcludeSelectors` - CSS selectors whose matching elements are removed before conversion
* `Html.ElementFilters` - predicate filters; an element for which any predicate returns true is removed

### Custom converter alias

You can also register a tag to reuse another tag's converter directly:

```cs
var converter = new ReverseMarkdown.Converter();
converter.Register("u", new ReverseMarkdown.Converters.AliasConverter(converter, "em"));
```

### Base64 Image Handling Examples

ReverseMarkdown provides flexible options for handling base64-encoded images (inline data URIs) during HTML to Markdown conversion.

**Include Base64 Images (Default)**

By default, base64-encoded images are included in the markdown output as-is:

<!-- snippet: Base64ImageInclude -->
<a id='snippet-Base64ImageInclude'></a>
```cs
var converter = new ReverseMarkdown.Converter();
string html = "<img src=\"data:image/png;base64,iVBORw0KGg...\" alt=\"Sample Image\"/>";
string result = converter.Convert(html);
// Output: ![Sample Image](data:image/png;base64,iVBORw0KGg...)
```
<sup><a href='/src/ReverseMarkdown.Test/Snippets.cs#L50-L57' title='Snippet source file'>snippet source</a> | <a href='#snippet-Base64ImageInclude' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Skip Base64 Images**

To ignore base64-encoded images entirely:

<!-- snippet: Base64ImageSkip -->
<a id='snippet-Base64ImageSkip'></a>
```cs
var config = new ReverseMarkdown.Config
{
    Images = { Base64Handling = Config.Base64ImageHandling.Skip },
};
var converter = new ReverseMarkdown.Converter(config);
string html = "<img src=\"data:image/png;base64,iVBORw0KGg...\" alt=\"Sample Image\"/>";
string result = converter.Convert(html);
// Output: (empty - image is skipped)
```
<sup><a href='/src/ReverseMarkdown.Test/Snippets.cs#L63-L74' title='Snippet source file'>snippet source</a> | <a href='#snippet-Base64ImageSkip' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Save Base64 Images to Disk**

To extract and save base64-encoded images to disk:

<!-- snippet: Base64ImageSaveToFile -->
<a id='snippet-Base64ImageSaveToFile'></a>
```cs
var config = new ReverseMarkdown.Config
{
    Images =
    {
        Base64Handling = Config.Base64ImageHandling.SaveToFile,
        Base64Directory = "/path/to/images",
    },
};
var converter = new ReverseMarkdown.Converter(config);
string html = "<img src=\"data:image/png;base64,iVBORw0KGg...\" alt=\"Sample Image\"/>";
string result = converter.Convert(html);
// Output: ![Sample Image](/path/to/images/image_0.png)
// Image file saved to: /path/to/images/image_0.png
```
<sup><a href='/src/ReverseMarkdown.Test/Snippets.cs#L80-L96' title='Snippet source file'>snippet source</a> | <a href='#snippet-Base64ImageSaveToFile' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Custom Filename Generator**

You can provide a custom filename generator for saved images:

<!-- snippet: Base64ImageCustomFilename -->
<a id='snippet-Base64ImageCustomFilename'></a>
```cs
var config = new ReverseMarkdown.Config
{
    Images =
    {
        Base64Handling = Config.Base64ImageHandling.SaveToFile,
        Base64Directory = "/path/to/images",
        Base64FileName = (index, mimeType) =>
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"converted_{timestamp}_{index}";
        },
    },
};
var converter = new ReverseMarkdown.Converter(config);
// Images will be saved as: converted_20260108_143022_0.png, converted_20260108_143022_1.jpg, etc.
```
<sup><a href='/src/ReverseMarkdown.Test/Snippets.cs#L102-L120' title='Snippet source file'>snippet source</a> | <a href='#snippet-Base64ImageCustomFilename' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Supported Image Formats:**
- PNG (`image/png`)
- JPEG (`image/jpeg`, `image/jpg`)
- GIF (`image/gif`)
- BMP (`image/bmp`)
- TIFF (`image/tiff`)
- WebP (`image/webp`)
- SVG (`image/svg+xml`)

## Breaking Changes

### v6.0.0

**Config reorganization (backward-compatible):**
* Options are now grouped: `Config.Images`, `Config.Links`, `Config.Tables`, `Config.Tags`, `Config.Html` and `Config.Formatting`. The former flat properties still work but are marked `[Obsolete]` and forward to the grouped members; they will be removed in a future major version.
* `Flavor` (the `MarkdownFlavor` enum) is now the single, canonical flavor selector. `SlackFlavored`, `TelegramMarkdownV2` and `CommonMark` are obsolete aliases of it. `GithubFlavored` remains a distinct switch (GFM conversion on the default writer), not the same as `Flavor = MarkdownFlavor.GitHub`.

### v5.0.0

**Configuration Changes:**
* `WhitelistUriSchemes` - Changed from `string[]` to `HashSet<string>` (read-only property). Use `.Add()` method to add schemes instead of array assignment
* `PassThroughTags` - Changed from `string[]` to `HashSet<string>`

**API Changes:**
* `IConverter` interface signature changed from `string Convert(HtmlNode node)` to `void Convert(TextWriter writer, HtmlNode node)`. If you have custom converters, you'll need to update them to write to the TextWriter instead of returning a string

**Target Framework Changes:**

* Removed support for legacy and end-of-life .NET versions. Only actively supported .NET versions are now targeted i.e. .NET 8, .NET 9 and .NET 10. (v6 re-added a `netstandard2.0` target — see [Supported frameworks](#supported-frameworks).)

### v2.0.0

* `UnknownTags` config has been changed to an enumeration

## Acknowledgements

This library's initial implementation ideas from the Ruby based Html to Markdown converter [xijo/reverse_markdown](https://github.com/xijo/reverse_markdown).

## Copyright

Copyright © Babu Annamalai

## License

ReverseMarkdown is licensed under [MIT](http://www.opensource.org/licenses/mit-license.php "Read more about the MIT license form"). Refer to [License file](https://github.com/mysticmind/reversemarkdown-net/blob/master/LICENSE) for more information.
