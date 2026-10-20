using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            var source = new NormalizedBlocks(buffer, lineEnding);
            var block = new char[BlockSize];

            int count;
            while ((count = source.Fill(block)) > 0)
            {
                output.Write(block, 0, count);
            }
        }

        /// <summary>
        /// Asynchronous form of <see cref="WriteTo"/>: one awaited write per block, and no
        /// synchronous writes, so it is safe on writers that disallow them (e.g. an ASP.NET Core
        /// response body).
        /// </summary>
        public static async Task WriteToAsync(
            StringBuilder buffer, TextWriter output, string lineEnding, CancellationToken cancellationToken)
        {
            var source = new NormalizedBlocks(buffer, lineEnding);
            var block = new char[BlockSize];

            int count;
            while ((count = source.Fill(block)) > 0)
            {
                await WriteAsync(output, block, count, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>Writes a string asynchronously, honouring cancellation on every target.</summary>
        public static Task WriteAsync(TextWriter output, string value, CancellationToken cancellationToken)
        {
#if NET8_0_OR_GREATER
            return output.WriteAsync(value.AsMemory(), cancellationToken);
#else
            cancellationToken.ThrowIfCancellationRequested();
            return output.WriteAsync(value);
#endif
        }

        private static Task WriteAsync(TextWriter output, char[] block, int count, CancellationToken cancellationToken)
        {
#if NET8_0_OR_GREATER
            return output.WriteAsync(block.AsMemory(0, count), cancellationToken);
#else
            // TextWriter.WriteAsync takes no token before .NET Core 3.0; check between blocks.
            cancellationToken.ThrowIfCancellationRequested();
            return output.WriteAsync(block, 0, count);
#endif
        }

        private static bool IsAnySeparator(char c) => c == '\r' || IsLineSeparator(c);

        private static bool IsLineSeparator(char c) =>
            c is '\n' or '\f' or '\u0085' or '\u2028' or '\u2029';

        /// <summary>
        /// Reads the trimmed buffer as blocks of normalized output. The state survives between
        /// <see cref="Fill"/> calls, so a CRLF split across source blocks and a line ending split
        /// across output blocks both come out whole.
        /// </summary>
        private sealed class NormalizedBlocks
        {
            private readonly StringBuilder _buffer;
            private readonly string _lineEnding;
            private readonly char[] _source = new char[BlockSize];
            private int _position;
            private readonly int _end;
            private int _sourceIndex;
            private int _sourceCount;
            private bool _pendingCarriageReturn;

            // Index into _lineEnding of the next character to emit, or -1 when none is pending.
            private int _lineEndingIndex = -1;

            public NormalizedBlocks(StringBuilder buffer, string lineEnding)
            {
                _buffer = buffer;
                _lineEnding = lineEnding;
                (_position, _end) = TrimBounds(buffer);
            }

            /// <summary>Fills <paramref name="block"/> and returns the count; 0 when done.</summary>
            public int Fill(char[] block)
            {
                var filled = 0;
                while (filled < block.Length)
                {
                    if (_lineEndingIndex >= 0)
                    {
                        var copy = Math.Min(_lineEnding.Length - _lineEndingIndex, block.Length - filled);
                        _lineEnding.CopyTo(_lineEndingIndex, block, filled, copy);
                        filled += copy;
                        _lineEndingIndex += copy;
                        if (_lineEndingIndex == _lineEnding.Length)
                        {
                            _lineEndingIndex = -1;
                        }

                        continue;
                    }

                    if (_sourceIndex == _sourceCount)
                    {
                        if (_position >= _end)
                        {
                            break;
                        }

                        _sourceCount = Math.Min(BlockSize, _end - _position);
                        _buffer.CopyTo(_position, _source, 0, _sourceCount);
                        _position += _sourceCount;
                        _sourceIndex = 0;
                    }

                    var c = _source[_sourceIndex];

                    // A CR at a block boundary may be followed by an LF in the next block; the pair
                    // is one separator, so the decision is deferred until the next character.
                    if (_pendingCarriageReturn)
                    {
                        _pendingCarriageReturn = false;
                        if (c == '\n')
                        {
                            _sourceIndex++;
                            continue;
                        }
                    }

                    if (c == '\r')
                    {
                        _sourceIndex++;
                        _pendingCarriageReturn = true;
                        _lineEndingIndex = 0;
                        continue;
                    }

                    if (IsLineSeparator(c))
                    {
                        _sourceIndex++;
                        _lineEndingIndex = 0;
                        continue;
                    }

                    // Copy the longest run of ordinary characters that fits in one go.
                    // Locals rather than fields keep this per-character loop tight.
                    var source = _source;
                    var runStart = _sourceIndex;
                    var index = runStart + 1;
                    var limit = Math.Min(_sourceCount, runStart + block.Length - filled);
                    while (index < limit && !IsAnySeparator(source[index]))
                    {
                        index++;
                    }

                    var length = index - runStart;
                    Array.Copy(source, runStart, block, filled, length);
                    filled += length;
                    _sourceIndex = index;
                }

                return filled;
            }
        }

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
