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

See [Configuration](/configuration) for the full option reference and [Flavors](/flavors/) for the
available output flavors.
