using System;

namespace MisfitBot_MKII
{
    public class PingPong
    {

        public PingPong()
        {
            Program.BotEvents.OnMessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(BotWideMessageArguments args)
        {
            if (args.message.ToLower() == "!ping" && args.source == MESSAGESOURCE.TWITCH)
            {
                Program.TwitchClient.SendMessage(args.channel, "PONG!");
            }
        }
    }
}