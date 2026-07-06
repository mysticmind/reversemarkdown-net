---
layout: home

hero:
  name: ReverseMarkdown v5
  text: HTML → Markdown for .NET
  tagline: A reliable HTML to Markdown converter built on HtmlAgilityPack, with GitHub, Slack, Telegram, and CommonMark output modes.
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
  - title: Reliable HTML parsing
    details: Uses HtmlAgilityPack to traverse the HTML DOM, handling common and nested markup - lists, tables, blockquotes, code, links, and images.
  - title: Multiple flavors
    details: GitHub Flavored Markdown, Slack mrkdwn, Telegram MarkdownV2, and a CommonMark-focused mode - each toggled with a Config flag.
  - title: Configurable
    details: Smart href handling, URI scheme whitelisting, base64 image handling, unknown-tag strategies, tag aliasing, and formatting controls.
  - title: Extensible
    details: Register custom converters against any tag with the IConverter model, or alias one tag to another.
---

::: tip Looking for v6?
This is the documentation for **ReverseMarkdown v5**. The latest release (v6) is a rewrite built on
AngleSharp and a Markdown DOM pipeline, with more flavors and better performance -
see the [v6 documentation](https://mysticmind.github.io/reversemarkdown-net/).
:::
