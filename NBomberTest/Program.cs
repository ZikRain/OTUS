using Common;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomberTest;
using System.Text;
using System.Text.Json;



var scenarioName = "Нагрузка";
Console.WriteLine($"Запуск нагрузочного тестирования \"{scenarioName}\"");

long id = 0;

var scenario = Scenario.Create(scenarioName, async (context) =>
{
    try
    {
        using var client = new SimpleClient();
        var connRes = await client.ConnectAsync();

        if (!connRes) return Response.Fail();

        var guid = Guid.NewGuid().ToString();
        var setUser = new UserProfile() { Id = id++, Created = DateTime.Now, UserName = guid };

        var setRes = await client.SetAsync(guid, setUser);
        var setResponse = GetResponse(setRes);

        if (!IsOk(setResponse))
            return setResponse;

        var getRes = await client.GetAsync(guid);

        var getUser = JsonSerializer.Deserialize<UserProfile>(GetStringByByteArray(getRes));

        if (getUser != null && getUser.Equal(setUser))
            return Response.Ok();

        return Response.Fail();
    }
    catch
    {
        return Response.Fail();
    }
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

bool IsOk(Response<object> response)
{
    return response == Response.Ok();
}

string GetStringByByteArray(byte[]? data)
{
    if (data == null) return string.Empty;

    return Encoding.UTF8.GetString(data).TrimStart("null").TrimEnd("\0").ToString();
}
Response<object> GetResponse(byte[]? data)
{
    return GetResponseByString(data == null ? string.Empty : GetStringByByteArray(data));
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
    }

    var user = JsonSerializer.Deserialize<UserProfile>(str);
    if (user != null)
        return Response.Ok();

    return Response.Fail();
}