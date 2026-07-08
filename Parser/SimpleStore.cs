using Common;

namespace Parser;

public class SimpleStore : IDisposable
{
    private long _getCount;
    private long _setCount;
    private long _delCount;

    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<string, byte[]> _store = [];

    public void Set(string key, UserProfile value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            _lock.EnterWriteLock();

            _store[key] = ParserHelper.ObjectToByteArray(value);

            Interlocked.Increment(ref _setCount);
        }
        finally 
        {
            _lock?.ExitWriteLock();
        }

    }

    public UserProfile? Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        try
        {
            _lock.EnterReadLock();

            if (!_store.TryGetValue(key, out var value))
                return null;

            Interlocked.Increment(ref _getCount);
            return ParserHelper.ByteArrayToObject<UserProfile>(value);
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

    public (bool res, UserProfile? val,string mes) TryApplyCommand(ParsedCommand parsed)
    {

        switch (parsed.Command.ToString().ToLower())
        {
            case "get":
                {
                    var val = Get(parsed.Key.ToString());
                    return (true, val, val is null ? ServerResponse.ResErr : ServerResponse.ResOK);
                }

            case "set":
                {
                    var val = CommandParser.ToUtf8BytesOptimized(parsed.Value);
                    Set(parsed.Key.ToString(), ParserHelper.ByteArrayToObject<UserProfile>(val));
                    return (true, null, ServerResponse.ResOK);
                }

            case "delete":
                {
                    var k = parsed.Key.ToString();
                    Delete(k);
                    return (true, null, ServerResponse.ResOK);
                }
            default: return (false, null, ServerResponse.ResUnk);
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
