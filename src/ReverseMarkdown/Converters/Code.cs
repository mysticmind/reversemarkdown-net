using System;
using System.IO;
using HtmlAgilityPack;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown.Converters {
    public class Code : ConverterBase {
        public Code(Converter converter) : base(converter)
        {
            Converter.Register("code", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (Converter.Config.TelegramMarkdownV2) {
                writer.Write('`');
                writer.Write(StringUtils.EscapeTelegramMarkdownV2Code(DecodeHtml(node.InnerText)));
                writer.Write('`');
                return;
            }

            if (Converter.Config.CommonMark) {
                var content = node.InnerHtml;
                var fence = CreateCommonMarkCodeFence(content);
                writer.Write(fence);
                var needsBacktickPadding = content.Length > 0 && (content[0] == '`' || content[^1] == '`');
                var needsWhitespacePadding = content.Length > 0 &&
                                            (char.IsWhiteSpace(content[0]) || char.IsWhiteSpace(content[^1]));
                var needsPadding = needsBacktickPadding || needsWhitespacePadding;

                if (needsPadding) {
                    writer.Write(' ');
                }

                DecodeHtml(writer, content);

                if (needsPadding) {
                    writer.Write(' ');
                }

                writer.Write(fence);
                return;
            }

            // Depending on the content "surrounding" the <code> element,
            // leading/trailing whitespace is significant. For example, the
            // following HTML renders as expected in a browser (meaning there is
            // proper spacing between words):
            //
            //   <p>The JavaScript<code> function </code>keyword...</p>
            //
            // However, if we simply trim the contents of the <code> element,
            // then the Markdown becomes:
            //
            //   The JavaScript`function`keyword...
            //
            // To avoid this scenario, do *not* trim the inner text of the
            // <code> element.
            //
            // For the HTML example above, the Markdown will be:
            //
            //   The JavaScript` function `keyword...
            //
            // While it might seem preferable to try to "fix" this by trimming
            // the <code> element and insert leading/trailing spaces as
            // necessary, things become complicated rather quickly depending
            // on the particular content. For example, what would be the
            // "correct" conversion of the following HTML?
            //
            //   <p>The JavaScript<b><code> function </code></b>keyword...</p>
            //
            // The simplest conversion to Markdown:
            //
            //   The JavaScript**` function `**keyword...
            //
            // Some other, arguably "better", alternatives (that would require
            // substantially more conversion logic):
            //
            //   The JavaScript** `function` **keyword...
            //
            //   The JavaScript **`function`** keyword...

            writer.Write('`');
            DecodeHtml(writer, node.InnerText);
            writer.Write('`');
        }

        private static string CreateCommonMarkCodeFence(string content)
        {
            var maxRun = 0;
            var currentRun = 0;
            foreach (var c in content) {
                if (c == '`') {
                    currentRun++;
                    if (currentRun > maxRun) {
                        maxRun = currentRun;
                    }
                }
                else {
                    currentRun = 0;
                }
            }

            var fenceLength = Math.Max(1, maxRun + 1);
            return new string('`', fenceLength);
        }
    }
}
