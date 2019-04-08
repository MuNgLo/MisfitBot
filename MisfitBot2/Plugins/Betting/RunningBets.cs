
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using MisfitBot2.Extensions.ChannelManager;

namespace MisfitBot2.Plugins.Betting
{
    public class RunningBets
    {
        private volatile Dictionary<string, RunningBet> CurrentlyRunning = new Dictionary<string, RunningBet>();
        private readonly string PLUGINNAME = "Betting";
        /// <summary>
        /// This need to be wrapped in a check for existing bet before called
        /// </summary>
        /// <param name="twitchChannel"></param>
        /// <returns></returns>
        public RunningBet GrabBet(string twitchChannel)
        {
            RunningBet result = CurrentlyRunning[twitchChannel];
            return result;
        }

        public bool HasABetRunning(string twitchChannel)
        {
            return CurrentlyRunning.ContainsKey(twitchChannel);
        }

        public async Task CloseBetting(string twitchChannel)
        {
            if (CurrentlyRunning.ContainsKey(twitchChannel))
            {
                string msg = CurrentlyRunning[twitchChannel].CloseBetting();
                Core.Twitch._client.SendMessage(twitchChannel, msg);
                if (CurrentlyRunning[twitchChannel]._discordChannel != 0)
                {
                    await SayOnDiscord(CurrentlyRunning[twitchChannel]._discordChannel, msg);
                }
            }
        }
        public async void CancelAllBets(BotChannel bChan)
        {
            if (CurrentlyRunning.ContainsKey(bChan.TwitchChannelName))
            {
                string message = $"All bets are off. Bets returned.";
                CurrentlyRunning[bChan.TwitchChannelName].CancelBets();
                Core.Twitch._client.SendMessage(bChan.TwitchChannelName, message);
                if (bChan.discordDefaultBotChannel != 0)
                {
                    await (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(message);
                }
                CurrentlyRunning.Remove(bChan.TwitchChannelName);
            }
        }
        /// <summary>
        /// Return False if betting isn't closed. True if it is.
        /// </summary>
        /// <param name="twitchChannel"></param>
        /// <returns></returns>
        public async Task<bool> FinishBetting(string twitchChannel, string winningOption)
        {
            if (CurrentlyRunning.ContainsKey(twitchChannel))
            {
                await CurrentlyRunning[twitchChannel].Finish(winningOption);
                CurrentlyRunning.Remove(twitchChannel);
            }
            return !CurrentlyRunning.ContainsKey(twitchChannel);
        }
        public async Task<bool> FinishBRBetting(string twitchChannel, string winningOption)
        {
            if (CurrentlyRunning.ContainsKey(twitchChannel))
            {
                await CurrentlyRunning[twitchChannel].FinishBR(winningOption);
                CurrentlyRunning.Remove(twitchChannel);
            }
            return !CurrentlyRunning.ContainsKey(twitchChannel);
        }
        public async Task Reminder(int second)
        {
            foreach (string key in CurrentlyRunning.Keys)
            {
                BotChannel bChan = await Core.Channels.GetTwitchChannelByID(CurrentlyRunning[key]._twitchChannelID);
                BettingSettings settings = await Settings(bChan);
                if (second % settings._msgCheckInterval == 0)
                {
                    if (CurrentlyRunning[key].IsRunning && CurrentlyRunning[key]._msgSinceLast >= settings.reminderMinMessageBetween && Core.CurrentTime > CurrentlyRunning[key]._lastReminder + settings._msgInterval)
                    {
                        string msg = CurrentlyRunning[key].ReminderMessage();
                        Core.Twitch._client.SendMessage(key, msg);
                        if(CurrentlyRunning[key]._discordChannel != 0)
                        {
                            await SayOnDiscord(CurrentlyRunning[key]._discordChannel, msg);
                        }
                        CurrentlyRunning[key]._msgSinceLast = 0;
                        CurrentlyRunning[key]._lastReminder = Core.CurrentTime;
                    }
                }
            }
        }
        public async Task ApexCloseCheck(int second)
        {
            foreach (string key in CurrentlyRunning.Keys)
            {
                if (CurrentlyRunning[key]._variant == BETVARIANT.APEX)
                {
                    BotChannel bChan = await Core.Channels.GetTwitchChannelByID(CurrentlyRunning[key]._twitchChannelID);
                    BettingSettings settings = await Settings(bChan);
                    if (CurrentlyRunning[key].IsRunning)
                    {
                        if (Core.CurrentTime > CurrentlyRunning[key]._timestamp + settings.apexOpenTimer)
                        {
                            await CloseBetting(bChan.TwitchChannelName);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Retuns true if a new betting phase is started. False if not.
        /// </summary>
        /// <param name="bChan"></param>
        /// <returns></returns>
        public async Task StartBetting(BotChannel bChan, List<string> options, BETVARIANT variant, ulong channel)
        {
            if (CurrentlyRunning.ContainsKey(bChan.TwitchChannelName))
            {
                return;
            }
            for (int i = 0; i < options.Count; i++)
            {
                options[i] = options[i].ToLower();
            }

            BettingSettings settings = await Settings(bChan);
            ulong channelForBet = 0;


            if (settings._defaultDiscordChannel != 0)
            {
                channelForBet = settings._defaultDiscordChannel; // _defaultDiscordChannel is the detting module default channel
            }

            if (channelForBet == 0 && bChan.discordDefaultBotChannel != 0)
            {
                channelForBet = bChan.discordDefaultBotChannel;
            }

            if (channel != 0)
            {
                channelForBet = channel; // If bet is started from discord it will run in that channel. Otherwise it defaults do default bot channel
            }



            CurrentlyRunning[bChan.TwitchChannelName] = new RunningBet(bChan.TwitchChannelID, options)
            {
                _variant = variant
            };

            if(channelForBet != 0)
            {
                CurrentlyRunning[bChan.TwitchChannelName]._discordChannel = channelForBet;
            }

            switch (variant)
            {
                case BETVARIANT.NORMAL:
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"Betting open! Type \"{Core._commandCharacter}bet <amount> <placement>\" to place a bet.  Valid options are {OptionListToString(options)}");
                    if(channelForBet != 0)
                    {
                        await (Core.Discord.GetChannel(channelForBet) as ISocketMessageChannel).SendMessageAsync($"Betting open! Type \"{Core._commandCharacter}bet <amount> <option>\" to place a bet.  Valid options are {OptionListToString(options)}");
                    }
                    break;
                case BETVARIANT.BATTLEROYALE:
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"Betting open! Type \"{Core._commandCharacter}bet <amount> <placement>\" to place a bet. Valid placements are 1 to 100.");
                    if (channelForBet != 0)
                    {
                        await (Core.Discord.GetChannel(channelForBet) as ISocketMessageChannel).SendMessageAsync($"Betting open! Type \"{Core._commandCharacter}bet <amount> <placement>\" to place a bet. Valid placements are 1 to 100.");
                    }
                    break;
                case BETVARIANT.DEVILDAGGERS:
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"Betting open! Type \"{Core._commandCharacter}bet <amount> <second>\" to place a bet.  Valid seconds are 1 to 1300.");
                    if (channelForBet != 0)
                    {
                        await (Core.Discord.GetChannel(channelForBet) as ISocketMessageChannel).SendMessageAsync($"Betting open! Type \"{Core._commandCharacter}bet <amount> <placement>\" to place a bet.  Valid seconds are 1 to 1300.");
                    }
                    break;
                case BETVARIANT.APEX:
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"Betting open! Type \"{Core._commandCharacter}bet <amount> <placement>\" to place a bet. Valid placements are 1 to 20. Betting closes in {settings.apexOpenTimer} seconds.");
                    if (channelForBet != 0)
                    {
                        await (Core.Discord.GetChannel(channelForBet) as ISocketMessageChannel).SendMessageAsync($"Betting open! Type \"{Core._commandCharacter}bet <amount> <placement>\" to place a bet. Valid placements are 1 to 20. Betting closes in {settings.apexOpenTimer} seconds.");
                    }
                    break;
            }
        }

        public async Task<bool> AddBet(BotChannel bChan, UserEntry user, string pickedOption, int amount)
        {
            if (!CurrentlyRunning.ContainsKey(bChan.TwitchChannelName) || !CurrentlyRunning[bChan.TwitchChannelName].IsRunning)
            {
                return false;
            }
            // Check the gold is enough for the bet
            if (await Core.Treasury.GetUserGold(user, bChan) < amount)
            {
                return false;
            }
            // IF BR/DD betvariant we only accept first bet
            if (CurrentlyRunning[bChan.TwitchChannelName]._variant != BETVARIANT.NORMAL)
            {
                if (CurrentlyRunning[bChan.TwitchChannelName].UserHasBets(user._twitchUID) > 0)
                {
                    return false;
                }
            }
            Core.Treasury.TakeGold(user, bChan, amount);
            CurrentlyRunning[bChan.TwitchChannelName].AddBet(user, pickedOption, amount);
            return true;
        }
        /// <summary>
        /// Validates a given option against running bet's option list.
        /// Returns true if valid;
        /// </summary>
        /// <param name="twitchChannel"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public bool ValidateOption(string twitchChannel, string option)
        {
            if (!CurrentlyRunning.ContainsKey(twitchChannel))
            {
                return false;
            }
            return CurrentlyRunning[twitchChannel].ValidateOption(option);
        }

        public bool ValidateBetting(string twitchChannel)
        {
            return CurrentlyRunning.ContainsKey(twitchChannel);
        }
        private async Task SayOnDiscord(ulong channelID, string message)
        {
            if (channelID != 0)
            {
                await (Core.Discord.GetChannel(channelID) as ISocketMessageChannel).SendMessageAsync(message);
            }
        }
        #region non interface base methods
        private async Task<BettingSettings> Settings(BotChannel bChan)
        {
            BettingSettings settings = new BettingSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as BettingSettings;
        }
        private string OptionListToString(List<string> options)
        {
            string text = string.Empty;
            foreach (string opt in options)
            {
                text += $"{opt}, ";
            }
            return text;
        }
        #endregion
    }
}
