﻿using Discord;
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
        public TwitchChannelChatCleared OnTwitchChannelChatCleared;
        public TwitchDisconnectedEvent OnTwitchDisconnected;
        public TwitchChannelJoinLeave OnTwitchChannelLeft;
        public TwitchChannelJoinLeave OnTwitchChannelJoined;
        public TwitchMessageSentEvent OnTwitchMessageSent;
        public TwitchWhisperMessageEvent OnTwitchWhisperReceived;

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

        internal async Task RaiseTwitchWhisperReceived(string username, string message)
        {
            await Task.Run(()=>{
            OnTwitchWhisperReceived?.Invoke(username, message);
            });
        }

        internal async Task RaiseOnTwitchMessageSent(string channel, string message)
        {
            await Task.Run(()=>{
            OnTwitchMessageSent?.Invoke(channel, message);
            });
        }

        internal async Task RaiseTwitchOnCommunitySubscription(string channel, CommunitySubscription giftedSubscription)
        {
            await Task.Run(()=>{
            JsonDumper.DumpObjectToJson(giftedSubscription, channel);
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
            await Task.Run(()=>{
                OnTwitchChannelJoined?.Invoke(channel, botname);
            });
        }
        #region Twitch raisers pass 1
        internal async Task RaiseOnTwitchConnected(string msg)
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, "Events", "Twitch Connected: " + msg));
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
            await Core.LOG(new LogMessage(LogSeverity.Error, "Events", "Twitch Connection Error: " + msg));
            OnTwitchConnectionError?.Invoke(msg);
        }
        internal async Task RaiseOnTwitchDisconnected(string msg)
        {
            await Core.LOG(new LogMessage(LogSeverity.Error, "Events", "Twitch Disconnected: " + msg));
            OnTwitchDisconnected?.Invoke();
        }
        #endregion

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

        internal async Task RaiseOnDiscordDisconnected(Exception e)
        {
            await Core.LOG(new LogMessage(LogSeverity.Error, "BotwideEvents", $"ClientDisconnected:{e.Message}"));
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
