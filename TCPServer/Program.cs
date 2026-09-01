using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using TCPServer;


Console.WriteLine("Старт сервера");

// Настройка OpenTelemetry TracerProvider
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("TCPServer")
    .AddConsoleExporter()
    .Build();

// Настройка OpenTelemetry MeterProvider
using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("TCPServer.Metrics")
    .AddConsoleExporter()
    .Build();


//Запуск Сервера
using (var server = new TcpServer())
{
    await server.StartAsync(8080);
    Console.ReadLine();
}


Console.WriteLine("Остановка сервера");
Console.ReadLine();
