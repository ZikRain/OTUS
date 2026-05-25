using System.Text;

namespace OTUSConsole;

public static class CommandParser
{
    public static ParseCommand Parse(ReadOnlySpan<char> str)
    {
        if (str.IsWhiteSpace() || str.IsEmpty)
            return new ParseCommand(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty);

        int start = 0;
        while (start < str.Length && char.IsWhiteSpace(str[start]))
            start++;

        if (start >= str.Length)
            return new ParseCommand(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty);



        int commandEnd = str[start..].IndexOf(' ');
        ReadOnlySpan<char> command;
        int currentPos;

        if (commandEnd == -1)
        {
            command = str[start..];
            return new ParseCommand(command, ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty);
        }
        else
        {
            command = str.Slice(start, commandEnd);
            currentPos = start + commandEnd;
        }

        while (currentPos < str.Length && char.IsWhiteSpace(str[currentPos]))
            currentPos++;

        if (currentPos >= str.Length)
            return new ParseCommand(command, ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty);

        int keyEnd = str[currentPos..].IndexOf(' ');
        ReadOnlySpan<char> key;

        if (keyEnd == -1)
        {
            key = str[currentPos..];
            return new ParseCommand(command, key, ReadOnlySpan<char>.Empty);
        }
        else
        {
            key = str.Slice(currentPos, keyEnd);
            currentPos += keyEnd;
        }

        while (currentPos < str.Length && char.IsWhiteSpace(str[currentPos]))
            currentPos++;

        if (currentPos >= str.Length)
            return new ParseCommand(command, key, ReadOnlySpan<char>.Empty);

        int valueEnd = str[currentPos..].IndexOf(' ');
        ReadOnlySpan<char> value;

        if (valueEnd == -1)
        {
            value = str[currentPos..];
        }
        else
        {
            value = str.Slice(currentPos, valueEnd);
        }

        return new ParseCommand(command, key, value);
    }

    public static byte[] ToUtf8BytesOptimized(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
            return [];

        int byteCount = Encoding.UTF8.GetByteCount(span);
        byte[] bytes = new byte[byteCount];
        Encoding.UTF8.GetBytes(span, bytes);
        return bytes;
    }

}
