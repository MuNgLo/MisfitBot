using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MisfitBot2.Plugins.Voting
{
    struct Vote
    {
        public UserEntry botUser;
        public string voteOption;
        public Vote(UserEntry user, string opt)
        {
            botUser = user;
            voteOption = opt;
        }
    }

    class RunningVote
    {
        public ulong discordguild = 0;
        public string twitchChannelName = string.Empty;
        public List<string> options;
        private List<Vote> votes;
        public RunningVote(BotChannel bChan, List<string> opt)
        {
            if (bChan.isLinked)
            {
                discordguild = bChan.GuildID;
                twitchChannelName = bChan.TwitchChannelName;
            }
            else if (bChan.GuildID != 0)
            {
                discordguild = bChan.GuildID;
                return;
            }
            else if (bChan.TwitchChannelName != string.Empty)
            {
                twitchChannelName = bChan.TwitchChannelName;
            }
            // TODO make it fail nicely
            options = opt;
            votes = new List<Vote>();
        }


        public async Task<BotChannel> GetBotChannel()
        {
            if (twitchChannelName != string.Empty)
            {
                return await Core.Channels.GetTwitchChannelByName(twitchChannelName);
            }
            if (discordguild != 0)
            {
                return await Core.Channels.GetDiscordGuildbyID(discordguild);
            }
            return null;
        }

        public bool AddVote(Vote newVote)
        {
            votes.RemoveAll(p => p.botUser == newVote.botUser);
            votes.Add(newVote);
            return votes.Exists(p => p.botUser == newVote.botUser);
        }
        public bool ValidateOption(string optionToValidate)
        {
            foreach (string option in options)
            {
                if(option.ToLower() == optionToValidate.ToLower())
                {
                    return true;
                }
            }
            return false;
        }
        public string FinishVote()
        {
            Dictionary<string, int> resultCount = new Dictionary<string, int>();
            if(votes.Count < 1)
            {
                return $"No votes where cast so no wining option.";
            }
            foreach (Vote v in votes)
            {
                if (!resultCount.ContainsKey(v.voteOption))
                {
                    resultCount[v.voteOption] = new int();
                }
                resultCount[v.voteOption]++;
            }
            string winner = "";
            int mostvotes = -1;
            foreach (string key in resultCount.Keys)
            {
                if (resultCount[key] >= mostvotes)
                {
                    winner = key;
                    mostvotes = resultCount[key];
                }
            }
            return $"Option {winner} won with {mostvotes} votes.";


        }
    }

    
}
