# Meet ReverseMarkdown

[![Build status](https://github.com/mysticmind/reversemarkdown-net/actions/workflows/ci.yaml/badge.svg)](https://github.com/mysticmind/reversemarkdown-net/actions/workflows/ci.yaml) [![NuGet Version](https://badgen.net/nuget/v/reversemarkdown)](https://www.nuget.org/packages/ReverseMarkdown/)

ReverseMarkdown is a Html to Markdown converter library in C#. Conversion is very reliable since the HtmlAgilityPack (HAP) library is used for traversing the HTML DOM.

If you have used and benefitted from this library. Please feel free to buy me a coffee!<br>
<a href="https://github.com/sponsors/mysticmind" target="_blank"><img height="30" style="border:0px;height:36px;" src="https://img.shields.io/static/v1?label=GitHub Sponsor&message=%E2%9D%A4&logo=GitHub" border="0" alt="GitHub Sponsor" /></a> <a href="https://ko-fi.com/babuannamalai" target="_blank"><img height="36" style="border:0px;height:36px;" src="https://cdn.ko-fi.com/cdn/kofi4.png?v=3" border="0" alt="Buy Me a Coffee at ko-fi.com" /></a> <a href="https://www.buymeacoffee.com/babuannamalai" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="36" width="174"></a>

## Usage

Install the package from NuGet using `Install-Package ReverseMarkdown` or clone the repository and build it yourself.

<!-- snippet: Usage -->
<a id='snippet-usage'></a>
```cs
var converter = new ReverseMarkdown.Converter();

string html = "This a sample <strong>paragraph</strong> from <a href=\"http://test.com\">my site</a>";

string result = converter.Convert(html);
```
<sup><a href='/src/ReverseMarkdown.Test/Snippets.cs#L12-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-usage' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-usagewithconfig'></a>
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
<sup><a href='/src/ReverseMarkdown.Test/Snippets.cs#L28-L44' title='Snippet source file'>snippet source</a> | <a href='#snippet-usagewithconfig' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Configuration options

* `DefaultCodeBlockLanguage` - Option to set the default code block language for Github style markdown if class based language markers are not available
* `GithubFlavored` - Github style markdown for br, pre and table. Default is false
* `SuppressNewlines` - Removes prefixed newlines from `div` tags. Default is false
* `ListBulletChar` - Allows you to change the bullet character. Default value is `-`. Some systems expect the bullet character to be `*` rather than `-`, this config allows you to change it.
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

> Note that UnknownTags config has been changed to an enumeration in v2.0.0 (breaking change)

## Features

* Supports all the established html tags like h1, h2, h3, h4, h5, h6, p, em, strong, i, b, blockquote, code, img, a, hr, li, ol, ul, table, tr, th, td, br
* Supports nested lists
* Github Flavoured Markdown conversion supported for br, pre and table. Use `var config = new ReverseMarkdown.Config(githubFlavoured:true);`. By default the table will always be converted to Github flavored markdown immaterial of this flag.

## Acknowledgements
This library's initial implementation ideas were from the Ruby based Html to Markdown converter [ xijo/reverse_markdown](https://github.com/xijo/reverse_markdown).

## Copyright

Copyright © Babu Annamalai

## License

ReverseMarkdown is licensed under [MIT](http://www.opensource.org/licenses/mit-license.php "Read more about the MIT license form"). Refer to [License file](https://github.com/mysticmind/reversemarkdown-net/blob/master/LICENSE) for more information.
