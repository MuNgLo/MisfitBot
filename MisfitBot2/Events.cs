using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MisfitBot2
{
    #region Event definitions
    public delegate void BanEvent(BanEventArguments e);
    public delegate void BitEvent(BitEventArguments e);
    public delegate void DiscordUserStartsStreamEvent(BotChannel bChan, UserEntry user, StreamingGame streaminfo);
    public delegate void TwitchChannelGoesLiveEvent(BotChannel bChan, int delay);
    public delegate void TwitchChannelGoesOfflineEvent(BotChannel bChan);
    public delegate void BotChannelMergeEvent(BotChannel discordBotChannel, BotChannel twitchBotChannel);
    public delegate void HostEvent(BotChannel bChan, HostEventArguments e);
    public delegate void NewDiscordMemberEvent(BotChannel bChan, UserEntry user);
    public delegate void RaidEvent(BotChannel bChan, RaidEventArguments e);
    public delegate void TwitchSubscriptionEvent(BotChannel bChan, TwitchSubEventArguments e);
    public delegate void UnBanEvent(UnBanEventArguments e);
    public delegate void UserEntryMergeEvents(UserEntry discordUser, UserEntry twitchUser);
    public delegate void ViewerCountEvent(BotChannel bChan, int oldCount, int newCount);

    #endregion

    public static class Events
    {
        public static BanEvent OnBanEvent;
        public static BitEvent OnBitEvent;
        public static BotChannelMergeEvent OnBotChannelMerge;
        public static DiscordUserStartsStreamEvent OnDiscordUserStartStream;
        public static HostEvent OnHostEvent;
        public static NewDiscordMemberEvent OnNewDiscordMember;
        public static RaidEvent OnRaidEvent;
        public static TwitchSubscriptionEvent OnTwitchSubEvent;
        public static TwitchChannelGoesLiveEvent OnTwitchChannelGoesLive;
        public static TwitchChannelGoesOfflineEvent OnTwitchChannelGoesOffline;
        public static UnBanEvent OnUnBanEvent;
        public static UserEntryMergeEvents OnUserEntryMerge;
        public static ViewerCountEvent OnViewercount;

        private static List<GenericTimeStamp> DiscordStreamerStamps;


        #region Botwide Events gets raised here
        internal static async Task RaiseDiscordUserUpdated(SocketGuildUser arg1, SocketGuildUser arg2)
        {

            if (arg1 == null || arg1 == null)
            {
                await Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseDiscordUserUpdated fed NULL parameter."));
                return;
            }
            if (arg1.IsBot) { return; }
            if (arg2.Activity == null) { return; }
            if (arg1.Activity == null) { return; }
            if (arg1.Activity.Type == ActivityType.Streaming)
            {
                if (arg2.Activity.Type != ActivityType.Streaming)
                {
                    // Discord users drops streaming flag
                    AddRefreshStamp(new GenericTimeStamp() { ulongID = arg1.Id, stringID = string.Empty, timestamp = Core.CurrentTime });
                }
                return;
            }
            if (arg2.Activity.Type == ActivityType.Streaming)
            {
                if (CheckStamp(arg1.Id))
                {
                    BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(arg1.Guild.Id);
                    if (bChan == null) { return; }
                    UserEntry user = await Core.UserMan.GetUserByDiscordID(arg1.Id);
                    if (user == null) { return; }
                    StreamingGame stream = arg2.Activity as StreamingGame;
                    if (stream == null) { return; }
                    //await Core.LOG(new LogMessage(LogSeverity.Error, "Events", $"RaiseDiscordUserUpdated {arg2.Username} StreamingGame. Stream.Name={stream.Name}, Stream.Type={stream.Type}, Stream.Url={stream.Url}"));
                    RaiseDiscordUserStartStream(bChan, user, stream);
                }
                return;
            }
        }
        private static void AddRefreshStamp(GenericTimeStamp newStamp)
        {
            if (DiscordStreamerStamps == null) { DiscordStreamerStamps = new List<GenericTimeStamp>(); }
            if (!DiscordStreamerStamps.Exists(p => p.ulongID == newStamp.ulongID))
            {
                DiscordStreamerStamps.Add(newStamp);
            }
            else
            {
                int index = DiscordStreamerStamps.FindIndex(p => p.ulongID == newStamp.ulongID);
                DiscordStreamerStamps[index] = newStamp;
            }
        }
        private static bool CheckStamp(ulong discordID)
        {
            if (DiscordStreamerStamps == null) { DiscordStreamerStamps = new List<GenericTimeStamp>(); }
            if (DiscordStreamerStamps.Exists(p => p.ulongID == discordID))
            {
                return Core.CurrentTime > DiscordStreamerStamps.Find(p => p.ulongID == discordID).timestamp + 2700;
            }
            return true;
        }

        public static void RaiseDiscordUserStartStream(BotChannel bChan, UserEntry user, StreamingGame streaminfo)
        {
            OnDiscordUserStartStream?.Invoke(bChan, user, streaminfo);
        }

        public static void RaiseUserLinkEvent(UserEntry discordProfile, UserEntry twitchProfile)
        {
            if (discordProfile == null || twitchProfile == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseUserLinkEvent fed NULL parameter."));

                return;
            }
            OnUserEntryMerge?.Invoke(discordProfile, twitchProfile);
        }
        public static void RaiseBitEvent(BitEventArguments e)
        {
            if (e == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseBitEvent fed NULL parameter."));

                return;
            }
            OnBitEvent?.Invoke(e);
        }
        public static void RaiseBanEvent(BanEventArguments e)
        {
            if (e == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseBanEvent fed NULL parameter."));
                return;
            }
            OnBanEvent?.Invoke(e);
        }
        public static void RaiseHostEvent(BotChannel bChan, HostEventArguments e)
        {
            if (bChan == null || e == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseHostEvent fed NULL parameter."));
                return;
            }
            OnHostEvent?.Invoke(bChan, e);
        }
        public static void RaiseRaidEvent(BotChannel bChan, RaidEventArguments e)
        {
            if (bChan == null || e == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseRaidEvent fed NULL parameter."));
                return;
            }
            Core.LOG(new LogMessage(LogSeverity.Error, "Events", $"RaiseRaidEvent for {e.TargetChannel}. Coming from {e.SourceChannel} with {e.RaiderCount} raiders."));

            OnRaidEvent?.Invoke(bChan, e);
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
        public static void RaiseOnTwitchChannelGoesLive(BotChannel bChan, int delay)
        {
            if (bChan == null) { return; }
            bChan.isLive = true;
            OnTwitchChannelGoesLive?.Invoke(bChan, delay);
        }
        public static void RaiseOnTwitchChannelGoesOffline(BotChannel bChan)
        {
            if (bChan == null) { return; }
            bChan.isLive = false;
            OnTwitchChannelGoesOffline?.Invoke(bChan);
        }
        public static void RaiseOnViewerCount(BotChannel bChan, int oldCount, int newCount)
        {
            if (bChan == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseOnViewerCount fed NULL bChan."));
                return;
            }
            OnViewercount?.Invoke(bChan, oldCount, newCount);
        }
        public static void RaiseOnTwitchSubscription(BotChannel bChan, TwitchSubEventArguments e)
        {
            if (bChan == null || e == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseOnTwitchSubscription fed NULL parameter."));
                return;
            }
            if (e.subContext == TWSUBCONTEXT.UNKNOWN)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseOnTwitchSubscription UKNOWN sub context."));
                return;
            }
            OnTwitchSubEvent?.Invoke(bChan, e);
        }

        internal static void RaiseOnBotChannelMerge(BotChannel discordProfile, BotChannel twitchProfile)
        {
            if (discordProfile == null || twitchProfile == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseOnBotChannelMerge fed NULL parameter."));
                return;
            }
            OnBotChannelMerge?.Invoke(discordProfile, twitchProfile);
        }
        #endregion

    }
}
