using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;

namespace MisfitBot2.Plugins.Betting
{
    public enum BETVARIANT
    {
        NORMAL, 
        BATTLEROYALE, 
        DEVILDAGGERS, 
        APEX
    }
    public class RunningBet
    {
        private readonly string PLUGINNAME = "Betting";
        public bool IsRunning { get; private set; } = true;
        public BETVARIANT _variant = BETVARIANT.NORMAL;
        public string _twitchChannelID;
        private List<string> _options;
        private List<IndividualBet> _bets;
        public bool _isFinished { get; private set; } = false;
        public int _lastReminder = 0;
        public int _msgSinceLast = 0;
        public ulong _discordChannel = 0;
        public readonly int _timestamp;
        public RunningBet(string twitchChannelID, List<string> opt)
        {
            _twitchChannelID = twitchChannelID;
            _bets = new List<IndividualBet>();
            _options = opt;
            _timestamp = Core.CurrentTime;
        }
        public int CurrentPool
        {
            get
            {
                int pool = 0;
                foreach (IndividualBet bet in _bets)
                {
                    pool += bet._amount;
                }
                return pool;
            }
        }

        public string CloseBetting()
        {
            IsRunning = false;
            if (_variant == BETVARIANT.NORMAL)
            {
                return "Betting closed: " + OptionWithGold();
            }
            else
            {
                return "Betting Closed: " + BRReminderText();
            }
        }

        public async void CancelBets()
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByID(_twitchChannelID);
            foreach(IndividualBet bet in _bets)
            {
                await Core.Treasury.GiveGold(bChan, bet._user, bet._amount);
                bet._amount = -1;
            }
            _bets.RemoveAll(p => p._amount == -1);
            if(_bets.Count > 0)
            {
                Core.Twitch._client.SendMessage(
                    bChan.TwitchChannelName, 
                    $"{_bets.Count} users failed to get their bet back."
                    );

            }
        }

        private string BRReminderText()
        {
            string result = string.Empty;
            List<string> pickedOptions = new List<string>();
            pickedOptions.AddRange(_options);
            List<BetSum> optionsWithBets = new List<BetSum>();
            foreach(string option in _options)
            {
                if(_bets.FindAll(p=>p._optionPick == option).Count < 1)
                {
                    pickedOptions.Remove(option);
                }
            }

            foreach(string option in pickedOptions)
            {
                if(!optionsWithBets.Exists(p=>p.option == option))
                {
                    optionsWithBets.Add(new BetSum(option, 0));
                }
            }
            foreach(IndividualBet bet in _bets)
            {
                    optionsWithBets.Find(p => p.option == bet._optionPick).betAmount += bet._amount;
            }





                List<BetSum> SortedList = optionsWithBets.OrderByDescending(p => p.betAmount).ToList();
            if (SortedList.Count > 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    result += $" { SortedList[i].option}({SortedList[i].betAmount})";
                }
            }
            else
            {
                for (int i = 0; i < SortedList.Count; i++)
                {
                    result += $" { SortedList[i].option}({SortedList[i].betAmount})";
                }
            }

            if(result == string.Empty) { result = $" No bets placed this time."; }

