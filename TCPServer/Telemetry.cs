using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TCPServer;

public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new(nameof(TcpServer), "1.0.0");
    public static readonly Meter Meter = new($"{nameof(TcpServer)}.Metrics", "1.0.0");

    // Счетчики метрик
    public static readonly Counter<int> ConnectionsCounter = Meter.CreateCounter<int>(
        "tcp.connections.total",
        description: "Общее кол-во подключений"
    );

    public static readonly Counter<int> CommandsCounter = Meter.CreateCounter<int>(
        "tcp.commands.total",
        description: "Общее кол-во команд в работе"
    );

    public static readonly Histogram<double> CommandDurationHistogram = Meter.CreateHistogram<double>(
        "tcp.command.duration",
        unit: "ms",
        description: "Длительность выполнения команд"
    );

    public static readonly Counter<int> ErrorsCounter = Meter.CreateCounter<int>(
        "tcp.errors.total",
        description: "Общее кол-во ошибок"
    );
}
