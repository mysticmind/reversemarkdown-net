# Meet ReverseMarkdown

[![Build status](https://github.com/mysticmind/reversemarkdown-net/actions/workflows/ci.yaml/badge.svg)](https://github.com/mysticmind/reversemarkdown-net/actions/workflows/ci.yaml) [![NuGet Version](https://badgen.net/nuget/v/reversemarkdown)](https://www.nuget.org/packages/ReverseMarkdown/)

ReverseMarkdown is a Html to Markdown converter library in C#. Conversion is very reliable since the HtmlAgilityPack (HAP) library is used for traversing the HTML DOM.

If you have used and benefitted from this library. Please feel free to sponsor me!<br>
<a href="https://github.com/sponsors/mysticmind" target="_blank"><img height="30" style="border:0px;height:36px;" src="https://img.shields.io/static/v1?label=GitHub Sponsor&message=%E2%9D%A4&logo=GitHub" border="0" alt="GitHub Sponsor" /></a>

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
    // Include the unknown tag completely in the result (default as well)
    UnknownTags = Config.UnknownTagsOption.PassThrough,
    // generate GitHub flavoured markdown, supported for BR, PRE and table tags
    GithubFlavored = true,
    // will ignore all comments
    RemoveComments = true,
    // remove markdown output for links where appropriate
    SmartHrefHandling = true
};

var converter = new ReverseMarkdown.Converter(config);
```
<sup><a href='/src/ReverseMarkdown.Test/Snippets.cs#L28-L44' title='Snippet source file'>snippet source</a> | <a href='#snippet-UsageWithConfig' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Configuration options

* `DefaultCodeBlockLanguage` - Option to set the default code block language for Github style markdown if class based language markers are not available
* `GithubFlavored` - Github style markdown for br, pre and table. Default is false
* `SlackFlavored` - Slack style markdown formatting. When enabled, uses `*` for bold, `_` for italic, `~` for strikethrough, and `•` for list bullets. Default is false
* `CleanupUnnecessarySpaces` - Cleanup unnecessary spaces in the output. Default is true
* `SuppressDivNewlines` - Removes prefixed newlines from `div` tags. Default is false
* `ListBulletChar` - Allows you to change the bullet character. Default value is `-`. Some systems expect the bullet character to be `*` rather than `-`, this config allows you to change it. Note: This option is ignored when `SlackFlavored` is enabled
* `RemoveComments` - Remove comment tags with text. Default is false
* `SmartHrefHandling` - How to handle `<a>` tag href attribute
  * `false` - Outputs `[{name}]({href}{title})` even if the name and href is identical. This is the default option.
  * `true` - If the name and href equals, outputs just the `name`. Note that if the Uri is not well formed as per [`Uri.IsWellFormedUriString`](https://docs.microsoft.com/en-us/dotnet/api/system.uri.iswellformeduristring) (i.e string is not correctly escaped like `http://example.com/path/file name.docx`) then markdown syntax will be used anyway.

    If `href` contains `http/https` protocol, and `name` doesn't but otherwise are the same, output `href` only

    If `tel:` or `mailto:` scheme, but afterwards identical with name, output `name` only.
* `UnknownTags` - handle unknown tags.
  * `UnknownTagsOption.PassThrough` - Include the unknown tag completely into the result. That is, the tag along with the text will be left in output. This is the default
  * `UnknownTagsOption.Drop` - Drop the unknown tag and its content
  * `UnknownTagsOption.Bypass` - Ignore the unknown tag but try to convert its content
  * `UnknownTagsOption.Raise` - Raise an error to let you know
* `PassThroughTags` - Pass a list of tags to pass through as-is without any processing.
* `WhitelistUriSchemes` - Specify which schemes (without trailing colon) are to be allowed for `<a>` and `<img>` tags. Others will be bypassed (output text or nothing). By default allows everything.

  If `string.Empty` provided and when `href` or `src` schema couldn't be determined - whitelists

  Schema is determined by `Uri` class, with exception when url begins with `/` (file schema) and `//` (http schema)
* `TableWithoutHeaderRowHandling` - handle table without header rows
  * `TableWithoutHeaderRowHandlingOption.Default` - First row will be used as header row (default)
  * `TableWithoutHeaderRowHandlingOption.EmptyRow` - An empty row will be added as the header row
* `TableHeaderColumnSpanHandling` - Set this flag to handle or process table header column with column spans. Default is true
* `Base64Images` - Control how base64-encoded images (inline data URIs) are handled during conversion
  * `Base64ImageHandling.Include` - Include base64-encoded images in the markdown output as-is (default behavior)
  * `Base64ImageHandling.Skip` - Skip/ignore base64-encoded images entirely
  * `Base64ImageHandling.SaveToFile` - Save base64-encoded images to disk and reference the saved file path in markdown. Requires `Base64ImageSaveDirectory` to be set
* `Base64ImageSaveDirectory` - When `Base64Images` is set to `SaveToFile`, specifies the directory path where images should be saved
* `Base64ImageFileNameGenerator` - When `Base64Images` is set to `SaveToFile`, this function generates a filename for each saved image. The function receives the image index (int) and MIME type (string), and should return a filename without extension. If not specified, images will be named as `image_0`, `image_1`, etc.

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
    Base64Images = Config.Base64ImageHandling.Skip
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
    Base64Images = Config.Base64ImageHandling.SaveToFile,
    Base64ImageSaveDirectory = "/path/to/images"
};
var converter = new ReverseMarkdown.Converter(config);
string html = "<img src=\"data:image/png;base64,iVBORw0KGg...\" alt=\"Sample Image\"/>";
string result = converter.Convert(html);
// Output: ![Sample Image](/path/to/images/image_0.png)
// Image file saved to: /path/to/images/image_0.png
```
<sup><a href='/src/ReverseMarkdown.Test/Snippets.cs#L80-L93' title='Snippet source file'>snippet source</a> | <a href='#snippet-Base64ImageSaveToFile' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Custom Filename Generator**

You can provide a custom filename generator for saved images:

<!-- snippet: Base64ImageCustomFilename -->
<a id='snippet-Base64ImageCustomFilename'></a>
```cs
var config = new ReverseMarkdown.Config
{
    Base64Images = Config.Base64ImageHandling.SaveToFile,
    Base64ImageSaveDirectory = "/path/to/images",
    Base64ImageFileNameGenerator = (index, mimeType) => 
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"converted_{timestamp}_{index}";
    }
};
var converter = new ReverseMarkdown.Converter(config);
// Images will be saved as: converted_20260108_143022_0.png, converted_20260108_143022_1.jpg, etc.
```
<sup><a href='/src/ReverseMarkdown.Test/Snippets.cs#L99-L114' title='Snippet source file'>snippet source</a> | <a href='#snippet-Base64ImageCustomFilename' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Supported Image Formats:**
- PNG (`image/png`)
- JPEG (`image/jpeg`, `image/jpg`)
- GIF (`image/gif`)
- BMP (`image/bmp`)
- TIFF (`image/tiff`)
- WebP (`image/webp`)
- SVG (`image/svg+xml`)

## Features

* Supports all the established html tags like h1, h2, h3, h4, h5, h6, p, em, strong, i, b, blockquote, code, img, a, hr, li, ol, ul, table, tr, th, td, br
* Supports nested lists
* Github Flavoured Markdown conversion supported for br, pre, tasklists and table. Use `var config = new ReverseMarkdown.Config(githubFlavoured:true);`. By default the table will always be converted to Github flavored markdown immaterial of this flag
* Slack Flavoured Markdown conversion supported. Use `var config = new ReverseMarkdown.Config { SlackFlavored = true };`
* Improved performance with optimized text writer approach and O(1) ancestor lookups
* Support for nested tables (converted as HTML inside markdown)
* Support for table captions (rendered as paragraph above table)
* Base64-encoded image handling with options to include as-is, skip, or save to disk

## Breaking Changes

### v5.0.0

**Configuration Changes:**
* `WhitelistUriSchemes` - Changed from `string[]` to `HashSet<string>` (read-only property). Use `.Add()` method to add schemes instead of array assignment
* `PassThroughTags` - Changed from `string[]` to `HashSet<string>`

**API Changes:**
* `IConverter` interface signature changed from `string Convert(HtmlNode node)` to `void Convert(TextWriter writer, HtmlNode node)`. If you have custom converters, you'll need to update them to write to the TextWriter instead of returning a string

**Target Framework Changes:**

* Removed support for legacy and end-of-life .NET versions. Only actively supported .NET versions are now targeted i.e. .NET 8, .NET 9 and .NET 10.

### v2.0.0

* `UnknownTags` config has been changed to an enumeration

## Acknowledgements

This library's initial implementation ideas from the Ruby based Html to Markdown converter [xijo/reverse_markdown](https://github.com/xijo/reverse_markdown).

## Copyright

Copyright © Babu Annamalai

## License

ReverseMarkdown is licensed under [MIT](http://www.opensource.org/licenses/mit-license.php "Read more about the MIT license form"). Refer to [License file](https://github.com/mysticmind/reversemarkdown-net/blob/master/LICENSE) for more information.
