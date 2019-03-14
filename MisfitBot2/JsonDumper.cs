using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace MisfitBot2
{
    static public class JsonDumper
    {
        static public void DumpObjectToJson(Object obj)
        {
            if (!System.IO.Directory.Exists("DebugDumps"))
            {
                System.IO.Directory.CreateDirectory("DebugDumps");
            }
            int time = Core.CurrentTime;
            int index = 1;
            string fileName = $"DebugDumps/DataDump-{index}--{time}.json";
            string data = JsonConvert.SerializeObject(obj, Formatting.Indented);
            while (File.Exists(fileName)) { index++; }
            File.WriteAllText(fileName, data);

        }
        static public void DumpStringToFile(string text)
        {
            if (!System.IO.Directory.Exists("DebugDumps"))
            {
                System.IO.Directory.CreateDirectory("DebugDumps");
            }
            int time = Core.CurrentTime;
            int index = 1;
            string fileName = $"DebugDumps/DataDump-{index}--{time}.txt";
            while (File.Exists(fileName)) { index++; }
            File.WriteAllText(fileName, text);
        }
    }
}
