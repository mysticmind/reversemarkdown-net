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

`Convert` returns the Markdown as a string. When the output is headed for a file or any other
`TextWriter`, pass the writer instead and the Markdown is written to it directly:

snippet: sample_stream_to_writer

The output is identical to the string overload, including trimming and the configured
`Formatting.OutputLineEnding`. The gain is peak memory, not speed: the converter still renders into
an internal buffer, but it skips the full-size copies the string overload makes to produce the final
string, which matters on large documents.

Only the output side streams. The HTML input is still a string, and it is parsed into an in-memory
document before anything is written.

The converter writes to the writer but does not flush, close or dispose it; that stays with the
caller (the `using` above does it for a file).

`Render` has the same overload for a document you have already parsed (see
[Transform the Markdown DOM](/extending#transform-the-markdown-dom)):

snippet: sample_render_to_writer

One exception: with the Slack, CommonMark and GitHub flavors, `Convert(html, writer)` and
`ConvertAsync` still build the string internally before writing it out. The output is correct, but
there is no memory saving.

### Writing asynchronously

The overloads above write synchronously. ASP.NET Core disallows synchronous writes to the response
body by default, so a writer over `Response.Body` throws an `InvalidOperationException` once its
buffer fills. Use `ConvertAsync` there, which awaits every write and takes a `CancellationToken`:

snippet: sample_convert_async

Parsing and rendering still run synchronously before the first write; only the output is
asynchronous. The output is identical to the other overloads, and `RenderAsync` covers a document
you have already parsed.

See [Configuration](/configuration) for the full option reference and [Flavors](/flavors/) for the
available output flavors.
