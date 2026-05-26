using System.Text;

namespace Parser;

public static class CommandParser
{
    public static ParsedCommand Parse(ReadOnlySpan<char> str)
    {
        if (str.IsWhiteSpace() || str.IsEmpty)
            return ParsedCommand.GetEmpty();

        int start = 0;
        while (start < str.Length && char.IsWhiteSpace(str[start]))
            start++;

        if (start >= str.Length)
            return ParsedCommand.GetEmpty();



        int commandEnd = str[start..].IndexOf(' ');
        ReadOnlySpan<char> command;
        int currentPos;

        if (commandEnd == -1)
        {
            command = str[start..];
            return new ParsedCommand(command, [], []);
        }
        else
        {
            command = str.Slice(start, commandEnd);
            currentPos = start + commandEnd;
        }

        while (currentPos < str.Length && char.IsWhiteSpace(str[currentPos]))
            currentPos++;

        if (currentPos >= str.Length)
            return new ParsedCommand(command, [], []);

        int keyEnd = str[currentPos..].IndexOf(' ');
        ReadOnlySpan<char> key;

        if (keyEnd == -1)
        {
            key = str[currentPos..];
            return new ParsedCommand(command, key, []);
        }
        else
        {
            key = str.Slice(currentPos, keyEnd);
            currentPos += keyEnd;
        }

        while (currentPos < str.Length && char.IsWhiteSpace(str[currentPos]))
            currentPos++;

        if (currentPos >= str.Length)
            return new ParsedCommand(command, key, []);

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

        return new ParsedCommand(command, key, value);
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
