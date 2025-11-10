using System;
using System.Collections.Generic;
using HtmlAgilityPack;


namespace ReverseMarkdown;

public class ConverterContext {
    private readonly Dictionary<string, List<HtmlNode>> _ancestors = new();

    /// <summary>
    /// Enter a node to track ancestors with time complexity O(1)
    /// </summary>
    public void Enter(HtmlNode node)
    {
        var parent = node.ParentNode;
        if (parent == null!) return;

        if (_ancestors.TryGetValue(parent.Name, out var list)) {
            list.Add(parent);
        }
        else {
            _ancestors[parent.Name] = [parent];
        }
    }

    /// <summary>
    /// Leave a node to track ancestors with time complexity O(1)
    /// </summary>
    public void Leave(HtmlNode node)
    {
        var parent = node.ParentNode;
        if (parent == null!) return;

        if (_ancestors.TryGetValue(parent.Name, out var list)) {
            list.Remove(parent);
        }
        else {
            throw new InvalidOperationException("Node was not entered");
        }
    }

    /// <summary>
    /// Ancestors lookup with time complexity O(1)
    /// </summary>
    public bool AncestorsAny(string nodeName)
    {
        return _ancestors.GetValueOrDefault(nodeName)?.Count > 0;
    }

    /// <summary>
    /// Ancestors count with time complexity O(1)
    /// </summary>
    public int AncestorsCount(string nodeName)
    {
        return _ancestors.GetValueOrDefault(nodeName)?.Count ?? 0;
    }
}
