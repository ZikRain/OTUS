using Common;
using System.Net.Sockets;

namespace Parser;

public class SimpleStore : IDisposable
{
    #region Statistic
    private long _getCount;
    private long _setCount;
    private long _delCount;
    public (long getCount, long setCount, long delCount) GetStatistic() => (_getCount, _setCount, _delCount);
    #endregion

    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<string, byte[]> _store = [];

    #region Основные методы (byte[])

    public void Set(string key, byte[] data)
    {
        if (string.IsNullOrWhiteSpace(key) || data == null)
            return;

        try
        {
            _lock.EnterWriteLock();
            _store[key] = data;
            Interlocked.Increment(ref _setCount);
        }
        finally
        {
            _lock?.ExitWriteLock();
        }
    }

    public byte[]? GetBytes(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        try
        {
            _lock.EnterReadLock();

            if (!_store.TryGetValue(key, out var value))
                return null;

            Interlocked.Increment(ref _getCount);
            return value;
        }
        finally
        {
            _lock?.ExitReadLock();
        }
    }

    #endregion

    #region Методы для работы с объектами UserProfile

    public void Set(string key, UserProfile value)
    {
        if (string.IsNullOrWhiteSpace(key) || value == null)
            return;

        try
        {
            _lock.EnterWriteLock();

            // Используем сгенерированный метод ToByteArray
            _store[key] = value.ToByteArray();

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

            // Используем сгенерированный метод FromByteArray
            return UserProfile.FromByteArray(value);
        }
        finally
        {
            _lock?.ExitReadLock();
        }
    }

    #endregion

    #region Методы для работы через Stream

    /// <summary>
    /// Сохраняет данные из Stream в хранилище
    /// </summary>
    public void SetFromStream(string key, Stream stream)
    {
        if (string.IsNullOrWhiteSpace(key) || stream == null)
            return;

        try
        {
            _lock.EnterWriteLock();

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            _store[key] = ms.ToArray();

            Interlocked.Increment(ref _setCount);
        }
        finally
        {
            _lock?.ExitWriteLock();
        }
    }

    /// <summary>
    /// Сохраняет UserProfile из Stream в хранилище (десериализует из бинарного формата)
    /// </summary>
    public void SetUserFromStream(string key, Stream stream)
    {
        if (string.IsNullOrWhiteSpace(key) || stream == null)
            return;

        try
        {
            _lock.EnterWriteLock();

            // Десериализуем UserProfile из Stream
            var user = UserProfile.DeserializeFromBinary(stream);

            // Сериализуем в byte[] для хранения
            _store[key] = user.ToByteArray();

            Interlocked.Increment(ref _setCount);
        }
        finally
        {
            _lock?.ExitWriteLock();
        }
    }

    /// <summary>
    /// Асинхронно сохраняет UserProfile из Stream в хранилище
    /// </summary>
    public async Task SetUserFromStreamAsync(string key, Stream stream, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || stream == null)
            return;

        try
        {
            _lock.EnterWriteLock();

            // Асинхронно десериализуем UserProfile из Stream
            var user = await UserProfile.DeserializeFromBinaryAsync(stream, cancellationToken);

            // Сериализуем в byte[] для хранения
            _store[key] = user.ToByteArray();

            Interlocked.Increment(ref _setCount);
        }
        finally
        {
            _lock?.ExitWriteLock();
        }
    }

    /// <summary>
    /// Получает данные и записывает их в Stream
    /// </summary>
    public bool GetToStream(string key, Stream stream)
    {
        if (string.IsNullOrWhiteSpace(key) || stream == null)
            return false;

        try
        {
            _lock.EnterReadLock();

            if (!_store.TryGetValue(key, out var value))
                return false;

            Interlocked.Increment(ref _getCount);

            // Записываем данные в Stream
            stream.Write(value, 0, value.Length);
            stream.Flush();

            return true;
        }
        finally
        {
            _lock?.ExitReadLock();
        }
    }

    /// <summary>
    /// Получает UserProfile и записывает его в Stream (сериализует в бинарный формат)
    /// </summary>
    public bool GetUserToStream(string key, Stream stream)
    {
        if (string.IsNullOrWhiteSpace(key) || stream == null)
            return false;

        try
        {
            _lock.EnterReadLock();

            if (!_store.TryGetValue(key, out var value))
                return false;

            Interlocked.Increment(ref _getCount);

            // Десериализуем из byte[] в UserProfile
            var user = UserProfile.FromByteArray(value);

            // Сериализуем UserProfile в Stream
            user.SerializeToBinary(stream);
            stream.Flush();

            return true;
        }
        finally
        {
            _lock?.ExitReadLock();
        }
    }

    /// <summary>
    /// Асинхронно получает UserProfile и записывает его в Stream
    /// </summary>
    public async Task<bool> GetUserToStreamAsync(string key, Stream stream, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || stream == null)
            return false;

        try
        {
            _lock.EnterReadLock();

            if (!_store.TryGetValue(key, out var value))
                return false;

            Interlocked.Increment(ref _getCount);

            // Десериализуем из byte[] в UserProfile
            var user = UserProfile.FromByteArray(value);

            // Асинхронно сериализуем UserProfile в Stream
            await user.SerializeToBinaryAsync(stream, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            return true;
        }
        finally
        {
            _lock?.ExitReadLock();
        }
    }

    #endregion

    #region Методы для работы с NetworkStream

    /// <summary>
    /// Сохраняет UserProfile из NetworkStream (с префиксом длины)
    /// </summary>
    public async Task<bool> SetUserFromNetworkAsync(string key, NetworkStream networkStream, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || networkStream == null)
            return false;

        try
        {
            // Читаем длину сообщения (4 байта)
            var lengthBytes = new byte[4];
            int bytesRead = 0;
            while (bytesRead < 4)
            {
                bytesRead += await networkStream.ReadAsync(lengthBytes, bytesRead, 4 - bytesRead, cancellationToken);
            }
            var length = BitConverter.ToInt32(lengthBytes, 0);

            if (length <= 0 || length > 10 * 1024 * 1024) // Максимум 10MB
                return false;

            // Читаем данные
            var data = new byte[length];
            bytesRead = 0;
            while (bytesRead < length)
            {
                bytesRead += await networkStream.ReadAsync(data, bytesRead, length - bytesRead, cancellationToken);
            }

            // Десериализуем UserProfile
            using var ms = new MemoryStream(data);
            var user = UserProfile.DeserializeFromBinary(ms);

            // Сохраняем
            Set(key, user);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Отправляет UserProfile в NetworkStream (с префиксом длины)
    /// </summary>
    public async Task<bool> GetUserToNetworkAsync(string key, NetworkStream networkStream, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || networkStream == null)
            return false;

        try
        {
            var user = Get(key);
            if (user == null)
                return false;

            // Сериализуем в MemoryStream
            using var ms = new MemoryStream();
            await user.SerializeToBinaryAsync(ms, cancellationToken);
            var data = ms.ToArray();

            // Отправляем длину
            var lengthBytes = BitConverter.GetBytes(data.Length);
            await networkStream.WriteAsync(lengthBytes, 0, 4, cancellationToken);

            // Отправляем данные
            await networkStream.WriteAsync(data, 0, data.Length, cancellationToken);
            await networkStream.FlushAsync(cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Команды

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


    public (bool res, UserProfile? val, string mes) TryApplyCommand(ParsedCommand parsed)
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

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _lock?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}