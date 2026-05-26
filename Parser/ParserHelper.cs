namespace Parser;

public static class ParserHelper
{
    public static string ToParsedString(this ParsedCommand parsed)
    {
        return $"c:{parsed.Command} k:{parsed.Key} v:{parsed.Value}";
    }
}
