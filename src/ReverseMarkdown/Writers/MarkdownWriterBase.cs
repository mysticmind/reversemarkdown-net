using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReverseMarkdown.Dom;

namespace ReverseMarkdown.Writers
{
    /// <summary>
    /// Base for flavor writers. Implements CommonMark-ish defaults; flavor writers override
    /// only the node renderings that differ and the <see cref="Degrade"/> hook for nodes the
    /// flavor cannot represent natively. Block separation is structural (decided here), not
    /// encoded in the tree — see docs/v6/architecture.md §5.
    /// </summary>
    public abstract class MarkdownWriterBase : IMdVisitor, IMarkdownWriter
    {
        protected MarkdownWriterBase(Config config)
        {
            Config = config;
        }

        protected Config Config { get; }

        protected StringBuilder Buffer { get; } = new();

        // Flavor-overridable seams.
        protected virtual string StrongDelimiter => "**";
        protected virtual string EmphasisDelimiter => "*";
        protected virtual string StrikethroughDelimiter => "~~";
        protected virtual string UnorderedBullet => "-";

        public virtual string Write(MarkdownDocument document)
        {
            Buffer.Clear();
            WritePreamble(document);
            Visit(document);

            // Footnote definitions are collected during reading and emitted at the document end.
            foreach (var footnote in document.Meta.Footnotes)
            {
                Buffer.Append("\n\n");
                footnote.Accept(this);
            }

            return Buffer.ToString();
        }

        public virtual void Visit(MarkdownDocument node) => WriteBlocks(node.Children);

        public virtual void Visit(MdHeading node)
        {
            Buffer.Append('#', node.Level).Append(' ');
            WriteInline(node.Children);
            TrimTrailingSpaces();
        }

        public virtual void Visit(MdParagraph node)
        {
            WriteInline(node.Children);
            TrimTrailingSpaces();
        }

        // Use *** (not ---) so a thematic break inside a "- " list item isn't ambiguous.
        public virtual void Visit(MdThematicBreak node) => Buffer.Append("***");

        public virtual void Visit(MdBlockquote node)
        {
            var inner = Capture(() => WriteBlocks(node.Children)).Replace("\r\n", "\n");
            var lines = inner.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    Buffer.Append('\n');
                }

