using OTUSConsole;
using System.Diagnostics;

var store = new SimpleStore();

var strs = new string[] { 
    "COMMAND",
    "COMMAND KEY",
    "COMMAND KEY VALUE",
    "COMMAND  KEY VALUE",
    "COMMAND KEY  VALUE",
    "COMMAND  KEY  VALUE",
    "COMMAND  KEY  VALUE  ",
    "COMMAND  KEY  VALUE ERROR",
    "SET user:1 data1",
    "SET user:2  data2",
    "SET user:3   data3",
    "SET user:4    data4",
    "SET user:4     data4",
    "SET user:5      data5",
    " SET user:6      data6",
    "  SET user:7      data7",
    "   SET user:8      data8",
    "    SET user:9      data9",
    "    SET user:9      data10",
    "    SET user:9      data11",
    "    SET user:9      data12",
    "GET user:1" ,
};

foreach (var str in strs)
{
    var res = CommandParser.Parse(str);
    store.TryApplyCommand(res);
}