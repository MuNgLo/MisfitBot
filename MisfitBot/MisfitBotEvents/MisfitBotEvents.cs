using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace MisfitBot_MKII.MisfitBotEvents
{
    public class BotwideEvents
    {
        // Muchos Importante tings first
        public TwitchNewFollowerEvent OnTwitchFollow; // Hooked up in AdminPlugin
        public TwitchSubGiftEvent OnTwitchSubGift; // Hooked up in AdminPlugin
        public RaidEvent OnRaidEvent; // Hooked up in AdminPlugin
        public TwitchResubscriberEvent OnTwitchReSubscriber; // Hooked up in AdminPlugin
        public TwitchNewsubscriberEvent OnTwitchNewSubscriber; // Hooked up in AdminPlugin
        public TwitchCommunitySubscriptionEvent OnTwitchCommunitySubscription;
        //public TwitchSubGiftEvent OnTwitchSubEvent;


        // Discord
        public DiscordConnectedEvent OnDiscordConnected;
        public DiscordDisConnectedEvent OnDiscordDisConnected;
        public DiscordGuildAvailableEvent OnDiscordGuildAvailable;
        public DiscordNewMemberEvent OnDiscordNewMember;
        public DiscordMemberLeftEvent OnDiscordMemberLeft;
        public DiscordMemberUpdatedEvent OnDiscordMemberUpdated;
        public DiscordMembersDownloadedEvent OnDiscordMembersDownloaded;
        public DiscordReadyEvent OnDiscordReady;
        // Twitch
        public TwitchConnectedEvent OnTwitchConnected;
        public TwitchConnectionErrorEvent OnTwitchConnectionError;
        public TwitchChannelChatClearedEvent OnTwitchChannelChatCleared;
        public TwitchChannelJoinLeaveEvent OnTwitchChannelLeft;
        public TwitchChannelJoinLeaveEvent OnTwitchChannelJoined;
        public TwitchDisconnectedEvent OnTwitchDisconnected;
        public TwitchHostEvent OnTwitchHostEvent;
        public TwitchMessageClearedEvent OnTwitchMessageCleared;
        public TwitchMessageSentEvent OnTwitchMessageSent;
        public TwitchUserJoinLeaveEvent OnTwitchUserJoin;
        public TwitchUserJoinLeaveEvent OnTwitchUserLeave;
        public TwitchWhisperMessageEvent OnTwitchWhisperReceived;
        // Botwide
        public MessageReceivedEvent OnMessageReceived;
        public CommandReceivedEvent OnCommandReceived;

        // Below needs to be verified
        public BanEvent OnBanEvent;
        public BitEvent OnBitEvent;
        public BotChannelMergeEvent OnBotChannelMerge;
        public DiscordUserStartsStreamEvent OnDiscordUserStartStream;
        public TwitchChannelGoesLiveEvent OnTwitchChannelGoesLive;
        public TwitchChannelGoesOfflineEvent OnTwitchChannelGoesOffline;
        public UnBanEvent OnUnBanEvent;
        public UserEntryMergeEvents OnUserEntryMerge;
        public ViewerCountEvent OnViewercount;

        private List<GenericTimeStamp> DiscordStreamerStamps;

        internal async Task RaiseTwitchOnUserJoin(BotChannel bChan, UserEntry user){
            await Task.Run(()=>{
                if(bChan!=null && user != null){
                    OnTwitchUserJoin?.Invoke(bChan, user);
                }
            });
        }
        internal async Task RaiseTwitchOnUserLeave(BotChannel bChan, UserEntry user){
            await Task.Run(()=>{
                if(bChan!=null && user != null){
                    OnTwitchUserLeave?.Invoke(bChan, user);
                }
            });
        }
        internal async Task RaiseTwitchOnReSubscriber(BotChannel bChan, TwitchReSubArguments args)
        {
            await Task.Run(()=>{
                if(bChan!=null && args != null){
                    OnTwitchReSubscriber?.Invoke(bChan, args);
                }
            });
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EVENTS", $"{args.userDisplayname} resubscribed to channel {bChan.TwitchChannelName}."));
        }
        internal async Task RaiseTwitchOnNewSubscriber(BotChannel bChan, TwitchNewSubArguments args)
        {
            await Task.Run(()=>{
                if(bChan!=null && args != null){
                    OnTwitchNewSubscriber?.Invoke(bChan, args);
                }
            });
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EVENTS", $"{args.userDisplayname} subscribed to channel {bChan.TwitchChannelName}."));
        }
        internal async void RaiseTwitchOnSubGift(BotChannel bChan, TwitchSubGiftEventArguments args){
            await Task.Run(()=>{
                if(bChan != null){OnTwitchSubGift?.Invoke(bChan, args);}
                if(Program.Debugmode){ Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EVENTS", args.LogString)); }
            });
        }

        #region Botwide Events gets raised here
        internal void RaiseOnMessageReceived(BotWideMessageArguments args)
        {
            OnMessageReceived?.Invoke(args);
        }
        internal void RaiseOnCommandRecieved(BotWideCommandArguments args)
        {
            OnCommandReceived?.Invoke(args);
        }
        internal void RaiseOnDiscordReady()
        {
            OnDiscordReady?.Invoke();
        }
        internal void RaiseOnDiscordConnected()
        {
            OnDiscordConnected?.Invoke();
        }

        internal async Task RaiseTwitchWhisperReceived(string username, string message)
        {
            await Task.Run(()=>{
            OnTwitchWhisperReceived?.Invoke(username, message);
            });
        }

        internal async Task RaiseTwitchOnBeingHosted(HostedEventArguments args)
        {
            BotChannel bchan = await Program.Channels.GetTwitchChannelByName(args.HostedChannel);
            if(bchan!=null){
                OnTwitchHostEvent?.Invoke(bchan, args);
            }
        }

        internal async void RaiseonTwitchMessageCleared(BotChannel bChan, OnMessageClearedArgs e)
        {
            await Task.Run(()=>{
                    OnTwitchMessageCleared?.Invoke(bChan, e);
            });
        }

        internal async Task RaiseOnTwitchMessageSent(string channel, string message)
        {
            await Task.Run(()=>{
            OnTwitchMessageSent?.Invoke(channel, message);
            });
        }

        internal async Task RaiseTwitchOnCommunitySubscription(BotChannel bChan, string message)
        {
            await Task.Run(()=>{
                OnTwitchCommunitySubscription?.Invoke(bChan, message);
            });
        }

        internal async Task RaiseTwitchOnChannelLeave(string channel, string botname)
        {
            await Task.Run(()=>{
                OnTwitchChannelLeft?.Invoke(channel, botname);
            });
        }
        internal async Task RaiseTwitchOnChannelJoined(string channel, string botname)
        {
            if(Program.BotName == botname){return;}
            await Task.Run(()=>{
                OnTwitchChannelJoined?.Invoke(channel, botname);
            });
        }
        #region Twitch raisers pass 1
        internal async Task RaiseOnTwitchConnected(string msg)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Events", "Twitch Connected: " + msg));
            OnTwitchConnected?.Invoke(msg);
        }

        

        internal async Task RaiseTwitchOnChannelChatCleared(string channel)
        {
            await Task.Run(()=>{
                OnTwitchChannelChatCleared?.Invoke(channel);
            });
        }

        internal async Task RaiseOnTwitchConnectionError(string msg)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "Events", "Twitch Connection Error: " + msg));
            OnTwitchConnectionError?.Invoke(msg);
        }
        internal async Task RaiseOnTwitchDisconnected(string msg)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "Events", "Twitch Disconnected: " + msg));
            OnTwitchDisconnected?.Invoke();
        }
        #endregion

        internal async Task RaiseOnDiscordGuildAvailable(SocketGuild arg)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Events", "Discord Guild Available: " + arg.Name));
            OnDiscordGuildAvailable?.Invoke(arg);
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
                await Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "Events", "RaiseDiscordUserUpdated fed NULL parameter."));
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

        

        internal async Task RaiseOnDiscordDisconnected(Exception e)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "BotwideEvents", $"ClientDisconnected:{e.Message}"));
            OnDiscordDisConnected?.Invoke();
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
                Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "Events", "RaiseUserLinkEvent fed NULL parameter."));

                return;
            }
            OnUserEntryMerge?.Invoke(discordProfile, twitchProfile);
        }
        internal void RaiseBitEvent(BitEventArguments e)
        {
            if (e == null)
            {
                Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "Events", "RaiseBitEvent fed NULL parameter."));
                return;
            }
            if(Program.Debugmode){ Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EVENTS", $"Twitch channel {e.bChan.TwitchChannelName} BITS {e.bitsGiven} from {e.user._twitchDisplayname}. Total {e.bitsTotal}")); }
            OnBitEvent?.Invoke(e);
        }
        internal void RaiseBanEvent(BanEventArguments e)
        {
            if (e == null)
            {
                Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "Events", "RaiseBanEvent fed NULL parameter."));
                return;
            }
            OnBanEvent?.Invoke(e);
        }

        internal void RaiseOnTwitchFollow(BotChannel bChan, UserEntry user)
        {
            if (bChan != null && user != null)
            {
                Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Events", $"New twitch follower:{user._twitchDisplayname}"));
                OnTwitchFollow?.Invoke(bChan, user);
            }
        }

        internal void RaiseHostEvent(BotChannel bChan, HostedEventArguments e)
        {
            if (bChan == null || e == null)
            {
                Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "Events", "RaiseHostEvent fed NULL parameter."));
                return;
            }
            if(Program.Debugmode){ Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EVENTS", $"Twitch channel {e.HostedChannel} hosted by {e.HostChannel}")); }

            OnTwitchHostEvent?.Invoke(bChan, e);
        }
        internal void RaiseRaidEvent(BotChannel bChan, RaidEventArguments e)
        {
            if (bChan == null || e == null)
            {
                Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "Events", "RaiseRaidEvent fed NULL parameter."));
                return;
            }
            Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "Events", $"RaiseRaidEvent for {e.TargetChannel}. Coming from {e.SourceChannel} with {e.RaiderCount} raiders."));

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
            if(Program.Debugmode){ Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EVENTS", $"Twitch channel {bChan.TwitchChannelName} went live")); }
            OnTwitchChannelGoesLive?.Invoke(bChan, delay);
        }
        internal void RaiseOnTwitchChannelGoesOffline(BotChannel bChan)
        {
            if (bChan == null) { return; }
            bChan.isLive = false;
            if(Program.Debugmode){ Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EVENTS", $"Twitch channel {bChan.TwitchChannelName} went offline")); }
            OnTwitchChannelGoesOffline?.Invoke(bChan);
        }
        internal void RaiseOnViewerCount(BotChannel bChan, int oldCount, int newCount)
        {
            if (bChan == null)
            {
                Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "EVENTS", "RaiseOnViewerCount fed NULL bChan."));
                return;
            }
            if(Program.Debugmode){ Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EVENTS", $"{bChan.TwitchChannelName} has {newCount} viewers. ({newCount - oldCount})")); }
            OnViewercount?.Invoke(bChan, oldCount, newCount);
        }

        internal void RaiseOnBotChannelMerge(BotChannel discordProfile, BotChannel twitchProfile)
        {
            if (discordProfile == null || twitchProfile == null)
            {
                Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "EVENTS", "RaiseOnBotChannelMerge fed NULL parameter."));
                return;
            }
            if(Program.Debugmode){ Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EVENTS", $"BotChannel Merge between D:{discordProfile.GuildName} and TW:{twitchProfile.TwitchChannelName}")); }
            OnBotChannelMerge?.Invoke(discordProfile, twitchProfile);
        }
        #endregion

    }
}
