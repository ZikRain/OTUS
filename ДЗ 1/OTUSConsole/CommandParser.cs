using System.Text;

namespace OTUSConsole;

public static class CommandParser
{
    public static void Parse(ReadOnlySpan<char> str, out ReadOnlySpan<char> command, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value)
    {
        command = default;
        key = default;
        value = default;

        var splits = str.Split(' ');

        foreach(var range in splits)
        {
            var valR = str[range.Start.Value..range.End.Value];

            if (valR.IsEmpty || valR.IsWhiteSpace())
                continue;

            if (command.IsEmpty)
                command = valR;
            else if(key.IsEmpty)
                key = valR;
            else if(value.IsEmpty)
                value = valR;
        }

    }

    public static void ParseAndPrint(ReadOnlySpan<char> str, out ReadOnlySpan<char> command, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value)
    {
        Parse(str, out command, out key, out value);

        PrintCommandsTableValue(command, key, value, str);
    }

    public static void PrintCommandsTableHeader()
    {
        Console.WriteLine("┌──────────────────────────────────────────────────┬──────────────────┬──────────────────┬──────────────────┐");
        Console.WriteLine("│ StartSpan                                        │ Command          │ Key              │ Value            │");
        Console.WriteLine("├──────────────────────────────────────────────────┼──────────────────┼──────────────────┼──────────────────┤");
    }

    public static void PrintCommandsTableValue(ReadOnlySpan<char> command, ReadOnlySpan<char> key, ReadOnlySpan<char> value, ReadOnlySpan<char> startStr =default)
    {
        Console.WriteLine($"│ {startStr,-48} │ {command,-16} │ {key,-16} │ {value,-16} │");

    }

    public static void PrintCommandsTableFooter()
    {
        Console.WriteLine("└──────────────────────────────────────────────────┴──────────────────┴──────────────────┴──────────────────┘");
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
