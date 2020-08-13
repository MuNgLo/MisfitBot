using System;
using System.Threading.Tasks;
using Discord;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.Communication.Events;

namespace MisfitBot_MKII
{
    internal class EventCatcherTwitch
    {
        internal EventCatcherTwitch(ITwitchClient client, bool doLog)
        {
            //client.OnBeingHosted 
            //client.OnChannelStateChanged
            client.OnChatCleared += TwitchOnChatCleared;
            client.OnChatColorChanged += TwitchOnChatColorChanged;
            //client.OnChatCommandReceived
            client.OnCommunitySubscription += TwitchOnCommunitySubscription;
            client.OnConnected += TwitchOnConnected;
            client.OnConnectionError += TwitchOnConnectionError;
            client.OnDisconnected += OnDisconnected;
            client.OnError += TwitchOnError;
            //client.OnExistingUsersDetected
            //client.OnGiftedSubscription
            //client.OnHostingStarted
            //client.OnHostingStopped
            //client.OnHostLeft
            client.OnIncorrectLogin += TwitchOnIncorrectLogin;
            client.OnJoinedChannel += TwitchOnJoinedChannel;
            client.OnLeftChannel += TwitchOnLeftChannel;
            if (doLog) { client.OnLog += OnLog; }
            //client.OnMessageCleared
            client.OnMessageReceived += OnMessageReceived;
            client.OnMessageSent += TwitchOnMessageSent;
            client.OnMessageThrottled += TwitchOnMessageThrottled;
            //client.OnModeratorJoined
            //client.OnModeratorLeft
            //client.OnModeratorsReceived
            //client.OnNewSubscriber
            //client.OnNowHosting
            client.OnRaidNotification += OnRaidNotification;
            client.OnReconnected += TwitchOnReconnect;
            client.OnReSubscriber += TwitchOnReSubscriber;
            client.OnRitualNewChatter += TwitchOnRitualNewChatter;
            //client.OnSendReceiveData
            client.OnUserBanned += TwitchOnUserBanned;
            client.OnUserJoined += TwitchOnUserJoined;
            //client.OnUserLeft
            //client.OnUserStateChanged
            //client.OnUserTimedout
            //client.OnVIPsReceived
            //client.OnWhisperCommandReceived
            client.OnWhisperReceived += TwitchWhisperReceived;
            //client.OnWhisperSent
            client.OnWhisperThrottled += TwitchOnWhisperThrottled;
        }

        private void TwitchOnReconnect(object sender, OnReconnectedEventArgs e)
        {
            Core.LOG(new LogMessage(
                LogSeverity.Warning, "EventCatcherTwitch",  
                $"Twitch reconnect!"));
        }

        private void TwitchOnChatColorChanged(object sender, OnChatColorChangedArgs e)
        {
            // WTF! Look into this WHy just channel? Whne does it fire?
        }

        private void TwitchOnError(object sender, OnErrorEventArgs e)
        {
            Core.LOG(new LogMessage(
                LogSeverity.Error, "EventCatcherTwitch",  
                $"Twitch Error! {e.Exception.Message}"));
        }

        private async void TwitchOnMessageSent(object sender, OnMessageSentArgs e)
        {
            await Program.BotEvents.RaiseOnTwitchMessageSent(e.SentMessage.Channel, e.SentMessage.Message);
        }

        private void TwitchOnIncorrectLogin(object sender, OnIncorrectLoginArgs e)
        {
            Core.LOG(new LogMessage(
                LogSeverity.Error, "EventCatcherTwitch",  
                $"Twitch login failed! {e.Exception.Message}"));
        }

        private void TwitchOnRitualNewChatter(object sender, OnRitualNewChatterArgs e)
        {
            JsonDumper.DumpObjectToJson(e, "TwitchOnRitualNewChatter");/// TODO look up when this fires
        }

        private void TwitchOnMessageThrottled(object sender, OnMessageThrottledEventArgs e)
        {
            Core.LOG(new LogMessage(LogSeverity.Warning, "EventCatcherTwitch",  $"Twitch Message Throttled!! {e.AllowedInPeriod}:Allowed {e.Period}:Period {e.SentMessageCount}:sent count"));
        }

        private void TwitchOnWhisperThrottled(object sender, OnWhisperThrottledEventArgs e)
        {
            Core.LOG(new LogMessage(LogSeverity.Warning, "EventCatcherTwitch",  $"Twitch Whisper Throttled!! {e.AllowedInPeriod}:Allowed {e.Period}:Period {e.SentWhisperCount}:sent count"));
        }

