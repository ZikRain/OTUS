namespace Common;

public static class ServerResponse
{

    public const string ResOK = "OK\r\n";
    public const string ResErr = "(nil)\r\n";
    public const string ResUnk = "-ERR Unknown command\r\n";
    // Максимальный размер сообщения (4 КБ)
    public const int MaxBufferSize = 4096;
}
