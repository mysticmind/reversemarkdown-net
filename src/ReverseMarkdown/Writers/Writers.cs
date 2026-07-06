using System;
using System.Linq;
using ReverseMarkdown.Dom;
using ReverseMarkdown.Helpers;

namespace ReverseMarkdown.Writers
{
    /// <summary>
    /// The library's default writer. Phase B uses the <see cref="MarkdownWriterBase"/>
    /// behavior; v5-default specifics (indented code, <c>* * *</c> rules, etc.) land as
    /// overrides here in a later phase. Byte parity with v5 is explicitly not a goal.
    /// </summary>
    public sealed class DefaultWriter : MarkdownWriterBase
    {
        public DefaultWriter(Config config) : base(config)
        {
        }

        private int _strikeDepth;
        private int _superscriptDepth;

        // v5 collapses nested same-type emphasis to a single outer wrap (e.g. <em><em>x</em></em>
        // → *x*), rather than the base writer's alternating delimiters (*_x_*).
        public override void Visit(MdEmphasis node)
        {
            if (EmphasisDepth >= 1)
            {
                WriteInline(node.Children);
                return;
            }

            base.Visit(node);
        }

        public override void Visit(MdStrong node)
        {
            if (StrongDepth >= 1)
            {
                WriteInline(node.Children);
                return;
            }

            base.Visit(node);
        }

        public override void Visit(MdStrikethrough node)
        {
            if (_strikeDepth >= 1)
            {
                WriteInline(node.Children);
                return;
            }

            _strikeDepth++;
            base.Visit(node);
            _strikeDepth--;
        }

        public override void Visit(MdSuperscript node)
        {
            if (_superscriptDepth >= 1)
            {
                WriteInline(node.Children);
                return;
            }

            _superscriptDepth++;
            base.Visit(node);
            _superscriptDepth--;
        }

        // v5 inserts a space between two adjacent same-type emphasis runs so their delimiters
        // don't merge (*a**b* mis-parses; emit *a* *b*). Mixed types (em then strong) are left
        // untouched.
        protected override string? InlineSeparator(MdInline previous, MdInline next)
        {
            // CommonMark intraword emphasis: an emphasis run sitting flush against a word character
            // (e.g. he<strong>ll</strong>o) must be spaced out (he **ll** o) so the delimiters bind.
            if (Config.CommonMark && Config.CommonMarkIntrawordEmphasisSpacing)
            {
                var boundaryIsWord = IsWordChar(LastChar(previous)) && IsWordChar(FirstChar(next));
                if (boundaryIsWord && (IsEmphasisRun(next) || IsEmphasisRun(previous)))
                {
                    return " ";
                }
            }

            return (previous, next) switch
            {
                (MdEmphasis, MdEmphasis) => " ",
                (MdStrong, MdStrong) => " ",
                _ => null,
            };
        }

        // v5 folds a continuation paragraph in a list item behind a blank line so it round-trips as
        // a separate paragraph rather than lazily merging into the item's first line.
        protected override bool BlankLineBeforeContinuationParagraph => true;

        private static bool IsEmphasisRun(MdInline node) => node is MdEmphasis or MdStrong;

        private static bool IsWordChar(char? value) => value.HasValue && char.IsLetterOrDigit(value.Value);

        // Boundary text characters of an inline node (the last/first character of its flattened
        // text content), used to detect word-character adjacency across an emphasis boundary.
        private static char? LastChar(MdInline node) => FlattenText(node) is { Length: > 0 } s ? s[^1] : null;

        private static char? FirstChar(MdInline node) => FlattenText(node) is { Length: > 0 } s ? s[0] : null;

        private static string FlattenText(MdNode node)
        {
            if (node is MdText text)
            {
                return text.Value;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var child in node.EnumerateChildren())
            {
                sb.Append(FlattenText(child));
            }

            return sb.ToString();
        }

        // v5 percent-encodes spaces and parentheses in a link href (rather than CommonMark's
        // backslash/<…> escaping), so the destination round-trips without breaking the ().
        private bool _inLinkText;

