namespace Parser;

public readonly ref struct ParsedCommand(ReadOnlySpan<char> command, ReadOnlySpan<char> key, ReadOnlySpan<char> val)
{
    public ReadOnlySpan<char> Command { get; } = command;
    public ReadOnlySpan<char> Key { get; } = key;
    public ReadOnlySpan<char> Value { get; } = val;

    public static ParsedCommand GetEmpty()
    {
        return new ParsedCommand([], [], []);
    }
}