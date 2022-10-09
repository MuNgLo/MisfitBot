using System;
using MisfitBot_MKII;
using MisfitBot_MKII.DiscordWrap;
using MisfitBot_MKII.Statics;


namespace ExamplePlugin
{
    //  https://cann0nf0dder.wordpress.com/2020/08/30/basic-dotnet-command-calls-to-create-a-c-project-in-visual-studio-code/#Adding-Projects
    public class ExamplePlugin : PluginBase
    {
        public ExamplePlugin():base("example", "ExamplePlugin", 3, "Just an example")
        {
            Program.BotEvents.OnMessageReceived += OnMessageReceived;
        }



        [SubCommand("test", 0), CommandHelp("This is just a test command that don't do anything!"), CommandVerified(3)]
        public void SubCommandTesthandler(BotChannel bChan, BotWideCommandArguments args)
        {
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            response.message = "Exampleplugin subcommandExample working here!";
            Respond(bChan, response);
        }


        private async void OnMessageReceived(BotWideMessageArguments args)
        {
            if (args.message.ToLower() == $"ping")
            {
                if (args.source == MESSAGESOURCE.TWITCH)
                {
                    Program.TwitchSayMessage(args.channel, "PONG! Cucumberz");
                }
                if (args.source == MESSAGESOURCE.DISCORD)
                {
                    await DiscordClient.DiscordSayMessage(args.channelID, "PONG!");
                }
            }
        }

        public override void OnSecondTick(int seconds)
        {
        }

        public override void OnMinuteTick(int minutes)
        {
        }

        public override void OnUserEntryMergeEvent(UserEntry discordUser, UserEntry twitchUser)
        {
        }

        public override void OnBotChannelEntryMergeEvent(BotChannel discordGuild, BotChannel twitchChannel)
        {
        }
    }
}
