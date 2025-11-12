using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace MisfitBot_MKII.Statics
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
        static public void DumpObjectToJson(Object obj, string prefix=null)
        {
            if (!System.IO.Directory.Exists("DebugDumps"))
            {
                System.IO.Directory.CreateDirectory("DebugDumps");
            }
            int time = Core.CurrentTime;
            int index = 1;
            string fileName = FormatFileName(index, time, prefix);
            while (File.Exists(fileName))
            {
                index++;
                fileName = FormatFileName(index, time, prefix);
            }
            string data = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(fileName, data);

        }

        static private string FormatFileName(int index, int time, string prefix=null)
        {
            string fileName = $"DebugDumps/DataDump-{index}--{time}.json";
            if (prefix != null)
            {
                fileName = $"DebugDumps/{prefix}-{index}.json";
            }
            return fileName;
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
