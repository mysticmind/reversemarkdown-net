// On downlevel targets the polyfill extension methods (see Helpers/Polyfills.cs) are
// brought into scope everywhere so the rest of the code can call the modern instance
// methods (ReplaceLineEndings, Contains(char), DistinctBy, GetValueOrDefault, ...)
// unchanged. On net6.0+ the BCL provides these, so no global using is needed.
#if NETSTANDARD2_0 || NETFRAMEWORK
global using ReverseMarkdown.Compat;
#endif
