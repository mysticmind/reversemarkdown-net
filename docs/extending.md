# Extending

v5 converts each HTML tag with an `IConverter`. You can register your own converter for any tag, or
alias one tag to another.

## Alias one tag to another

The quickest customization is to reuse an existing tag's converter. For example, to convert `<u>`
like `<em>`:

```cs
var converter = new ReverseMarkdown.Converter();
converter.Register("u", new ReverseMarkdown.Converters.AliasConverter(converter, "em"));
```

The same effect is available declaratively via the [`TagAliases`](/configuration#tags) option:

```cs
var config = new ReverseMarkdown.Config();
config.TagAliases["u"] = "em";

var converter = new ReverseMarkdown.Converter(config);
```

## Wrap an unknown tag

To wrap an unknown tag's content with markdown without writing a converter, use
[`UnknownTagsReplacer`](/configuration#tags):

```cs
var config = new ReverseMarkdown.Config();
config.UnknownTagsReplacer["u"] = "*"; // <u>text</u> -> *text*

var converter = new ReverseMarkdown.Converter(config);
```

## Custom converter

For full control, implement `IConverter` and register it against a tag name. The interface writes
to a `TextWriter`:

```cs
public interface IConverter
{
    void Convert(TextWriter writer, HtmlNode node);
}
```

Deriving from `ConverterBase` gives you access to the owning `Converter` and helpers such as
`TreatChildrenAsString(node)`:

```cs
using System.IO;
using HtmlAgilityPack;
using ReverseMarkdown;
using ReverseMarkdown.Converters;

public sealed class HighlightConverter : ConverterBase
{
    public HighlightConverter(Converter converter) : base(converter)
    {
        // Register this converter for the <mark> tag.
        Converter.Register("mark", this);
    }

    public override void Convert(TextWriter writer, HtmlNode node)
    {
        writer.Write("==");
        writer.Write(TreatChildrenAsString(node));
        writer.Write("==");
    }
}
```

```cs
var converter = new ReverseMarkdown.Converter();
_ = new HighlightConverter(converter);

var result = converter.Convert("<mark>important</mark>");
// ==important==
```

You can also register a converter directly without deriving from `ConverterBase`, as long as it
implements `IConverter`:

```cs
converter.Register("mark", new HighlightConverter(converter));
```

::: tip v6 changed the extension model
v6 replaced the `IConverter` / `HtmlNode` model with a reader/writer model over a Markdown DOM
(`IMdReader` + `[MarkdownReader]`, and `Parse`/`Render`). Custom v5 converters do not carry over.
See the [v6 extending guide](https://mysticmind.github.io/reversemarkdown-net/extending).
:::
