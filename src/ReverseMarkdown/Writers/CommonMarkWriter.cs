using System.Text;
using ReverseMarkdown.Dom;

namespace ReverseMarkdown.Writers
{
    /// <summary>
    /// CommonMark writer. Unlike the base flavor, text is rendered roundtrip-faithfully:
    /// soft line breaks are preserved (not collapsed to spaces), markup-significant characters
    /// are escaped, and line-start markers are escaped so literal text is not reinterpreted.
    /// </summary>
    public class CommonMarkWriter : MarkdownWriterBase
    {
        public CommonMarkWriter(Config config) : base(config)
        {
        }

        protected override void WriteText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            // CommonMark preserves significant whitespace in text (multiple spaces, tabs); only
            // normalize CR out. Newlines stay as soft breaks.
            var content = value.Replace("\r", string.Empty);

            // Escape markup-significant characters so literal text round-trips. Ampersand first
            // so a literal "&ouml;" isn't reinterpreted as an entity.
            content = content
                .Replace("&", "&amp;")
                .Replace("\\", "\\\\")
                .Replace("`", "\\`")
                .Replace("*", "\\*")
                .Replace("_", "\\_")
                .Replace("[", "\\[")
                .Replace("]", "\\]")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");

            content = EscapeLineStarts(content);

            // GitHub Flavored Markdown treats "![" as an image; escape a literal "!" so that a
            // "!" immediately before a link doesn't form an image. (Bare-URL autolinking is GFM's
            // expected behavior and is left intact.)
            if (Config.Flavor == Config.MarkdownFlavor.GitHub)
            {
                content = content.Replace("!", "\\!");
            }

            // A blank line inside one text run must stay within the paragraph: encode as &#10;.
            content = System.Text.RegularExpressions.Regex.Replace(
                content, "\n{2,}", m => string.Concat(System.Linq.Enumerable.Repeat("&#10;", m.Value.Length)));

            // Suppress redundant leading whitespace (space or newline) at a boundary so a
            // preceding hard break ("  \n") isn't doubled into a paragraph split.
            if (AtWhitespaceBoundary())
            {
                content = content.TrimStart(' ', '\n');

                // A leading tab at a line start would be read as indented code; encode it.
                var tabs = 0;
                while (tabs < content.Length && content[tabs] == '\t')
                {
                    tabs++;
                }

                if (tabs > 0)
                {
                    content = string.Concat(System.Linq.Enumerable.Repeat("&#9;", tabs)) + content.Substring(tabs);
                }
            }

            Buffer.Append(content);
        }

        // Escape leading block markers (#, list bullets/numbers, setext underlines) per line.
        private static string EscapeLineStarts(string content)
        {
            if (string.IsNullOrEmpty(content) || content.IndexOf('\n') < 0 && !StartsWithMarker(content))
            {
                return StartsWithMarker(content) ? EscapeLine(content) : content;
            }

            var lines = content.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                lines[i] = EscapeLine(lines[i]);
            }

            return string.Join("\n", lines);
        }

        private static bool StartsWithMarker(string line)
        {
            var i = 0;
            while (i < line.Length && i < 3 && line[i] == ' ')
            {
                i++;
            }

            if (i >= line.Length)
            {
                return false;
            }

            var c = line[i];
            return c is '#' or '-' or '*' or '+' || char.IsDigit(c);
        }

        private static string EscapeLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return line;
            }

            var index = 0;
            while (index < line.Length && index < 3 && line[index] == ' ')
            {
                index++;
            }

            if (index >= line.Length)
            {
                return line;
            }

            var current = line[index];

            if (current == '#')
            {
                return line.Insert(index, "\\");
            }

            if (current is '-' or '*' or '+')
            {
                // bullet marker (followed by space) or setext/thematic run
                if (IsMarkerFollowedBySpace(line, index) || IsRepeatedRun(line, index, current))
                {
                    return line.Insert(index, "\\");
                }
            }

            if (char.IsDigit(current))
            {
                var end = index;
                while (end < line.Length && char.IsDigit(line[end]))
                {
                    end++;
                }

                if (end < line.Length && (line[end] == '.' || line[end] == ')') && IsMarkerFollowedBySpace(line, end))
                {
                    return line.Insert(end, "\\");
                }
            }

            return line;
        }

        private static bool IsMarkerFollowedBySpace(string line, int markerIndex)
        {
            var next = markerIndex + 1;
            return next < line.Length && line[next] == ' ';
        }

        private static bool IsRepeatedRun(string line, int index, char ch)
        {
            var trimmed = line[index..].TrimEnd();
            if (trimmed.Length < 3)
            {
                return false;
            }

            foreach (var c in trimmed)
            {
                if (c != ch && c != ' ')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
