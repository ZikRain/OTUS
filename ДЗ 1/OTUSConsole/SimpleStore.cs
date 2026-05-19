using System.Text;

namespace OTUSConsole;
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
        _store.Remove(key);
    }

    public (bool,byte[]?) TryApplyCommand(ReadOnlySpan<char> command, ReadOnlySpan<char> key, ReadOnlySpan<char> value)
    {

        switch (command.ToString().ToLower())
        {
            case "get":
                {
                    var val = Get(key.ToString());
                    return (true, val);
                }
            
            case "set":
                {
                    var val = CommandParser.ToUtf8BytesOptimized(value);
                    Set(key.ToString(), val);
                    return (true, val);
                }

            case "delete":
                {
                    var k = key.ToString();
                    var val = Get(k);
                    Delete(k);
                    return (true, val);
                }
            default: return (false, null);
        }
    }

    public void PrintDictionary()
    {
        if (_store == null ||  _store.Count == 0)
        {
            Console.WriteLine("┌──────────────────────────────────────────────────┐");
            Console.WriteLine("│ Simple Store is Empty                            │");
            Console.WriteLine("└──────────────────────────────────────────────────┘");
            return;
        }

        Console.WriteLine("┌──────────────────────────────────────────────────┐");
        Console.WriteLine("│ Simple Store                                     │");
        Console.WriteLine("├─────────────────────────┬────────────────────────┤");

        foreach (var kvp in _store)
        {
            var str = Encoding.UTF8.GetString(kvp.Value);

            Console.WriteLine($"│ {kvp.Key,-23} │ {str,-22} │");
        }

        Console.WriteLine( "└─────────────────────────┴────────────────────────┘");

    }

}
