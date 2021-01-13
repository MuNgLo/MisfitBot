using System;
using MisfitBot_MKII;

namespace ExamplePlugin
{
    //  https://cann0nf0dder.wordpress.com/2020/08/30/basic-dotnet-command-calls-to-create-a-c-project-in-visual-studio-code/#Adding-Projects
    public class ExamplePlugin : PluginBase , IService
    {
        public ExamplePlugin()
        {
            Program.BotEvents.OnMessageReceived += OnMessageReceived;
            Program.BotEvents.OnTwitchConnected += OnTwitchConnected;
            Program.BotEvents.OnDiscordConnected += OnDiscordConnected;
            //Program.BotEvents.OnDiscordGuildAvailable += OnDiscordGuildAvailable;
            Core.LOG(new LogEntry(LOGSEVERITY.INFO,
            "PLUGIN",
            "ExamplePlugin loaded."));
        }

        

        private void OnDiscordConnected()
        {
            

        }

        private void OnTwitchConnected(string msg)
        {



        }

        private async void OnMessageReceived(BotWideMessageArguments args)
        {
            if (args.message.ToLower() == $"{CMC}ping")
            {
                if (args.source == MESSAGESOURCE.TWITCH)
                {
                    Program.TwitchSayMessage(args.channel, "PONG! Cucumberz");
                }
                if (args.source == MESSAGESOURCE.DISCORD)
                {
                    await Program.DiscordSayMessage(args.channel, "PONG!");
                }
            }
        }

        public void OnSecondTick(int seconds)
        {
            throw new NotImplementedException();
        }

        public void OnMinuteTick(int minutes)
        {
            throw new NotImplementedException();
        }

        public void OnUserEntryMergeEvent(UserEntry discordUser, UserEntry twitchUser)
        {
            throw new NotImplementedException();
        }

        public void OnBotChannelEntryMergeEvent(BotChannel discordGuild, BotChannel twitchChannel)
        {
            throw new NotImplementedException();
        }
    }
}