        private async void TwitchWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            await Program.BotEvents.RaiseTwitchWhisperReceived(e.WhisperMessage.Username, e.WhisperMessage.Message);
        }

        private async void TwitchOnCommunitySubscription(object sender, OnCommunitySubscriptionArgs e)
        {
            await Program.BotEvents.RaiseTwitchOnCommunitySubscription(e.Channel, e.GiftedSubscription);
        }

        private async void TwitchOnLeftChannel(object sender, OnLeftChannelArgs e)
        {
            await Program.BotEvents.RaiseTwitchOnChannelLeave(e.Channel, e.BotUsername);
        }

        private async void TwitchOnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            await Program.BotEvents.RaiseTwitchOnChannelJoined(e.Channel, e.BotUsername);
        }

        private async void TwitchOnChatCleared(object sender, OnChatClearedArgs e)
        {
            await Program.BotEvents.RaiseTwitchOnChannelChatCleared(e.Channel);
        }

        #region Need confirmation
        /// <summary>
        /// UNTESTED!!!!! TODO TEST THIS YOU LUMP OF SHIT
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TwitchOnExistingUsersDetected(object sender, OnExistingUsersDetectedArgs e)
        {
            /*TwitchLib.Api.V5.Models.Users.Users users = await _api.V5.Users.GetUsersByNameAsync(e.Users);
            foreach (TwitchLib.Api.V5.Models.Users.User user in users.Matches)
            {
                string twitchID = user.Id;
            }*/
            await Core.LOG(new LogMessage(LogSeverity.Info, "EventCatcherTwitch", $"TwitchOnExistingUsersDetected"));
        }
        private async void TwitchOnReSubscriber(object sender, OnReSubscriberArgs e)
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, "EventCatcherTwitch", $"{e.ReSubscriber.DisplayName} resubscribed to channel {e.Channel}."));
        }
        private async void TwitchOnUserJoined(object sender, OnUserJoinedArgs e)
        {
            UserEntry user = await Program.Users.GetUserByTwitchUserName(e.Username);
            if (user == null)
            {
                // RAISE User joined twitch chat botevent
                return;
            }
        }
        #endregion

        #region pass 1
        /// <summary>
        /// Fires when Twitch client connected to twitch.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TwitchOnConnected(object sender, OnConnectedArgs e)
        {
            await Program.BotEvents.RaiseOnTwitchConnected(e.AutoJoinChannel);
        }
        /// <summary>
        /// Fires when twitch client gets an errror I guess
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async void TwitchOnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            await Program.BotEvents.RaiseOnTwitchConnectionError(e.ToString());
        }
        /// <summary>
        ///  Fires when Twitch client is disconnected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async void OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            await Program.BotEvents.RaiseOnTwitchDisconnected(e.ToString());
        }
        /// <summary>
        /// Just grab log output from Twitch client.await Only used if start argument logtwitch is given.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async void OnLog(object sender, OnLogArgs e)
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, "EventCatcherTwitch", $"LOG:{e.Data}"));
        }
        /// <summary>
        /// Reformats any message recieved and raises the botwide event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            UserEntry usr = await Program.Users.GetUserByTwitchID(e.ChatMessage.UserId);
            Program.BotEvents.RaiseOnMessageReceived(new BotWideMessageArguments()
            {
                source = MESSAGESOURCE.TWITCH,
                channel = e.ChatMessage.Channel,
                user = usr,
                message = e.ChatMessage.Message
            });
        }
        /// <summary>
        /// Fires when a raid notification is detected in chat
        /// </summary>
        private async void OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(e.Channel);
            int i = 0;
            int.TryParse(e.RaidNotification.MsgParamViewerCount, out i);
            Program.BotEvents.RaiseRaidEvent(bChan, new RaidEventArguments(e.RaidNotification.DisplayName, e.Channel, i));
        }
        /// <summary>
        /// Reformats and raise a botwide event when a twitch user is banned
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async void TwitchOnUserBanned(object sender, OnUserBannedArgs e)
        {
            UserEntry BannedUser = await Program.Users.GetUserByTwitchUserName(e.UserBan.Username);
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(e.UserBan.Channel);
            BanEventArguments banEvent = new BanEventArguments(
                bChan,
                null,
                BannedUser,
                Core.CurrentTime,
                0,
                e.UserBan.BanReason,
                true
                );
            Program.BotEvents.RaiseBanEvent(banEvent);
        }
        #endregion

    }// EOC
}// EON