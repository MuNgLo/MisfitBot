using System;
using Discord.WebSocket;
using TwitchLib.Client.Events;

namespace MisfitBot_MKII
{
    public class PingPong
    {

        public PingPong()
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
            Program.TwitchClient.JoinChannel("munglo"); // TODO remove this ffs
        }

        private async void OnMessageReceived(BotWideMessageArguments args)
        {
            if (args.message.ToLower() == "!ping")
            {
                if (args.source == MESSAGESOURCE.TWITCH)
                {
                    Program.TwitchClient.SendMessage(args.channel, "PONG!");
                }
                if (args.source == MESSAGESOURCE.DISCORD)
                {
                    await (Program.DiscordClient.GetChannel(Core.StringToUlong(args.channel)) as ISocketMessageChannel).SendMessageAsync("PONG!");
                }
            }
        }
    }
}