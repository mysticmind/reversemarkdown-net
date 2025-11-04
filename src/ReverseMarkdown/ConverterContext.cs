#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;


namespace ReverseMarkdown {
    public class ConverterContext {
        private readonly Dictionary<string, List<HtmlNode>> _ancestors = new();

        public void Enter(HtmlNode node)
        {
            if (_ancestors.TryGetValue(node.Name, out var list)) {
                list.Add(node);
            }
            else {
                _ancestors[node.Name] = [node];
            }
        }

        public void Leave(HtmlNode node)
        {
            if (_ancestors.TryGetValue(node.Name, out var list)) {
                list.Remove(node);
            }
            else {
                throw new InvalidOperationException("Node was not entered");
            }
        }

        public bool AncestorsAny(string nodeName)
        {
            return _ancestors.GetValueOrDefault(nodeName)?.Count > 0;
        }

        public int AncestorsCount(string nodeName)
        {
            return _ancestors.GetValueOrDefault(nodeName)?.Count ?? 0;
        }

        public HtmlNode? Ancestor(string nodeName)
        {
            return _ancestors.GetValueOrDefault(nodeName)?.LastOrDefault();
        }
    }
}
