using System;
//using Discord.WebSocket;
using MisfitBot_MKII;
using MisfitBot_MKII.Statics;
using MisfitBot_MKII.DiscordWrap;

namespace AdminPlugin
{
    // Invite link https://discordapp.com/oauth2/authorize?client_id=295257486708047882&scope=bot&permissions=0
    public class AdminPlugin : PluginBase
    {
        public AdminPlugin():base("AdminPlugin", 0)
        {
            Program.BotEvents.OnMessageReceived += OnMessageReceived;
            Program.BotEvents.OnTwitchConnected += OnTwitchConnected;
            Program.BotEvents.OnDiscordConnected += OnDiscordConnected;
            Program.BotEvents.OnCommandReceived += OnCommandRecieved;
            //Program.BotEvents.OnDiscordGuildAvailable += OnDiscordGuildAvailable;

            Program.BotEvents.OnTwitchFollow += OnTwitchNewFollower;
            Program.BotEvents.OnRaidEvent += OnTwitchRaid;
            Program.BotEvents.OnTwitchCommunitySubscription += OnTwitchCommunitySubscription;
            Program.BotEvents.OnTwitchSubGift += OnTwitchSubGift;
            Program.BotEvents.OnTwitchReSubscriber += OnTwitchResub;
            Program.BotEvents.OnTwitchNewSubscriber += OnTwitchNewSub;
        }

        #region Listeners to announce shiite
        #region Twitch Subscription methods
        private async void OnTwitchCommunitySubscription(BotChannel bChan, string message)
        {
            if(bChan.discordAdminChannel > 0){
                await SayOnDiscordAdmin(bChan, message);
            }
        }
        private async void OnTwitchSubGift(BotChannel bChan, TwitchSubGiftEventArguments e)
        {
            if(bChan.discordAdminChannel > 0){
                await SayOnDiscordAdmin(bChan, $"{e.userDisplayname} gave {e.recipientDisplayname} a {e.subscriptionplanName}.");
            }
        }
        private async void OnTwitchResub(BotChannel bChan, TwitchReSubArguments args)
        {
           if(bChan.discordAdminChannel > 0){
                await SayOnDiscordAdmin(bChan, $"{args.userDisplayname} resubbed for a total of {args.monthsTotal} months and a streak of {args.monthsStreak} months.");
            }
        }
        private async void OnTwitchNewSub(BotChannel bChan, TwitchNewSubArguments e)
        {
            if(bChan.discordAdminChannel > 0){
                await SayOnDiscordAdmin(bChan, $"{e.userDisplayname} subscribed for the first time.");
            }
        }
        #endregion
        private async void OnTwitchNewFollower(BotChannel bChan, UserEntry user){
            if(bChan.discordAdminChannel >0){
                await SayOnDiscordAdmin(bChan, $"Hey we got a new Twitch follower! {user._twitchDisplayname}");
            }
        }
        private async void OnTwitchRaid(BotChannel bChan, RaidEventArguments e)
        {
            if(bChan.discordAdminChannel >0){
                await SayOnDiscordAdmin(bChan, $"RAID!! {e.SourceChannel} coming in with {e.RaiderCount} raiders.");
            }
        }
        #endregion

        private async void OnCommandRecieved(BotWideCommandArguments args)
        {
            BotChannel bChan = await GetBotChannel(args);
            BotWideResponseArguments response = new BotWideResponseArguments(args);

            // TEMPORARY this should later move to a better suited plugin
            if(args.command == "juanage")
            {
            


                response.message = JuanAge();
                Respond(bChan, response);
                return;
            }


            if (args.isBroadcaster || args.isModerator || args.canManageMessages)
            {

                switch (args.command)
                {
                    case "twitch":
                        // anything twitch has to go through discord
                        if (args.source != MESSAGESOURCE.DISCORD) { return; }
                        // Clean command response
                        if (args.arguments.Count == 0)
                        {
                            if (bChan.TwitchChannelName == string.Empty)
                            {
                                response.message = $"There is no twitch channel tied to this Discord. Use \"{CMC}twitch channel <NameOfTwitchChannel>\" to tie a channel to this Discord.";
                            }
                            else
                            {
                                response.message = $"Currently this Discord is tied to the Twitch channel \"{bChan.TwitchChannelName}\"";
                            }
                            Respond(bChan, response);
                            return;
                        }
                        switch (args.arguments[0])
                        {
                            case "channel":
                                if (args.arguments.Count == 2)
                                {
                                    TwitchLib.Api.V5.Models.Users.Users users = await Program.TwitchAPI.V5.Users.GetUserByNameAsync(args.arguments[1].ToLower());
                                    if (users.Matches.Length != 1)
                                    {
                                        // Failed to look up twitch channel so notify and exit
                                        response.message = "Sorry. Could not find that channel. Make sure you enter it correctly and try again.";
                                        Respond(bChan, response);
                                        return;
                                    }
                                    bChan.TwitchChannelName = args.arguments[1].ToLower();
                                    bChan.TwitchChannelID = users.Matches[0].Id;
                                    bChan.TwitchAutojoin = true;
                                    response.message = $"This Discord is now tied to the Twitch channel \"{bChan.TwitchChannelName}\".";
                                    Program.Channels.ChannelSave(bChan);
                                    if(Program.TwitchConnected){
                                        await Program.Channels.JoinAllAutoJoinTwitchChannels();
                                    }else{
                                        response.message += " Not connected to Twitch so can't join the channel right now.";
                                    }
                                    Respond(bChan, response);
                                }
                                break;
                        }
                        break;
                    case "pubsub":
                        // has to go through discord
                        if (args.source != MESSAGESOURCE.DISCORD) { return; }
                        // Clean command response
                        if (args.arguments.Count == 0 || args.arguments[0] == "help")
                        {
                            response.message = PubSubHelpDump(bChan);
                            Respond(bChan, response);
                            return;
                        }
                        switch (args.arguments[0])
                        {
                            case "settoken":
                            if(args.arguments.Count == 2){
                                bChan.pubsubOauth = Cipher.Encrypt(args.arguments[1]);
                                Program.Channels.ChannelSave(bChan);
                                response.message = "Token set. Engaging PubSub Connection!";
                                Program.PubSubStart(bChan);
                                Program.DiscordRemoveMessage(Core.StringToUlong(args.channel), args.messageID);
                            }else{
                                response.message = "Did you forget the token?";
                            }
                                Respond(bChan, response);
                                return;
                            case "cleartoken":
                                bChan.pubsubOauth = string.Empty;
                                Program.Channels.ChannelSave(bChan);
                                response.message = "ClearToken";
                                Program.PubSubStop(bChan);
                                Respond(bChan, response);
                                return;
                            case "start":
                                Program.PubSubStart(bChan);
                                return;
                            case "stop":
                                Program.PubSubStop(bChan);
                                return;
                            /*case "status":
                                response.message = Program.PubSubStatus(bChan);
                                Respond(bChan, response);
                                return;
                                */
                        }
                        break;
                        case "setadminchannel":
                            bChan.discordAdminChannel = Core.StringToUlong( args.channel);
                            Program.Channels.ChannelSave(bChan);
                            response.message = $"This is now set as the default adminchannel for this DiscordServer. This is needed to direct some important messages and notifications";
                            Respond(bChan, response);
                        return;
                }

            }
        }

