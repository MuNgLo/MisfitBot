using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Data.SQLite;
namespace MisfitBot_MKII.Statics
{
    public static class Core
    {
        public static SQLiteConnection Data;
        public static ConfigurationHandler Configs;
        public static TimerStuff Timers;
        //public static UserManagerService UserMan;
        //public static TreasureService Treasury;
        public static int CurrentTime { private set { } get { return UnixTime(); } }
        public static int LastLaunchTime; // To keep track of how long the bot has been running
        public static int UpTime { private set { } get { return UnixTime() - LastLaunchTime; } }
        public static JuansLog LOGGER;
        public static Func<LogEntry, Task> LOG;

        public static JsonSerializer serializer = new JsonSerializer();

        #region Supporting basic methods
        private static int UnixTime()
        {
            return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        
        public static ulong StringToUlong(string text)
        {
            ulong.TryParse(text, out ulong key);
            return key;
        }
        #endregion
    }

}
