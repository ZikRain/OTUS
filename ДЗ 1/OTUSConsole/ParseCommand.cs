namespace OTUSConsole;

public readonly ref struct ParseCommand(ReadOnlySpan<char> command, ReadOnlySpan<char> key, ReadOnlySpan<char> val)
{
    public ReadOnlySpan<char> Command { get; } = command;
    public ReadOnlySpan<char> Key { get; } = key;
    public ReadOnlySpan<char> Value { get; } = val;
}
