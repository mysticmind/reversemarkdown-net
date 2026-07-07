# Extending

## Alias a tag

Reuse another tag's conversion with `Tags.Aliases` (key = tag to remap, value = tag to convert
it as):

snippet: sample_alias

## Custom readers

Implement `IMdReader`, mark it with `[MarkdownReader("tag", ...)]`, and pass its assembly to the
converter. A custom reader overrides the built-in one for those tags. Readers need a parameterless
constructor and the `using ReverseMarkdown.Dom;` and `using ReverseMarkdown.Readers;` namespaces.

snippet: sample_plain_link_reader

Register it by passing the assembly to the converter:

snippet: sample_custom_reader_wire

### Trimming and Native AOT

ReverseMarkdown is trim- and [Native AOT](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)-compatible.
The default conversion path uses no reflection.

The `[MarkdownReader]` + assembly form above discovers readers by **scanning assemblies with
reflection**, which the trimmer can remove and Native AOT cannot analyze. Those overloads are marked
`[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`, so the analyzer will warn if you use them in
a trimmed/AOT app.

Under trimming or AOT, register readers **explicitly** instead - no attribute, no assembly, no
reflection:

```cs
var converter = new ReverseMarkdown.Converter();
converter.RegisterReader("mark", new HighlightReader());
```

`RegisterReader` overrides the built-in reader for that tag, exactly like the scanned form. Call it
before converting; it is not safe to call concurrently with a conversion.

### Recipe: convert only a whitelist of tags, rest as plain text

Register a reader that reads an element's children (which strips the tag but keeps its text) for
the tags you want flattened, and set `Tags.Unknown = Bypass` so any remaining tag is stripped too.

snippet: sample_strip_to_text

snippet: sample_strip_to_text_usage

`<p>`, `<li>`, `<ol>`/`<ul>`, and `<a>` keep converting to markdown; everything else comes out as
plain text:

```
<p>Hi <strong>bold</strong> <a href="http://x.com">click</a></p>
->  Hi bold click (http://x.com)
```

## Transform the Markdown DOM

For anything the reader hooks don't cover, `converter.Parse(html)` returns a mutable
`MarkdownDocument` you can traverse and transform before `converter.Render(document)`.

snippet: sample_parse_render
