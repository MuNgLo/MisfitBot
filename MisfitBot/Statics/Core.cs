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

        public static async Task LogInfo(string msg)
        {
            await LogInfo("Bot", msg);
        }
        internal static async Task LogResponse(string v)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.RESPONSE, "Bot", v));
        }
        public static async Task LogInfo(string sender, string msg)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, sender, msg));
        }

        /// <summary>
        /// Makes sure Core is setup and have what it needs
        /// </summary>
        public static void Init()
        {
            Core.Configs = new ConfigurationHandler();
            Core.LastLaunchTime = Core.CurrentTime;
            Core.Timers = new TimerStuff();
            Core.LOGGER = new JuansLog();
            Core.LOG = Core.LOGGER.LogThis;
            Core.serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
        }

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
