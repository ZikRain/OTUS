namespace Parser;

public class SimpleStore
{
    private readonly Dictionary<string, byte[]> _store = [];

    public void Set(string key, byte[] value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        _store[key] = value;
    }

    public byte[]? Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        return _store[key];
    }

    public void Delete(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        _store.Remove(key);
    }

    public (bool, byte[]?) TryApplyCommand(ParsedCommand parsed)
    {

        switch (parsed.Command.ToString().ToLower())
        {
            case "get":
                {
                    var val = Get(parsed.Key.ToString());
                    return (true, val);
                }

            case "set":
                {
                    var val = CommandParser.ToUtf8BytesOptimized(parsed.Value);
                    Set(parsed.Key.ToString(), val);
                    return (true, val);
                }

            case "delete":
                {
                    var k = parsed.Key.ToString();
                    var val = Get(k);
                    Delete(k);
                    return (true, val);
                }
            default: return (false, null);
        }
    }
}
