using System;


namespace ReverseMarkdown.Helpers;

/// <summary>
/// Enumerates the lines in a string as ReadOnlySpan&lt;char&gt;
/// </summary>
internal ref struct LineSplitEnumerator(string text) {
    private int _pos = 0;
    private ReadOnlySpan<char> _current;

    public LineSplitEnumerator GetEnumerator() => this;
    public ReadOnlySpan<char> Current => _current;

    public bool MoveNext()
    {
        var pos = _pos;
        if ((uint) pos >= (uint) text.Length) {
            return false;
        }

        _current = ReadNextLine(pos);
        return true;
    }

    private ReadOnlySpan<char> ReadNextLine(int pos)
    {
        var remaining = text.AsSpan(pos);
        var foundLineLength = remaining.IndexOfAny('\r', '\n');
        if (foundLineLength >= 0) {
            var result = text.AsSpan(pos, foundLineLength);

            var ch = remaining[foundLineLength];
            pos += foundLineLength + 1;
            if (ch == '\r') {
                if ((uint) pos < (uint) text.Length && text[pos] == '\n') {
                    pos++;
                }
            }

            _pos = pos;

            return result;
        }
        else {
            var result = text.AsSpan(pos);
            _pos = text.Length;
            return result;
        }
    }
}
