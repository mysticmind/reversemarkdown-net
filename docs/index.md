---
layout: home

hero:
  name: ReverseMarkdown
  text: HTML → Markdown for .NET
  tagline: A fast, spec-compliant HTML to Markdown converter. v6 is built on AngleSharp's HTML5 parser and a Markdown DOM pipeline, with seven output flavors.
  image:
    src: /logo.png
    alt: ReverseMarkdown
  actions:
    - theme: brand
      text: Get Started
      link: /guide/getting-started
    - theme: alt
      text: Flavors
      link: /flavors/
    - theme: alt
      text: View on GitHub
      link: https://github.com/mysticmind/reversemarkdown-net

features:
  - title: Seven flavors
    details: Default, GitHub, CommonMark, Slack, Telegram, MultiMarkdown, and Pandoc - selected with a single Flavor enum.
  - title: Spec-compliant round-trips
    details: CommonMark and GitHub Flavored Markdown round-trip at 100% against canonical cmark-gfm; MultiMarkdown and Pandoc verified against canonical pandoc.
  - title: Extensible
    details: Plug in custom readers, alias tags, or transform the Markdown DOM directly via Parse/Render.
  - title: Broad framework support
    details: Targets netstandard2.0, net8.0, net9.0, and net10.0 - runs on .NET Framework 4.6.1+, .NET Core 2.0+, Mono, and Unity.
  - title: Trimming & Native AOT ready
    details: The default conversion path uses no reflection, so it publishes clean under trimming and Native AOT. Add custom readers with RegisterReader. CI fails on any trim/AOT warning.
    link: /guide/supported-frameworks#trimming-and-native-aot
    linkText: How it works
---
