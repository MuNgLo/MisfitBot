using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MisfitBot_MKII;
using MisfitBot_MKII.DiscordWrap;
using MisfitBot_MKII.Statics;


namespace CommunityPicksPlugin
{
    public class CommunityPicksPlugin : PluginBase
    {
        private PickCache cache;

        public CommunityPicksPlugin() : base("pick", "CommunityPicksPlugin", 3, "Set up big votes of pre approved things")
        {
            cache = new PickCache();
        }

        #region Command Methods
        [SubCommand("new", 0), CommandHelp("Create a new pick in the channel"), CommandSourceAccess(MESSAGESOURCE.DISCORD)]
        public async Task CreatePickCommand(BotChannel bChan, BotWideCommandArguments args)
        {
            if (!args.canManageMessages) { return; }
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            if (!CreateNewPick(bChan, args.channelID, String.Join(' ', args.arguments)))
            {
                response.message = "Pick creation failed";
                await Respond(bChan, response);
            }
        }

        [SingleCommand("nominate"), CommandHelp("Create a new pick in the channel"), CommandSourceAccess(MESSAGESOURCE.DISCORD)]
        public async void NominateCommand(BotChannel bChan, BotWideCommandArguments args)
        {
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            response.message = await Nominate(bChan, args.user, args.channelID, String.Join(' ', args.arguments));
            await Respond(bChan, response);
        }
        #endregion
        #region Internal Methods
        /// <summary>
        /// This tries to create a new pick and returns a bool True if successful
        /// </summary>
        private bool CreateNewPick(BotChannel bChan, ulong dChan, string title)
        {
            cache.CreateNewPick(bChan, dChan, title);
            return cache.HasPick(dChan);
        }
        /// <summary>
        /// User nominate pick alternative.
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="dChan"></param>
        /// <param name="nomination"></param>
        /// <returns></returns>
        private async Task<string> Nominate(BotChannel bChan, UserEntry user, ulong dChan, string nomination)
        {
            return await cache.Nominate(dChan, bChan, user, nomination);
        }
        #endregion
        #region Abstract forced from base class


        public override void OnMinuteTick(int minutes)
        {
        }

        public override void OnUserEntryMergeEvent(UserEntry discordUser, UserEntry twitchUser)
        {
        }

        public override void OnBotChannelEntryMergeEvent(BotChannel discordGuild, BotChannel twitchChannel)
        {
        }
        #endregion
    }
}
