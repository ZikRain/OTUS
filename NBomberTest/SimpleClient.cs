using Common;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace NBomberTest;

public class SimpleClient(Socket? socket = null):IDisposable
{
    private readonly Socket _socket = socket ?? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private readonly IPEndPoint _iPEndPoint = new(IPAddress.Loopback, 8080);
    public async Task<bool> ConnectAsync()
    {
        try
        {
            await _socket.ConnectAsync(_iPEndPoint);

            return _socket.Connected;
        }
        catch
        {
            return false;
        }
    }

    public async Task<byte[]?> SetAsync(string key, UserProfile user)
    {
        var value = JsonSerializer.Serialize(user);

        var data = Encoding.UTF8.GetBytes($"SET {key} {value}");

        await _socket.SendAsync(data);

        var buffer = new byte[ServerResponse.MaxBufferSize];

        await _socket.ReceiveAsync(buffer);

        return buffer;

    }

    public async Task<byte[]?> GetAsync(string key)
    {
        var data = Encoding.UTF8.GetBytes($"GET {key}");
        await _socket.SendAsync(data, SocketFlags.None);

        var buffer = new byte[ServerResponse.MaxBufferSize];

        var received = await _socket.ReceiveAsync(buffer, SocketFlags.None);

        return received > 0 ? buffer : null;

    }

    public void Dispose()
    {
        _socket?.Dispose();
        GC.SuppressFinalize(this);
    }
}
