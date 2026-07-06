# Supported Frameworks

ReverseMarkdown v5 targets:

- `net46`
- `netstandard2.0`
- `net8.0`
- `net9.0`
- `net10.0`

The `netstandard2.0` target means v5 also runs on .NET Framework 4.6.1+, .NET Core 2.0+, Mono, and
Unity. The `net46` target covers older .NET Framework 4.6 hosts directly.

## Dependency

v5 depends on [HtmlAgilityPack](https://www.nuget.org/packages/HtmlAgilityPack/) for HTML parsing.

::: tip v6 changed the engine
v6 replaced HtmlAgilityPack with [AngleSharp](https://anglesharp.io/) and targets
`netstandard2.0;net8.0;net9.0;net10.0`. If you are choosing between versions, see the
[v6 documentation](https://mysticmind.github.io/reversemarkdown-net/).
:::
