using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.Communication.Events;
using MisfitBot_MKII.Statics;
using System.Threading.Tasks;
using System;

namespace MisfitBot_MKII.MisfitBotEvents
{
    internal class EventCatcherTwitch
    {
        internal EventCatcherTwitch(ITwitchClient client, bool doLog)
        {
            client.OnChatCommandReceived += TwitchOnChatCommandReceived;
            client.OnConnected += TwitchOnConnected;
            client.OnConnectionError += TwitchOnConnectionError;
            client.OnDisconnected += OnDisconnected;
            client.OnError += TwitchOnError;


            //client.OnChannelStateChanged += TwitchChannelStateChanged;
            //client.OnChatCleared += TwitchOnChatCleared;

            client.OnExistingUsersDetected += TwitchOnExistingUsersDetected; // Maybe only the twitch user extension should listen to this
            client.OnIncorrectLogin += TwitchOnIncorrectLogin;
            //client.OnJoinedChannel += TwitchOnJoinedChannel;
            //client.OnLeftChannel += TwitchOnLeftChannel;
            //client.OnMessageCleared += TwitchOnMessageCleared;
            //client.OnMessageReceived += OnMessageReceived;
            //client.OnMessageSent += TwitchOnMessageSent;
            client.OnMessageThrottled += TwitchOnMessageThrottled;
            //client.OnModeratorJoined
            //client.OnModeratorLeft
            //client.OnModeratorsReceived
            //client.OnNowHosting
            //client.OnRaidNotification += OnRaidNotification;
            //client.OnReconnected += TwitchOnReconnect;
            //client.OnSendReceiveData
            //client.OnUserBanned += TwitchOnUserBanned;
            //client.OnUserJoined += TwitchOnUserJoined;
            //client.OnUserLeft += TwitchOnUserLeft;
            //client.OnUserStateChanged
            //client.OnUserTimedout
            //client.OnVIPsReceived
            //client.OnWhisperCommandReceived
            //client.OnWhisperReceived += TwitchWhisperReceived;
            //client.OnWhisperSent
            //client.OnWhisperThrottled += TwitchOnWhisperThrottled;

            //client.OnCommunitySubscription += TwitchOnCommunitySubscription;
            //client.OnGiftedSubscription += TwitchOnGiftedSubscription;
            //client.OnNewSubscriber += TwitchOnNewSubscriber;
            //client.OnReSubscriber += TwitchOnReSubscriber;
        }

    

        private async Task TwitchOnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            //if(e.Command.ChatMessage.IsMe){return;}
            UserEntry usr = await Program.Users.GetUserByTwitchUserName(e.ChatMessage.Username);
            if (usr == null) return;

            Program.BotEvents.RaiseOnCommandReceived(new BotWideCommandArguments()
            {
                source = MESSAGESOURCE.TWITCH,
                channel = e.ChatMessage.Channel,
                isBroadcaster = e.ChatMessage.IsBroadcaster,
                isModerator = e.ChatMessage.UserType == TwitchLib.Client.Enums.UserType.Moderator,
                user = usr,
                userDisplayName = e.ChatMessage.DisplayName,
                command = e.Command.Name.ToLower(),
                message = e.ChatMessage.Message,
                arguments = e.Command.ArgumentsAsList
            });
        }

        private async void TwitchOnMessageCleared(object sender, OnMessageClearedArgs e)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(e.Channel);
            if (bChan != null)
            {
                Program.BotEvents.RaiseOnTwitchMessageCleared(bChan, e);
            }
        }

        private async void TwitchOnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(e.Channel);
            Program.BotEvents.RaiseTwitchOnSubGift(bChan, new TwitchSubGiftEventArguments(e));
        }
