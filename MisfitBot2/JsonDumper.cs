using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace MisfitBot2
{
    /// <summary>
    /// Static class to make it easy to dump objects into files for debugging.
    /// </summary>
    static public class JsonDumper
    {
        /// <summary>
        /// Serialize an object to Json and write it to a file
        /// </summary>
        /// <param name="obj"></param>
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
        /// <summary>
        /// Writes a string to a file.
        /// </summary>
        /// <param name="text"></param>
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
