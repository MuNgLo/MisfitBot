using System;
using System.Collections.Generic;
using System.Text;
using TwitchLib.Client.Interfaces;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Events;
using TwitchLib.Api;
using TwitchLib.Client.Models;
using TwitchLib.Client;
using MisfitBot_MKII;
using Discord;
using System.Threading.Tasks;
using Discord.WebSocket;
using MisfitBot_MKII.Twitch;
using Microsoft.Extensions.Logging;

namespace MisfitBot_MKII.Services
{
    public class TwitchService
    {
        private readonly string PLUGINNAME = "TwitchService";
        public string _channel = "munglo"; // TODO make this first launch parameter I guess
        public readonly ITwitchAPI _api;
        public readonly ITwitchClient _client;
        public TwitchCredentials _credentials; // TODO make this into adminservice stuff
        public TwitchUsers _twitchUsers;
        // CONSTRUCTOR
        public TwitchService()
        {
            _credentials = new TwitchCredentials();
            _twitchUsers = new TwitchUsers();
            var logger = new KSLogger();
            _api = new TwitchAPI();
            //_api = new TwitchAPI(logger);
            _api.Settings.SkipDynamicScopeValidation = true;
            _api.Settings.ClientId = _credentials._clientid;
            _api.Settings.AccessToken = "";
            ConnectionCredentials cred = new ConnectionCredentials(_credentials._username, _credentials._oauth);
            _client = new TwitchClient();
            _client.Initialize(cred, _channel);
            _client.RemoveChatCommandIdentifier('!');
            _client.AddChatCommandIdentifier(Program.CommandCharacter);
            _client.OnConnected += TwitchOnConnected;
            _client.OnDisconnected += TwitchOnDisconnected;
            _client.OnReSubscriber += TwitchOnReSubscriber;
            _client.OnConnectionError += TwitchOnConnectionError;
            //_client.OnLog += TwitchOnLog;
            _client.OnUserBanned += TwitchOnUserBanned;
            _client.OnUserJoined += TwitchOnUserJoined;
            _client.OnMessageReceived += TwitchOnMessageReceived;
            _client.OnExistingUsersDetected += TwitchOnExistingUsersDetected;
            _client.OnRaidNotification += OnRaidNotification;
            _client.Connect();
            //Core.Twitch = this;
        }// EO CONSTRUCTOR

        /// <summary>
        /// Fires when a raid notification is detected in chat
        /// </summary>
        private async void OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            JsonDumper.DumpObjectToJson(e, "Raid"); // TODO Remove
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(e.Channel);
            int i = 0;
            int.TryParse(e.RaidNotification.MsgParamViewerCount, out i);
            Program.BotEvents.RaiseRaidEvent(bChan, new RaidEventArguments(e.RaidNotification.DisplayName, e.Channel, i));
        }

        private void TwitchOnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            _twitchUsers.TouchUser(e.ChatMessage.Channel, e.ChatMessage.Username, e.ChatMessage.DisplayName);
        }
        private async void TwitchOnUserJoined(object sender, OnUserJoinedArgs e)
        {
            UserEntry user = await Program.Users.GetUserByTwitchUserName(e.Username);
            if(user == null)
            {
                _twitchUsers.TouchUser(e.Channel, e.Username, e.Username);
                return;
            }
            _twitchUsers.TouchUser(e.Channel, e.Username, user._twitchDisplayname);
        }
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
        private async void TwitchOnLog(object sender, OnLogArgs e)
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, PLUGINNAME, $"LOG:{e.Data}"));
        }
        private async void TwitchOnReSubscriber(object sender, OnReSubscriberArgs e)
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, "TwitchService", $"{e.ReSubscriber.DisplayName} resubscribed to channel {e.Channel}."));
        }

        #region Connection related events
        private async void TwitchOnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            await Core.LOG(new LogMessage(LogSeverity.Error, "TwitchService", $"Connection Error!! {e.Error.Message}."));
        }
        private async void TwitchOnConnected(object sender, OnConnectedArgs e)
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, "TwitchService", "Twitch Connected"));
            await Program.Channels.JoinAutojoinChannels();
        }
        private async void TwitchOnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            await Core.LOG(new LogMessage(LogSeverity.Error, "TwitchService", $"Disconnected from Twitch. Reconnecting... :: {e}"));
            _client.Reconnect();
        }
        #endregion

        /// <summary>
        /// UNTESTED!!!!! TODO TEST THIS YOU LUMP OF SHIT
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TwitchOnExistingUsersDetected(object sender, OnExistingUsersDetectedArgs e)
        {
            TwitchLib.Api.V5.Models.Users.Users users = await _api.V5.Users.GetUsersByNameAsync(e.Users);
            foreach (TwitchLib.Api.V5.Models.Users.User user in users.Matches)
            {
                string twitchID = user.Id;
                _twitchUsers.TouchUser(e.Channel, user.Name, user.DisplayName);
                await Core.LOG(new LogMessage(LogSeverity.Info, "TwitchService", $"TwitchOnExistingUsersDetected :: {user.DisplayName} id:{user.Id}"));
            }
        }


    }


    
}
