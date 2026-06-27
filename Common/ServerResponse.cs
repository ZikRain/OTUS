namespace Common;

public static class ServerResponse
{

    public const string ResOK = "OK\r\n";
    public const string ResErr = "(nil)\r\n";
    public const string ResUnk = "-ERR Unknown command\r\n";
    public const int MaxBufferSize = 32;
}
