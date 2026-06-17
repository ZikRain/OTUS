namespace Parser;

public class SimpleStore : IDisposable
{
    private long _getCount;
    private long _setCount;
    private long _delCount;

    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    private readonly Dictionary<string, byte[]> _store = [];

    public void Set(string key, byte[] value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            _lock.EnterWriteLock();

            _store[key] = value;

            Interlocked.Increment(ref _setCount);
        }
        finally 
        {
            _lock?.ExitWriteLock();
        }

    }

    public byte[]? Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        try
        {
            _lock.EnterReadLock();

            if (!_store.ContainsKey(key))
                return null;

            Interlocked.Increment(ref _getCount);
            return _store[key];
        }
        finally
        {
            _lock?.ExitReadLock();
        }
    }

    public void Delete(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            _lock.EnterWriteLock();
            
            if (_store.Remove(key))
                Interlocked.Increment(ref _delCount);
        }
        finally
        {
            _lock?.ExitWriteLock();
        }
    }

    public (long getCount, long setCount, long delCount) GetStatistic() =>  (_getCount, _setCount, _delCount);

    public (bool res, byte[]? val,string mes) TryApplyCommand(ParsedCommand parsed)
    {

        switch (parsed.Command.ToString().ToLower())
        {
            case "get":
                {
                    var val = Get(parsed.Key.ToString());
                    return (true, val, val is null ? "(nil)\r\n" : "OK\r\n");
                }

            case "set":
                {
                    var val = CommandParser.ToUtf8BytesOptimized(parsed.Value);
                    Set(parsed.Key.ToString(), val);
                    return (true, null, "OK\r\n");
                }

            case "delete":
                {
                    var k = parsed.Key.ToString();
                    Delete(k);
                    return (true, null, "OK\r\n");
                }
            default: return (false, null, "-ERR Unknown command\r\n");
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
