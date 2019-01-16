using System;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace MisfitBot2.Extensions.ChannelManager
{
    public class TwPubSub
    {
        private TwitchPubSub Client;
        private int _lastViewerCount = 0;
        private readonly string EXTENSIONNAME = "TwPubSub";
        private readonly string _oauth;
        private readonly string _twitchID;
        private readonly string _twitchChannelName;
        public TwPubSub(string OAuth, string twitchID, string twitchChannelName)
        {
            _oauth = OAuth;
            _twitchID = twitchID;
            _twitchChannelName = twitchChannelName;
            Client = new TwitchPubSub();
            #region untested
            Client.OnBan += Client_OnBan;
            Client.OnChannelCommerceReceived += Client_OnChannelCommerceReceived;
            Client.OnChannelExtensionBroadcast += Client_OnChannelExtensionBroadcast;
            Client.OnClear += Client_OnClear;
            Client.OnEmoteOnly += Client_OnEmoteOnly;
            Client.OnEmoteOnlyOff += Client_OnEmoteOnlyOff;
            Client.OnFollow += Client_OnFollow;
            Client.OnPubSubServiceClosed += OnPubSubServiceClosed;
            Client.OnPubSubServiceError += OnPubSubServiceError;
            Client.OnR9kBeta += Client_OnR9kBeta;
            Client.OnR9kBetaOff += Client_OnR9kBetaOff;
            Client.OnSubscribersOnly += Client_OnSubscribersOnly;
            Client.OnSubscribersOnlyOff += Client_OnSubscribersOnlyOff;
            Client.OnTimeout += Client_OnTimeout;
            Client.OnUnban += Client_OnUnban;
            Client.OnUntimeout += Client_OnUntimeout;
            Client.OnWhisper += Client_OnWhisper;
            #endregion
            #region works
            Client.OnBitsReceived += OnBitsReceived;
            Client.OnViewCount += OnViewCount;
            Client.OnChannelSubscription += OnChannelSubscription;
            Client.OnHost += Client_OnHost;
            Client.OnStreamUp += Client_OnStreamUp;
            Client.OnStreamDown += Client_OnStreamDown;
            Client.OnPubSubServiceConnected += OnPubSubServiceConnected;
            Client.OnListenResponse += OnListenResponse;
            Connect();
            #endregion
        }
        public void Close()
        {
            Client.Disconnect();
        }
        public void Connect()
        {
            Client.ListenToBitsEvents(_twitchID);
            Client.ListenToSubscriptions(_twitchID);
            Client.ListenToFollows(_twitchID);
            Client.ListenToVideoPlayback(_twitchChannelName);
            Client.ListenToChatModeratorActions(_twitchID, _twitchID);
            Client.Connect();
        }
        #region untested
        private void Client_OnWhisper(object sender, OnWhisperArgs e)
        {
            Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"{_twitchChannelName} :: OnWhisper. {e.Whisper.ToString()}"
                ));
        }
        private void Client_OnR9kBetaOff(object sender, OnR9kBetaOffArgs e)
        {
            Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"{_twitchChannelName} :: OnR9kBetaOff."
                ));
        }
        private void Client_OnR9kBeta(object sender, OnR9kBetaArgs e)
        {
            Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"{_twitchChannelName} :: OnR9kBeta."
                ));
        }
        private async void OnPubSubServiceClosed(object sender, EventArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(_twitchChannelName);
            if (bChan != null)
            {
                if (bChan.discordAdminChannel != 0)
                {
                    await(Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                        $"PubSub Service CLOSED!!"
                        );
                }
            }
        }
        private void Client_OnEmoteOnlyOff(object sender, OnEmoteOnlyOffArgs e)
        {
            Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"{_twitchChannelName} :: OnEmoteOnlyOff."
                ));
        }
        private void Client_OnEmoteOnly(object sender, OnEmoteOnlyArgs e)
        {
            Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"{_twitchChannelName} :: OnEmoteOnly."
                ));
        }
        private void Client_OnChannelExtensionBroadcast(object sender, OnChannelExtensionBroadcastArgs e)
        {
            Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"{_twitchChannelName} :: OnChannelExtensionBroadcast."
                ));
        }
        private void Client_OnChannelCommerceReceived(object sender, OnChannelCommerceReceivedArgs e)
        {
            Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"{_twitchChannelName} :: OnChannelCommerceReceived."
                ));
        }
        #endregion

        #region Works fine
        private async void Client_OnUnban(object sender, OnUnbanArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(_twitchChannelName);
            UserEntry mod = await Core.UserMan.GetUserByTwitchID(e.UnbannedByUserId);
            UserEntry BannedUser = await Core.UserMan.GetUserByTwitchID(e.UnbannedUserId);
            if (BannedUser != null)
            {
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                    $"{_twitchChannelName} :: {e.UnbannedBy} removed ban on {BannedUser._twitchDisplayname}"
                    ));
            }

            UnBanEventArguments unbanEvent = new UnBanEventArguments(
                bChan,
                mod,
                BannedUser,
                Core.CurrentTime,
                true
                );
            Core.RaiseUnBanEvent(unbanEvent);
        }
        private async void Client_OnBan(object sender, OnBanArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(_twitchChannelName);
            UserEntry mod = await Core.UserMan.GetUserByTwitchID(e.BannedByUserId);
            UserEntry BannedUser = await Core.UserMan.GetUserByTwitchID(e.BannedUserId);
            if (BannedUser != null)
            {
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                    $"{_twitchChannelName} :: {e.BannedBy} banned {BannedUser._twitchDisplayname} for \"{e.BanReason}\""
                    ));
            }

            BanEventArguments banEvent = new BanEventArguments(
                bChan,
                mod,
                BannedUser,
                Core.CurrentTime,
                0,
                e.BanReason,
                true
                );
            Core.RaiseBanEvent(banEvent);

        }
        private async void Client_OnUntimeout(object sender, OnUntimeoutArgs e)
        {
            UserEntry user = await Core.UserMan.GetUserByTwitchID(e.UntimeoutedUserId);
            if (user != null)
            {
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                    $"{_twitchChannelName} :: {e.UntimeoutedBy} removed timeout on {user._twitchDisplayname}"
                    ));
            }
        }
        private async void Client_OnTimeout(object sender, OnTimeoutArgs e)
        {
            UserEntry user = await Core.UserMan.GetUserByTwitchID(e.TimedoutUserId);
            if (user != null)
            {
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                    $"{_twitchChannelName} :: {e.TimedoutBy} timedout {user._twitchDisplayname} for {e.TimeoutDuration} because \"{e.TimeoutReason}\""
                    ));
            }
        }
        private void Client_OnSubscribersOnlyOff(object sender, OnSubscribersOnlyOffArgs e)
        {
            Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"{_twitchChannelName} :: OnSubscribersOnlyOff."
                ));
        }
        private void Client_OnSubscribersOnly(object sender, OnSubscribersOnlyArgs e)
        {
            Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"{_twitchChannelName} :: OnSubscribersOnly."
                ));
        }
        private void Client_OnClear(object sender, OnClearArgs e)
        {
            Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"{_twitchChannelName} :: OnClear."
                ));
        }
        private async void OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(_twitchChannelName);
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, EXTENSIONNAME,
                $"{_twitchChannelName} :: OnPubSubServiceError."
                ));
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, EXTENSIONNAME,
                $"{e?.Exception.Message}"
                ));
            if(bChan.discordAdminChannel != 0)
            {
                await (Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                    $"PubSub ERROR: Channel({_twitchChannelName}) {e?.Exception.Message}"
                    );
            }
        }
        private void OnViewCount(object sender, OnViewCountArgs e)
        {
            if (_lastViewerCount != e.Viewers)
            {
                _lastViewerCount = e.Viewers;
                //Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                //    $"OnViewCount. {e.Viewers} :: {e.Viewers} viewers."
                //    ));
            }
        }
        private async void OnChannelSubscription(object sender, OnChannelSubscriptionArgs e)
        {
            if(e.Subscription.RecipientId == null)
            {
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, "RecipientId is NULL"));
            }
            // DEBUG Save the output of this as an example somewhere on Trello
            /*await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, "--------------------------"));
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, $"ChannelId : {e.Subscription.ChannelId}"));
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, $"ChannelName : {e.Subscription.ChannelName}"));
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, $"Context : {e.Subscription.Context}"));
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, $"RecipientDisplayName : {e.Subscription.RecipientDisplayName}"));
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, $"RecipientId : {e.Subscription.RecipientId}"));
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, $"RecipientName : {e.Subscription.RecipientName}"));
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, $"SubMessage : {e.Subscription.SubMessage}"));
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, $"SubscriptionPlan : {e.Subscription.SubscriptionPlan}"));
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, $"SubscriptionPlanName : {e.Subscription.SubscriptionPlanName}"));
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, $"Time : {e.Subscription.Time}"));
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, $"UserId : {e.Subscription.UserId}"));
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, $"Username : {e.Subscription.Username}"));
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, "--------------------------"));
            */

            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.Subscription.ChannelName);

            if (e.Subscription.RecipientName == null) // TODO this need verification
            {
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                    $"{_twitchChannelName} :: {e.Subscription.Username} subscribed for {e.Subscription.Months} months"
                    ));
                if (bChan.discordAdminChannel != 0)
                {
                    await (Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                        $"{e.Subscription.Username} subscribed for {e.Subscription.Months}. \"{e.Subscription.SubMessage.Message}\""
                        );
                }
            }
            else
            {
                string msg = $"{_twitchChannelName} :: {e.Subscription.DisplayName} gifted {e.Subscription.RecipientDisplayName} " +
                    $"a {e.Subscription.SubscriptionPlanName} Sub to ";
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, msg));
                if (bChan == null) { return; }
                if (bChan.discordAdminChannel != 0)
                {
                    await (Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(msg);
                }
            }
            /*UserEntry user = null;
            if (e.Subscription.UserId != null && e.Subscription.UserId != string.Empty)
            {
                await Core.UserMan.GetUserByTwitchID(e.Subscription.UserId);
            }
            if (user == null) { return; }

            UserEntry recipient = null;
            if (e.Subscription.RecipientId != null && e.Subscription.RecipientId != string.Empty)
            {
                recipient = await Core.UserMan.GetUserByTwitchID(e.Subscription.RecipientId);
            }*/


        }
        private void Client_OnHost(object sender, OnHostArgs e)
        {
            Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"{e.HostedChannel} hosted by {e.Moderator}"
                ));
        }
        private async void Client_OnFollow(object sender, OnFollowArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(_twitchChannelName);
            UserEntry user = await Core.UserMan.GetUserByTwitchID(e.UserId);
            if (bChan != null && user != null)
            {
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, $"New twitch follower:{e.DisplayName}"));
                if (bChan.discordAdminChannel != 0)
                {
                    await (Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                        $"Hey we got a new Twitch follower! {e.DisplayName}");
                }
            }
        }

        private async void OnPubSubServiceConnected(object sender, EventArgs e)
        {
            // SendTopics accepts an oauth optionally, which is necessary for some topics
            Client.SendTopics(_oauth);
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"PubSub connected for {_twitchChannelName}."
                ));
            /*BotChannel bChan = Core.Channels._botChannels.GetTwitchChannelByID(_twitchID);
            if (bChan != null)
            {
                if (bChan.discordAdminChannel != 0)
                {
                    await(Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                        $"PubSub Service connected."
                        );
                }
            }*/
        }
        private async void OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (!e.Successful)
            {
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"Failed to listen to {e.Topic}! Response: {e.Response.Error}"
                ));
                BotChannel bChan = await Core.Channels.GetTwitchChannelByName(_twitchChannelName);
                if (bChan != null)
                {
                    if (bChan.discordAdminChannel != 0)
                    {
                        await(Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                            $"Failed to listen to {e.Topic}. Response: {e.Response.Error}"
                            );
                    }
                }
            }
            else
            {
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                    $"Listening to {e.Topic} for {_twitchChannelName}."
                    ));
            }
        }

        private async void OnBitsReceived(object sender, OnBitsReceivedArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(_twitchChannelName);
            UserEntry user = await Core.UserMan.GetUserByTwitchID(e.UserId);
            if (user != null && bChan != null)
            {

                BitEventArguments bitEvent = new BitEventArguments(
                    bChan,
                    user,
                    Core.CurrentTime,
                    e.BitsUsed,
                    e.TotalBitsUsed,
                    e.Context,
                    e.ChatMessage
                );
                Core.RaiseBitEvent(bitEvent);
            }
        }
        private async void Client_OnStreamDown(object sender, OnStreamDownArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(_twitchChannelName);
            if (bChan == null) { return; }
            if (bChan.discordAdminChannel != 0)
            {
                await(Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                    $"{_twitchChannelName} went offline."
                    );
                return;
            }
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"{_twitchChannelName} went offline."
                ));
        }
        private async void Client_OnStreamUp(object sender, OnStreamUpArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(_twitchChannelName);
            if (bChan == null) { return; }
            if (bChan.discordAdminChannel != 0)
            {
                await(Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                    $"{_twitchChannelName} went live. {e.PlayDelay}s delay."
                    );
            }
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
               $"{_twitchChannelName} went live. {e.PlayDelay}s delay."
                ));
            Core.RaiseOnBotChannelGoesLive(bChan);
        }
        #endregion
    }
}
