using System.Text;
using System.Text.RegularExpressions;
using ReverseMarkdown.Dom;

namespace ReverseMarkdown.Writers
{
    /// <summary>
    /// Shared CommonMark text helpers used by the CommonMark-family writers: selective escaping of
    /// link/image/reference pattern delimiters (so literal text that resembles a link round-trips as
    /// text), and word-boundary detection for intraword emphasis spacing.
    /// </summary>
    internal static class CommonMarkText
    {
        // Link/image/reference/definition patterns whose bracket & paren delimiters are escaped so
        // literal text resembling them does not render as a link.
        private static readonly Regex InlineLinkOrImagePattern =
            new(@"!?\[[^\]\r\n]*\]\([^\)\r\n]*\)", RegexOptions.Compiled);

        private static readonly Regex ReferenceLinkPattern =
            new(@"\[[^\]\r\n]+\]\[[^\]\r\n]*\]", RegexOptions.Compiled);

        private static readonly Regex LinkDefinitionPattern =
            new(@"(?m)^ {0,3}\[[^\]\r\n]+\]:", RegexOptions.Compiled);

        private static bool IsDelimiter(char character) =>
            character is '[' or ']' or '(' or ')' or '{' or '}';

        /// <summary>Escape only the bracket/paren delimiters that participate in a markdown
        /// link/image/reference pattern; stray brackets/braces elsewhere are left literal.</summary>
        public static string EscapePatternDelimiters(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return content;
            }

            var shouldEscape = new bool[content.Length];
            var hasDelimitersToEscape =
                MarkPatternDelimiters(shouldEscape, content, InlineLinkOrImagePattern) |
                MarkPatternDelimiters(shouldEscape, content, ReferenceLinkPattern) |
                MarkPatternDelimiters(shouldEscape, content, LinkDefinitionPattern) |
                MarkUnbalancedBrackets(shouldEscape, content) |
                MarkBracketBeforeParen(shouldEscape, content);

            if (!hasDelimitersToEscape)
            {
                return content;
            }

            var escaped = new StringBuilder(content.Length);
            for (var i = 0; i < content.Length; i++)
            {
                if (shouldEscape[i] && (i == 0 || content[i - 1] != '\\'))
                {
                    escaped.Append('\\');
                }

                escaped.Append(content[i]);
            }

            return escaped.ToString();
        }

        // Escape brackets that are unbalanced within this text run: a "[" with no later "]" (or a
        // "]" with no earlier "["). These are the pieces of a link/image pattern that straddles an
        // element boundary (e.g. "[foo <a>bar</a>](/uri)"), which per-run pattern matching cannot
        // see; escaping the dangling bracket keeps the run literal. A balanced "[a]" stays literal.
        private static bool MarkUnbalancedBrackets(bool[] shouldEscape, string content)
        {
            var found = false;
            var openStack = new System.Collections.Generic.Stack<int>();
            for (var i = 0; i < content.Length; i++)
            {
                if (content[i] == '[')
                {
                    openStack.Push(i);
                }
                else if (content[i] == ']')
                {
                    if (openStack.Count > 0)
                    {
                        openStack.Pop();
                    }
                    else
                    {
                        shouldEscape[i] = true;
                        found = true;
                    }
                }
            }

            foreach (var openIndex in openStack)
            {
                shouldEscape[openIndex] = true;
                found = true;
            }

            return found;
        }

        // Escape a "]" that is immediately followed by "(" — the boundary of a link/image where the
        // destination "(...)" continues past the text run (e.g. "[link](" then an element then ")").
        // Breaking the "]" keeps the run from being parsed as the start of a link.
        private static bool MarkBracketBeforeParen(bool[] shouldEscape, string content)
        {
            var found = false;
            for (var i = 0; i + 1 < content.Length; i++)
            {
                if (content[i] == ']' && content[i + 1] == '(')
                {
                    shouldEscape[i] = true;
                    found = true;
                }
            }

            return found;
        }

        private static bool MarkPatternDelimiters(bool[] shouldEscape, string content, Regex pattern)
        {
            var foundDelimiters = false;
            foreach (Match match in pattern.Matches(content))
            {
                var end = match.Index + match.Length;
                for (var i = match.Index; i < end; i++)
                {
                    if (IsDelimiter(content[i]))
                    {
                        shouldEscape[i] = true;
                        foundDelimiters = true;
                    }
                }
            }

            return foundDelimiters;
        }

        public static bool IsEmphasisRun(MdInline node) => node is MdEmphasis or MdStrong;

        public static bool IsWordChar(char? value) => value.HasValue && char.IsLetterOrDigit(value.Value);

        // Boundary text characters of an inline node (the last/first character of its flattened text
        // content), used to detect word-character adjacency across an emphasis boundary.
        public static char? LastChar(MdInline node) => FlattenText(node) is { Length: > 0 } s ? s[^1] : null;

        public static char? FirstChar(MdInline node) => FlattenText(node) is { Length: > 0 } s ? s[0] : null;

        private static string FlattenText(MdNode node)
        {
            if (node is MdText text)
            {
                return text.Value;
            }

            var sb = new StringBuilder();
            foreach (var child in node.EnumerateChildren())
            {
                sb.Append(FlattenText(child));
            }

            return sb.ToString();
        }
    }
}
