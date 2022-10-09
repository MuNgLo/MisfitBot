using System;
using MisfitBot_MKII;
using MisfitBot_MKII.DiscordWrap;
using MisfitBot_MKII.Statics;


namespace CommunityPicksPlugin
{
    //  https://cann0nf0dder.wordpress.com/2020/08/30/basic-dotnet-command-calls-to-create-a-c-project-in-visual-studio-code/#Adding-Projects
    public class CommunityPicksPlugin : PluginBase
    {
        public CommunityPicksPlugin():base("picks", "CommunityPicksPlugin", 3, "Just an example")
        {
            Program.BotEvents.OnMessageReceived += OnMessageReceived;
        }



        [SubCommand("test", 0), CommandHelp("This is just a test command that don't do anything!"), CommandVerified(3)]
        public void SubCommandTesthandler(BotChannel bChan, BotWideCommandArguments args)
        {
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            response.message = "CommunityPicksPlugin subcommandExample working here!";
            Respond(bChan, response);
        }


        private async void OnMessageReceived(BotWideMessageArguments args)
        {
            if (args.message.ToLower() == $"pick")
            {
                if (args.source == MESSAGESOURCE.TWITCH)
                {
                    Program.TwitchSayMessage(args.channel, "Puck! wopwopwop");
                }
                if (args.source == MESSAGESOURCE.DISCORD)
                {
                    await DiscordClient.DiscordSayMessage(args.channelID, "Puck! wopwopwop");
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
