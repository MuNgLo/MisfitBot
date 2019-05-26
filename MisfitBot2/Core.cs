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

        public static BitEvent OnBitEvent;
        public static BanEvent OnBanEvent;
        public static HostEvent OnHostEvent;
        public static UnBanEvent OnUnBanEvent;
        public static NewDiscordMember OnNewDiscordMember;
        public static BotChannelGoesLive OnBotChannelGoesLive;
        public static BotChannelGoesOffline OnBotChannelGoesOffline;
        public static UserEntryMerge OnUserEntryMerge;
        public static ViewerCountEvent OnViewercount;


        public static SQLiteConnection Data;

        public static ConfigurationHandler Configs;
        public static TimerStuff Timers;
        public static UserManagerService UserMan;
        public static TwitchService Twitch;
        public static TreasureService Treasury;
        //public static DiscordNET Discord;
        public static DiscordSocketClient Discord;
        public static ChannelManager Channels;
        public static int CurrentTime { private set { } get { return UnixTime(); } }
        public static int LastLaunchTime; // To keep track of how long the bot has been running
        public static int UpTime { private set { } get { return UnixTime() - LastLaunchTime; } }
        public static JuansLog LOGGER;
        public static Func<LogMessage, Task> LOG;
        public static JsonSerializer serializer = new JsonSerializer();
        // TODO Incorperate this into Admin module
        public static List<String> IgnoredTwitchUsers = new List<string> {
            "juanthebot",
            "nightbot",
            "fatmanleg",
            "electricallongboard",
            "mr__glitch",
            "electricalskateboard",
            "boogey2003"
            };

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
        #region Botwide Events gets raised here
        public static void RaiseUserLinkEvent(UserEntry discordProfile, UserEntry twitchProfile)
        {
            OnUserEntryMerge?.Invoke(discordProfile, twitchProfile);
        }
        public static void RaiseBitEvent(BitEventArguments e)
        {
            OnBitEvent?.Invoke(e);
        }
        public static void RaiseBanEvent(BanEventArguments e)
        {
            OnBanEvent?.Invoke(e);
        }
        public static void RaiseHostEvent(BotChannel bChan, HostEventArguments e)
        {
            OnHostEvent?.Invoke(bChan, e);
        }
        public static void RaiseUnBanEvent(UnBanEventArguments e)
        {
            OnUnBanEvent?.Invoke(e);
        }
        public static void RaiseOnNewDiscordMember(BotChannel bChan, UserEntry user)
        {
            if (bChan == null) { return; }
            if (user == null) { return; }
            OnNewDiscordMember?.Invoke(bChan, user);
        }
        public static void RaiseOnBotChannelGoesLive(BotChannel bChan, int delay)
        {
            if (bChan == null) { return; }
            bChan.isLive = true;
            OnBotChannelGoesLive?.Invoke(bChan, delay);
        }
        public static void RaiseOnBotChannelGoesOffline(BotChannel bChan)
        {
            if (bChan == null) { return; }
            bChan.isLive = false;
            OnBotChannelGoesOffline?.Invoke(bChan);
        }
        public static void RaiseOnViewerCount(BotChannel bChan, int oldCount, int newCount)
        {
            OnViewercount?.Invoke(bChan, oldCount, newCount);
        }
        #endregion
    }

}
