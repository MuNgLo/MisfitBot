using System;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace MisfitBot_MKII.Extensions.ChannelManager
{
    /// <summary>
    /// TwitchLib PubSub wrapper. Connects on creation.
    /// </summary>
    public class TwPubSub
    {
        private TwitchPubSub Client;
        private readonly string EXTENSIONNAME = "TwPubSub";
        private readonly string _oauth;
        private readonly string _twitchID;
        private readonly string _twitchChannelName;
        private bool _verbose = false;
        private bool _closing = false;

        public TwPubSub(string OAuth, string twitchID, string twitchChannelName, bool verbose)
        {
            _oauth = OAuth;
            _twitchID = twitchID;
            _twitchChannelName = twitchChannelName;
            _verbose = verbose;
            Client = new TwitchPubSub();

            Client.OnLog += OnLog;

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
            Client.OnHost += Client_OnHost; // Fires when PubSub receives notice that the channel being listened to is hosting another channel.
            Client.OnStreamUp += Client_OnStreamUp;
            Client.OnStreamDown += Client_OnStreamDown;
            Client.OnPubSubServiceConnected += OnPubSubServiceConnected;
            Client.OnListenResponse += OnListenResponse;

            //Client.ListenToChannelExtensionBroadcast
            //Client.ListenToCommerce

            Connect();
            #endregion
        }

        private void OnLog(object sender, OnLogArgs e)
        {
            Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, "PUBSUB", $"Exception : {e.Data}"));
        }

        public void Close()
        {
            _closing = true;
            Client.Disconnect();
        }
        public async void Connect()
        {
            UserEntry botUser = await Program.Users.GetUserByTwitchUserName("juanthebot");
            int botClientID = -1;
            int.TryParse(botUser._twitchUID, out botClientID);

            Client.ListenToFollows(_twitchID);// Don't need OAUTH
            Client.ListenToVideoPlayback(_twitchChannelName);// Don't need OAUTH
            Client.ListenToSubscriptions(_twitchID);// Don't need OAUTH
            Client.ListenToBitsEvents(_twitchID);// Don't need OAUTH
            
            //Client.ListenToChannelExtensionBroadcast(_twitchID, _twitchID);// Needs OAUTH
            Client.ListenToChatModeratorActions("8ku2p031bud14h8lbvj5jevqc2gyi3", _twitchID);// Needs OAUTH
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
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(_twitchChannelName);
            if (bChan != null)
            {
                if (bChan.discordAdminChannel != 0)
                {
                    await(Program.DiscordClient.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
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
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(_twitchChannelName);
            UserEntry mod = await Program.Users.GetUserByTwitchID(e.UnbannedByUserId);
            UserEntry BannedUser = await Program.Users.GetUserByTwitchID(e.UnbannedUserId);
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
            Program.BotEvents.RaiseUnBanEvent(unbanEvent);
        }
        private async void Client_OnBan(object sender, OnBanArgs e)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(_twitchChannelName);
            UserEntry mod = await Program.Users.GetUserByTwitchID(e.BannedByUserId);
            UserEntry BannedUser = await Program.Users.GetUserByTwitchID(e.BannedUserId);
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
            Program.BotEvents.RaiseBanEvent(banEvent);
        }
        private async void Client_OnUntimeout(object sender, OnUntimeoutArgs e)
        {
            UserEntry user = await Program.Users.GetUserByTwitchID(e.UntimeoutedUserId);
            if (user != null)
            {
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                    $"{_twitchChannelName} :: {e.UntimeoutedBy} removed timeout on {user._twitchDisplayname}"
                    ));
            }
        }
        private async void Client_OnTimeout(object sender, OnTimeoutArgs e)
        {
            UserEntry user = await Program.Users.GetUserByTwitchID(e.TimedoutUserId);
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
            if (_closing) { return; }
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(_twitchChannelName);
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, EXTENSIONNAME,
                $"OnPubSubServiceError({_twitchChannelName}). {e?.Exception.Message}"
                ));
            if(bChan.discordAdminChannel != 0 && _verbose)
            {
                await (Program.DiscordClient.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                    $"PubSub ERROR: Channel({_twitchChannelName}) {e?.Exception.Message}"
                    );
            }
        }
        private async void OnViewCount(object sender, OnViewCountArgs e)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(_twitchChannelName);
            if (bChan != null)
            {
                if (bChan.viewerCount != e.Viewers)
                {
                    bChan.viewerCount = e.Viewers;
                    Program.Channels.ChannelSave(bChan);
                }
            }
        }
        private async void OnChannelSubscription(object sender, OnChannelSubscriptionArgs e)
        {
            JsonDumper.DumpObjectToJson(e, "TwitchSUB"); // collect a few of these so we know what we are dealing with
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(e.Subscription.ChannelName);
            //Program.BotEvents.RaiseOnTwitchSubscription(bChan, new TwitchSubGiftEventArguments(e)); // TODO HOOK IT UP SILLY
        }
        private async void Client_OnHost(object sender, OnHostArgs e)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByID(_twitchChannelName);
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, "PUBSUB", "Channel hosted another channel. This neds to be hooked up"));
        }
        private async void Client_OnFollow(object sender, OnFollowArgs e)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(_twitchChannelName);
            UserEntry user = await Program.Users.GetUserByTwitchID(e.UserId);
            if (bChan != null && user != null)
            {
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME, $"New twitch follower:{e.DisplayName}"));
                if (bChan.discordAdminChannel != 0)
                {
                    await (Program.DiscordClient.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                        $"Hey we got a new Twitch follower! {e.DisplayName}");
                }
            }
        }

        private async void OnPubSubServiceConnected(object sender, EventArgs e)
        {
            // SendTopics accepts an oauth optionally, which is necessary for some topics
            //Client.SendTopics(Crypto.Cipher.Decrypt(_oauth));
            Client.SendTopics("syfu784cl6x248veumbpccpluo7ur2");
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"PubSub connected for {_twitchChannelName}."
                ));
            BotChannel bChan = await Program.Channels.GetTwitchChannelByID(_twitchID);
            if (bChan != null)
            {
                if (bChan.discordAdminChannel != 0 && Program.DiscordClient != null)
                {
                    if((Program.DiscordClient.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel) == null) { return; }
                    await(Program.DiscordClient.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                        $"PubSub Service connected."
                        ); 
                }
            }
        }
        private async void OnListenResponse(object sender, OnListenResponseArgs e)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(_twitchChannelName);
            if (!e.Successful)
            {

                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, EXTENSIONNAME,
                $"Failed to listen to {e.Topic}! Response: {e.Response.Error}"
                ));
                if (bChan != null)
                {
                    if (bChan.discordAdminChannel != 0)
                    {
                        await (Program.DiscordClient.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
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
                if (_verbose)
                {
                    if (bChan.discordAdminChannel != 0)
                    {
                        // This line crashed once Fuck knows how or why
                        await (Program.DiscordClient.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                                $"Listening to {e.Topic}."
                                );
                    }
                }
            }
        }

        private async void OnBitsReceived(object sender, OnBitsReceivedArgs e)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(_twitchChannelName);
            UserEntry user = await Program.Users.GetUserByTwitchID(e.UserId);
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
                Program.BotEvents.RaiseBitEvent(bitEvent);
            }
        }
        private async void Client_OnStreamDown(object sender, OnStreamDownArgs e)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(_twitchChannelName);
            Program.BotEvents.RaiseOnTwitchChannelGoesOffline(bChan);
        }
        private async void Client_OnStreamUp(object sender, OnStreamUpArgs e)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(_twitchChannelName);
            Program.BotEvents.RaiseOnTwitchChannelGoesLive(bChan,  e.PlayDelay);
        }
        #endregion
    }
}
