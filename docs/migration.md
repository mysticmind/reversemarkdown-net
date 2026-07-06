# Migrate from v5

v6 is a rewrite of the conversion engine, but the public surface stays largely source-compatible:
the old `Config` members remain as `[Obsolete]` shims that forward to the new grouped members.
Existing code keeps compiling (with deprecation warnings guiding you to the new API).

## Engine

- HTML is now parsed with **AngleSharp** (HTML5-compliant) instead of HtmlAgilityPack, and rendered
  through per-flavor writers over an intermediate Markdown DOM.
- Custom converters written against the v5 `HtmlNode`/`IConverter` model no longer apply. Use the
  v6 [reader/writer model](/extending) instead — `IMdReader` + `[MarkdownReader]`, or the
  `Parse`/`Render` DOM.

## Config reorganization

Options are now grouped. The former flat properties are obsolete aliases:

| v5 (obsolete) | v6 |
| --- | --- |
| `SmartHrefHandling` | `Links.SmartHref` |
| `WhitelistUriSchemes` | `Links.WhitelistedSchemes` |
| `TableWithoutHeaderRowHandling` | `Tables.WithoutHeaderRow` |
| `TableHeaderColumnSpanHandling` | `Tables.HeaderColumnSpans` |
| `UnknownTags` | `Tags.Unknown` |
| `UnknownTagsReplacer` | `Tags.Replacer` |
| `TagAliases` | `Tags.Aliases` |
| `PassThroughTags` | `Tags.PassThrough` |
| `Base64Images` | `Images.Base64Handling` |
| `Base64ImageSaveDirectory` | `Images.Base64Directory` |
| `Base64ImageFileNameGenerator` | `Images.Base64FileName` |
| `LazyImageSrcFallback` | `Images.LazySrcFallback` |
| `LazyImageSourceAttributes` | `Images.LazySourceAttributes` |
| `CleanupUnnecessarySpaces` | `Formatting.CleanupSpaces` |
| `SuppressDivNewlines` | `Formatting.SuppressDivNewlines` |
| `RemoveComments` | `Formatting.RemoveComments` |
| `ConvertPreContentAsHtml` | `Formatting.PreAsHtml` |
| `EscapeMarkdownLineStarts` | `Formatting.EscapeLineStarts` |
| `OutputLineEnding` | `Formatting.OutputLineEnding` |
| `ListBulletChar` | `Formatting.ListBulletChar` |
| `DefaultCodeBlockLanguage` | `Formatting.DefaultCodeBlockLanguage` |

## Flavor model

- **`Flavor`** (the `MarkdownFlavor` enum) is now the single, canonical flavor selector.
- `SlackFlavored`, `TelegramMarkdownV2`, and `CommonMark` are **obsolete aliases** of `Flavor`.
- **`GithubFlavored` stays distinct** — it produces clean GFM markdown on the default writer and is
  **not** the same as `Flavor = MarkdownFlavor.GitHub` (the CommonMark-based GitHub writer, which
  preserves raw HTML). See [GitHub](/flavors/github).

## Target frameworks

v6 targets `netstandard2.0;net8.0;net9.0;net10.0`. The `netstandard2.0` target restores support for
.NET Framework 4.6.1+, .NET Core 2.0+, Mono, and Unity. The HtmlAgilityPack dependency is removed
(replaced by AngleSharp).
