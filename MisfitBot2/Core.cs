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
        public static TwitchSubscriptionEvent OnTwitchSubEvent;
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
        internal static async Task RaiseDiscordUserUpdated(SocketGuildUser arg1, SocketGuildUser arg2)
        {
            if (arg1 == null || arg1 == null)
            {
                await Core.LOG(new LogMessage(LogSeverity.Error, $"Core", "RaiseDiscordUserUpdated fed NULL parameter."));
                return;
            }
            if (arg1.IsBot) { return; }
            if(arg2.Activity == null){return;}
            if(arg1.Activity == null){return; }
            if(arg1.Activity.Type == ActivityType.Streaming){return; }
            if (arg2.Activity.Type == ActivityType.Streaming)
            {
                await Core.LOG(new LogMessage(LogSeverity.Error, "Core", $"RaiseDiscordUserUpdated {arg2.Username} started streaming."));
                return;
            }
        }
        public static void RaiseUserLinkEvent(UserEntry discordProfile, UserEntry twitchProfile)
        {
            if (discordProfile == null || twitchProfile == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, $"Core", "RaiseUserLinkEvent fed NULL parameter."));

                return;
            }
            OnUserEntryMerge?.Invoke(discordProfile, twitchProfile);
        }
        public static void RaiseBitEvent(BitEventArguments e)
        {
            if (e == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, $"Core", "RaiseBitEvent fed NULL parameter."));

                return;
            }
            OnBitEvent?.Invoke(e);
        }
        public static void RaiseBanEvent(BanEventArguments e)
        {
            if (e == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, $"Core", "RaiseBanEvent fed NULL parameter."));
                return;
            }
            OnBanEvent?.Invoke(e);
        }
        public static void RaiseHostEvent(BotChannel bChan, HostEventArguments e)
        {
            if(bChan == null || e == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, $"Core", "RaiseHostEvent fed NULL parameter."));
                return;
            }
            OnHostEvent?.Invoke(bChan, e);
        }
        public static void RaiseUnBanEvent(UnBanEventArguments e)
        {
            if (e == null)
            {
                return;
            }
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
            if (bChan == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, $"Core", "RaiseOnViewerCount fed NULL bChan."));
                return;
            }
            OnViewercount?.Invoke(bChan, oldCount, newCount);
        }
        public static void RaiseOnTwitchSubscription(BotChannel bChan, TwitchSubEventArguments e)
        {
            if (bChan == null || e == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, $"Core", "RaiseOnTwitchSubscription fed NULL parameter."));
                return;
            }
            if (e.subContext == TWSUBCONTEXT.UNKNOWN)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, $"Core", "RaiseOnTwitchSubscription UKNOWN sub context."));
                return;
            }
            OnTwitchSubEvent?.Invoke(bChan, e);
        }
        #endregion
    }

}
