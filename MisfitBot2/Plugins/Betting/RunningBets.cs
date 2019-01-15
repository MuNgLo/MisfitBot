
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

        public void CloseBetting(string twitchChannel)
        {
            if (CurrentlyRunning.ContainsKey(twitchChannel))
            {
                Core.Twitch._client.SendMessage(twitchChannel, CurrentlyRunning[twitchChannel].CloseBetting());
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
        internal void Reminder(int minimumDelay)
        {
            foreach (string key in CurrentlyRunning.Keys)
            {
                if (CurrentlyRunning[key].IsRunning && CurrentlyRunning[key]._msgSinceLast >= 8 && Core.CurrentTime > CurrentlyRunning[key]._lastReminder + minimumDelay)
                {
                    string msg = CurrentlyRunning[key].ReminderMessage();
                    Core.Twitch._client.SendMessage(key, msg);
                    CurrentlyRunning[key]._msgSinceLast = 0;
                    CurrentlyRunning[key]._lastReminder = Core.CurrentTime;
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
                channelForBet = settings._defaultDiscordChannel;
            }
            else if (channelForBet == 0 && bChan.discordDefaultBotChannel != 0)
            {
                channelForBet = bChan.discordDefaultBotChannel;
            }
            else if (channel != 0)
            {
                channelForBet = channel;
            }



            CurrentlyRunning[bChan.TwitchChannelName] = new RunningBet(bChan.TwitchChannelID, options)
            {
                _variant = variant
            };

            switch (variant)
            {
                case BETVARIANT.NORMAL:
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"Betting open! Type \"!bet <amount> <placement>\" to place a bet.  Valid options are {OptionListToString(options)}");
                    if(channelForBet != 0)
                    {
                        await (Core.Discord.GetChannel(channelForBet) as ISocketMessageChannel).SendMessageAsync($"Betting open! Type \"!bet <amount> <option>\" to place a bet.  Valid options are {OptionListToString(options)}");
                    }
                    break;
                case BETVARIANT.BATTLEROYALE:
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, "Betting open! Type \"!bet <amount> <placement>\" to place a bet. Valid placements are 1 to 100.");
                    if (channelForBet != 0)
                    {
                        await (Core.Discord.GetChannel(channelForBet) as ISocketMessageChannel).SendMessageAsync("Betting open! Type \"!bet <amount> <placement>\" to place a bet. Valid placements are 1 to 100.");
                    }
                    break;
                case BETVARIANT.DEVILDAGGERS:
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, "Betting open! Type \"!bet <amount> <placement>\" to place a bet.  Valid seconds are 1 to 1300.");
                    if (channelForBet != 0)
                    {
                        await (Core.Discord.GetChannel(channelForBet) as ISocketMessageChannel).SendMessageAsync("Betting open! Type \"!bet <amount> <placement>\" to place a bet.  Valid seconds are 1 to 1300.");
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
            // IF NR/DD betvariant we only accept first bet
            if (CurrentlyRunning[bChan.TwitchChannelName]._variant != BETVARIANT.NORMAL)
            {
                if (CurrentlyRunning[bChan.TwitchChannelName].UserHasBets(user._twitchUID) > 0)
                {
                    return false;
                }
            }
            
            // Check the gold is enough for the bet
            int userGold = await Core.Treasury.GetUserGold(user, bChan);
            if (userGold < amount)
            {
                return false;
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
