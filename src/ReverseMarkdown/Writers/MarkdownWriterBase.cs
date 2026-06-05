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

        public virtual string Write(MarkdownDocument document)
        {
            Buffer.Clear();
            Visit(document);
            return Buffer.ToString();
        }

        public virtual void Visit(MarkdownDocument node) => WriteBlocks(node.Children);

        public virtual void Visit(MdHeading node)
        {
            Buffer.Append('#', node.Level).Append(' ');
            WriteInline(node.Children);
        }

        public virtual void Visit(MdParagraph node) => WriteInline(node.Children);

        public virtual void Visit(MdThematicBreak node) => Buffer.Append("---");

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
            var first = true;
            var index = 0;
            foreach (var item in node.Items)
            {
                if (!first)
                {
                    Buffer.Append('\n');
                }

                first = false;

                var marker = node.Ordered ? $"{node.Start + index}. " : "- ";
                if (item.Checked is { } isChecked)
                {
                    marker += isChecked ? "[x] " : "[ ] ";
                }

                var inner = Capture(() => WriteItemBlocks(item.Children)).Replace("\r\n", "\n");
                var lines = inner.Split('\n');
                var indent = new string(' ', marker.Length);

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

        public virtual void Visit(MdCodeBlock node)
        {
            Buffer.Append("```").Append(node.Language ?? string.Empty).Append('\n').Append(node.Literal);
            if (node.Literal.Length == 0 || node.Literal[^1] != '\n')
            {
                Buffer.Append('\n');
            }

            Buffer.Append("```");
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

        public virtual void Visit(MdHtmlBlock node) => Buffer.Append(node.Html);

        public virtual void Visit(MdText node) => Buffer.Append(node.Value);

        public virtual void Visit(MdStrong node) => Wrap("**", node.Children);

        public virtual void Visit(MdEmphasis node) => Wrap("*", node.Children);

        public virtual void Visit(MdStrikethrough node) => Wrap("~~", node.Children);

        public virtual void Visit(MdLink node)
        {
            Buffer.Append('[');
            WriteInline(node.Children);
            Buffer.Append("](").Append(node.Url);
            if (!string.IsNullOrEmpty(node.Title))
            {
                Buffer.Append(" \"").Append(node.Title).Append('"');
            }

            Buffer.Append(')');
        }

        public virtual void Visit(MdImage node)
        {
            Buffer.Append("![").Append(node.Alt).Append("](").Append(node.Url);
            if (!string.IsNullOrEmpty(node.Title))
            {
                Buffer.Append(" \"").Append(node.Title).Append('"');
            }

            Buffer.Append(')');
        }

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
            var pad = literal.Length > 0 && (literal[0] == '`' || literal[^1] == '`') ? " " : string.Empty;
            Buffer.Append(fence).Append(pad).Append(literal).Append(pad).Append(fence);
        }

        public virtual void Visit(MdLineBreak node) => Buffer.Append(node.Hard ? "  \n" : "\n");

        public virtual void Visit(MdRawInline node) => Buffer.Append(node.Html);

        /// <summary>Render block children separated by one blank line.</summary>
        protected void WriteBlocks(IEnumerable<MdBlock> blocks)
        {
            var first = true;
            foreach (var block in blocks)
            {
                if (!first)
                {
                    Buffer.Append("\n\n");
                }

                first = false;
                block.Accept(this);
            }
        }

        /// <summary>Render a list item's block children tightly (single newline between blocks).</summary>
        protected void WriteItemBlocks(IEnumerable<MdBlock> blocks)
        {
            var first = true;
            foreach (var block in blocks)
            {
                if (!first)
                {
                    Buffer.Append('\n');
                }

                first = false;
                block.Accept(this);
            }
        }

        protected void WriteInline(IEnumerable<MdInline> inlines)
        {
            foreach (var inline in inlines)
            {
                inline.Accept(this);
            }
        }

        protected void Wrap(string delimiter, IEnumerable<MdInline> children)
        {
            Buffer.Append(delimiter);
            WriteInline(children);
            Buffer.Append(delimiter);
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