                Buffer.Append(lines[i].Length == 0 ? ">" : "> " + lines[i]);
            }
        }

        public virtual void Visit(MdList node)
        {
            // Alternate this list's marker if it directly follows a same-type sibling list.
            var siblingRun = _adjacentListRun;
            _adjacentListRun = 0; // nested lists start fresh

            var orderedDelimiter = siblingRun % 2 == 1 ? ")" : ".";
            string unorderedBullet;
            if (UnorderedBullet == "-")
            {
                unorderedBullet = new[] { "-", "*", "+" }[siblingRun % 3];
            }
            else
            {
                unorderedBullet = UnorderedBullet;
            }

            var first = true;
            var index = 0;
            foreach (var item in node.Items)
            {
                if (!first)
                {
                    // Loose lists separate items with a blank line.
                    Buffer.Append(node.Tight ? "\n" : "\n\n");
                }

                first = false;

                var marker = node.Ordered
                    ? $"{node.Start + index}{orderedDelimiter} "
                    : unorderedBullet + " ";

                // Continuation/nesting indent aligns with the list marker only; a task-list
                // checkbox ("[x] ") is item content, so it is not part of the indent.
                var indent = new string(' ', ContinuationIndent(marker.Length));
                if (item.Checked is { } isChecked)
                {
                    marker += isChecked ? "[x] " : "[ ] ";
                }

                var inner = Capture(() => WriteItemBlocks(item.Children, node.Tight)).Replace("\r\n", "\n");
                var lines = inner.Split('\n');

                Buffer.Append(marker).Append(lines[0]);
                for (var k = 1; k < lines.Length; k++)
                {
                    Buffer.Append('\n');
                    if (lines[k].Length > 0)
                    {
                        Buffer.Append(indent);
                    }

                    Buffer.Append(lines[k]);
                }

                index++;
            }
        }

        public virtual void Visit(MdListItem node) => WriteItemBlocks(node.Children);

        /// <summary>
        /// Width of the indent used to keep a list item's continuation blocks (extra paragraphs,
        /// code, nested lists) attached to the item. CommonMark aligns with the marker; some
        /// flavors (MultiMarkdown) require a fixed tab stop instead.
        /// </summary>
        protected virtual int ContinuationIndent(int markerWidth) => markerWidth;

        public virtual void Visit(MdCodeBlock node)
        {
            // The fence must be longer than the longest backtick run in the content, otherwise a
            // ``` line inside the code would close the block early (CommonMark fence rule).
            var fence = new string('`', System.Math.Max(3, LongestBacktickRun(node.Literal) + 1));
            Buffer.Append(fence).Append(node.Language ?? string.Empty).Append('\n').Append(node.Literal);
            if (node.Literal.Length == 0 || node.Literal[^1] != '\n')
            {
                Buffer.Append('\n');
            }

            Buffer.Append(fence);
        }

        private static int LongestBacktickRun(string text)
        {
            var longest = 0;
            var current = 0;
            foreach (var ch in text)
            {
                if (ch == '`')
                {
                    current++;
                    if (current > longest)
                    {
                        longest = current;
                    }
                }
                else
                {
                    current = 0;
                }
            }

            return longest;
        }

        public virtual void Visit(MdTable node)
        {
            if (node.Rows.Count == 0)
            {
                return;
            }

            if (!string.IsNullOrEmpty(node.Caption))
            {
                Buffer.Append(node.Caption).Append("\n\n");
            }

            // Pick the header row; if none is marked, use the first row (v5 default behavior).
            var headerRow = node.Rows.FirstOrDefault(r => r.IsHeader) ?? node.Rows[0];
            var bodyRows = node.Rows.Where(r => !ReferenceEquals(r, headerRow));
            var columns = node.Rows.Max(r => r.Cells.Count);

            WriteTableRow(headerRow, columns);
            Buffer.Append('\n');
            WriteTableDelimiter(headerRow, columns);
            foreach (var row in bodyRows)
            {
                Buffer.Append('\n');
                WriteTableRow(row, columns);
            }
        }

        public virtual void Visit(MdTableRow node)
        {
            var first = true;
            foreach (var cell in node.Cells)
            {
                if (!first)
                {
                    Buffer.Append(' ');
                }

                first = false;
                cell.Accept(this);
            }
        }

        public virtual void Visit(MdTableCell node) => WriteItemBlocks(node.Children);

        private void WriteTableRow(MdTableRow row, int columns)
        {
            Buffer.Append('|');
            for (var i = 0; i < columns; i++)
            {
                var text = i < row.Cells.Count
                    ? Capture(() => WriteItemBlocks(row.Cells[i].Children))
                        .Replace("\r\n", " ").Replace('\n', ' ').Replace("|", "\\|").Trim()
                    : string.Empty;
                Buffer.Append(' ').Append(text).Append(" |");
            }
        }

        private void WriteTableDelimiter(MdTableRow headerRow, int columns)
        {
            Buffer.Append('|');
            for (var i = 0; i < columns; i++)
            {
                var align = i < headerRow.Cells.Count ? headerRow.Cells[i].Align : ColumnAlignment.None;
                var marker = align switch
                {
                    ColumnAlignment.Left => ":---",
                    ColumnAlignment.Center => ":---:",
                    ColumnAlignment.Right => "---:",
                    _ => "---",
                };
                Buffer.Append(' ').Append(marker).Append(" |");
            }
        }

        public virtual void Visit(MdDefinitionList node)
        {
            var first = true;
            foreach (var item in node.Items)
            {
                if (!first)
                {
                    Buffer.Append('\n');
                }

                first = false;
                item.Accept(this);
            }
        }

        public virtual void Visit(MdDefinitionTerm node)
        {
            WriteInline(node.Children);
            TrimTrailingSpaces();
        }

        public virtual void Visit(MdDefinitionDescription node)
        {
            var inner = Capture(() => WriteItemBlocks(node.Children))
                .Replace("\r\n", " ").Replace('\n', ' ').Trim();
            Buffer.Append(":   ").Append(inner);
        }

        public virtual void Visit(MdFootnoteReference node) => Buffer.Append("[^").Append(node.Id).Append(']');

        // Default: standard <cite> renders as italic; MMD/Pandoc override to a citation key.
        public virtual void Visit(MdCitation node) => Wrap(EmphasisDelimiter, node.Children);

        // Default/MMD use LaTeX-style \(..\) / \[..\]; Pandoc overrides to $..$ / $$..$$.
        public virtual void Visit(MdMath node)
        {
            if (node.Display)
            {
                Buffer.Append("\\[").Append(node.Literal).Append("\\]");
            }
            else
            {
                Buffer.Append("\\(").Append(node.Literal).Append("\\)");
            }
        }

        public virtual void Visit(MdFootnoteDefinition node)
        {
            Buffer.Append("[^").Append(node.Id).Append("]: ");
            var inner = Capture(() => WriteItemBlocks(node.Children))
                .Replace("\r\n", " ").Replace('\n', ' ').Trim();
            Buffer.Append(inner);
        }

        // Default: fenced divs / bracketed spans degrade to just their content.
        public virtual void Visit(MdFencedDiv node) => WriteBlocks(node.Children);

        public virtual void Visit(MdBracketedSpan node) => WriteInline(node.Children);

        // Default: a line block degrades to its inline content (line breaks preserved).
        public virtual void Visit(MdLineBlock node)
        {
            WriteInline(node.Children);
            TrimTrailingSpaces();
        }

        public virtual void Visit(MdHtmlBlock node) => Buffer.Append(node.Html);

        /// <summary>Format Pandoc attributes as <c>{#id .class key="value"}</c>.</summary>
        protected static string FormatAttributes(MdAttributes? attributes)
        {
            if (attributes is null)
            {
                return "{}";
            }

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(attributes.Id))
            {
                parts.Add("#" + attributes.Id);
            }

            foreach (var cls in attributes.Classes)
            {
                parts.Add("." + cls);
            }

            foreach (var pair in attributes.Properties)
            {
                parts.Add($"{pair.Key}=\"{pair.Value}\"");
            }

            return "{" + string.Join(" ", parts) + "}";
        }

        public virtual void Visit(MdText node) => WriteText(node.Value);

        private int _strongDepth;
        private int _emphasisDepth;

        public virtual void Visit(MdStrong node)
        {
            _strongDepth++;
            Wrap(StrongDelimiterAt(_strongDepth), node.Children);
            _strongDepth--;
        }

        public virtual void Visit(MdEmphasis node)
        {
            _emphasisDepth++;
            Wrap(EmphasisDelimiterAt(_emphasisDepth), node.Children);
            _emphasisDepth--;
        }

        // Alternate delimiters when nesting so em-in-em / strong-in-strong don't merge
        // (e.g. em>em renders *_foo_* not **foo**).
        protected virtual string StrongDelimiterAt(int depth) => depth % 2 == 1 ? StrongDelimiter : "__";

        protected virtual string EmphasisDelimiterAt(int depth) => depth % 2 == 1 ? EmphasisDelimiter : "_";

        public virtual void Visit(MdStrikethrough node) => Wrap(StrikethroughDelimiter, node.Children);

        public virtual void Visit(MdSuperscript node) => Wrap("^", node.Children);

        // Default/CommonMark have no subscript syntax: degrade to inline HTML (content kept).
        // MMD/Pandoc writers override this to `~text~`.
        public virtual void Visit(MdSubscript node)
        {
            Buffer.Append("<sub>");
            WriteInline(node.Children);
            Buffer.Append("</sub>");
        }

        public virtual void Visit(MdLink node)
        {
            Buffer.Append('[');
            WriteInline(node.Children);
            Buffer.Append("](").Append(EncodeLinkDestination(node.Url));
            if (!string.IsNullOrEmpty(node.Title))
            {
                Buffer.Append(" \"").Append(EncodeTitle(node.Title)).Append('"');
            }

            Buffer.Append(')');
        }

        public virtual void Visit(MdImage node)
        {
            var alt = node.Alt
                .Replace("\\", "\\\\").Replace("[", "\\[").Replace("]", "\\]");
            Buffer.Append("![").Append(alt).Append("](").Append(EncodeLinkDestination(node.Url));
            if (!string.IsNullOrEmpty(node.Title))
            {
                Buffer.Append(" \"").Append(EncodeTitle(node.Title)).Append('"');
            }

            Buffer.Append(')');
        }

        // CommonMark link destination: use the <...> form when it contains spaces, otherwise
        // backslash-escape parentheses so unbalanced/embedded parens don't break parsing.
        private static string EncodeLinkDestination(string url)
        {
            if (url.IndexOf(' ') >= 0 || url.IndexOf('\n') >= 0)
            {
                return "<" + url.Replace("<", "\\<").Replace(">", "\\>") + ">";
            }

            return url.Replace("(", "\\(").Replace(")", "\\)");
        }

        private static string EncodeTitle(string title) => title.Replace("\"", "\\\"");

        public virtual void Visit(MdInlineCode node)
        {
            var literal = node.Literal;
            var longestRun = 0;
            var run = 0;
            foreach (var c in literal)
            {
                if (c == '`')
                {
                    run++;
                    longestRun = run > longestRun ? run : longestRun;
                }
                else
                {
                    run = 0;
                }
            }

            var fence = new string('`', longestRun + 1);
            // Pad with a space each side when the content starts/ends with a backtick or a space:
            // the reader strips one space each side, round-tripping the original literal.
            var needsPad = literal.Length > 0 &&
                           (literal[0] is '`' or ' ' || literal[^1] is '`' or ' ') &&
                           literal.Trim().Length > 0;
            var pad = needsPad ? " " : string.Empty;
            Buffer.Append(fence).Append(pad).Append(literal).Append(pad).Append(fence);
        }

        public virtual void Visit(MdLineBreak node) => Buffer.Append(node.Hard ? "  \n" : "\n");

        public virtual void Visit(MdRawInline node) => Buffer.Append(node.Html);

        // How many same-type lists precede the current one in this block sequence (for marker
        // alternation, so adjacent lists don't merge).
        private int _adjacentListRun;

        /// <summary>Render block children separated by one blank line.</summary>
        protected void WriteBlocks(IEnumerable<MdBlock> blocks)
        {
            MdBlock? prev = null;
            var run = 0;
            foreach (var block in blocks)
            {
                if (prev is not null)
                {
                    Buffer.Append("\n\n");
                }

                run = prev is MdList pl && block is MdList bl && pl.Ordered == bl.Ordered ? run + 1 : 0;
                _adjacentListRun = run;
                block.Accept(this);
                prev = block;
            }
        }

        /// <summary>
        /// Render a list item's block children. In a loose item, consecutive non-list blocks
        /// (e.g. multiple paragraphs) are separated by a blank line; a tight item — or a nested
        /// list — joins with a single newline so it indents on the next line.
        /// </summary>
        protected void WriteItemBlocks(IEnumerable<MdBlock> blocks, bool tight = true)
        {
            var list = blocks.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                if (i > 0)
                {
                    // Tight items join blocks with a single newline; loose items use a blank line
                    // (a nested list still attaches because it is indented).
                    Buffer.Append(tight ? "\n" : "\n\n");
                }

                list[i].Accept(this);
            }
        }

        protected void WriteInline(IEnumerable<MdInline> inlines)
        {
            foreach (var inline in inlines)
            {
                inline.Accept(this);
            }
        }

        /// <summary>
        /// Wrap inline content in a delimiter, moving any leading/trailing spaces *outside*
        /// the delimiters (emphasis markers must hug non-space content to render). Mirrors v5's
        /// emphasis whitespace guard.
        /// </summary>
        protected void Wrap(string delimiter, IEnumerable<MdInline> children)
        {
            var inner = Capture(() =>
            {
                foreach (var child in children)
                {
                    child.Accept(this);
                }
            });

            var core = inner.Trim(' ');
            if (core.Length == 0)
            {
                Buffer.Append(inner);
                return;
            }

            if (inner.Length > 0 && inner[0] == ' ')
            {
                Buffer.Append(' ');
            }

            Buffer.Append(delimiter).Append(core).Append(delimiter);

            if (inner.Length > 0 && inner[^1] == ' ')
            {
                Buffer.Append(' ');
            }
        }

        /// <summary>Write text content with HTML-style whitespace collapsing: runs of
        /// whitespace (incl. newlines/tabs from source indentation) become a single space, and
        /// a leading space is suppressed when the output is already at a whitespace boundary.</summary>
        protected virtual void WriteText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            var collapsed = CollapseWhitespace(value);
            if (collapsed.Length == 0)
            {
                return;
            }

            if (collapsed[0] == ' ' && AtWhitespaceBoundary())
            {
                collapsed = collapsed.Substring(1);
            }

            AppendText(collapsed);
        }

        /// <summary>
        /// Append normalized text to the buffer, escaping markdown emphasis delimiters
        /// (<c>*</c>/<c>_</c>) so literal text isn't reinterpreted. Override to change escaping
        /// (e.g. Slack escapes nothing; Telegram escapes the MarkdownV2 set).
        /// </summary>
        protected virtual void AppendText(string text)
        {
            foreach (var c in text)
            {
                if (c is '*' or '_')
                {
                    Buffer.Append('\\');
                }

                Buffer.Append(c);
            }
        }

        /// <summary>Emit document-level preamble (e.g. metadata / YAML frontmatter). Default: none.</summary>
        protected virtual void WritePreamble(MarkdownDocument document)
        {
        }

        protected bool AtWhitespaceBoundary()
        {
            if (Buffer.Length == 0)
            {
                return true;
            }

            var last = Buffer[^1];
            return last == ' ' || last == '\n';
        }

        private protected void TrimTrailingSpaces()
        {
            while (Buffer.Length > 0 && (Buffer[^1] == ' ' || Buffer[^1] == '\n'))
            {
                Buffer.Length--;
            }
        }

        private static string CollapseWhitespace(string s)
        {
            var sb = new StringBuilder(s.Length);
            var inWhitespace = false;
            foreach (var c in s)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!inWhitespace)
                    {
                        sb.Append(' ');
                        inWhitespace = true;
                    }
                }
                else
                {
                    sb.Append(c);
                    inWhitespace = false;
                }
            }

            return sb.ToString();
        }

        /// <summary>Render via <paramref name="render"/> and return the produced text without
        /// leaving it in the buffer — used for post-processing (e.g. blockquote line prefixes).</summary>
        protected string Capture(System.Action render)
        {
            var start = Buffer.Length;
            render();
            var text = Buffer.ToString(start, Buffer.Length - start);
            Buffer.Length = start;
            return text;
        }

        /// <summary>
        /// Fallback for a node a flavor cannot represent natively. Default emits the source
        /// tag as raw HTML when known; flavor writers may override to drop, throw, or inline.
        /// </summary>
        protected virtual void Degrade(MdNode node)
        {
            if (node.SourceTag is { Length: > 0 } tag)
            {
                Buffer.Append('<').Append(tag).Append("></").Append(tag).Append('>');
            }
        }
    }
}
