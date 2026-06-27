using Common;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomberTest;
using System.Text;



var scenarioName = "Нагрузка";
Console.WriteLine($"Запуск нагрузочного тестирования \"{scenarioName}\"");

var scenario = Scenario.Create(scenarioName, async (context) =>
{
    using var client = new SimpleClient();
    var connRes = await client.ConnectAsync();

    if (!connRes) return Response.Fail();

    var guid = Guid.NewGuid().ToString();
    var res = await client.SetAsync(guid, guid);

    return GetResponse(res);
});


scenario
    .WithWarmUpDuration(TimeSpan.FromSeconds(10))
    .WithLoadSimulations
    (
        LoadSimulation.NewInject(100, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30))
    );

var stats = NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportFormats(ReportFormat.Html)
    .Run();

var scenarioStats = stats.ScenarioStats.Get(scenarioName);

Console.WriteLine($"Всего: {scenarioStats.AllRequestCount}");
Console.WriteLine($"RPS: {scenarioStats.AllRequestCount/ scenarioStats.Duration.TotalSeconds}");
Console.WriteLine($"Ошибок: {scenarioStats.AllFailCount}");
Console.WriteLine($"Latency p95: {scenarioStats.Ok.Latency.Percent95}");
Console.WriteLine($"Latency p99: {scenarioStats.Ok.Latency.Percent99}");

Console.ReadKey();

Response<object> GetResponse(byte[]? data)
{
    return GetResponseByString(data == null ? string.Empty : Encoding.UTF8.GetString(data).TrimEnd("\0").ToString());
}

Response<object> GetResponseByString(string str)
{
    switch (str)
    {
        case ServerResponse.ResOK:
        {
            return Response.Ok();
        }
        case ServerResponse.ResErr:
        case ServerResponse.ResUnk:
        {
            return Response.Fail();
        }
        default: return Response.Fail();
    }
}