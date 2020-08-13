using System;
using Discord.WebSocket;
using MisfitBot_MKII;

namespace ExamplePlugin
{
    public class ExamplePlugin : ServiceBase
    {
        public ExamplePlugin()
        {
            Program.BotEvents.OnMessageReceived += OnMessageReceived;
            Program.BotEvents.OnTwitchConnected += OnTwitchConnected;
            Program.BotEvents.OnDiscordConnected += OnDiscordConnected;
            //Program.BotEvents.OnDiscordGuildAvailable += OnDiscordGuildAvailable;
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
            if (args.message.ToLower() == "!ping")
            {
                if (args.source == MESSAGESOURCE.TWITCH)
                {
                    Program.TwitchSayMessage(args.channel, "PONG!");
                }
                if (args.source == MESSAGESOURCE.DISCORD)
                {
                    await Program.DiscordSayMessage(args.channel, "PONG!");
                }
            }
        }
    }
}
