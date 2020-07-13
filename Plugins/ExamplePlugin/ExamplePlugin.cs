using System;
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
        }

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
