# Planning: MultiMarkdown (MMD) and Pandoc Markdown Flavor Support

## Context

The reversemarkdown-net library converts HTML to Markdown. It currently supports 4 flavors via boolean flags on `Config.cs`: `GithubFlavored`, `SlackFlavored`, `TelegramMarkdownV2`, `CommonMark`. Each converter checks these booleans for flavor-specific output.

**Goal:** Add MultiMarkdown and Pandoc as new flavors with all their extended features. Refactor the boolean-flag architecture to a `MarkdownFlavor` enum with backward-compatible wrappers.

---

## Current Architecture

### Config (`src/ReverseMarkdown/Config.cs`)
- Boolean properties: `GithubFlavored` (line 10), `SlackFlavored` (line 12), `TelegramMarkdownV2` (line 17), `CommonMark` (line 22)
- Enums: `UnknownTagsOption`, `TableWithoutHeaderRowHandlingOption`, `Base64ImageHandling`
- Additional options: `ListBulletChar`, `SmartHrefHandling`, `ConvertPreContentAsHtml`, etc.

### Converter (`src/ReverseMarkdown/Converter.cs`)
- Reflection-based converter discovery and registration (lines 37-91)
- `Register(tagName, converter)` method (line 192)
- `Lookup(tagName)` resolver with aliases, replacements, passthrough (lines 236-257)
- Main `Convert(string html)` method with flavor-specific post-processing (lines 95-161)
- `ConverterContext` via `AsyncLocal` for per-conversion state (lines 25-27)

### ConverterContext (`src/ReverseMarkdown/ConverterContext.cs`)
- Tracks ancestor nodes for context-aware conversion
- `Enter()`, `Leave()`, `AncestorsAny()`, `AncestorsCount()` methods

### Converters (`src/ReverseMarkdown/Converters/`)
- 24 tag converters, each implementing `IConverter`
- Flavor-specific branching in ~21 files across ~63 check sites
- Pattern: `if (Config.CommonMark) ... else if (Config.SlackFlavored) ... else if (Config.TelegramMarkdownV2) ...`

### Key Converter Behaviors by Flavor

