# Meet ReverseMarkdown

<p align="center">
  <img src="assets/logo.png" alt="ReverseMarkdown logo" width="160" />
</p>

[![Build status](https://github.com/mysticmind/reversemarkdown-net/actions/workflows/ci.yaml/badge.svg)](https://github.com/mysticmind/reversemarkdown-net/actions/workflows/ci.yaml) [![NuGet Version](https://badgen.net/nuget/v/reversemarkdown)](https://www.nuget.org/packages/ReverseMarkdown/) [![Docs](https://img.shields.io/badge/docs-vitepress-brightgreen)](https://mysticmind.github.io/reversemarkdown-net/)

ReverseMarkdown is a HTML to Markdown converter library in C#. v6 uses AngleSharp's HTML5-compliant parser and a Markdown DOM pipeline for reliable, performant conversion.

> **📖 Full documentation: [mysticmind.github.io/reversemarkdown-net](https://mysticmind.github.io/reversemarkdown-net/)**

> **Using v5.x?** See the [v5.x documentation](https://github.com/mysticmind/reversemarkdown-net/blob/5.x/README.md).

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

With configuration and a flavor:

```cs
var config = new ReverseMarkdown.Config
{
    Flavor = MarkdownFlavor.GitHub,
    Formatting = { RemoveComments = true },
    Links = { SmartHref = true },
};

var converter = new ReverseMarkdown.Converter(config);
```

## Features

- **Seven output flavors** - Default, GitHub, CommonMark, Slack, Telegram, MultiMarkdown, and Pandoc, selected via the `Flavor` enum.
- **Spec-compliant round-trips** - CommonMark and GitHub Flavored Markdown round-trip at 100% against canonical cmark-gfm; MultiMarkdown and Pandoc verified against canonical pandoc.
- **Extensible** - custom readers (`IMdReader` + `[MarkdownReader]`), tag aliases, and a `Parse`/`Render` Markdown DOM for direct transformation.
- **Tables, links, and images** - nested tables and captions, smart href handling, URI-scheme whitelisting, and base64 image handling (include / skip / save to disk).
- **Broad framework support** - targets `netstandard2.0`, `net8.0`, `net9.0`, and `net10.0` (runs on .NET Framework 4.6.1+, .NET Core 2.0+, Mono, and Unity).

## Performance

v6 replaces the v5 HtmlAgilityPack engine with an AngleSharp HTML5 parser feeding a Markdown DOM
and per-flavor writers. It is roughly **2–2.6× faster** than v5 and allocates about **2.7× less
memory**.

<p align="center">
  <img src="benchmark-v5-v6.png" alt="ReverseMarkdown v5 vs v6 performance" width="720" />
</p>

Measured with BenchmarkDotNet on .NET 9.0 (Apple M1), comparing published `ReverseMarkdown` 5.5.0
vs 6.0.0 on the same inputs (lower is better):

| Fixture         | Mean time v5 → v6                  | Allocated v5 → v6            |
| --------------- | ---------------------------------- | --------------------------- |
| 1000 paragraphs | 13.7 → **5.2 ms** (2.6× faster)    | 21 → **7 MB** (2.8× less)   |
| 10k paragraphs  | 119.1 → **63.1 ms** (1.9× faster)  | 183 → **67 MB** (2.7× less) |
| huge.html       | 540.6 → **244.3 ms** (2.2× faster) | 779 → **288 MB** (2.7× less)|

See the [Performance guide](https://mysticmind.github.io/reversemarkdown-net/guide/performance) for
details and how to reproduce the benchmark.

## Documentation

The full guide lives at **[mysticmind.github.io/reversemarkdown-net](https://mysticmind.github.io/reversemarkdown-net/)**:

- [Getting Started](https://mysticmind.github.io/reversemarkdown-net/guide/getting-started)
- [Performance](https://mysticmind.github.io/reversemarkdown-net/guide/performance)
- [Flavors](https://mysticmind.github.io/reversemarkdown-net/flavors/)
- [Configuration reference](https://mysticmind.github.io/reversemarkdown-net/configuration)
- [Extending (custom readers, recipes)](https://mysticmind.github.io/reversemarkdown-net/extending)
- [Migrating from v5](https://mysticmind.github.io/reversemarkdown-net/migration)

## Acknowledgements

This library's initial implementation ideas from the Ruby based Html to Markdown converter [xijo/reverse_markdown](https://github.com/xijo/reverse_markdown).

## Copyright

Copyright © Babu Annamalai

## License

ReverseMarkdown is licensed under the [MIT License](https://github.com/mysticmind/reversemarkdown-net/blob/master/LICENSE).
