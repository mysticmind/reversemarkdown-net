# Meet ReverseMarkdown

<p align="center">
  <img src="assets/logo.png" alt="ReverseMarkdown logo" width="160" />
</p>

[![Build status](https://github.com/mysticmind/reversemarkdown-net/actions/workflows/ci.yaml/badge.svg)](https://github.com/mysticmind/reversemarkdown-net/actions/workflows/ci.yaml) [![NuGet Version](https://badgen.net/nuget/v/reversemarkdown)](https://www.nuget.org/packages/ReverseMarkdown/) [![Docs](https://img.shields.io/badge/docs-vitepress-brightgreen)](https://mysticmind.github.io/reversemarkdown-net/v5/)

ReverseMarkdown is a HTML to Markdown converter library in C#. Conversion is reliable since the HtmlAgilityPack (HAP) library is used for traversing the HTML DOM.

> **📖 Full documentation: [mysticmind.github.io/reversemarkdown-net/v5](https://mysticmind.github.io/reversemarkdown-net/v5/)**

> **Looking for v6?** v6 is a rewrite built on AngleSharp with a Markdown DOM pipeline, more flavors, and better performance. See the [v6 documentation](https://mysticmind.github.io/reversemarkdown-net/).

If you have used and benefitted from this library. Please feel free to sponsor me!<br>
<a href="https://github.com/sponsors/mysticmind" target="_blank"><img height="30" style="border:0px;height:36px;" src="https://img.shields.io/static/v1?label=GitHub Sponsor&message=%E2%9D%A4&logo=GitHub" border="0" alt="GitHub Sponsor" /></a>

## Install

```bash
dotnet add package ReverseMarkdown
```

## Quick start

```cs
var converter = new ReverseMarkdown.Converter();

string html = "This a sample <strong>paragraph</strong> from <a href=\"http://test.com\">my site</a>";

string result = converter.Convert(html);
// This a sample **paragraph** from [my site](http://test.com)
```

With configuration:

```cs
var config = new ReverseMarkdown.Config
{
    // Include the unknown tag completely in the result (default as well)
    UnknownTags = Config.UnknownTagsOption.PassThrough,
    // Generate GitHub-flavored markdown for br, pre and table
    GithubFlavored = true,
    // Remove all comments
    RemoveComments = true,
    // Omit markdown link syntax where the text and href are the same
    SmartHrefHandling = true
};

var converter = new ReverseMarkdown.Converter(config);
```

## Features

- **Broad tag support** - h1-h6, p, em, strong, i, b, blockquote, code, img, a, hr, li, ol, ul, table, tr, th, td, br, pre, del, strike, sup, dl, dt, dd, div, and span, including nested lists and tables.
- **Markdown flavors** - GitHub Flavored Markdown, Slack mrkdwn, Telegram MarkdownV2, and a CommonMark-focused mode, each toggled with a `Config` flag.
- **Tables** - nested tables (as HTML inside markdown), captions (rendered above the table), and configurable header handling.
- **Links and images** - smart href handling, URI scheme whitelisting, and base64 image handling (include / skip / save to disk).
- **Extensible** - custom converters (`IConverter`), tag aliasing, and unknown-tag strategies.
- **Broad framework support** - targets `net46`, `netstandard2.0`, `net8.0`, `net9.0`, and `net10.0`.

## Documentation

The full guide lives at **[mysticmind.github.io/reversemarkdown-net/v5](https://mysticmind.github.io/reversemarkdown-net/v5/)**:

- [Getting Started](https://mysticmind.github.io/reversemarkdown-net/v5/guide/getting-started)
- [Flavors](https://mysticmind.github.io/reversemarkdown-net/v5/flavors/)
- [Configuration reference](https://mysticmind.github.io/reversemarkdown-net/v5/configuration)
- [Extending (custom converters)](https://mysticmind.github.io/reversemarkdown-net/v5/extending)

## Acknowledgements

This library's initial implementation ideas from the Ruby based Html to Markdown converter [xijo/reverse_markdown](https://github.com/xijo/reverse_markdown).

## Copyright

Copyright © Babu Annamalai

## License

ReverseMarkdown is licensed under the [MIT License](https://github.com/mysticmind/reversemarkdown-net/blob/master/LICENSE).
