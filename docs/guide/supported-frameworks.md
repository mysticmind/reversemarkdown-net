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