        // v5 escapes only the bracket delimiters in link text (so *, _ in a URL-like text stay
        // literal); elsewhere it escapes emphasis delimiters and entity-encodes angle brackets so
        // they aren't read as inline HTML.
        protected override void AppendText(string text)
        {
            if (_inLinkText)
            {
                foreach (var c in text)
                {
                    if (c is '[' or ']')
                    {
                        Buffer.Append('\\');
                    }

                    Buffer.Append(c);
                }

                return;
            }

            // CommonMark: a plain-text run that looks like a markdown link/image/reference (e.g.
            // "[label](url)") must have only its pattern delimiters escaped so it round-trips as
            // literal text; stray brackets/braces elsewhere (e.g. "[a]", "{plain}") stay literal.
            if (Config.CommonMark)
            {
                text = EscapeCommonMarkPatternDelimiters(text);
            }

            // Content inside a literal backtick code span is verbatim (v5 un-escapes key chars
            // there), so only escape outside backticks.
            var inBackticks = false;
            foreach (var c in text)
            {
                if (c == '`')
                {
                    inBackticks = !inBackticks;
                    Buffer.Append(c);
                }
                else if (inBackticks)
                {
                    Buffer.Append(c);
                }
                else
                {
                    switch (c)
                    {
                        case '<': Buffer.Append("&lt;"); break;
                        case '>': Buffer.Append("&gt;"); break;
                        case '*':
                        case '_': Buffer.Append('\\').Append(c); break;
                        default: Buffer.Append(c); break;
                    }
                }
            }
        }

        // CommonMark link/image/reference/definition patterns whose bracket & paren delimiters are
        // escaped so literal text that resembles them does not render as a link (v5 Text.cs parity).
        private static readonly System.Text.RegularExpressions.Regex CommonMarkInlineLinkOrImagePattern =
            new(@"!?\[[^\]\r\n]*\]\([^\)\r\n]*\)", System.Text.RegularExpressions.RegexOptions.Compiled);

        private static readonly System.Text.RegularExpressions.Regex CommonMarkReferenceLinkPattern =
            new(@"\[[^\]\r\n]+\]\[[^\]\r\n]*\]", System.Text.RegularExpressions.RegexOptions.Compiled);

        private static readonly System.Text.RegularExpressions.Regex CommonMarkLinkDefinitionPattern =
            new(@"(?m)^ {0,3}\[[^\]\r\n]+\]:", System.Text.RegularExpressions.RegexOptions.Compiled);

        private static bool IsCommonMarkDelimiter(char character) =>
            character is '[' or ']' or '(' or ')' or '{' or '}';

        private static string EscapeCommonMarkPatternDelimiters(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return content;
            }

            var shouldEscape = new bool[content.Length];
            var hasDelimitersToEscape =
                MarkCommonMarkPatternDelimiters(shouldEscape, content, CommonMarkInlineLinkOrImagePattern) |
                MarkCommonMarkPatternDelimiters(shouldEscape, content, CommonMarkReferenceLinkPattern) |
                MarkCommonMarkPatternDelimiters(shouldEscape, content, CommonMarkLinkDefinitionPattern);

            if (!hasDelimitersToEscape)
            {
                return content;
            }

            var escaped = new System.Text.StringBuilder(content.Length);
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

        private static bool MarkCommonMarkPatternDelimiters(
            bool[] shouldEscape, string content, System.Text.RegularExpressions.Regex pattern)
        {
            var foundDelimiters = false;
            foreach (System.Text.RegularExpressions.Match match in pattern.Matches(content))
            {
                var end = match.Index + match.Length;
                for (var i = match.Index; i < end; i++)
                {
                    if (IsCommonMarkDelimiter(content[i]))
                    {
                        shouldEscape[i] = true;
                        foundDelimiters = true;
                    }
                }
            }

            return foundDelimiters;
        }

        public override void Visit(MdParagraph node)
        {
            if (!Config.EscapeMarkdownLineStarts)
            {
                base.Visit(node);
                return;
            }

            // Escape markdown block markers (#, -/*/+, N./N), setext rules) at the start of each
            // line so literal paragraph text isn't reinterpreted as a heading/list/rule.
            var text = Capture(() => WriteInline(node.Children));
            Buffer.Append(EscapeMarkdownLineStarts(text));
            TrimTrailingSpaces();
        }

