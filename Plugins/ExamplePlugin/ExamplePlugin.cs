﻿using System;
using MisfitBot_MKII;
using MisfitBot_MKII.DiscordWrap;
using MisfitBot_MKII.Statics;

namespace ExamplePlugin
{
    //  https://cann0nf0dder.wordpress.com/2020/08/30/basic-dotnet-command-calls-to-create-a-c-project-in-visual-studio-code/#Adding-Projects
    public class ExamplePlugin : PluginBase
    {
        public ExamplePlugin()
        {
            Program.BotEvents.OnMessageReceived += OnMessageReceived;
            version = "1.0";
            Core.LOG(new LogEntry(LOGSEVERITY.INFO,
            "PLUGIN",
            $"ExamplePlugin v{version} loaded."));
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
                    await DiscordClient.DiscordSayMessage(args.channel, "PONG!");
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
    }
}
