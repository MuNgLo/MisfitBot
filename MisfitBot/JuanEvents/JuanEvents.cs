using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace MisfitBot_MKII.JuanEvents
{
    #region Event definitions
    // All Discord events here and they all need to start with Discord and end with Event
    public delegate void DiscordConnectedEvent();
    public delegate void DiscordGuildAvailableEvent(SocketGuild arg);
    public delegate void DiscordNewMemberEvent(BotChannel bChan, UserEntry user);
    public delegate void DiscordMemberLeftEvent(BotChannel bChan, UserEntry user);
    public delegate void DiscordMemberUpdatedEvent(BotChannel bchan, UserEntry currentUser, UserEntry oldUser);
    public delegate void DiscordMembersDownloadedEvent(SocketGuild arg);
    public delegate void DiscordReadyEvent();
    // All Twitch events here all all need to start with Twitch and end with Event
    public delegate void TwitchConnectedEvent(OnConnectedArgs args);
    public delegate void TwitchDisconnectedEvent();

    // Botwide type events
    public delegate void MessageReceivedEvent(BotWideMessageArguments args);

    // Below needs to be verified
    public delegate void BanEvent(BanEventArguments e);
    public delegate void BitEvent(BitEventArguments e);
    public delegate void DiscordUserStartsStreamEvent(BotChannel bChan, UserEntry user, StreamingGame streaminfo);
    public delegate void TwitchChannelGoesLiveEvent(BotChannel bChan, int delay);
    public delegate void TwitchChannelGoesOfflineEvent(BotChannel bChan);
    public delegate void BotChannelMergeEvent(BotChannel discordBotChannel, BotChannel twitchBotChannel);
    public delegate void HostEvent(BotChannel bChan, HostEventArguments e);
    public delegate void RaidEvent(BotChannel bChan, RaidEventArguments e);
    public delegate void TwitchSubscriptionEvent(BotChannel bChan, TwitchSubEventArguments e);
    public delegate void UnBanEvent(UnBanEventArguments e);
    public delegate void UserEntryMergeEvents(UserEntry discordUser, UserEntry twitchUser);
    public delegate void ViewerCountEvent(BotChannel bChan, int oldCount, int newCount);
    #endregion

    public class BotwideEvents
    {
        // Discord
        public DiscordConnectedEvent OnDiscordConnected;
        public DiscordGuildAvailableEvent OnDiscordGuildAvailable;
        public DiscordNewMemberEvent OnDiscordNewMember;
        public DiscordMemberLeftEvent OnDiscordMemberLeft;
        public DiscordMemberUpdatedEvent OnDiscordMemberUpdated;
        public DiscordMembersDownloadedEvent OnDiscordMembersDownloaded;
        public DiscordReadyEvent OnDiscordReady;
        // Twitch
        public TwitchConnectedEvent OnTwitchConnected;
        public TwitchDisconnectedEvent OnTwitchDisconnected;
        // Botwide
        public MessageReceivedEvent OnMessageReceived;

        // Below needs to be verified
        public BanEvent OnBanEvent;
        public BitEvent OnBitEvent;
        public BotChannelMergeEvent OnBotChannelMerge;
        public DiscordUserStartsStreamEvent OnDiscordUserStartStream;
        public HostEvent OnHostEvent;
        public RaidEvent OnRaidEvent;
        public TwitchSubscriptionEvent OnTwitchSubEvent;
        public TwitchChannelGoesLiveEvent OnTwitchChannelGoesLive;
        public TwitchChannelGoesOfflineEvent OnTwitchChannelGoesOffline;
        public UnBanEvent OnUnBanEvent;
        public UserEntryMergeEvents OnUserEntryMerge;
        public ViewerCountEvent OnViewercount;

        private List<GenericTimeStamp> DiscordStreamerStamps;


        #region Botwide Events gets raised here
        internal void RaiseOnMessageReceived(BotWideMessageArguments args)
        {
            OnMessageReceived?.Invoke(args);
        }
        internal void RaiseOnDiscordReady()
        {
            OnDiscordReady?.Invoke();
        }
        internal void RaiseOnDiscordConnected()
        {
            OnDiscordConnected?.Invoke();
        }
        internal void RaiseOnTwitchConnected(object sender, OnConnectedArgs args)
        {
            OnTwitchConnected?.Invoke(args);
        }
        internal Task RaiseOnDiscordGuildAvailable(SocketGuild arg)
        {
            OnDiscordGuildAvailable?.Invoke(arg);
            return Task.CompletedTask;
        }
        internal void RaiseOnDiscordNewMember(BotChannel bChan, UserEntry user)
        {
            if (bChan == null) { return; }
            if (user == null) { return; }
            OnDiscordNewMember?.Invoke(bChan, user);
        }
        internal Task RaiseOnDiscordMembersDownloaded(SocketGuild arg)
        {
            if (arg != null)
            {
                OnDiscordMembersDownloaded?.Invoke(arg);
            }
            return Task.CompletedTask;
        }
        internal void RaiseOnDiscordGuildMemberUpdated(BotChannel bChan, UserEntry currentUser, UserEntry oldUser)
        {
            if (bChan != null && currentUser != null && oldUser != null)
            {
                OnDiscordMemberUpdated?.Invoke(bChan, currentUser, oldUser);
            }
        }

        internal void RaiseOnDiscordMemberLeft(BotChannel bChan, UserEntry user)
        {
            OnDiscordMemberLeft?.Invoke(bChan, user);
        }

        // Below needs to be verified
        internal async Task RaiseDiscordUserUpdated(SocketUser arg1, SocketUser arg2)
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
                    foreach (SocketGuild guild in arg1.MutualGuilds)
                    {
                        BotChannel bChan = await Program.Channels.GetDiscordGuildbyID(guild.Id);
                        if (bChan == null) { return; }
                        UserEntry user = await Program.Users.GetUserByDiscordID(arg1.Id);
                        if (user == null) { return; }
                        StreamingGame stream = arg2.Activity as StreamingGame;
                        if (stream == null) { return; }
                        //await Core.LOG(new LogMessage(LogSeverity.Error, "Events", $"RaiseDiscordUserUpdated {arg2.Username} StreamingGame. Stream.Name={stream.Name}, Stream.Type={stream.Type}, Stream.Url={stream.Url}"));
                        RaiseDiscordUserStartStream(bChan, user, stream);
                    }
                }
                return;
            }
        }
        private void AddRefreshStamp(GenericTimeStamp newStamp)
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
        private bool CheckStamp(ulong discordID)
        {
            if (DiscordStreamerStamps == null) { DiscordStreamerStamps = new List<GenericTimeStamp>(); }
            if (DiscordStreamerStamps.Exists(p => p.ulongID == discordID))
            {
                return Core.CurrentTime > DiscordStreamerStamps.Find(p => p.ulongID == discordID).timestamp + 2700;
            }
            return true;
        }
        internal void RaiseDiscordUserStartStream(BotChannel bChan, UserEntry user, StreamingGame streaminfo)
        {
            OnDiscordUserStartStream?.Invoke(bChan, user, streaminfo);
        }
        internal void RaiseUserLinkEvent(UserEntry discordProfile, UserEntry twitchProfile)
        {
            if (discordProfile == null || twitchProfile == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseUserLinkEvent fed NULL parameter."));

                return;
            }
            OnUserEntryMerge?.Invoke(discordProfile, twitchProfile);
        }
        internal void RaiseBitEvent(BitEventArguments e)
        {
            if (e == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseBitEvent fed NULL parameter."));

                return;
            }
            OnBitEvent?.Invoke(e);
        }
        internal void RaiseBanEvent(BanEventArguments e)
        {
            if (e == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseBanEvent fed NULL parameter."));
                return;
            }
            OnBanEvent?.Invoke(e);
        }
        internal void RaiseHostEvent(BotChannel bChan, HostEventArguments e)
        {
            if (bChan == null || e == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseHostEvent fed NULL parameter."));
                return;
            }
            OnHostEvent?.Invoke(bChan, e);
        }
        internal void RaiseRaidEvent(BotChannel bChan, RaidEventArguments e)
        {
            if (bChan == null || e == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseRaidEvent fed NULL parameter."));
                return;
            }
            Core.LOG(new LogMessage(LogSeverity.Error, "Events", $"RaiseRaidEvent for {e.TargetChannel}. Coming from {e.SourceChannel} with {e.RaiderCount} raiders."));

            OnRaidEvent?.Invoke(bChan, e);
        }
        internal void RaiseUnBanEvent(UnBanEventArguments e)
        {
            if (e == null)
            {
                return;
            }
            OnUnBanEvent?.Invoke(e);
        }

        internal void RaiseOnTwitchChannelGoesLive(BotChannel bChan, int delay)
        {
            if (bChan == null) { return; }
            bChan.isLive = true;
            OnTwitchChannelGoesLive?.Invoke(bChan, delay);
        }
        internal void RaiseOnTwitchChannelGoesOffline(BotChannel bChan)
        {
            if (bChan == null) { return; }
            bChan.isLive = false;
            OnTwitchChannelGoesOffline?.Invoke(bChan);
        }
        internal void RaiseOnViewerCount(BotChannel bChan, int oldCount, int newCount)
        {
            if (bChan == null)
            {
                Core.LOG(new LogMessage(LogSeverity.Error, "Events", "RaiseOnViewerCount fed NULL bChan."));
                return;
            }
            OnViewercount?.Invoke(bChan, oldCount, newCount);
        }
        internal void RaiseOnTwitchSubscription(BotChannel bChan, TwitchSubEventArguments e)
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
        internal void RaiseOnBotChannelMerge(BotChannel discordProfile, BotChannel twitchProfile)
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
