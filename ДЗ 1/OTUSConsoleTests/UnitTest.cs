using OTUSConsole;

namespace OTUSConsoleTests;

public class UnitTest
{
    [Fact]
    public void TestCommandSET()
    {
        var str = "COMMAND KEY VALUE";

        CommandParser.Parse(str, out var c, out var k, out var v);

        Assert.False(c.IsEmpty);
        Assert.Equal("COMMAND",c.ToString());

        Assert.False(k.IsEmpty);
        Assert.Equal("KEY",k.ToString());

        Assert.False(v.IsEmpty);
        Assert.Equal("VALUE",v.ToString());
    }

    [Fact]
    public void TestCommandGET()
    {
        var str = "COMMAND KEY";

        CommandParser.Parse(str, out var c, out var k, out var v);

        Assert.False(c.IsEmpty);
        Assert.Equal("COMMAND", c.ToString());

        Assert.False(k.IsEmpty);
        Assert.Equal("KEY", k.ToString());

        Assert.True(v.IsEmpty);
    }

    [Fact]
    public void TestUncurrentCommand()
    {
        var str = "COMMAND";

        CommandParser.Parse(str, out var c, out var k, out var v);

        Assert.False(c.IsEmpty);
        Assert.Equal("COMMAND", c.ToString());

        Assert.True(k.IsEmpty);

        Assert.True(v.IsEmpty);

        var str2 = "COMMAND KEY VALUE ERROR";
        
        CommandParser.Parse(str2, out var c2, out var k2, out var v2);

        Assert.False(c2.IsEmpty);
        Assert.Equal("COMMAND", c2.ToString());

        Assert.False(k2.IsEmpty);
        Assert.Equal("KEY", k2.ToString());

        Assert.False(v2.IsEmpty);
        Assert.Equal("VALUE", v2.ToString());
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

        foreach(var str in strs)
        {
            CommandParser.Parse(str, out var c, out var k, out var v);

            Assert.False(c.IsEmpty);
            Assert.Equal("COMMAND", c.ToString());

            Assert.False(k.IsEmpty);
            Assert.Equal("KEY", k.ToString());

            Assert.False(v.IsEmpty);
            Assert.Equal("VALUE", v.ToString());
        }
    }
}
