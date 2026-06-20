using Parser;
using System.Buffers;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace TCPServer;

public class TcpServer(Socket? socket = null, SimpleStore? store = null)
{
    private readonly Socket _socket = socket ?? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private readonly SimpleStore _store = store ?? new SimpleStore();

    public async Task StartAsync(int port, int backlog = 100, IPAddress? address = null)
    {
        var ip = address ?? IPAddress.Loopback;
        _socket.Bind(new IPEndPoint(ip, port));
        _socket.Listen(backlog);

        Log($"Начинаем слушать {port}");

        while (true)
        {
            Socket clientSocket;
            try
            {
                clientSocket = await _socket.AcceptAsync();
            }
            catch(Exception ex)
            {
                // при ошибке выхода/закрытия сокета прерываем цикл
                Log($"Error:Server: {ex.Message}");
                break;
            }

            // Для каждого принятого клиента запускаем отдельную задачу на обработку
            _ = Task.Run(() => ProcessClientAsync(clientSocket));
        }
    }

    private async Task ProcessClientAsync(Socket clientSocket)
    {
        var pool = ArrayPool<byte>.Shared;
        var buffer = pool.Rent(4096);

        Log($"Открыл соединение", clientSocket);

        try
        {
            try
            {
                while (true)
                {
                    int received;
                    try
                    {
                        Log($"Ждем данные", clientSocket);
                        received = await clientSocket.ReceiveAsync(buffer.AsMemory(0, buffer.Length), SocketFlags.None);
                    }
                    catch
                    {
                        Log("Ошибка чтения — завершаем обработку клиента", clientSocket);
                        break;
                    }

                    if (received == 0)
                    {
                        Log("Закрыл соединение", clientSocket);
                        break;
                    }


                    try
                    {
                        var data = Encoding.UTF8.GetString(buffer, 0, received).AsSpan();
                        var pars = CommandParser.Parse(data);
                        Log(pars.ToParsedString(), clientSocket);


                        var res = _store.TryApplyCommand(pars);
                        Log(res.mes, clientSocket);
                        await clientSocket.SendAsync(res.val ?? Encoding.UTF8.GetBytes(res.mes));

                    }
                    catch (Exception ex)
                    {
                        Log($"Error:Parse: {ex.Message}", clientSocket);
                    }
                }
            }
            finally
            {
                try 
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();

                } 
                catch (Exception ex)
                {
                    Log($"Error:Close: {ex.Message}",clientSocket);
                }
            }
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    void Log(string str, Socket? socket = null)
    {
        var endPoint = socket == null ? _socket.LocalEndPoint : socket.RemoteEndPoint;
        Console.WriteLine($"[{endPoint}]:: {str}");
    }

}