| Converter | Default | GFM | Slack | Telegram | CommonMark |
|-----------|---------|-----|-------|----------|------------|
| Strong.cs | `**` | `**` | `*` | `*` | `**`/`__` |
| Em.cs | `*` | `*` | `_` | `_` | `*`/`_` |
| S.cs | `~~` | `~~` | `~` | `~` | HTML |
| Br.cs | `  ` | `\n` | `\n` | `\n` | `\` |
| Hr.cs | `* * *` | `* * *` | Exception | `\-\-\-` | `* * *` |
| Table.cs | pipe | pipe | Exception | code fence | HTML |
| Pre.cs | indent | fenced | indent | fenced | fenced |
| Img.cs | `![]()` | `![]()` | Exception | fallback | HTML |
| Li.cs | bullet | checkbox | bullet `•` | escaped | HTML |
| Sup.cs | `^text^` | `^text^` | Exception | `^text^` | HTML |

### Currently Unsupported Features (relevant to MMD/Pandoc)
- Footnotes — no converter
- Subscript (`<sub>`) — handled as unknown tag
- Abbreviations (`<abbr>`) — handled as unknown tag
- Citations (`<cite>`) — handled as unknown tag
- Math (LaTeX) — no detection
- Metadata/frontmatter — `<head>` is skipped (Converter.cs:134-136)
- Definition lists — exist but render as bullet lists, not proper `Term\n:   Definition` syntax
- Fenced divs, bracketed spans, heading attributes — no support

---

## Feature Mapping: MMD vs Pandoc

| Feature | HTML Source | MMD Output | Pandoc Output | Shared? |
|---------|------------|------------|---------------|---------|
| Footnotes | `<sup><a href="#fn1">` | `[^1]` / `[^1]: text` | `[^1]` / `[^1]: text` | Yes |
| Definition lists | `<dl><dt><dd>` | `Term\n:   Def` | `Term\n:   Def` | Yes |
| Subscript | `<sub>` | `~text~` | `~text~` | Yes |
| Superscript | `<sup>` | `^text^` (already works) | `^text^` | Yes |
| Strikethrough | `<s>/<del>` | `~~text~~` | `~~text~~` | Yes |
| Tables | `<table>` | Pipe tables + caption below | Pipe tables | Mostly |
| Fenced code | `<pre><code>` | ` ``` ` | ` ``` ` | Yes |
| Math inline | `<code class="math inline">` | `\\(...\\)` | `$...$` | Diff syntax |
| Math display | `<div class="math display">` | `\\[...\\]` | `$$...$$` | Diff syntax |
| Abbreviations | `<abbr title="...">` | `*[ABBR]: Full` | passthrough | MMD only |
| Citations | `<cite>` / `data-cite` | `[#key]` | `[@key]` | Diff syntax |
| Cross-references | `<a href="#id">` | `[text][]` | standard link | MMD only |
| Fenced divs | `<div class="x">` | passthrough | `::: {.x}` | Pandoc only |
| Bracketed spans | `<span class="x">` | passthrough | `[text]{.x}` | Pandoc only |
| Heading attributes | `<h2 id="x" class="y">` | standard | `## Text {#x .y}` | Pandoc only |
| Metadata (MMD) | `<meta>` in `<head>` | `Key: Value` pairs | — | MMD only |
| Metadata (Pandoc) | `<meta>` in `<head>` | — | YAML frontmatter | Pandoc only |
| Line blocks | `<div class="line-block">` | passthrough | `\| line text` | Pandoc only |
| Task lists | `<input type="checkbox">` | not supported | `[x]` / `[ ]` | GFM+Pandoc |

---

## Tasks

### Phase 0: Architectural Refactor

#### Task 0.1: Add `MarkdownFlavor` enum to `Config.cs`

**File:** `src/ReverseMarkdown/Config.cs`

Add enum before the class or inside the `Config` class (follow existing pattern of nested enums like `UnknownTagsOption` at line 108):

```csharp
public enum MarkdownFlavor
{
    Default,
    GithubFlavored,
    SlackFlavored,
    TelegramMarkdownV2,
    CommonMark,
    MultiMarkdown,
    Pandoc
}
```

Add property:
```csharp
public MarkdownFlavor Flavor { get; set; } = MarkdownFlavor.Default;
```

#### Task 0.2: Convert boolean properties to backward-compatible wrappers

**File:** `src/ReverseMarkdown/Config.cs`

Transform each boolean (lines 10, 12, 17, 22) from auto-properties to wrappers:

```csharp
// Line 10: was `public bool GithubFlavored { get; set; } = false;`
public bool GithubFlavored
{
    get => Flavor == MarkdownFlavor.GithubFlavored;
    set { if (value) Flavor = MarkdownFlavor.GithubFlavored;
          else if (Flavor == MarkdownFlavor.GithubFlavored) Flavor = MarkdownFlavor.Default; }
}

// Line 12: was `public bool SlackFlavored { get; set; } = false;`
public bool SlackFlavored
{
    get => Flavor == MarkdownFlavor.SlackFlavored;
    set { if (value) Flavor = MarkdownFlavor.SlackFlavored;
          else if (Flavor == MarkdownFlavor.SlackFlavored) Flavor = MarkdownFlavor.Default; }
}

// Line 17: was `public bool TelegramMarkdownV2 { get; set; } = false;`
public bool TelegramMarkdownV2
{
    get => Flavor == MarkdownFlavor.TelegramMarkdownV2;
    set { if (value) Flavor = MarkdownFlavor.TelegramMarkdownV2;
          else if (Flavor == MarkdownFlavor.TelegramMarkdownV2) Flavor = MarkdownFlavor.Default; }
}

// Line 22: was `public bool CommonMark { get; set; } = false;`
public bool CommonMark
{
    get => Flavor == MarkdownFlavor.CommonMark;
    set { if (value) Flavor = MarkdownFlavor.CommonMark;
          else if (Flavor == MarkdownFlavor.CommonMark) Flavor = MarkdownFlavor.Default; }
}
```

Note: The `set { if (value) ... else if (current) reset }` pattern ensures `config.GithubFlavored = false` only resets to Default if it was currently GithubFlavored, preventing one flavor's `= false` from clearing another.

#### Task 0.3: Add convenience boolean properties for new flavors

**File:** `src/ReverseMarkdown/Config.cs`

```csharp
public bool MultiMarkdown
{
    get => Flavor == MarkdownFlavor.MultiMarkdown;
    set { if (value) Flavor = MarkdownFlavor.MultiMarkdown;
          else if (Flavor == MarkdownFlavor.MultiMarkdown) Flavor = MarkdownFlavor.Default; }
}

public bool Pandoc
{
    get => Flavor == MarkdownFlavor.Pandoc;
    set { if (value) Flavor = MarkdownFlavor.Pandoc;
          else if (Flavor == MarkdownFlavor.Pandoc) Flavor = MarkdownFlavor.Default; }
}
```

#### Task 0.4: Add capability query helpers

**File:** `src/ReverseMarkdown/Config.cs`

These centralize the flavor→feature mapping so converters don't need to know which flavors support what:

```csharp
// Shared features
public bool SupportsFootnotes => Flavor is MarkdownFlavor.MultiMarkdown or MarkdownFlavor.Pandoc;
public bool SupportsDefinitionLists => Flavor is MarkdownFlavor.MultiMarkdown or MarkdownFlavor.Pandoc;
public bool SupportsMath => Flavor is MarkdownFlavor.MultiMarkdown or MarkdownFlavor.Pandoc;
public bool SupportsSubscript => Flavor is MarkdownFlavor.MultiMarkdown or MarkdownFlavor.Pandoc;
public bool SupportsCitations => Flavor is MarkdownFlavor.MultiMarkdown or MarkdownFlavor.Pandoc;
public bool SupportsMetadata => Flavor is MarkdownFlavor.MultiMarkdown or MarkdownFlavor.Pandoc;

public bool SupportsPipeTables => Flavor is MarkdownFlavor.Default or MarkdownFlavor.GithubFlavored
    or MarkdownFlavor.MultiMarkdown or MarkdownFlavor.Pandoc;
public bool SupportsStrikethrough => Flavor is MarkdownFlavor.GithubFlavored or MarkdownFlavor.SlackFlavored
    or MarkdownFlavor.TelegramMarkdownV2 or MarkdownFlavor.MultiMarkdown or MarkdownFlavor.Pandoc;
public bool SupportsTaskLists => Flavor is MarkdownFlavor.GithubFlavored or MarkdownFlavor.Pandoc;
public bool SupportsFencedCodeBlocks => Flavor is not MarkdownFlavor.Default;

// MMD-only features
public bool SupportsAbbreviations => Flavor is MarkdownFlavor.MultiMarkdown;
public bool SupportsCrossReferences => Flavor is MarkdownFlavor.MultiMarkdown;

// Pandoc-only features
public bool SupportsFencedDivs => Flavor is MarkdownFlavor.Pandoc;
public bool SupportsBracketedSpans => Flavor is MarkdownFlavor.Pandoc;
public bool SupportsHeadingAttributes => Flavor is MarkdownFlavor.Pandoc;
public bool SupportsLineBlocks => Flavor is MarkdownFlavor.Pandoc;
```

#### Task 0.5: Update `ListBulletChar` getter

**File:** `src/ReverseMarkdown/Config.cs` (line 83)

Currently: `get => SlackFlavored ? '•' : _listBulletChar;`
This already works via the wrapper. No change needed.

#### Task 0.6: Run existing test suite

Verify all ~193 test cases pass with the refactored Config. The boolean wrappers should make this transparent.

```bash
dotnet test src/ReverseMarkdown.Test/
```

---

### Phase 1: Shared Features (MMD + Pandoc)

#### Task 1.1: Create `Converters/Sub.cs` — Subscript

**New file:** `src/ReverseMarkdown/Converters/Sub.cs`

```csharp
using System.IO;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Sub : ConverterBase
    {
        public Sub(Converter converter) : base(converter)
        {
            Converter.Register("sub", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (Converter.Config.SlackFlavored)
                throw new SlackUnsupportedTagException(node.Name);

            if (!Converter.Config.SupportsSubscript)
            {
                // Passthrough as HTML for flavors that don't support subscript
                writer.Write($"<sub>{TreatChildrenAsString(node)}</sub>");
                return;
            }

            var content = TreatChildrenAsString(node).Trim();
            if (string.IsNullOrEmpty(content)) return;

            writer.Write('~');
            writer.Write(content);
            writer.Write('~');
        }
    }
}
```

#### Task 1.2: Modify `Converters/Dt.cs` — Definition list term

**File:** `src/ReverseMarkdown/Converters/Dt.cs`

Currently (lines 12-19):
```csharp
public override void Convert(TextWriter writer, HtmlNode node)
{
    writer.Write(Converter.Config.ListBulletChar);
    writer.Write(' ');
    var content = TreatChildrenAsString(node).Trim();
    writer.Write(content);
    writer.WriteLine();
}
```

Change to:
```csharp
public override void Convert(TextWriter writer, HtmlNode node)
{
    var content = TreatChildrenAsString(node).Trim();

    if (Converter.Config.SupportsDefinitionLists)
    {
        // MMD/Pandoc: Term on its own line
        writer.Write(content);
        writer.WriteLine();
    }
    else
    {
        writer.Write(Converter.Config.ListBulletChar);
        writer.Write(' ');
        writer.Write(content);
        writer.WriteLine();
    }
}
```

#### Task 1.3: Modify `Converters/Dd.cs` — Definition list description

**File:** `src/ReverseMarkdown/Converters/Dd.cs`

Currently (lines 12-20):
```csharp
public override void Convert(TextWriter writer, HtmlNode node)
{
    writer.Write(new string(' ', 4));
    writer.Write(Converter.Config.ListBulletChar);
    writer.Write(' ');
    var content = TreatChildrenAsString(node).Trim();
    writer.Write(content);
    writer.WriteLine();
}
```

Change to:
```csharp
public override void Convert(TextWriter writer, HtmlNode node)
{
    var content = TreatChildrenAsString(node).Trim();

    if (Converter.Config.SupportsDefinitionLists)
    {
        // MMD/Pandoc: `:   Definition`
        writer.Write(":   ");
        writer.Write(content);
        writer.WriteLine();
    }
    else
    {
        writer.Write(new string(' ', 4));
        writer.Write(Converter.Config.ListBulletChar);
        writer.Write(' ');
        writer.Write(content);
        writer.WriteLine();
    }
}
```

#### Task 1.4: Extend `ConverterContext.cs` — Add footnote, abbreviation, metadata state

**File:** `src/ReverseMarkdown/ConverterContext.cs`

Add after existing fields:

```csharp
// Footnote collection for MMD/Pandoc
private readonly Dictionary<string, string> _footnotes = new();
public bool CollectingFootnotes { get; set; }
public void AddFootnote(string id, string text) => _footnotes[id] = text;
public IReadOnlyDictionary<string, string> Footnotes => _footnotes;

// Abbreviation collection for MMD
private readonly Dictionary<string, string> _abbreviations = new();
public void AddAbbreviation(string abbr, string fullText) => _abbreviations[abbr] = fullText;
public IReadOnlyDictionary<string, string> Abbreviations => _abbreviations;

// Metadata collection for MMD/Pandoc
private readonly List<KeyValuePair<string, string>> _metadata = new();
public void AddMetadata(string key, string value) => _metadata.Add(new(key, value));
public IReadOnlyList<KeyValuePair<string, string>> Metadata => _metadata;
```

#### Task 1.5: Modify `Converters/A.cs` — Footnote reference detection

**File:** `src/ReverseMarkdown/Converters/A.cs`

In the `Convert` method, add early detection before normal link processing:

```csharp
// When footnotes are supported, detect footnote references
if (Converter.Config.SupportsFootnotes)
{
    var href = node.GetAttributeValue("href", "");

    // Detect footnote reference: <sup><a href="#fn1">1</a></sup>
    if (href.StartsWith("#fn") && node.ParentNode?.Name == "sup")
    {
        var id = href.TrimStart('#');
        // Remove "fn" prefix, keep the number
        var num = id.StartsWith("fn") ? id.Substring(2) : id;
        writer.Write($"[^{num}]");
        return;
    }

    // Detect footnote back-reference (↩, ↩︎, etc.)
    if (Converter.Context.CollectingFootnotes)
    {
        var text = TreatChildrenAsString(node).Trim();
        if (text is "↩" or "↩︎" or "\u21A9" or "\u21A9\uFE0E")
            return; // Suppress back-reference links
    }
}
```

Also handle alternative footnote patterns:
- `href="#footnote-1"` — normalize to `[^1]`
- `class="footnote-ref"` — common in Pandoc HTML output
- `role="doc-noteref"` — ARIA-based footnote markers

#### Task 1.6: Modify `Converters/Div.cs` — Footnote section detection

**File:** `src/ReverseMarkdown/Converters/Div.cs`

The `Div` converter handles `<section>` (registered at line 28). Add footnote section detection:

```csharp
// Detect footnote section: <section class="footnotes"> or <div class="footnotes">
if (Converter.Config.SupportsFootnotes)
{
    var cls = node.GetAttributeValue("class", "");
    if (cls.Contains("footnotes") || node.GetAttributeValue("role", "") == "doc-endnotes")
    {
        // Process footnotes section — collect [^id]: text entries
        Converter.Context.CollectingFootnotes = true;
        CollectFootnotes(node);
        Converter.Context.CollectingFootnotes = false;
        return; // Don't emit the section itself
    }
}
```

Add helper method:
```csharp
private void CollectFootnotes(HtmlNode sectionNode)
{
    // Find all <li id="fn1"> or <li id="footnote-1"> elements
    var items = sectionNode.SelectNodes(".//li[@id]");
    if (items == null) return;

    foreach (var li in items)
    {
        var id = li.GetAttributeValue("id", "");
        // Extract number from "fn1", "fn-1", "footnote-1" patterns
        var match = Regex.Match(id, @"(?:fn|footnote)-?(\d+)");
        if (!match.Success) continue;

        var num = match.Groups[1].Value;
        var text = TreatChildrenAsString(li).Trim();
        Converter.Context.AddFootnote(num, text);
    }
}
```

#### Task 1.7: Modify `Converter.cs` — Post-processing for collected state

**File:** `src/ReverseMarkdown/Converter.cs`

After `var result = ConvertNode(root);` (line 139), add:

```csharp
// Append footnotes for MMD/Pandoc
if (Config.SupportsFootnotes && Context.Footnotes.Count > 0)
{
    var sb = new StringBuilder(result);
    sb.AppendLine();
    sb.AppendLine();
    foreach (var fn in Context.Footnotes.OrderBy(f => int.TryParse(f.Key, out var n) ? n : 0))
    {
        sb.AppendLine($"[^{fn.Key}]: {fn.Value}");
    }
    result = sb.ToString();
}

// Append abbreviations for MMD
if (Config.SupportsAbbreviations && Context.Abbreviations.Count > 0)
{
    var sb = new StringBuilder(result);
    sb.AppendLine();
    sb.AppendLine();
    foreach (var abbr in Context.Abbreviations)
    {
        sb.AppendLine($"*[{abbr.Key}]: {abbr.Value}");
    }
    result = sb.ToString();
}

// Prepend metadata for MMD/Pandoc
if (Config.SupportsMetadata && Context.Metadata.Count > 0)
{
    var sb = new StringBuilder();
    if (Config.Pandoc)
    {
        sb.AppendLine("---");
        foreach (var meta in Context.Metadata)
            sb.AppendLine($"{meta.Key}: \"{meta.Value}\"");
        sb.AppendLine("---");
        sb.AppendLine();
    }
    else if (Config.MultiMarkdown)
    {
        foreach (var meta in Context.Metadata)
            sb.AppendLine($"{meta.Key}: {meta.Value}");
        sb.AppendLine();
    }
    sb.Append(result);
    result = sb.ToString();
}
```

#### Task 1.8: Modify `Converters/Code.cs` — Math detection

**File:** `src/ReverseMarkdown/Converters/Code.cs`

Add math detection before normal code processing:

```csharp
if (Converter.Config.SupportsMath)
{
    var cls = node.GetAttributeValue("class", "");
    if (cls.Contains("math"))
    {
        var mathContent = node.InnerText;
        var isDisplay = cls.Contains("display");

        if (Converter.Config.MultiMarkdown)
        {
            writer.Write(isDisplay ? "\\\\[" : "\\\\(");
            writer.Write(mathContent);
            writer.Write(isDisplay ? "\\\\]" : "\\\\)");
        }
        else // Pandoc
        {
            writer.Write(isDisplay ? "$$" : "$");
            writer.Write(mathContent);
            writer.Write(isDisplay ? "$$" : "$");
        }
        return;
    }
}
```

#### Task 1.9: Modify `Converters/Table.cs` — MMD/Pandoc routing

**File:** `src/ReverseMarkdown/Converters/Table.cs`

Currently the flavor checks (lines 18-29) go: CommonMark → Slack → Telegram → default pipe table.

Add MMD/Pandoc before the default path. Both support pipe tables, so the default path works. The only difference is MMD caption placement:

```csharp
// After Telegram check, before default:
if (Converter.Config.MultiMarkdown)
{
    // Render pipe table (same as default)
    // But move caption below the table in [caption] format
    var caption = node.SelectSingleNode("caption");
    var tableContent = /* render pipe table */;
    writer.Write(tableContent);
    if (caption != null)
    {
        writer.Write($"[{TreatChildrenAsString(caption).Trim()}]");
        writer.WriteLine();
    }
    return;
}
// Pandoc falls through to default pipe table rendering
```

#### Task 1.10: Verify `Pre.cs` routes correctly for MMD/Pandoc

**File:** `src/ReverseMarkdown/Converters/Pre.cs` (line 23)

Currently checks `Config.GithubFlavored || Config.CommonMark || Config.TelegramMarkdownV2` for fenced code blocks.

Change to use capability: `Config.SupportsFencedCodeBlocks` (which includes MMD and Pandoc).

---

### Phase 2: MMD-Specific Features

#### Task 2.1: Create `Converters/Abbr.cs` — Abbreviations

**New file:** `src/ReverseMarkdown/Converters/Abbr.cs`

HTML: `<abbr title="HyperText Markup Language">HTML</abbr>`
MMD appends: `*[HTML]: HyperText Markup Language` at document end.

```csharp
public class Abbr : ConverterBase
{
    public Abbr(Converter converter) : base(converter)
    {
        Converter.Register("abbr", this);
    }

    public override void Convert(TextWriter writer, HtmlNode node)
    {
        var content = TreatChildrenAsString(node).Trim();
        var title = node.GetAttributeValue("title", "");

        if (Converter.Config.SupportsAbbreviations && !string.IsNullOrEmpty(title))
        {
            Converter.Context.AddAbbreviation(content, title);
        }

        // Always emit the abbreviation text inline
        writer.Write(content);
    }
}
```

#### Task 2.2: Create `Converters/Cite.cs` — Citations

**New file:** `src/ReverseMarkdown/Converters/Cite.cs`

```csharp
public class Cite : ConverterBase
{
    public Cite(Converter converter) : base(converter)
    {
        Converter.Register("cite", this);
    }

    public override void Convert(TextWriter writer, HtmlNode node)
    {
        var content = TreatChildrenAsString(node).Trim();
        var citeKey = node.GetAttributeValue("data-cite", "");

        if (Converter.Config.SupportsCitations && !string.IsNullOrEmpty(citeKey))
        {
            if (Converter.Config.MultiMarkdown)
                writer.Write($"[#{citeKey}]");
            else // Pandoc
                writer.Write($"[@{citeKey}]");
            return;
        }

        // Default: render as italic (standard <cite> rendering)
        writer.Write('*');
        writer.Write(content);
        writer.Write('*');
    }
}
```

#### Task 2.3: Modify `Converters/A.cs` — Cross-references

**File:** `src/ReverseMarkdown/Converters/A.cs`

When MMD and `href` starts with `#` (internal anchor to a heading), emit `[text][]`:

```csharp
if (Converter.Config.SupportsCrossReferences)
{
    var href = node.GetAttributeValue("href", "");
    if (href.StartsWith("#") && !href.StartsWith("#fn"))
    {
        var text = TreatChildrenAsString(node).Trim();
        writer.Write($"[{text}][]");
        return;
    }
}
```

#### Task 2.4: Create `Converters/Meta.cs` — Metadata

**New file:** `src/ReverseMarkdown/Converters/Meta.cs`

```csharp
public class Meta : ConverterBase
{
    public Meta(Converter converter) : base(converter)
    {
        Converter.Register("meta", this);
    }

    public override void Convert(TextWriter writer, HtmlNode node)
    {
        if (!Converter.Config.SupportsMetadata) return;

        var name = node.GetAttributeValue("name", "");
        var content = node.GetAttributeValue("content", "");

        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(content))
        {
            Converter.Context.AddMetadata(name, content);
        }
    }
}
```

#### Task 2.5: Modify `Converter.cs` — Process `<head>` for metadata

**File:** `src/ReverseMarkdown/Converter.cs` (lines 134-136)

Currently:
```csharp
if (root.Descendants("body").Any()) {
    root = root.SelectSingleNode("//body");
}
```

Change to:
```csharp
if (root.Descendants("body").Any())
{
    // Process <head> first for metadata collection (MMD/Pandoc)
    if (Config.SupportsMetadata)
    {
        var head = root.SelectSingleNode("//head");
        if (head != null)
        {
            ConvertNode(head); // This triggers Meta.cs for <meta> tags
        }
    }
    root = root.SelectSingleNode("//body");
}
```

---

### Phase 3: Pandoc-Specific Features

#### Task 3.1: Modify `Converters/Div.cs` — Fenced divs

**File:** `src/ReverseMarkdown/Converters/Div.cs`

When Pandoc and `<div>` has a `class` attribute:

```csharp
if (Converter.Config.SupportsFencedDivs)
{
    var cls = node.GetAttributeValue("class", "");
    var id = node.GetAttributeValue("id", "");
    if (!string.IsNullOrEmpty(cls) || !string.IsNullOrEmpty(id))
    {
        var attrs = new List<string>();
        if (!string.IsNullOrEmpty(cls))
            attrs.AddRange(cls.Split(' ').Select(c => $".{c}"));
        if (!string.IsNullOrEmpty(id))
            attrs.Insert(0, $"#{id}");

        writer.WriteLine();
        writer.WriteLine($"::: {{{string.Join(" ", attrs)}}}");
        TreatChildren(writer, node);
        writer.WriteLine(":::");
        writer.WriteLine();
        return;
    }
}
```

#### Task 3.2: Create `Converters/Span.cs` — Bracketed spans

**New file:** `src/ReverseMarkdown/Converters/Span.cs`

Note: `<span>` is currently handled by `ByPass.cs` (registered at line 12). The new `Span.cs` converter will override this registration since converters registered later replace earlier ones.

```csharp
public class Span : ConverterBase
{
    public Span(Converter converter) : base(converter)
    {
        // Note: this overrides the ByPass registration for "span"
        Converter.Register("span", this);
    }

    public override void Convert(TextWriter writer, HtmlNode node)
    {
        var content = TreatChildrenAsString(node);

        if (Converter.Config.SupportsBracketedSpans)
        {
            var cls = node.GetAttributeValue("class", "");
            var id = node.GetAttributeValue("id", "");
            if (!string.IsNullOrEmpty(cls) || !string.IsNullOrEmpty(id))
            {
                var attrs = new List<string>();
                if (!string.IsNullOrEmpty(cls))
                    attrs.AddRange(cls.Split(' ').Select(c => $".{c}"));
                if (!string.IsNullOrEmpty(id))
                    attrs.Insert(0, $"#{id}");

                writer.Write($"[{content.Trim()}]{{{string.Join(" ", attrs)}}}");
                return;
            }
        }

        // Also handle math spans
        if (Converter.Config.SupportsMath)
        {
            var cls = node.GetAttributeValue("class", "");
            if (cls.Contains("math"))
            {
                var mathContent = node.InnerText;
                var isDisplay = cls.Contains("display");
                if (Converter.Config.MultiMarkdown)
                {
                    writer.Write(isDisplay ? "\\\\[" : "\\\\(");
                    writer.Write(mathContent);
                    writer.Write(isDisplay ? "\\\\]" : "\\\\)");
                }
                else
                {
                    writer.Write(isDisplay ? "$$" : "$");
                    writer.Write(mathContent);
                    writer.Write(isDisplay ? "$$" : "$");
                }
                return;
            }
        }

        // Default: bypass (just output content, no wrapper)
        writer.Write(content);
    }
}
```

#### Task 3.3: Modify `Converters/H.cs` — Heading attributes

**File:** `src/ReverseMarkdown/Converters/H.cs`

After generating the heading text, when Pandoc and heading has `id` or `class`:

```csharp
if (Converter.Config.SupportsHeadingAttributes)
{
    var cls = node.GetAttributeValue("class", "");
    var id = node.GetAttributeValue("id", "");
    if (!string.IsNullOrEmpty(cls) || !string.IsNullOrEmpty(id))
    {
        var attrs = new List<string>();
        if (!string.IsNullOrEmpty(id))
            attrs.Add($"#{id}");
        if (!string.IsNullOrEmpty(cls))
            attrs.AddRange(cls.Split(' ').Select(c => $".{c}"));

        writer.Write($" {{{string.Join(" ", attrs)}}}");
    }
}
```

#### Task 3.4: Modify `Converters/Div.cs` — Line blocks

**File:** `src/ReverseMarkdown/Converters/Div.cs`

When Pandoc and `<div>` has `class="line-block"`:

```csharp
if (Converter.Config.SupportsLineBlocks)
{
    var cls = node.GetAttributeValue("class", "");
    if (cls.Contains("line-block"))
    {
        writer.WriteLine();
        // Each child line (split by <br>) gets a `| ` prefix
        var lines = TreatChildrenAsString(node).Split('\n');
        foreach (var line in lines)
        {
            writer.Write("| ");
            writer.WriteLine(line.TrimEnd());
        }
        writer.WriteLine();
        return;
    }
}
```

#### Task 3.5: Modify `Converters/Li.cs` — Task lists for Pandoc

**File:** `src/ReverseMarkdown/Converters/Li.cs` (lines 91-101)

Currently task list checkbox detection is gated on `Config.GithubFlavored`. Change to use `Config.SupportsTaskLists` (which includes Pandoc).

---

### Phase 4: Existing Converter Updates for New Flavors

Ensure converters that currently check specific flavors gracefully handle MMD/Pandoc.

#### Task 4.1: `Converters/Strong.cs` — Emphasis markers

**File:** `src/ReverseMarkdown/Converters/Strong.cs`

MMD/Pandoc use `**` for bold (same as default). No change needed — the default path handles this.

#### Task 4.2: `Converters/Em.cs` — Emphasis markers

**File:** `src/ReverseMarkdown/Converters/Em.cs`

MMD/Pandoc use `*` for italic (same as default). No change needed.

#### Task 4.3: `Converters/S.cs` — Strikethrough

**File:** `src/ReverseMarkdown/Converters/S.cs`

MMD/Pandoc use `~~` (same as default). No change needed — but verify the CommonMark HTML passthrough doesn't trigger for MMD/Pandoc.

#### Task 4.4: `Converters/Br.cs` — Line breaks

**File:** `src/ReverseMarkdown/Converters/Br.cs`

MMD/Pandoc should use standard two-space line breaks (default path). Verify no unexpected fallthrough.

#### Task 4.5: `Converters/Hr.cs` — Horizontal rule

**File:** `src/ReverseMarkdown/Converters/Hr.cs`

MMD/Pandoc use `* * *` or `---` (default path `* * *` works). No change needed.

#### Task 4.6: `Converters/Img.cs` — Images

**File:** `src/ReverseMarkdown/Converters/Img.cs`

MMD/Pandoc use `![alt](url)` (default path). No change needed.

#### Task 4.7: `Converters/Sup.cs` — Superscript

**File:** `src/ReverseMarkdown/Converters/Sup.cs`

MMD/Pandoc use `^text^` (same as default path). Verify footnote `<sup>` wrapping doesn't conflict with Task 1.5.

#### Task 4.8: `Converters/Ol.cs` — Lists

**File:** `src/ReverseMarkdown/Converters/Ol.cs`

Currently CommonMark outputs raw HTML (line 17). MMD/Pandoc should NOT do this — they use normal list rendering. Verify the CommonMark check doesn't match MMD/Pandoc.

---

### Phase 5: Tests

#### Task 5.1: Add test cases for definition lists

```csharp
[Fact]
public void MultiMarkdown_DefinitionList()
{
    var html = "<dl><dt>Term</dt><dd>Definition</dd></dl>";
    var config = new Config { Flavor = MarkdownFlavor.MultiMarkdown };
    var converter = new Converter(config);
    var result = converter.Convert(html);
    Assert.Contains("Term", result);
    Assert.Contains(":   Definition", result);
}
```

#### Task 5.2: Add test cases for subscript

```csharp
[Fact]
public void MultiMarkdown_Subscript()
{
    var html = "H<sub>2</sub>O";
    var config = new Config { Flavor = MarkdownFlavor.MultiMarkdown };
    var result = new Converter(config).Convert(html);
    Assert.Contains("H~2~O", result);
}
```

#### Task 5.3: Add test cases for footnotes

Test against multiple HTML patterns:
- Pandoc-generated footnote HTML
- WordPress-style footnotes
- Generic `<sup><a href="#fn1">` pattern

#### Task 5.4: Add test cases for math

Test inline and display math for both MMD (`\\(...\\)`) and Pandoc (`$...$`) syntaxes.

#### Task 5.5: Add test cases for Pandoc-specific features

- Fenced divs: `<div class="warning">` → `::: {.warning}`
- Bracketed spans: `<span class="highlight">` → `[text]{.highlight}`
- Heading attributes: `<h2 id="x">` → `## Text {#x}`
- Line blocks: `<div class="line-block">` → `| lines`

#### Task 5.6: Add test cases for MMD-specific features

- Abbreviations: `<abbr title="...">` → `*[ABBR]: Full text` at end
- Citations: `<cite data-cite="key">` → `[#key]`
- Table captions: caption rendered below table
- Cross-references: `<a href="#heading">` → `[text][]`

#### Task 5.7: Add test cases for metadata

- MMD: `<meta name="author" content="John">` → `author: John` at top
- Pandoc: same input → YAML frontmatter

#### Task 5.8: Backward compatibility tests

Verify that using the old boolean API produces identical output:
```csharp
var config1 = new Config { GithubFlavored = true };
var config2 = new Config { Flavor = MarkdownFlavor.GithubFlavored };
// Both should produce identical output for all existing test cases
```

---

## File Change Summary

| Action | File | Phase |
|--------|------|-------|
| Modify | `src/ReverseMarkdown/Config.cs` | 0 |
| Modify | `src/ReverseMarkdown/ConverterContext.cs` | 1 |
| Modify | `src/ReverseMarkdown/Converter.cs` | 1, 2 |
| Modify | `src/ReverseMarkdown/Converters/Dt.cs` | 1 |
| Modify | `src/ReverseMarkdown/Converters/Dd.cs` | 1 |
| Modify | `src/ReverseMarkdown/Converters/A.cs` | 1, 2 |
| Modify | `src/ReverseMarkdown/Converters/Div.cs` | 1, 3 |
| Modify | `src/ReverseMarkdown/Converters/Code.cs` | 1 |
| Modify | `src/ReverseMarkdown/Converters/Table.cs` | 1 |
| Modify | `src/ReverseMarkdown/Converters/Pre.cs` | 1 |
| Modify | `src/ReverseMarkdown/Converters/H.cs` | 3 |
| Modify | `src/ReverseMarkdown/Converters/Li.cs` | 3 |
| Modify | `src/ReverseMarkdown/Converters/Ol.cs` | 4 (verify only) |
| New | `src/ReverseMarkdown/Converters/Sub.cs` | 1 |
| New | `src/ReverseMarkdown/Converters/Abbr.cs` | 2 |
| New | `src/ReverseMarkdown/Converters/Cite.cs` | 2 |
| New | `src/ReverseMarkdown/Converters/Span.cs` | 3 |
| New | `src/ReverseMarkdown/Converters/Meta.cs` | 2 |
| New | Test cases in `src/ReverseMarkdown.Test/` | 5 |

---

## Verification

1. `dotnet build` — ensure no compilation errors
2. `dotnet test src/ReverseMarkdown.Test/` — all existing tests pass (backward compat)
3. Run new MMD/Pandoc-specific tests
4. Manual testing with real HTML documents from Pandoc and MMD processors

---

## Spec-Compliance Results (v6 Markdown DOM path)

Roundtrip fidelity of the v6 path measured against the **canonical reference
binaries** over the 651-example commonmark.json corpus:
`spec HTML → v6.Convert(flavor) → reference renderer → HTML′`, compared parser-fairly.

| Flavor | Reference binary | Start | Now |
|--------|------------------|-------|-----|
| CommonMark | cmark-gfm | — | **100%** (651/651, gated) |
| GitHub (GFM) | cmark-gfm (per-section flags) | — | **100%** (672/672, gated) |
| MultiMarkdown | `multimarkdown --snippet --nosmart` | 81.9% | **95.1%** (619/651, gated ≥0.95) |
| Pandoc | `pandoc -f … -t html --wrap=none` | 84.2% | **91.7%** (597/651, gated ≥0.91) |

### Real conversion fixes (shared base / readers)
- List-item continuation indent: 4-space tab stop for MMD (vs CommonMark marker width).
- Code fences sized past the longest backtick run so embedded ``` lines don't close early.
- Inline raw-HTML passthrough for MMD (del/s/i-class/span have no MMD markdown; plain
  em/strong/i/b still convert so MMD's text-derived heading ids stay clean).
- Trim block-separator whitespace at implicit-paragraph edges (root + nested containers).
- Detect a bare single-token code class (`<code class="ruby">`) as the fence language.
- Collapse soft line breaks in heading content to spaces (ATX is single-line).
- Separate adjacent same-type lists with `<!-- -->` for Pandoc/MMD (they treat -,*,+ as one
  type; bullet alternation only works for CommonMark) via `ListSeparatorComment` seam.

### Fair symmetric Canon normalizations (applied to both sides)
MMD-generated heading/figure-img anchor ids, `class="```"` fence artifact, leading marker
spacing inside `<li>`/`<hN>`, flow-text whitespace collapse, whole-document `<p>` unwrap,
empty `<a>`/inline-pair adoption artifacts, Pandoc `<ol type="1">` default style, empty
`<!-- -->` list separators.

### Remaining failures are largely irreducible (not chasing to a literal 100%)
- **markdown-in-alt images** (MMD ~7): `<img alt="foo bar">` cannot recover that the source
  was `*foo* bar` — information lost on HTML parse.
- **malformed reference-tool output** (Links ~5): MMD/Pandoc emit broken HTML (e.g.
  `<a href="</my">`) for pathological link markdown; no converter round-trips it.
- **raw-HTML-table conversion** (HTML blocks, both): the reference tools keep a raw `<table>`
  as `<tbody><td>`; v6 converts to a pipe table that re-renders as `<thead><th>`. A markdown
  pipe table is *defined* to have a header row, so this structural difference is forced by the
  target format, not a conversion error. The harness now normalizes table *structure*
  (thead/tbody, th↔td, colgroup, align styles) symmetrically, so simple tables pass and only
  genuinely lossy cells (multi-line / nested-block content that cannot be a pipe cell) still
  fail — which is the correct signal.
- **tool-internal asymmetries** (Pandoc code spans/tabs): Pandoc's HTML reader collapses inline
  code whitespace / expands tabs differently than its markdown reader.
