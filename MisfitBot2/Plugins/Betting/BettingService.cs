using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MisfitBot2.Plugins.Betting;
using MisfitBot2.Extensions.ChannelManager;
using Discord.Commands;
using TwitchLib.Client.Models;

namespace MisfitBot2.Services
{
    public struct ApexQueuEntry
    {
        public int timeToStart;
        public string bChanKey;
    }

    public class BettingService : ServiceBase, IService
    {
        private readonly string PLUGINNAME = "Betting";
        private RunningBets Bets = new RunningBets();
        private List<ApexQueuEntry> apexQueue = new List<ApexQueuEntry>();

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
            BettingSettings settings = await Settings(bChan);
            switch (e.Command.CommandText.ToLower())
            {
                case "bets":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        switch (e.Command.ArgumentsAsList[0].ToLower())
                        {
                            // Cancel bets
                            case "cancel":
                                if (Bets.ValidateBetting(e.Command.ChatMessage.Channel))
                                {
                                    CancelBets(bChan);
                                }
                                apexQueue.RemoveAll(p => p.bChanKey == bChan.Key);
                                break;
                            // Close bets
                            case "close":
                                if (e.Command.ArgumentsAsList.Count < 2)
                                {
                                    if (Bets.ValidateBetting(e.Command.ChatMessage.Channel))
                                    {
                                        await CloseBet(e.Command.ChatMessage.Channel);
                                    }
                                }
                                else
                                {
                                    // Close and finish bets
                                    if (!ValidateOption(e.Command.ChatMessage.Channel, e.Command.ArgumentsAsList[1].ToLower()))
                                    {
                                        Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, "That was never an option! Get better mods....");
                                        return;
                                    }
                                    RunningBet bet = Bets.GrabBet(e.Command.ChatMessage.Channel);
                                    await FinishRunningBet(bChan,bet,settings, e.Command.ArgumentsAsList[1].ToLower());
                                }
                                break;
                            // Open bets
                            case "open":
                                if (e.Command.ArgumentsAsList.Count < 1)
                                {
                                    return;
                                }
                                else if (e.Command.ArgumentsAsList.Count == 2)
                                {
                                    if (e.Command.ArgumentsAsList[1].ToLower() == "br")
                                    {
                                        List<string> options = new List<string>();
                                        options.Add("nouse");
                                        for (int place = 1; place <= 100; place++)
                                        {
                                            options.Add(place.ToString());
                                        }
                                        await OpenBet(bChan, options, BETVARIANT.BATTLEROYALE);
                                    }
                                    if (e.Command.ArgumentsAsList[1].ToLower() == "dd")
                                    {
                                        List<string> options = new List<string>();
                                        options.Add("nouse");
                                        for (int place = 1; place <= 1300; place++)
                                        {
                                            options.Add(place.ToString());
                                        }
                                        await OpenBet(bChan, options, BETVARIANT.DEVILDAGGERS);
                                    }
                                    if (e.Command.ArgumentsAsList[1].ToLower() == "apex")
                                    {
                                        List<string> options = new List<string>();
                                        options.Add("nouse");
                                        for (int place = 1; place <= 20; place++)
                                        {
                                            options.Add(place.ToString());
                                        }
                                        await OpenBet(bChan, options, BETVARIANT.APEX);
                                    }
                                }
                                else if (e.Command.ArgumentsAsList.Count > 1)
                                {
                                    await OpenBet(bChan, e.Command.ArgumentsAsList, BETVARIANT.NORMAL);
                                }
                                break;
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
            }
        }
        private void TwitchOnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {

            if (Bets.HasABetRunning(e.ChatMessage.Channel))
            {
                Bets.GrabBet(e.ChatMessage.Channel)._msgSinceLast++;
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
                if (arguments[0].ToLower() == "apex")
                {
                    List<string> options = new List<string>();
                    for (int place = 1; place <= 20; place++)
                    {
                        options.Add(place.ToString());
                    }
                    await OpenBet(bChan, options, BETVARIANT.APEX, context.Channel.Id);
                }
            }
            else if (arguments.Count > 1)
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
                if (Bets.GrabBet(bChan.TwitchChannelName)._variant != BETVARIANT.NORMAL)
                {
                    await FinishBRBet(bChan.TwitchChannelName, arg.ToLower());
                    return;
                }
                if (await FinishBet(bChan.TwitchChannelName, arg.ToLower()))
                {
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, "Betting closed!");
                    return;
                }
            }

        }
        public async Task DiscordBet(ICommandContext context, List<string> arguments)
        {
            if (arguments.Count != 2) { return; }
            int.TryParse(arguments[0], out int i);
            if (i < 1) { return; }
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            await PlaceBet(bChan, context.User.Id, arguments[1].ToLower(), i);
        }
        public async Task DiscordCancelBets(ICommandContext Context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan != null)
            {
                if (bChan.TwitchChannelName != null && bChan.TwitchChannelName != string.Empty)
                {
                    CancelBets(bChan);
                }
            }
        }
        #endregion
        #region internal methods
        private async Task CloseBet(string twitchChannel)
        {
            await Bets.CloseBetting(twitchChannel);
        }
        private void CancelBets(BotChannel bChan)
        {
            Bets.CancelAllBets(bChan);
        }
        private async Task<bool> FinishBet(string twitchChannel, string winningOption)
        {
            return await Bets.FinishBetting(twitchChannel, winningOption);
        }
        private async Task<bool> FinishBRBet(string twitchChannel, string winningOption)
        {
            return await Bets.FinishBRBetting(twitchChannel, winningOption);
        }
        private async Task OpenBet(BotChannel bChan, List<string> args, BETVARIANT variant, ulong discordChannelID = 0)
        {
            if (discordChannelID == 0 && bChan.discordDefaultBotChannel != 0)
            {
                discordChannelID = bChan.discordDefaultBotChannel;
            }
            args.RemoveAt(0); // removes the open/close bit before processing
            await Bets.StartBetting(bChan, args, variant, discordChannelID);
        }
        private async Task PlaceBet(BotChannel bChan, ulong discordUserID, string option, int value)
        {
            UserEntry user = await Core.UserMan.GetUserByDiscordID(discordUserID);
            if (user != null)
            {
                await Bets.AddBet(bChan,
                    user,
                    option,
                    value);
            }
        }
        private async Task PlaceBet(BotChannel bChan, string twitchUsername, string option, int value)
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
        private bool ValidateOption(string twitchChannel, string option)
        {
            return Bets.ValidateOption(twitchChannel, option);
        }
        private async Task FinishRunningBet(BotChannel bChan, RunningBet bet, BettingSettings settings, string winningOption)
        {
            if (bet._variant == BETVARIANT.APEX)
            {
                if (await FinishBRBet(bChan.TwitchChannelName, winningOption))
                {
                    ApexQueuEntry entry = new ApexQueuEntry()
                    {
                        timeToStart = Core.CurrentTime + settings.apexRoundPause,
                        bChanKey = bChan.Key
                    };
                    apexQueue.Add(entry);
                }
                return;
            }
            else if (bet._variant != BETVARIANT.NORMAL)
            {
                await FinishBRBet(bChan.TwitchChannelName, winningOption);
                return;
            }


            if (await FinishBet(bChan.TwitchChannelName, winningOption))
            {
                Core.Twitch._client.SendMessage(bChan.TwitchChannelName, "Betting closed!");
                return;
            }
        }
        #endregion

        #region Interface methods
        public async void OnSecondTick(int second)
        {
            await Bets.Reminder(second);
            await Bets.ApexCloseCheck(second);
            foreach (ApexQueuEntry entry in apexQueue)
            {
                if (Core.CurrentTime > entry.timeToStart)
                {
                    BotChannel bChan = await Core.Channels.GetBotchannelByKey(entry.bChanKey);
                    if (bChan == null)
                    {
                        await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Error, PLUGINNAME, "Could not resolve bChan Key to a botchannel instance."));
                        return;
                    }
                    List<string> options = new List<string>();
                    for (int place = 1; place <= 20; place++)
                    {
                        options.Add(place.ToString());
                    }
                    await OpenBet(bChan, options, BETVARIANT.APEX, bChan.discordDefaultBotChannel);
                }
            }
            // Clean queue 
            foreach (JoinedChannel channel in Core.Twitch._client.JoinedChannels)
            {
                BotChannel bChan = await Core.Channels.GetTwitchChannelByName(channel.Channel);
                if (bChan == null)
                {
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Error, PLUGINNAME, "Could not resolve JoinedChannel.Channel to a botchannel instance."));
                    return;
                }
                if (Bets.ValidateBetting(channel.Channel))
                {
                    apexQueue.RemoveAll(p => p.bChanKey == bChan.Key);
                }
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
