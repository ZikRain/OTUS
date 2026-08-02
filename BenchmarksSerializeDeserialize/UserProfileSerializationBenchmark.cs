using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Common;
using System.Text;
using System.Text.Json;

namespace BenchmarksSerializeDeserialize;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class UserProfileSerializationBenchmark
{
    private UserProfile _userProfile;
    private byte[] _jsonBytes;
    private byte[] _binaryBytes;
    private MemoryStream _memoryStream;

    [GlobalSetup]
    public void Setup()
    {
        // Создаем тестовый объект с реальными данными
        _userProfile = new UserProfile
        {
            Id = 1234567890123456L,
            UserName = "john_doe_123",
            Created = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc)
        };

        // Подготавливаем JSON для десериализации
        var json = JsonSerializer.Serialize(_userProfile);
        _jsonBytes = Encoding.UTF8.GetBytes(json);

        // Подготавливаем бинарные данные для десериализации
        using var ms = new MemoryStream();
        _userProfile.SerializeToBinary(ms);
        _binaryBytes = ms.ToArray();

        _memoryStream = new MemoryStream();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _memoryStream?.Dispose();
    }

    #region Сериализация

    [Benchmark(Baseline = true, Description = "JSON Serialize")]
    public byte[] JsonSerialize()
    {
        return JsonSerializer.SerializeToUtf8Bytes(_userProfile);
    }

    [Benchmark(Description = "Binary Serialize (Generated)")]
    public byte[] BinarySerialize()
    {
        using var ms = new MemoryStream();
        _userProfile.SerializeToBinary(ms);
        return ms.ToArray();
    }

    [Benchmark(Description = "Binary Serialize (ToByteArray)")]
    public byte[] BinarySerializeToByteArray()
    {
        return _userProfile.ToByteArray();
    }

    [Benchmark(Description = "Binary Serialize (Reused Stream)")]
    public byte[] BinarySerializeReusedStream()
    {
        _memoryStream.SetLength(0);
        _memoryStream.Position = 0;
        _userProfile.SerializeToBinary(_memoryStream);
        return _memoryStream.ToArray();
    }

    #endregion

    #region Десериализация

    [Benchmark(Description = "JSON Deserialize")]
    public UserProfile JsonDeserialize()
    {
        return JsonSerializer.Deserialize<UserProfile>(_jsonBytes);
    }

    [Benchmark(Description = "Binary Deserialize (From Stream)")]
    public UserProfile BinaryDeserialize()
    {
        using var ms = new MemoryStream(_binaryBytes);
        return UserProfile.DeserializeFromBinary(ms);
    }

    [Benchmark(Description = "Binary Deserialize (FromByteArray)")]
    public UserProfile BinaryDeserializeFromByteArray()
    {
        return UserProfile.FromByteArray(_binaryBytes);
    }

    #endregion

    #region Комплексное тестирование (сериализация + десериализация)

    [Benchmark(Description = "JSON Roundtrip")]
    public UserProfile JsonRoundtrip()
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(_userProfile);
        return JsonSerializer.Deserialize<UserProfile>(json);
    }

    [Benchmark(Description = "Binary Roundtrip (Stream)")]
    public UserProfile BinaryRoundtrip()
    {
        using var ms = new MemoryStream();
        _userProfile.SerializeToBinary(ms);
        ms.Position = 0;
        return UserProfile.DeserializeFromBinary(ms);
    }

    [Benchmark(Description = "Binary Roundtrip (ByteArray)")]
    public UserProfile BinaryRoundtripByteArray()
    {
        var bytes = _userProfile.ToByteArray();
        return UserProfile.FromByteArray(bytes);
    }

    #endregion

    #region Сравнение размера данных

    [Benchmark(Description = "JSON Size (bytes)")]
    public int JsonSize()
    {
        return JsonSerializer.SerializeToUtf8Bytes(_userProfile).Length;
    }

    [Benchmark(Description = "Binary Size (bytes)")]
    public int BinarySize()
    {
        using var ms = new MemoryStream();
        _userProfile.SerializeToBinary(ms);
        return (int)ms.Length;
    }

    #endregion

    #region Тестирование с разными размерами данных

    [Params(10, 100, 1000)]
    public int NameLength { get; set; }

    private UserProfile GenerateUserWithLongName(int length)
    {
        return new UserProfile
        {
            Id = 1234567890123456L,
            UserName = new string('a', length),
            Created = DateTime.Now
        };
    }

    [Benchmark(Description = "JSON Serialize (Long Name)")]
    public byte[] JsonSerializeLongName()
    {
        var user = GenerateUserWithLongName(NameLength);
        return JsonSerializer.SerializeToUtf8Bytes(user);
    }

    [Benchmark(Description = "Binary Serialize (Long Name)")]
    public byte[] BinarySerializeLongName()
    {
        var user = GenerateUserWithLongName(NameLength);
        using var ms = new MemoryStream();
        user.SerializeToBinary(ms);
        return ms.ToArray();
    }

    [Benchmark(Description = "JSON Size (Long Name)")]
    public int JsonSizeLongName()
    {
        var user = GenerateUserWithLongName(NameLength);
        return JsonSerializer.SerializeToUtf8Bytes(user).Length;
    }

    [Benchmark(Description = "Binary Size (Long Name)")]
    public int BinarySizeLongName()
    {
        var user = GenerateUserWithLongName(NameLength);
        using var ms = new MemoryStream();
        user.SerializeToBinary(ms);
        return (int)ms.Length;
    }

    #endregion

    #region Тестирование с разными количествами объектов в списке

    [Params(10, 100, 1000)]
    public int ObjectCount { get; set; }

    private List<UserProfile> GenerateUserList(int count)
    {
        var list = new List<UserProfile>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(new UserProfile
            {
                Id = 1000000000000000L + i,
                UserName = $"user_{i:D4}",
                Created = DateTime.Now.AddDays(-i)
            });
        }
        return list;
    }

    private byte[] _jsonListBytes;
    private byte[] _binaryListBytes;

    [IterationSetup]
    public void SetupList()
    {
        var list = GenerateUserList(ObjectCount);
        _jsonListBytes = JsonSerializer.SerializeToUtf8Bytes(list);

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(list.Count);
        foreach (var user in list)
        {
            user.SerializeToBinary(ms);
        }
        _binaryListBytes = ms.ToArray();
    }

    [Benchmark(Description = "JSON Serialize List")]
    public byte[] JsonSerializeList()
    {
        var list = GenerateUserList(ObjectCount);
        return JsonSerializer.SerializeToUtf8Bytes(list);
    }

    [Benchmark(Description = "Binary Serialize List")]
    public byte[] BinarySerializeList()
    {
        var list = GenerateUserList(ObjectCount);
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(list.Count);
        foreach (var user in list)
        {
            user.SerializeToBinary(ms);
        }
        return ms.ToArray();
    }

    [Benchmark(Description = "JSON Deserialize List")]
    public List<UserProfile> JsonDeserializeList()
    {
        return JsonSerializer.Deserialize<List<UserProfile>>(_jsonListBytes);
    }

    [Benchmark(Description = "Binary Deserialize List")]
    public List<UserProfile> BinaryDeserializeList()
    {
        using var ms = new MemoryStream(_binaryListBytes);
        using var reader = new BinaryReader(ms);
        var count = reader.ReadInt32();
        var result = new List<UserProfile>(count);

        for (int i = 0; i < count; i++)
        {
            result.Add(UserProfile.DeserializeFromBinary(ms));
        }
        return result;
    }

    #endregion
}