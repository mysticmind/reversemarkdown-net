// Native AOT smoke test: published with PublishAot=true to prove ReverseMarkdown converts
// correctly in a trimmed, ahead-of-time-compiled app (no reflection fallout, AngleSharp included).
// Exits non-zero if the output is wrong, so CI fails on a regression.
using ReverseMarkdown;
using ReverseMarkdown.Dom;
using ReverseMarkdown.Readers;

var config = new Config { GithubFlavored = true };

// Exercise the HTML preprocessing pipeline (delegate-backed steps, no reflection).
config.Preprocess
    .RemoveScripts()
    .RemoveStyles()
    .Remove("div.ad")
    .Rename("b", "strong");

var converter = new Converter(config);

// Exercise the AOT-safe custom-reader API (no assembly scanning).
converter.RegisterReader("mark", new HighlightReader());

const string html = """
    <style>h1 { color: red }</style>
    <h1>Title</h1>
    <div class="ad">spam</div>
    <p>Hello <b>AOT</b> from <a href="http://x.com">link</a> <mark>hi</mark></p>
    <ul><li>one</li><li>two</li></ul>
    <script>track()</script>
    """;

var md = converter.Convert(html);
Console.WriteLine(md);

var ok =
    md.Contains("# Title") &&
    md.Contains("**AOT**") &&
    md.Contains("[link](http://x.com)") &&
    md.Contains("==hi==") &&
    md.Contains("- one") &&
    !md.Contains("spam") &&
    !md.Contains("track()");

if (!ok)
{
    Console.Error.WriteLine("AOT smoke FAILED: unexpected conversion output.");
    return 1;
}

Console.WriteLine("AOT smoke OK");
return 0;

// A custom reader registered explicitly (trim/AOT-safe path).
sealed class HighlightReader : IMdReader
{
    public void Read(AngleSharp.Dom.IElement element, ReaderContext ctx)
    {
        ctx.Emit(new MdRawInline("==") { SourceTag = element.LocalName });
        ctx.ReadChildren(element);
        ctx.Emit(new MdRawInline("==") { SourceTag = element.LocalName });
    }
}