        private static string EscapeMarkdownLineStarts(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return content;
            }

            var lines = content.Replace("\r\n", "\n").Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                lines[i] = EscapeLineStart(lines[i]);
            }

            return string.Join("\n", lines);
        }

        private static string EscapeLineStart(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return line;
            }

            var index = 0;
            while (index < line.Length && line[index] == ' ' && index < 3)
            {
                index++;
            }

            if (index >= line.Length || line[index] == '\\')
            {
                return line;
            }

            var current = line[index];
            if (IsSetextUnderline(line, index) || current == '#')
            {
                return line.Insert(index, "\\");
            }

            if (current is '-' or '*' or '+' && IsLineMarker(line, index))
            {
                return line.Insert(index, "\\");
            }

            if (char.IsDigit(current))
            {
                var digitEnd = index;
                while (digitEnd < line.Length && char.IsDigit(line[digitEnd]))
                {
                    digitEnd++;
                }

                if (digitEnd < line.Length && line[digitEnd] is '.' or ')' && IsLineMarker(line, digitEnd))
                {
                    return line.Insert(digitEnd, "\\");
                }
            }

            return line;
        }

        private static bool IsLineMarker(string line, int markerIndex)
        {
            var next = markerIndex + 1;
            return next < line.Length && line[next] == ' ';
        }

        private static bool IsSetextUnderline(string line, int index)
        {
            var trimmed = line[index..].TrimEnd();
            if (trimmed.Length < 3 || (trimmed[0] != '-' && trimmed[0] != '='))
            {
                return false;
            }

            return trimmed.All(c => c == trimmed[0]);
        }

        public override void Visit(MdLink node)
        {
            // v5 trims the link text (e.g. a leading tab in the anchor content).
            _inLinkText = true;
            var text = Capture(() => WriteInline(node.Children)).Trim();
            _inLinkText = false;
            Buffer.Append('[').Append(text);
            Buffer.Append("](").Append(PercentEncodeHref(node.Url));
            if (!string.IsNullOrEmpty(node.Title))
            {
                Buffer.Append(" \"").Append(node.Title.Replace("\"", "\\\"")).Append('"');
            }

            Buffer.Append(')');
        }

        private static string PercentEncodeHref(string url) =>
            url.Replace(" ", "%20").Replace("(", "%28").Replace(")", "%29");
    }

    /// <summary>
    /// GitHub Flavored Markdown writer. GFM is CommonMark + extensions, so it inherits the
    /// CommonMark text handling (escaping, soft breaks, line-start escaping) and adds the GFM
    /// extensions (pipe tables, task lists, <c>~~</c> strikethrough, autolinks) from the base.
    /// </summary>
    public sealed class GithubWriter : CommonMarkWriter
    {
        public GithubWriter(Config config) : base(config)
        {
        }
    }

    /// <summary>
    /// Slack mrkdwn writer: single-char emphasis (<c>*</c>/<c>_</c>/<c>~</c>), bullet •,
    /// <c>&lt;url|text&gt;</c> links, bold "headings", and unsupported constructs (tables,
    /// images, rules, superscript) raise as in v5.
    /// </summary>
    public sealed class SlackWriter : MarkdownWriterBase
    {
        public SlackWriter(Config config) : base(config)
        {
        }

        protected override string StrongDelimiter => "*";
        protected override string EmphasisDelimiter => "_";
        protected override string StrikethroughDelimiter => "~";
        protected override string UnorderedBullet => "•";

        // Slack has no nested-emphasis alternation.
        protected override string StrongDelimiterAt(int depth) => "*";
        protected override string EmphasisDelimiterAt(int depth) => "_";

        // Slack mrkdwn does not use backslash escaping.
        protected override void AppendText(string text) => Buffer.Append(text);

        public override void Visit(MdHeading node) => Wrap("*", node.Children); // Slack has no headings

        public override void Visit(MdLink node)
        {
            var text = Capture(() => WriteInline(node.Children)).Trim();
            if (string.IsNullOrEmpty(text) || text == node.Url)
            {
                Buffer.Append('<').Append(node.Url).Append('>');
            }
            else
            {
                Buffer.Append('<').Append(node.Url).Append('|').Append(text).Append('>');
            }
        }

        public override void Visit(MdImage node) => throw new SlackUnsupportedTagException("img");

        public override void Visit(MdTable node) => throw new SlackUnsupportedTagException("table");

        public override void Visit(MdThematicBreak node) => throw new SlackUnsupportedTagException("hr");

        public override void Visit(MdSuperscript node) => throw new SlackUnsupportedTagException("sup");
    }

    /// <summary>
    /// Telegram MarkdownV2 writer: single-char emphasis, escaped special characters in text,
    /// escaped thematic break, and bold "headings".
    /// </summary>
    public sealed class TelegramWriter : MarkdownWriterBase
    {
        public TelegramWriter(Config config) : base(config)
        {
        }

        protected override string StrongDelimiter => "*";
        protected override string EmphasisDelimiter => "_";
        protected override string StrikethroughDelimiter => "~";

        protected override string StrongDelimiterAt(int depth) => "*";
        protected override string EmphasisDelimiterAt(int depth) => "_";

        protected override void AppendText(string text) =>
            Buffer.Append(StringUtils.EscapeTelegramMarkdownV2(text));

        // List markers are MarkdownV2 special characters and must be escaped (\- / 1\.).
        protected override string OrderedListMarker(int number, string delimiter) => $"{number}\\{delimiter}";

        protected override string UnorderedListMarker(string bullet) => "\\" + bullet;

        public override void Visit(MdHeading node) => Wrap("*", node.Children); // Telegram has no headings

        public override void Visit(MdThematicBreak node) => Buffer.Append("\\-\\-\\-");

        public override void Visit(MdLink node)
        {
            Buffer.Append('[');
            WriteInline(node.Children); // text is escaped by AppendText
            Buffer.Append("](").Append(StringUtils.EscapeTelegramMarkdownV2LinkUrl(node.Url)).Append(')');
        }

        // Telegram MarkdownV2 has no image syntax: fall back to a link (alt text or "Image").
        public override void Visit(MdImage node)
        {
            var text = string.IsNullOrEmpty(node.Alt) ? "Image" : node.Alt;
            Buffer.Append('[').Append(StringUtils.EscapeTelegramMarkdownV2(text))
                .Append("](").Append(StringUtils.EscapeTelegramMarkdownV2LinkUrl(node.Url)).Append(')');
        }

        // Telegram has no superscript: fall back to caret notation (x^2, no closing caret).
        public override void Visit(MdSuperscript node)
        {
            Buffer.Append('^');
            WriteInline(node.Children);
        }

        // Telegram has no tables: fall back to a fenced code block showing the rendered table.
        public override void Visit(MdTable node)
        {
            var rendered = Capture(() => base.Visit(node));
            Buffer.Append("```\n").Append(rendered).Append("\n```");
        }
    }

    /// <summary>
    /// MultiMarkdown writer. Renders MMD-native constructs the base degrades — currently
    /// subscript (<c>~x~</c>). Footnotes/metadata/math/citations land with their readers in Phase E.
    /// </summary>
    public sealed class MultiMarkdownWriter : CommonMarkWriter
    {
        public MultiMarkdownWriter(Config config) : base(config)
        {
        }

        // MultiMarkdown supports native definition lists ("Term\n:   Definition").
        public override void Visit(MdDefinitionList node) => WriteColonDefinitionList(node);

        public override string Write(MarkdownDocument document)
        {
            base.Write(document); // leaves the rendered document in Buffer

            // MMD abbreviation definitions are appended at the document end.
            foreach (var abbreviation in document.Meta.Abbreviations)
            {
                Buffer.Append("\n\n*[").Append(abbreviation.Key).Append("]: ").Append(abbreviation.Value);
            }

            return Buffer.ToString();
        }

        public override void Visit(MdSubscript node) => Wrap("~", node.Children);

        // MultiMarkdown attaches list-item continuation blocks at a fixed 4-space tab stop,
        // not at the marker width the way CommonMark does.
        protected override int ContinuationIndent(int markerWidth) => 4;

        // MMD treats -, *, + as one list type, so adjacent lists need an explicit separator.
        protected override string? ListSeparatorComment => "<!-- -->";

        public override void Visit(MdCitation node)
        {
            if (!string.IsNullOrEmpty(node.Key))
            {
                Buffer.Append("[#").Append(node.Key).Append(']');
                return;
            }

            base.Visit(node);
        }

        protected override void WritePreamble(MarkdownDocument document)
        {
            if (document.Meta.Metadata.Count == 0)
            {
                return;
            }

            foreach (var pair in document.Meta.Metadata)
            {
                Buffer.Append(pair.Key).Append(": ").Append(pair.Value).Append('\n');
            }

            Buffer.Append('\n');
        }
    }

    /// <summary>
    /// Pandoc writer. Renders Pandoc-native constructs the base degrades — currently
    /// subscript (<c>~x~</c>). Fenced divs/bracketed spans/heading attrs land in Phase E.
    /// </summary>
    public sealed class PandocWriter : CommonMarkWriter
    {
        public PandocWriter(Config config) : base(config)
        {
        }

        protected override bool EscapeAtSigns => true;

        // Pandoc supports native definition lists ("Term\n:   Definition").
        public override void Visit(MdDefinitionList node) => WriteColonDefinitionList(node);

        public override void Visit(MdSubscript node) => Wrap("~", node.Children);

        // Pandoc treats -, *, + as one list type, so adjacent lists need an explicit separator.
        protected override string? ListSeparatorComment => "<!-- -->";

        // Pandoc folds a tight blockquote/code/heading into the preceding paragraph as a lazy
        // continuation, so a list item's continuation blocks need a blank line before them.
        protected override bool ForceBlankLineBeforeItemBlock => true;

        // Pandoc miscounts the indent of a code block opening on the list-marker line; emit the
        // marker alone and indent the block on the next line.
        protected override bool MarkerOnOwnLineForLeadingBlock => true;

        // Pandoc parses a symmetric "***foo***" as strong-outer, unlike CommonMark (em-outer). Only
        // the sole-child nesting produces that symmetric run, so disambiguate exactly there: an
        // emphasis directly wrapping just a strong (or vice versa) mixes families — asterisk for the
        // emphasis (works intraword, e.g. foo*__bar__*baz) and underscore for the inner strong — so
        // the delimiters never merge. Other arrangements keep the base per-type alternation.
        public override void Visit(MdEmphasis node)
        {
            if (node.Children.Count == 1 && node.Children[0] is MdStrong strong)
            {
                Buffer.Append('*');
                Wrap("__", strong.Children);
                Buffer.Append('*');
                return;
            }

            base.Visit(node);
        }

        public override void Visit(MdStrong node)
        {
            if (node.Children.Count == 1 && node.Children[0] is MdEmphasis emphasis)
            {
                Buffer.Append("**");
                Wrap("_", emphasis.Children);
                Buffer.Append("**");
                return;
            }

            base.Visit(node);
        }

        public override void Visit(MdCitation node)
        {
            if (!string.IsNullOrEmpty(node.Key))
            {
                Buffer.Append("[@").Append(node.Key).Append(']');
                return;
            }

            base.Visit(node);
        }

        public override void Visit(MdMath node)
        {
            var delimiter = node.Display ? "$$" : "$";
            Buffer.Append(delimiter).Append(node.Literal).Append(delimiter);
        }

        public override void Visit(MdCodeBlock node)
        {
            if (node.Literal.IndexOf('\t') >= 0)
            {
                Buffer.Append("<pre><code>")
                    .Append(EscapeRawHtmlText(TrimSingleTrailingNewline(node.Literal)))
                    .Append("</code></pre>");
                return;
            }

            var literal = node.Literal.Length > 0 && node.Literal[0] == '\n'
                ? node.Literal.Substring(1)
                : node.Literal;
            WriteFencedCodeBlock(literal, node.Language, node.LanguageIsAttribute);
        }

        public override void Visit(MdInlineCode node)
        {
            var literal = node.Literal;
            if (literal.Length > 0 && (literal[0] == ' ' || literal[^1] == ' '))
            {
                Buffer.Append("<code>").Append(EscapeRawHtmlText(literal)).Append("</code>");
                return;
            }

            base.Visit(node);
        }

        public override void Visit(MdLink node)
        {
            if (node.Title?.IndexOf('\n') >= 0)
            {
                Buffer.Append("<a href=\"").Append(EscapeRawHtmlAttribute(node.Url)).Append("\" title=\"")
                    .Append(EscapeRawHtmlAttribute(node.Title)).Append("\">")
                    .Append(EscapeRawHtmlText(Capture(() => WriteInline(node.Children))))
                    .Append("</a>");
                return;
            }

            base.Visit(node);
        }

        public override void Visit(MdHeading node)
        {
            base.Visit(node);
            if (node.Attributes is not null)
            {
                Buffer.Append(' ').Append(FormatAttributes(node.Attributes));
            }
        }

        public override void Visit(MdFencedDiv node)
        {
            Buffer.Append("::: ").Append(FormatAttributes(node.Attributes)).Append('\n');
            WriteBlocks(node.Children);
            Buffer.Append("\n:::");
        }

        public override void Visit(MdBracketedSpan node)
        {
            Buffer.Append('[');
            WriteInline(node.Children);
            Buffer.Append(']').Append(FormatAttributes(node.Attributes));
        }

        public override void Visit(MdLineBlock node)
        {
            var inner = Capture(() => WriteInline(node.Children)).Replace("\r\n", "\n");
            var lines = inner.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    Buffer.Append('\n');
                }

                Buffer.Append("| ").Append(lines[i].TrimEnd());
            }
        }

        protected override void WritePreamble(MarkdownDocument document)
        {
            if (document.Meta.Metadata.Count == 0)
            {
                return;
            }

            Buffer.Append("---\n");
            foreach (var pair in document.Meta.Metadata)
            {
                Buffer.Append(pair.Key).Append(": ").Append(pair.Value).Append('\n');
            }

            Buffer.Append("---\n\n");
        }

        private static string EscapeRawHtmlText(string text) => text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

        private static string EscapeRawHtmlAttribute(string text) => EscapeRawHtmlText(text).Replace("\"", "&quot;");

        private void WriteFencedCodeBlock(string literal, string? language, bool languageIsAttribute)
        {
            var fence = new string('`', System.Math.Max(3, LongestBacktickRun(literal) + 1));
            Buffer.Append(fence);
            if (!string.IsNullOrEmpty(language))
            {
                if (languageIsAttribute)
                {
                    Buffer.Append("{language=\"").Append(language.Replace("\"", "\\\"")).Append("\"}");
                }
                else
                {
                    Buffer.Append(language);
                }
            }

            Buffer.Append('\n').Append(literal);
            if (literal.Length == 0 || literal[^1] != '\n')
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

        private static string TrimSingleTrailingNewline(string text)
        {
            if (text.EndsWith("\r\n", StringComparison.Ordinal))
            {
                return text.Substring(0, text.Length - 2);
            }

            return text.EndsWith("\n", StringComparison.Ordinal) ? text.Substring(0, text.Length - 1) : text;
        }
    }

    /// <summary>
    /// Maps a <see cref="Config.MarkdownFlavor"/> to a writer. Flavors without a dedicated
    /// writer yet fall back to <see cref="DefaultWriter"/> (tracked in docs/v6/migration.md).
    /// </summary>
    public static class WriterFactory
    {
        public static IMarkdownWriter Create(Config.MarkdownFlavor flavor, Config config)
        {
            return flavor switch
            {
                Config.MarkdownFlavor.GitHub => new GithubWriter(config),
                Config.MarkdownFlavor.CommonMark => new CommonMarkWriter(config),
                Config.MarkdownFlavor.Slack => new SlackWriter(config),
                Config.MarkdownFlavor.Telegram => new TelegramWriter(config),
                Config.MarkdownFlavor.MultiMarkdown => new MultiMarkdownWriter(config),
                Config.MarkdownFlavor.Pandoc => new PandocWriter(config),
                _ => new DefaultWriter(config),
            };
        }
    }
}
