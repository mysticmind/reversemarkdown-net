# Getting Started

## Install

::: code-group

```bash [.NET CLI]
dotnet add package ReverseMarkdown
```

```powershell [Package Manager]
Install-Package ReverseMarkdown
```

:::

## Basic usage

The examples below assume `using ReverseMarkdown;`.

snippet: sample_basic_usage

Result:

```txt
This a sample **paragraph** from [my site](http://test.com)
```

## With configuration

Options are organized into groups on `Config`. Select an output flavor with `Flavor`, and set
grouped options such as `Links`, `Formatting`, and `Tags`:

snippet: sample_with_config

## Writing to a TextWriter

`Convert` returns the Markdown as a string. When the output is headed for a file, an HTTP response
or any other `TextWriter`, pass the writer instead and the Markdown is written to it directly:

snippet: sample_stream_to_writer

The output is identical to the string overload, including trimming and the configured
`Formatting.OutputLineEnding`. The gain is peak memory, not speed: the full Markdown string is never
allocated, which matters on large documents.

`Render` has the same overload for a document you have already parsed (see
[Extending](/extending)):

snippet: sample_render_to_writer

One exception: with the Slack, CommonMark and GitHub flavors, `Convert(html, writer)` still builds
the string internally before writing it out. The output is correct, but there is no memory saving.

See [Configuration](/configuration) for the full option reference and [Flavors](/flavors/) for the
available output flavors.
