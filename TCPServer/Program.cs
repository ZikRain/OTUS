using Parser;
using TCPServer;

Console.WriteLine("Старт сервера");

var server = new TcpServer();
await server.StartAsync(8080);


Console.ReadLine();
Console.WriteLine("Остановка сервера");