/*
        private async void TwitchChannelStateChanged(object sender, OnChannelStateChangedArgs e)
        {
             Structure Example
                "ChannelState": {
                    "BroadcasterLanguage": null,
                    "Channel": "munglo",
                    "EmoteOnly": false,
                    "FollowersOnly": null,
                    "Mercury": false,
                    "R9K": false,
                    "Rituals": false,
                    "RoomId": "42172284",
                    "SlowMode": 0,
                    "SubOnly": false
                    },
                "Channel": "munglo"
            
        }
*/


        private async Task TwitchOnError(object sender, OnErrorEventArgs e)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "EventCatcherTwitch",
                $"Twitch Error! {e.Exception.Message}"));
        }

        private async void TwitchOnMessageSent(object sender, OnMessageSentArgs e)
        {
            await Program.BotEvents.RaiseOnTwitchMessageSent(e.SentMessage.Channel, e.SentMessage.Message);
        }

        private async Task TwitchOnIncorrectLogin(object sender, OnIncorrectLoginArgs e)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "EventCatcherTwitch",
                $"Twitch login failed! {e.Exception.Message}"));
        }


        private async Task TwitchOnMessageThrottled(object sender, TwitchLib.Client.Events.OnMessageThrottledArgs e)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, "EventCatcherTwitch", $"Twitch Message Throttled!! {e.AllowedInPeriod}:Allowed {e.Period}:Period"));
        }

        //private void TwitchOnWhisperThrottled(object sender, OnWhisperThrottledEventArgs e)
        //{
        //    Core.LOG(new LogEntry(LOGSEVERITY.WARNING, "EventCatcherTwitch", $"Twitch Whisper Throttled!! {e.AllowedInPeriod}:Allowed {e.Period}:Period {e.SentWhisperCount}:sent count"));
        //}

        private async void TwitchWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            await Program.BotEvents.RaiseTwitchWhisperReceived(e.WhisperMessage.Username, e.WhisperMessage.Message);
        }

        private async void TwitchOnCommunitySubscription(object sender, OnCommunitySubscriptionArgs e)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(e.Channel);
            //await Program.BotEvents.RaiseTwitchOnCommunitySubscription(bChan, e.GiftedSubscription.SystemMsgParsed);
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
        private async Task TwitchOnExistingUsersDetected(object sender, OnExistingUsersDetectedArgs e)
        {
            TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse userList = await Program.Users.GetUsersByTwitchUsernamesFromAPI(e.Users);
            foreach (TwitchLib.Api.Helix.Models.Users.GetUsers.User user in userList.Users)
            {
                string twitchID = user.Id;
            }
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EventCatcherTwitch", $"TwitchOnExistingUsersDetected with {userList.Users.Length}"));
        }
        private async void TwitchOnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(e.Channel);
            await Program.BotEvents.RaiseTwitchOnNewSubscriber(bChan, new TwitchNewSubArguments(e));
        }
        private async void TwitchOnReSubscriber(object sender, OnReSubscriberArgs e)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(e.Channel);
            await Program.BotEvents.RaiseTwitchOnReSubscriber(bChan, new TwitchReSubArguments(e));
        }
        private async void TwitchOnUserLeft(object sender, OnUserLeftArgs e)
        {
            UserEntry user = await Program.Users.GetUserByTwitchUserName(e.Username);
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(e.Channel);
            if (user != null)
            {
                await Program.BotEvents.RaiseTwitchOnUserLeave(bChan, user);
                return;
            }
        }
        private async void TwitchOnUserJoined(object sender, OnUserJoinedArgs e)
        {
            UserEntry user = await Program.Users.GetUserByTwitchUserName(e.Username);
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(e.Channel);
            if (user != null)
            {
                await Program.BotEvents.RaiseTwitchOnUserJoin(bChan, user);
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
        private async Task TwitchOnConnected(object sender, TwitchLib.Client.Events.OnConnectedEventArgs e)
        {
            await Core.LogInfo("EventCatcherTwitch", $"TwitchOnConnected as [{e.BotUsername}]");
            await Program.BotEvents.RaiseOnTwitchConnected();
        }
        /// <summary>
        /// Fires when twitch client gets an error I guess
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task TwitchOnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            await Program.BotEvents.RaiseOnTwitchConnectionError(e.ToString());
        }
        /// <summary>
        ///  Fires when Twitch client is disconnected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task OnDisconnected(object sender, OnDisconnectedArgs e)
        {
            await Program.BotEvents.RaiseOnTwitchDisconnected(e.ToString());
        }
        /// <summary>
        /// Reformats any message received and raises the botWide event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            UserEntry usr = await Program.Users.GetUserByTwitchUserName(e.ChatMessage.Username);
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
        /// Reformats and raise a botWide event when a twitch user is banned
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
                "NotSet",
                true
                );
            Program.BotEvents.RaiseBanEvent(banEvent);
        }
        #endregion

    }// EOC
}// EON