using System;
using Discord.WebSocket;
using MisfitBot_MKII;

namespace AdminPlugin
{
    public class AdminPlugin : PluginBase
    {
        public AdminPlugin()
        {
            Program.BotEvents.OnMessageReceived += OnMessageReceived;
            Program.BotEvents.OnTwitchConnected += OnTwitchConnected;
            Program.BotEvents.OnDiscordConnected += OnDiscordConnected;
            Program.BotEvents.OnCommandReceived += OnCommandRecieved;
            //Program.BotEvents.OnDiscordGuildAvailable += OnDiscordGuildAvailable;
            Core.LOG(new LogEntry(LOGSEVERITY.INFO,
            "PLUGIN",
            "AdminPlugin loaded."));
        }

        private async void OnCommandRecieved(BotWideCommandArguments args)
        {
            BotChannel bChan = await GetBotChannel(args);
            BotWideResponseArguments response = new BotWideResponseArguments(args);

            if(args.isBroadcaster || args.isModerator || args.canManageMessages) {
                
                switch (args.command)
                {
                    case "twitch":
                        // anything twitch has to go through discord
                        if(args.source != MESSAGESOURCE.DISCORD) {return;}
                        // Clean command response
                        if(args.arguments.Count == 0){
                            if(bChan.TwitchChannelName == string.Empty){
                                response.message = $"There is no twitch channel tied to this Discord.";
                            }else{
                                response.message = $"Currently this Discord is tied to the Twitch channel \"{bChan.TwitchChannelName}\"";
                            }
                            Respond(bChan, response);
                            return;
                        }
                        switch (args.arguments[0])
                        {
                            case "channel":
                                if(args.arguments.Count == 2){
                                    TwitchLib.Api.V5.Models.Users.Users users = await Program.TwitchAPI.V5.Users.GetUserByNameAsync(args.arguments[1].ToLower());
                                    if(users.Matches.Length != 1){
                                        // Failed to look up twitch channel so notify and exit
                                        response.message = "Sorry. Could not find that channel. Make sure you enter it correctly and try again.";
                                        Respond(bChan, response);
                                        return;
                                    }
                                    bChan.TwitchChannelName = args.arguments[1].ToLower();
                                    bChan.TwitchAutojoin = true;
                                    response.message = $"This Discord is now tied to the Twitch channel \"{bChan.TwitchChannelName}\"";
                                    Program.Channels.ChannelSave(bChan);
                                    await Program.Channels.JoinAllAutoJoinTwitchChannels();
                                    Respond(bChan, response);
                                }
                                break;
                        }
                    break;
                    default:
                    break;
                }

            }
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
                    await Program.DiscordSayMessage(args.channel, "PONG! arrrgh");
                }
            }
        }
    }
}
