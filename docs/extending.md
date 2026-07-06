# Extending

## Alias a tag

Reuse another tag's conversion with `Tags.Aliases` (key = tag to remap, value = tag to convert
it as):

```csharp
var config = new ReverseMarkdown.Config();
config.Tags.Aliases["u"] = "em"; // convert <u> as if it were <em>
var converter = new ReverseMarkdown.Converter(config);
```

## Custom readers

Implement `IMdReader`, mark it with `[MarkdownReader("tag", ...)]`, and pass its assembly to the
converter. A custom reader overrides the built-in one for those tags. Readers need a parameterless
constructor.

```csharp
using ReverseMarkdown.Dom;
using ReverseMarkdown.Readers;

// Render <a> as "text (href)" instead of a markdown link.
[MarkdownReader("a")]
public class PlainLinkReader : IMdReader
{
    public void Read(AngleSharp.Dom.IElement element, ReaderContext ctx)
    {
        var href = element.GetAttribute("href") ?? "";
        var text = element.TextContent.Trim();
        ctx.Emit(new MdText($"{text} ({href})") { SourceTag = "a" });
    }
}

var converter = new ReverseMarkdown.Converter(
    new ReverseMarkdown.Config(),
    typeof(PlainLinkReader).Assembly);
```

### Recipe: convert only a whitelist of tags, rest as plain text

Register a reader that reads an element's children (which strips the tag but keeps its text) for
the tags you want flattened, and set `Tags.Unknown = Bypass` so any remaining tag is stripped too.

```csharp
[MarkdownReader("strong","b","em","i","del","s","h1","h2","h3","h4","h5","h6",
                "blockquote","code","pre","span","sub","sup","hr","br","img","table")]
public class StripToTextReader : IMdReader
{
    public void Read(AngleSharp.Dom.IElement element, ReaderContext ctx) => ctx.ReadChildren(element);
}

var config = new ReverseMarkdown.Config
{
    Tags = { Unknown = Config.UnknownTagsOption.Bypass }
};
var converter = new ReverseMarkdown.Converter(config, typeof(PlainLinkReader).Assembly);
```

`<p>`, `<li>`, `<ol>`/`<ul>`, and `<a>` keep converting to markdown; everything else comes out as
plain text:

```
<p>Hi <strong>bold</strong> <a href="http://x.com">click</a></p>
→  Hi bold click (http://x.com)
```

## Transform the Markdown DOM

For anything the reader hooks don't cover, `converter.Parse(html)` returns a mutable
`MarkdownDocument` you can traverse and transform before `converter.Render(document)`.

```csharp
var converter = new ReverseMarkdown.Converter();
var document = converter.Parse(html);
// inspect / filter / reshape the document here
var markdown = converter.Render(document);
```
