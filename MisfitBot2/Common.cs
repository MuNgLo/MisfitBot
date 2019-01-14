using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using MisfitBot2.Services;
using Newtonsoft.Json;
using TwitchLib.Client.Events;
namespace MisfitBot2
{
    public delegate void UserEntryMerge(UserEntry discordUser, UserEntry twitchUser);
    public delegate void BotChannelMergeEvent(BotChannel guildID, BotChannel twitchChannelName);
    public delegate void BotChannelGoesLive(BotChannel bChan);
    public delegate void BotChannelGoesOffline(BotChannel bChan);
    public delegate void BitEvent(BitEventArguments e);
    public delegate void BanEvent(BanEventArguments e);
    public delegate void UnBanEvent(UnBanEventArguments e);
    public delegate void NewDiscordMember(BotChannel bChan, UserEntry user);

    public class UserValues
    {
        public int _timestamp = 0;
        public UserValues(int time)
        {
            _timestamp = time;
        }
    }

    public class BitEventArguments
    {
        public BotChannel bChan;
        public UserEntry user;
        public int timestamp;
        public int bitsGiven;
        public int bitsTotal;
        public string context;
        public string chatmessage;
        public BitEventArguments(BotChannel chan, UserEntry usr, int time, int bits, int total, string con, string chatmsg)
        {
            bChan = chan;user = usr;timestamp = time;bitsGiven = bits;bitsTotal = total;context = con;chatmessage = chatmsg;
        }
    }
    public class BanEventArguments
    {
        public BotChannel bChan;
        public UserEntry enforcingUser;
        public UserEntry bannedUser;
        public bool isDiscord;
        public int timestamp;
        public int duration;
        public string reason;
        public BanEventArguments(BotChannel chan, UserEntry mod, UserEntry vic, int time, int dur, string cause, bool isTwitch)
        {
            bChan = chan; enforcingUser = mod; bannedUser = vic; timestamp = time; duration = dur; reason = cause; isDiscord = !isTwitch;
        }
    }
    public class UnBanEventArguments
    {
        public BotChannel bChan;
        public UserEntry enforcingUser;
        public UserEntry bannedUser;
        public bool isDiscord;
        public int timestamp;
        public UnBanEventArguments(BotChannel chan, UserEntry mod, UserEntry vic, int time, bool isTwitch)
        {
            bChan = chan; enforcingUser = mod; bannedUser = vic; timestamp = time; isDiscord = !isTwitch;
        }
    }
    /// <summary>
    /// Inherit from this for any pluginsetting. This contains all the common variable
    /// </summary>
    public class PluginSettingsBase
    {
        [JsonProperty]
        public volatile bool _active = true;
        [JsonProperty]
        public volatile int _defaultCooldown = 30;
        [JsonProperty]
        public ulong _defaultDiscordChannel = 0;
        [JsonProperty]
        public volatile string _defaultTwitchRoom = string.Empty;
    }
}


namespace MisfitBot2.Services
{
    interface IService
    {
        void OnSecondTick(int seconds);
        void OnMinuteTick(int minutes);
        void OnUserEntryMerge(UserEntry discordUser, UserEntry twitchUser);
        void OnBotChannelEntryMerge(BotChannel discordGuild, BotChannel twitchChannel);
    }
}
