﻿using Discord;
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
        public static BitEvent OnBitEvent;
        public static BanEvent OnBanEvent;
        public static UnBanEvent OnUnBanEvent;
        public static NewDiscordMember OnNewDiscordMember;
        public static BotChannelGoesLive OnBotChannelGoesLive;
        public static BotChannelGoesOffline OnBotChannelGoesOffline;

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
        public static int LastLaunchTime;
        public static int UpTime { private set { } get { return UnixTime() - LastLaunchTime; } }

        public static JuansLog LOGGER;

        public static Func<LogMessage, Task> LOG;
        public static JsonSerializer serializer = new JsonSerializer();
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

        public static void RaiseBitEvent(BitEventArguments e)
        {
            OnBitEvent?.Invoke(e);
        }

        public static void RaiseBanEvent(BanEventArguments e)
        {
            OnBanEvent?.Invoke(e);
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

        public static void RaiseOnBotChannelGoesLive(BotChannel bChan)
        {
            if (bChan == null) { return; }
            bChan.isLive = true;
            OnBotChannelGoesLive?.Invoke(bChan);
        }

        public static void RaiseOnBotChannelGoesOffline(BotChannel bChan)
        {
            if (bChan == null) { return; }
            bChan.isLive = false;
            OnBotChannelGoesOffline?.Invoke(bChan);
        }
    }

}