            return result;
        }

        private string OptionWithGold()
        {
            string result = string.Empty;
            Dictionary<string, int> bets = new Dictionary<string, int>();
            foreach (string option in _options)
            {
                if (!bets.ContainsKey(option))
                {
                    bets[option] = new int();
                }
                foreach (IndividualBet bet in _bets)
                {
                    if (bet._optionPick == option)
                    {
                        bets[option] += bet._amount;
                    }
                }
            }
            foreach (string key in bets.Keys)
            {
                result += $" {key}({bets[key]}g)";
            }

            return result;
        }

        public async Task Finish(string winningOption)
        {
            IsRunning = false;
            int pool = CurrentPool;
            int winningPool = 0;
            string message = string.Empty;
            foreach(IndividualBet bet in _bets)
            {
                if(bet._optionPick == winningOption)
                {
                    winningPool += bet._amount;
                }
            }
            BotChannel bChan = await Core.Channels.GetTwitchChannelByID(_twitchChannelID);

            if (pool < 1)
            {
                message = $"FINISH! No bets where placed.";
                Core.Twitch._client.SendMessage(bChan.TwitchChannelName, message);
                await SayOnDiscord(_discordChannel, message);
                return;
            }
            if (winningPool < 1)
            {
                message = $"FINISH! No Winners.";
                Core.Twitch._client.SendMessage(bChan.TwitchChannelName, message);
                await SayOnDiscord(_discordChannel, message);
                return;
            }


            float odds = (float)pool / (float)winningPool;

            string biggestWinner = string.Empty;
            int biggestWin = 0;

            foreach (IndividualBet bet in _bets)
            {
                if (bet._optionPick == winningOption)
                {
                    // DO PAYOUTS!!!
                    bet._winnings = (int)Math.Floor(bet._amount * odds);
                    await Core.Treasury.GiveGold(
                        bChan, 
                        bet._user,
                        bet._winnings);
                    if (bet._winnings > biggestWin)
                    {
                        biggestWin = bet._winnings;
                        biggestWinner = bet._user._twitchUsername;
                    }
                }
            }




            Core.Twitch._client.SendMessage((await Core.Channels.GetTwitchChannelByID(_twitchChannelID)).TwitchChannelName, 
                $"The bets ended as {OptionWithGold()}. {winningOption} was the winning option and  {biggestWinner} won most with {biggestWin}g.");
            if (_discordChannel != 0)
            {
                await SayOnDiscord(_discordChannel, $"The bets ended as {OptionWithGold()}. {winningOption} was the winning option and  {biggestWinner} won most with {biggestWin}g.");
            }
        }

        public async Task FinishBR(string winningOption)
        {
            IsRunning = false;
            int pool = CurrentPool;
            string message = string.Empty;
            BotChannel bChan = await Core.Channels.GetTwitchChannelByID(_twitchChannelID);
            if (pool < 1)
            {
                message = $"FINISH! No bets where placed.";
                if(_variant == BETVARIANT.APEX)
                {
                    BettingSettings settings = await Settings(bChan);
                    message += $" A new round of betting will start in {settings.apexRoundPause} seconds.";
                }
                Core.Twitch._client.SendMessage(bChan.TwitchChannelName, message);
                return;
            }

            int winningOptionInINT = 0;
            int.TryParse(winningOption, out winningOptionInINT);
            int optionInINT = 102;
            foreach (IndividualBet bet in _bets)
            {
                int.TryParse(bet._optionPick, out optionInINT);
                bet._brPlacementDistance = Math.Abs(winningOptionInINT - optionInINT);


            }

            List<IndividualBet> SortedList = _bets.OrderBy(p => p._brPlacementDistance).ToList();

            string msg = string.Empty;
            List<IndividualBet> winners = SortedList.FindAll(p => p._brPlacementDistance == SortedList[0]._brPlacementDistance);

            if (winners.Count > 5)
            {
                int index = 0;
                foreach (IndividualBet bet in winners)
                {
                    // DO PAYOUTS!!!
                    bet._winnings = pool / winners.Count;
                    await Core.Treasury.GiveGold(bChan, bet._user, bet._winnings);
                    if (index < 5)
                    {
                        msg += $" {bet._user._twitchUsername}({bet._winnings})";
                    }
                    index++;
                }
            }
            else
            {
                foreach (IndividualBet bet in winners)
                {
                    // DO PAYOUTS!!!
                    bet._winnings = pool / winners.Count;
                    await Core.Treasury.GiveGold(bChan, bet._user, bet._winnings);
                    msg += $" {bet._user._twitchUsername}({bet._winnings})";
                }
            }

            message = $"Winners: {msg}";
            if (_variant == BETVARIANT.APEX)
            {
                BettingSettings settings = await Settings(bChan);
                message += $" A new round of betting will start in {settings.apexRoundPause} seconds.";
            }


            Core.Twitch._client.SendMessage((await Core.Channels.GetTwitchChannelByID(_twitchChannelID)).TwitchChannelName, message);
            await SayOnDiscord(_discordChannel, message);
            return;
        }

        internal int UserHasBets(string twitchUID)
        {
            return _bets.FindAll(p => p._user._twitchUID == twitchUID).Count;
        }

        private async Task SayOnDiscord(ulong channelID, string message)
        {
            if(channelID != 0)
            {
                await (Core.Discord.GetChannel(channelID) as ISocketMessageChannel).SendMessageAsync(message);
            }
        }

        internal string ReminderMessage()
        {
            if (_variant == BETVARIANT.NORMAL)
            {
                return "Betting open! Type \"?bet <amount> <option>\" to place a bet. Current options:" + OptionWithGold();
            }else if (_variant != BETVARIANT.NORMAL)
            {
                return "Most bet options:" + BRReminderText();
            }
            return string.Empty;
        }

        /// <summary>
        /// Checks that the picked option is a valid one then adds the bet to the pool.
        /// </summary>
        /// <param name="twitchUsername"></param>
        /// <param name="pickedOption"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public void AddBet(UserEntry user, string pickedOption, int amount)
        {
            

            if (_options.Exists(p => p == pickedOption))
            {
                if (_bets.Exists(p => p._user._twitchUID == user._twitchUID))
                {
                    // User has existing bet for this option. If so add to it
                    if(_bets.Exists(p=>p._user._twitchUID == user._twitchUID && p._optionPick == pickedOption))
                    {
                        _bets.Find(p => p._user._twitchUID == user._twitchUID && p._optionPick == pickedOption)._amount += amount;
                    }
                    else
                    {
                        // add users first bet for this option
                        _bets.Add(new IndividualBet(user, pickedOption, amount));
                    }
                }
                else
                {
                    // add user's first bet
                    _bets.Add(new IndividualBet(user, pickedOption, amount));
                }
            }
        }

        public bool ValidateOption(string opt)
        {
            return _options.Exists(p => p == opt);
        }

        private async Task<BettingSettings> Settings(BotChannel bChan)
        {
            BettingSettings settings = new BettingSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as BettingSettings;
        }
    }



    public class BetSum
    {
        public string option;
        public int betAmount;
        public BetSum(string opt, int bet)
        {
            option = opt;
            betAmount = bet;
        }
    }
}
