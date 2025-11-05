using System;


namespace ReverseMarkdown;

public class UnknownTagException(string tagName) : Exception($"Unknown tag: {tagName}");
