using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using MisfitBot2.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.PubSub;
using System.Data.SQLite;
using MisfitBot2.Extensions.ChannelManager;
namespace MisfitBot2
{
    public static class Core
    {
        public static readonly char _commandCharacter = '?';

        
        public static SQLiteConnection Data;
        public static ConfigurationHandler Configs;
        public static TimerStuff Timers;
        public static UserManagerService UserMan;
        public static TwitchService Twitch;
        public static TreasureService Treasury;
        public static DiscordSocketClient Discord;
        public static ChannelManager Channels;
        public static int CurrentTime { private set { } get { return UnixTime(); } }
        public static int LastLaunchTime; // To keep track of how long the bot has been running
        public static int UpTime { private set { } get { return UnixTime() - LastLaunchTime; } }
        public static JuansLog LOGGER;
        public static Func<LogMessage, Task> LOG;

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
