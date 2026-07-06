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

```csharp
var converter = new ReverseMarkdown.Converter();

string html = "This a sample <strong>paragraph</strong> from " +
              "<a href=\"http://test.com\">my site</a>";

string result = converter.Convert(html);
```

Result:

```txt
This a sample **paragraph** from [my site](http://test.com)
```

## With configuration

Options are organized into groups on `Config`. Select an output flavor with `Flavor`, and set
grouped options such as `Links`, `Formatting`, and `Tags`:

```csharp
var config = new ReverseMarkdown.Config
{
    // generate GitHub flavoured markdown (br, pre -> fenced code, task lists)
    GithubFlavored = true,
    // include unknown tags completely in the result (the default)
    Tags = { Unknown = Config.UnknownTagsOption.PassThrough },
    // ignore all comments
    Formatting = { RemoveComments = true },
    // collapse a link to plain text when text and href match
    Links = { SmartHref = true },
};

var converter = new ReverseMarkdown.Converter(config);
```

See [Configuration](/configuration) for the full option reference and [Flavors](/flavors/) for the
available output flavors.
