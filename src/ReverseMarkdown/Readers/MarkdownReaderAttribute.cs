using System;

namespace ReverseMarkdown.Readers
{
    /// <summary>
    /// Marks an <see cref="IMdReader"/> with the HTML tag(s) it handles, so it can be
    /// auto-discovered from an assembly. Decorated readers must have a parameterless
    /// constructor. Built-in readers are registered centrally; this attribute is the
    /// extension point for custom readers supplied via additional assemblies.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class MarkdownReaderAttribute : Attribute
    {
        public MarkdownReaderAttribute(params string[] tags)
        {
            Tags = tags;
        }

        public string[] Tags { get; }
    }
}
