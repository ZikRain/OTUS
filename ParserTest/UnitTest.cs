using Parser;

namespace ParserTest;

public class UnitTest
{
    [Fact]
    public void TestCommandSET()
    {
        var str = "COMMAND KEY VALUE";

        var res = CommandParser.Parse(str);

        Assert.False(res.Command.IsEmpty);
        Assert.Equal("COMMAND", res.Command.ToString());

        Assert.False(res.Key.IsEmpty);
        Assert.Equal("KEY", res.Key.ToString());

        Assert.False(res.Value.IsEmpty);
        Assert.Equal("VALUE", res.Value.ToString());
    }

    [Fact]
    public void TestCommandGET()
    {
        var str = "COMMAND KEY";

        var res = CommandParser.Parse(str);

        Assert.False(res.Command.IsEmpty);
        Assert.Equal("COMMAND", res.Command.ToString());

        Assert.False(res.Key.IsEmpty);
        Assert.Equal("KEY", res.Key.ToString());

        Assert.True(res.Value.IsEmpty);
    }

    [Fact]
    public void TestUncurrentCommand()
    {
        var str = "COMMAND";

        var res = CommandParser.Parse(str);

        Assert.False(res.Command.IsEmpty);
        Assert.Equal("COMMAND", res.Command.ToString());

        Assert.True(res.Key.IsEmpty);

        Assert.True(res.Value.IsEmpty);

        var str2 = "COMMAND KEY VALUE ERROR";

        var res2 = CommandParser.Parse(str2);

        Assert.False(res2.Command.IsEmpty);
        Assert.Equal("COMMAND", res2.Command.ToString());

        Assert.False(res2.Key.IsEmpty);
        Assert.Equal("KEY", res2.Key.ToString());

        Assert.False(res2.Value.IsEmpty);
        Assert.Equal("VALUE", res2.Value.ToString());
    }

    [Fact]
    public void TestMoreSpaceCommand()
    {
        var strs = new string[] {
        "COMMAND  KEY VALUE",
        "COMMAND KEY  VALUE",
        "COMMAND  KEY  VALUE",
        "COMMAND  KEY  VALUE  ",
        "  COMMAND  KEY  VALUE",
        "  COMMAND  KEY  VALUE  ",
        "     COMMAND     KEY     VALUE     ",
    };

        foreach (var str in strs)
        {
            var res = CommandParser.Parse(str);

            Assert.False(res.Command.IsEmpty);
            Assert.Equal("COMMAND", res.Command.ToString());

            Assert.False(res.Key.IsEmpty);
            Assert.Equal("KEY", res.Key.ToString());

            Assert.False(res.Value.IsEmpty);
            Assert.Equal("VALUE", res.Value.ToString());
        }
    }

    [Fact]
    public async Task TestAsyncLockSimpleStore()
    {
        var tasks = new List<Task>();

        var st = new SimpleStore();

        var strs = new string[] {
        "SET 1 1",
        "GET 1",
        "SET 2 2",
        "GET 2",
        "SET 3 3",
        "GET 3",
        "SET 4 4",
        "GET 4",
        "SET 5 5",
        "GET 5",
        };

        foreach (var str in strs)
        {
            await Task.Run(() =>
            {
                var res = CommandParser.Parse(str);
                st.TryApplyCommand(res);
            });
        }

        Assert.Equal((5, 5, 0), st.GetStatistic());
    }
}