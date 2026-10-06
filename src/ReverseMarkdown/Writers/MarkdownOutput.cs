using System.IO;
using System.Text;

namespace ReverseMarkdown.Writers
{
    /// <summary>
    /// Streams a rendered buffer to a <see cref="TextWriter"/>, applying the same trimming and
    /// line-ending translation the string-returning path applies - without materializing the output
    /// as a string first.
    /// </summary>
    internal static class MarkdownOutput
    {
        private const int BlockSize = 8192;

        /// <summary>
        /// Writes <paramref name="buffer"/> to <paramref name="output"/>, trimming leading and
        /// trailing newline characters and normalizing every line ending to
        /// <paramref name="lineEnding"/>.
        /// </summary>
        /// <remarks>
        /// The recognized separators match <c>string.ReplaceLineEndings</c>: CRLF, CR, LF, FF, NEL
        /// (U+0085), LS (U+2028) and PS (U+2029). <c>TextWriterOutputTests</c> asserts this produces
        /// byte-identical output to the string path.
        /// </remarks>
        public static void WriteTo(StringBuilder buffer, TextWriter output, string lineEnding)
        {
            var (start, end) = TrimBounds(buffer);
            if (start >= end)
            {
                return;
            }

            var block = new char[BlockSize];
            var pendingCarriageReturn = false;

            for (var offset = start; offset < end; offset += BlockSize)
            {
                var count = System.Math.Min(BlockSize, end - offset);
                buffer.CopyTo(offset, block, 0, count);

                for (var i = 0; i < count; i++)
                {
                    var c = block[i];

                    // A CR at a block boundary may be followed by an LF in the next block; the pair
                    // is one separator, so the decision is deferred until the next character.
                    if (pendingCarriageReturn)
                    {
                        pendingCarriageReturn = false;
                        if (c == '\n')
                        {
                            continue;
                        }
                    }

                    if (c == '\r')
                    {
                        output.Write(lineEnding);
                        pendingCarriageReturn = true;
                        continue;
                    }

                    if (IsLineSeparator(c))
                    {
                        output.Write(lineEnding);
                        continue;
                    }

                    // Emit the longest run of ordinary characters in one call rather than per char.
                    var runStart = i;
                    while (i < count && !IsAnySeparator(block[i]))
                    {
                        i++;
                    }

                    output.Write(block, runStart, i - runStart);
                    i--;
                }
            }
        }

        private static bool IsAnySeparator(char c) => c == '\r' || IsLineSeparator(c);

        private static bool IsLineSeparator(char c) =>
            c is '\n' or '\f' or '\u0085' or '\u2028' or '\u2029';

        // Mirrors Trim('\n', '\r') on the rendered string.
        private static (int Start, int End) TrimBounds(StringBuilder buffer)
        {
            var start = 0;
            var end = buffer.Length;
            while (start < end && (buffer[start] == '\n' || buffer[start] == '\r'))
            {
                start++;
            }

            while (end > start && (buffer[end - 1] == '\n' || buffer[end - 1] == '\r'))
            {
                end--;
            }

            return (start, end);
        }
    }
}
