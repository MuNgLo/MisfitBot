using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MisfitBot2.Plugins.Betting;
using MisfitBot2.Extensions.ChannelManager;
using Discord.Commands;

namespace MisfitBot2.Services
{
    public class BettingService : ServiceBase, IService
    {
        private readonly string PLUGINNAME = "Betting";
        private RunningBets Bets = new RunningBets();
        private int _msgInterval = 60,  _msgCheckInterval = 10;
        
        // CONSTRUCTOR
        public BettingService()
        {
            Core.Twitch._client.OnChatCommandReceived += TwitchOnChatCommandReceived;
            Core.Twitch._client.OnMessageReceived += TwitchOnMessageReceived;
            TimerStuff.OnSecondTick += OnSecondTick;
        }

        #region Twitch methods
        private async void TwitchOnChatCommandReceived(object sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.Command.ChatMessage.Channel);
            switch (e.Command.CommandText.ToLower())
            {
                case "cancelbets":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        if (Bets.ValidateBetting(e.Command.ChatMessage.Channel))
                        {
                            CancelBets(e.Command.ChatMessage.Channel);
                        }
                    }
                        break;
                case "openbets":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {

                        if (e.Command.ArgumentsAsList.Count < 1)
                        {
                            return;
                        }
                        else if (e.Command.ArgumentsAsList.Count == 1)
                        {
                            if (e.Command.ArgumentsAsList[0].ToLower() == "br")
                            {
                                List<string> options = new List<string>();
                                for (int place = 1; place <= 100; place++)
                                {
                                    options.Add(place.ToString());
                                }
                                await OpenBet(bChan, options, BETVARIANT.BATTLEROYALE);
                            }
                            if (e.Command.ArgumentsAsList[0].ToLower() == "dd")
                            {
                                List<string> options = new List<string>();
                                for (int place = 1; place <= 1300; place++)
                                {
                                    options.Add(place.ToString());
                                }
                                await OpenBet(bChan, options, BETVARIANT.DEVILDAGGERS);
                            }
                        }
                        else if (e.Command.ArgumentsAsList.Count > 1)
                        {
                            await OpenBet(bChan, e.Command.ArgumentsAsList, BETVARIANT.NORMAL);
                        }
                    }
                    break;
                case "closebets":
                    if (e.Command.ArgumentsAsList.Count != 1) { return; }
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                        if (Bets.ValidateBetting(e.Command.ChatMessage.Channel))
                        {
                            if (!ValidateOption(e.Command.ChatMessage.Channel, e.Command.ArgumentsAsList[0].ToLower()))
                            {
                                Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, "That was never an option! Get better mods....");
                                return;
                            }

                            if ((await Bets.GrabBet(e.Command.ChatMessage.Channel))._variant != BETVARIANT.NORMAL)
                            {
                                FinishBRBet(e.Command.ChatMessage.Channel, e.Command.ArgumentsAsList[0].ToLower());
                                return;
                            }




