using Common;
using Parser;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TCPServer;

public class TcpServer(Socket? socket = null, SimpleStore? store = null) : IDisposable
{
    private readonly Socket _socket = socket ?? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private readonly SimpleStore _store = store ?? new SimpleStore();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(10, 10); // Максимум 10 одновременных подключений

    // Максимальный размер сообщения (4 КБ)
    private const int MaxMessageSize = ServerResponse.MaxBufferSize;

    // Размер буфера для чтения (чуть больше максимального размера для безопасности)
    private const int BufferSize = MaxMessageSize + 1024;

    public async Task StartAsync(int port, int backlog = 100, IPAddress? address = null)
    {
        var ip = address ?? IPAddress.Loopback;
        _socket.Bind(new IPEndPoint(ip, port));
        _socket.Listen(backlog);
        Log($"Начинаем слушать {port}. Максимум подключений: {_semaphore.CurrentCount}");

        while (true)
        {
            Socket clientSocket;
            try
            {
                // Начинаем трассировку принятия подключения
                using var activity = Telemetry.ActivitySource.StartActivity("AcceptConnection", ActivityKind.Server);
                activity?.SetTag("port", port);

                clientSocket = await _socket.AcceptAsync();

                activity?.SetTag("client.endpoint", clientSocket.RemoteEndPoint?.ToString());
                activity?.SetStatus(ActivityStatusCode.Ok);

                // Увеличиваем счетчик подключений
                Telemetry.ConnectionsCounter.Add(1);
            }
            catch (Exception ex)
            {
                // при ошибке выхода/закрытия сокета прерываем цикл
                Log($"Error:Server: {ex.Message}");

                using var activity = Telemetry.ActivitySource.StartActivity("AcceptConnectionError", ActivityKind.Server);
                activity?.SetTag("error", ex.Message);
                activity?.SetStatus(ActivityStatusCode.Error);
                Telemetry.ErrorsCounter.Add(1);
                break;
            }

            // Асинхронно ожидаем освобождения слота для нового подключения
            await _semaphore.WaitAsync();
            Log($"Принято новое подключение. Свободных слотов: {_semaphore.CurrentCount}", clientSocket);

            //Сообщение об успешном коннекте
            await clientSocket.SendAsync(Encoding.UTF8.GetBytes(ServerResponse.ResOK));

            // Для каждого принятого клиента запускаем отдельную задачу на обработку
            _ = Task.Run(() => ProcessClientAsync(clientSocket));
        }
    }

    private async Task ProcessClientAsync(Socket clientSocket)
    {
        var pool = ArrayPool<byte>.Shared;
        var buffer = pool.Rent(BufferSize);
        var clientEndpoint = clientSocket.RemoteEndPoint?.ToString();

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
                    catch (Exception ex)
                    {
                        Log($"Ошибка чтения: {ex.Message} — завершаем обработку клиента", clientSocket);
                        Telemetry.ErrorsCounter.Add(1);
                        break;
                    }

                    if (received == 0)
                    {
                        Log("Закрыл соединение", clientSocket);
                        break;
                    }

                    // Проверка на превышение максимального размера сообщения
                    if (received > MaxMessageSize)
                    {
                        Log($"Превышен максимальный размер сообщения: {received} байт (лимит: {MaxMessageSize} байт). Соединение будет закрыто.", clientSocket);

                        // Отправляем сообщение об ошибке перед закрытием
                        try
                        {
                            var errorMessage = Encoding.UTF8.GetBytes($"Error: Размер сообщения вышел за пределы {MaxMessageSize} байт");
                            await clientSocket.SendAsync(errorMessage, SocketFlags.None);
                        }
                        catch
                        {
                            // Игнорируем ошибки при отправке, так как соединение все равно будет закрыто
                        }

                        Telemetry.ErrorsCounter.Add(1);
                        break; // Выходим из цикла, что приведет к закрытию соединения
                    }

                    try
                    {
                        // Начинаем трассировку обработки команды
                        using var activity = Telemetry.ActivitySource.StartActivity("ProcessCommand", ActivityKind.Internal);
                        activity?.SetTag("client.endpoint", clientEndpoint);
                        activity?.SetTag("message.size", received);

                        var stopwatch = Stopwatch.StartNew();

                        var data = Encoding.UTF8.GetString(buffer, 0, received).AsSpan();
                        var pars = CommandParser.Parse(data);
                        Log(pars.ToParsedString(), clientSocket);

                        activity?.SetTag("command.type", pars.Command.ToString());
                        activity?.SetTag("command.key", pars.Key.ToString());

                        var res = _store.TryApplyCommand(pars);
                        Log(res.mes, clientSocket);

                        // Отправляем ответ клиенту
                        var responseData = ParserHelper.ObjectToByteArray(res.val) ?? Encoding.UTF8.GetBytes(res.mes);
                        await clientSocket.SendAsync(responseData);

                        stopwatch.Stop();
                        var duration = stopwatch.ElapsedMilliseconds;

                        // Записываем метрики
                        Telemetry.CommandsCounter.Add(1);
                        Telemetry.CommandDurationHistogram.Record(duration);

                        activity?.SetTag("command.duration.ms", duration);
                        activity?.SetTag("command.success", res.val != null);
                        activity?.SetStatus(ActivityStatusCode.Ok);
                    }
                    catch (Exception ex)
                    {
                        Log($"Error:Parse: {ex.Message}", clientSocket);

                        using var activity = Telemetry.ActivitySource.StartActivity("CommandError", ActivityKind.Internal);
                        activity?.SetTag("client.endpoint", clientEndpoint);
                        activity?.SetTag("error", ex.Message);
                        activity?.SetStatus(ActivityStatusCode.Error);

                        Telemetry.ErrorsCounter.Add(1);

                        // Отправляем сообщение об ошибке клиенту
                        try
                        {
                            var errorMessage = Encoding.UTF8.GetBytes($"Error: {ex.Message}");
                            await clientSocket.SendAsync(errorMessage, SocketFlags.None);
                        }
                        catch
                        {
                            // Игнорируем ошибки при отправке
                        }
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
                    Log($"Error:Close: {ex.Message}", clientSocket);
                    Telemetry.ErrorsCounter.Add(1);
                }
            }
        }
        finally
        {
            // Освобождаем слот в семафоре после завершения обработки клиента
            _semaphore.Release();
            Log($"Обработка клиента завершена. Свободных слотов: {_semaphore.CurrentCount}", clientSocket);

            pool.Return(buffer);
        }
    }

    void Log(string str, Socket? socket = null)
    {
        var endPoint = socket == null ? _socket.LocalEndPoint : socket.RemoteEndPoint;
        Console.WriteLine($"[{endPoint}]:: {str}");
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
        _socket?.Dispose();
        GC.SuppressFinalize(this);
    }
}