using System.Text.Json;

namespace Parser;

public static class ParserHelper
{
    public static string ToParsedString(this ParsedCommand parsed)
    {
        return $"c:{parsed.Command} k:{parsed.Key} v:{parsed.Value}";
    }

    public static byte[]? ObjectToByteArray<T>(T obj)
    {
        if (obj == null) return null;

        return JsonSerializer.SerializeToUtf8Bytes(obj);
    }

    public static T? ByteArrayToObject<T>(byte[] bytes)
    {
        return JsonSerializer.Deserialize<T>(bytes);
    }
}