        private string PubSubHelpDump(BotChannel bChan)
        {
            string twitchCheck = $"Currently this Discord is tied to the Twitch channel \"{bChan.TwitchChannelName}\"";
            if (bChan.TwitchChannelName == string.Empty)
            {
                twitchCheck = $"There is no twitch channel tied to this Discord. Use {CMC}twitch channel <CHANNELNAME> to set a channel.";
            }
            else
            {
            }
            string msg = $"```fix{System.Environment.NewLine}PubSub is how connections are made to Twitch to listen to events. This needs a valid token to work.{System.Environment.NewLine}" +
            $"Follow this link https://twitchtokengenerator.com/quick/YfuRoOx9WW to grab a token tied to your twitch account. It will only work on your channel and can be canceled through your Twitch settings page under Connections.{System.Environment.NewLine}" +
            $"Make sure to never share the Token with anyone.{System.Environment.NewLine}" +
            $"{System.Environment.NewLine}" +
            $"{twitchCheck}{System.Environment.NewLine}" +
            $"{System.Environment.NewLine}" +
            $"{CMC}pubsub is the base command.{System.Environment.NewLine}" +
            $"{System.Environment.NewLine}" +
            $"Arguments{System.Environment.NewLine}" +
            $"settoken <TOKEN> Use this to set the Token for the channel.{System.Environment.NewLine}" +
            $"cleartoken This removes any Token and shutsdown any running pubsub connection for the channel.{System.Environment.NewLine}";




            msg += "```";
            return msg;
        }





        /* Implement this in admin plugin   
        private async void OnDiscordGuildAvailable(SocketGuild arg)
        {
            var user = arg.GetUser(Program.DiscordClient.CurrentUser.Id);
            await user.ModifyAsync(
                x=>{
                    x.Nickname = Program.TwitchClient.TwitchUsername;
                }

            );
        }
        */

        private void OnDiscordConnected()
        {

        }

        private void OnTwitchConnected(string msg)
        {

        }

        private async void OnMessageReceived(BotWideMessageArguments args)
        {
            if (args.message.ToLower() == "!aping")
            {
                if (args.source == MESSAGESOURCE.TWITCH)
                {
                    Program.TwitchSayMessage(args.channel, "PONG! Soffa");
                }
                if (args.source == MESSAGESOURCE.DISCORD)
                {
                    await DiscordClient.DiscordSayMessage(args.channelID, "PONG! arrrgh");
                }
            }
        }
        public override void OnSecondTick(int seconds)
        {
            throw new NotImplementedException();
        }
        public override void OnMinuteTick(int minutes)
        {
            throw new NotImplementedException();
        }
        public override void OnUserEntryMergeEvent(UserEntry discordUser, UserEntry twitchUser)
        {
            throw new NotImplementedException();
        }
        public override void OnBotChannelEntryMergeEvent(BotChannel discordGuild, BotChannel twitchChannel)
        {
            throw new NotImplementedException();
        }

        private string JuanAge()
        {
            TimeSpan time = TimeSpan.FromSeconds(Core.UpTime);

            //here backslash is must to tell that colon is
            //not the part of format, it just a character that we want in output
            //string str = time.ToString(@"dd\d\a\y\s\ \ hh\:mm\:ss");
            string str = time.ToString(@"d\ \d\a\y\s\ \a\n\d\ \ hh\:mm\:ss");
            //seconds -= minutes * 60;
            //minutes -= hours * 60;
            return $"I have been running for {str}";
        }
    }// EOF CLASS
}