                            if (FinishBet(e.Command.ChatMessage.Channel, e.Command.ArgumentsAsList[0].ToLower()))
                            {
                                Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, "Betting closed!");
                                return;
                            }

                        }
                    break;
                case "bet":
                    if (e.Command.ArgumentsAsList.Count != 2) { return; }
                    int i = 0;
                    int.TryParse(e.Command.ArgumentsAsList[0], out i);
                    if (i < 1) { return; }

                    
                    await PlaceBet(bChan, e.Command.ChatMessage.Username, e.Command.ArgumentsAsList[1].ToLower(), i);


                    break;
                case "close":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        if (Bets.ValidateBetting(e.Command.ChatMessage.Channel))
                        {
                            CloseBet(e.Command.ChatMessage.Channel);
                        }
                    }
                    break;
            }
        }
        private async void TwitchOnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {

                if (Bets.HasABetRunning(e.ChatMessage.Channel))
                {
                    (await Bets.GrabBet(e.ChatMessage.Channel))._msgSinceLast++;
                }
        }
        #endregion
        #region Discord methods
        public async Task DiscordSetChannel(ICommandContext Context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            BettingSettings settings = await Settings(bChan);
            settings._defaultDiscordChannel = Context.Channel.Id;
            SaveBaseSettings(PLUGINNAME, bChan, settings);
            await Context.Message.Channel.SendMessageAsync("This is now the dedicated betting channel. Betting commands are accepted in this channel only.");
        }
        public async Task DiscordSetActive(bool flag, ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            BettingSettings settings = await Settings(bChan);
            if (settings._active == flag) { return; }
            settings._active = flag;
            SaveBaseSettings(PLUGINNAME, bChan, settings);
            if (settings._active)
            {
                await context.Channel.SendMessageAsync($"Betting module are now active.");
            }
            else
            {
                await context.Channel.SendMessageAsync($"Betting module are now inactive.");
            }
        }
        public async Task DiscordStartBetting(ICommandContext context, List<string> arguments)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }


            if (arguments.Count < 1)
            {
                return;
            }
            else if (arguments.Count == 1)
            {
                if (arguments[0].ToLower() == "br")
                {
                    List<string> options = new List<string>();
                    for (int place = 1; place <= 100; place++)
                    {
                        options.Add(place.ToString());
                    }
                    await OpenBet(bChan, options, BETVARIANT.BATTLEROYALE);
                }
                if (arguments[0].ToLower() == "dd")
                {
                    List<string> options = new List<string>();
                    for (int place = 1; place <= 1300; place++)
                    {
                        options.Add(place.ToString());
                    }
                    await OpenBet(bChan, options, BETVARIANT.DEVILDAGGERS);
                }
            }else if (arguments.Count > 1)
            {
                await OpenBet(bChan, arguments, BETVARIANT.NORMAL, context.Channel.Id);
            }
        }
        public async Task DiscordStopBetting(ICommandContext context, string arg)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (Bets.ValidateBetting(bChan.TwitchChannelName))
            {
                if (!ValidateOption(bChan.TwitchChannelName, arg.ToLower()))
                {
                    await context.Message.Channel.SendMessageAsync("That was never an option! Get better mods....");
                    return;
                }
                if ((await Bets.GrabBet(bChan.TwitchChannelName))._variant != BETVARIANT.NORMAL)
                {
                    FinishBRBet(bChan.TwitchChannelName, arg.ToLower());
                    return;
                }
                if (FinishBet(bChan.TwitchChannelName, arg.ToLower()))
                {
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, "Betting closed!");
                    return;
                }
            }

        }
        public async Task DiscordBet(ICommandContext context, List<string> arguments)
        {
            if (arguments.Count != 2) { return; }
            int i = 0;
            int.TryParse(arguments[0], out i);
            if (i < 1) { return; }
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            await PlaceBet(bChan, context.User.Id, arguments[1].ToLower(), i);
        }
        #endregion
        #region internal methods
        private void CloseBet(string twitchChannel)
        {
            Bets.CloseBetting(twitchChannel);
        }
        private void CancelBets(string twitchChannel)
        {
            Bets.CancelAllBets(twitchChannel);
        }
        private bool FinishBet(string twitchChannel, string winningOption)
        {
            return Bets.FinishBetting(twitchChannel, winningOption);
        }
        private bool FinishBRBet(string twitchChannel, string winningOption)
        {
            return Bets.FinishBRBetting(twitchChannel, winningOption);
        }
        private async Task OpenBet(BotChannel bChan, List<string> args, BETVARIANT variant, ulong discordChannelID=0)
        {
            await Bets.StartBetting(bChan, args, variant, discordChannelID);
        }
        private async Task PlaceBet(BotChannel bChan, ulong discordUserID, string option, int value)
        {

        }
        private async Task PlaceBet(BotChannel bChan, string twitchUsername,  string option, int value)
        {





                UserEntry user = await Core.UserMan.GetUserByTwitchUserName(twitchUsername);
                if (user != null)
                {
                    await Bets.AddBet(bChan,
                        user,
                        option,
                        value);
                }
            
           
        }
        private void Reminder(int minimumDelay)
        {
            Bets.Reminder(minimumDelay);
        }
        private bool ValidateOption(string twitchChannel, string option)
        {
            return Bets.ValidateOption(twitchChannel, option);
        }
        
        #endregion

        #region Interface methods
        public void OnSecondTick(int second)
        {
            if(second % _msgCheckInterval == 0)
            {
                Reminder(_msgInterval);
            }
        }
        public void OnMinuteTick(int minutes)
        {
            throw new NotImplementedException();
        }
        public void OnUserEntryMerge(UserEntry discordUser, UserEntry twitchUser)
        {
            throw new NotImplementedException();
        }
        public void OnBotChannelEntryMerge(BotChannel discordGuild, BotChannel twitchChannel)
        {
            throw new NotImplementedException();
        }
        public void NewUserValuesEntry(ulong userID, ulong guildID)
        {
            throw new NotImplementedException();
        }
        public void NewUserValuesEntry(string twitchUserID, string twitchChannelID)
        {
            throw new NotImplementedException();
        }
        public Task SetDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            throw new NotImplementedException();
        }
        public Task ClearDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region non interface base methods
        private async Task<BettingSettings> Settings(BotChannel bChan)
        {
            BettingSettings settings = new BettingSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as BettingSettings;
        }
        #endregion


    }
}
