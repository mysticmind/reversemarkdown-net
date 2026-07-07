# Supported Frameworks

ReverseMarkdown targets `netstandard2.0`, `net8.0`, `net9.0`, and `net10.0`.

The `netstandard2.0` target means it also runs on:

- .NET Framework 4.6.1+
- .NET Core 2.0+
- Mono / Xamarin
- Unity

in addition to modern .NET (8/9/10).

## Dependencies

- [AngleSharp](https://www.nuget.org/packages/AngleSharp/) - the HTML5-compliant parser used by
  the v6 conversion pipeline.

On `netstandard2.0`, the library uses the source-only [Polyfill](https://www.nuget.org/packages/Polyfill/)
package (no runtime dependency) so modern BCL/LINQ/Regex APIs compile on the older target.

## Trimming and Native AOT

ReverseMarkdown is compatible with [trimming](https://learn.microsoft.com/dotnet/core/deploying/trimming/)
and [Native AOT](https://learn.microsoft.com/dotnet/core/deploying/native-aot/) on the modern
targets (`net8.0`+). The library is marked `IsAotCompatible`, and its CI publishes a Native AOT app
on every change and fails on any trim (`IL2xxx`) or AOT (`IL3xxx`) warning.

The **default conversion path uses no reflection**, so `new Converter().Convert(html)` is trim/AOT
safe out of the box.

The one reflection-based feature is the `[MarkdownReader]` + assembly-scanning form of
[custom readers](/extending#custom-readers): those constructor overloads are annotated
`[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` and will produce analyzer warnings under
trimming/AOT. Register readers explicitly instead - no reflection:

```cs
var converter = new ReverseMarkdown.Converter();
converter.RegisterReader("mark", new HighlightReader());
```

See [Trimming and Native AOT](/extending#trimming-and-native-aot) in the extending guide for details.

::: tip netstandard2.0
`netstandard2.0` is not a Native AOT target, so `IsAotCompatible` is not applied there. AOT is a
`net8.0`+ concern.
:::
