# Getting Started

## Install

Install the [ReverseMarkdown](https://www.nuget.org/packages/ReverseMarkdown/) package from NuGet:

```bash
dotnet add package ReverseMarkdown
```

Or with the Package Manager Console:

```powershell
Install-Package ReverseMarkdown
```

## Basic conversion

Create a `Converter` and call `Convert`:

```cs
var converter = new ReverseMarkdown.Converter();

string html = "This a sample <strong>paragraph</strong> from <a href=\"http://test.com\">my site</a>";

string result = converter.Convert(html);
// This a sample **paragraph** from [my site](http://test.com)
```

## With configuration

Pass a `Config` to customize behavior:

```cs
var config = new ReverseMarkdown.Config
{
    // Include the unknown tag completely in the result (this is also the default)
    UnknownTags = Config.UnknownTagsOption.PassThrough,
    // Generate GitHub-flavored markdown (br, pre and table)
    GithubFlavored = true,
    // Remove all comments
    RemoveComments = true,
    // Omit markdown link syntax where the text and href are the same
    SmartHrefHandling = true
};

var converter = new ReverseMarkdown.Converter(config);
```

See the [Configuration reference](/configuration) for the full list of options.

## Preserving markdown-like text

If your HTML contains literal text that looks like markdown (for example `# Heading` or `- Item`)
and you want it preserved rather than interpreted, enable `EscapeMarkdownLineStarts` or use the
[CommonMark mode](/flavors/commonmark):

```cs
var config = new ReverseMarkdown.Config
{
    EscapeMarkdownLineStarts = true
    // or: CommonMark = true
};

var converter = new ReverseMarkdown.Converter(config);
```

## Treating `<pre>` as HTML

To treat `<pre>` (and `<pre><code>`) content as normal HTML instead of a fenced code block:

```cs
var config = new ReverseMarkdown.Config
{
    ConvertPreContentAsHtml = true
};

var converter = new ReverseMarkdown.Converter(config);
```
