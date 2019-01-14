using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace MisfitBot2
{
    public class BotChannel
    {
        public volatile bool isLinked = false;
        public ulong GuildID = 0;
        public volatile string GuildName = string.Empty;
        public ulong discordDefaultBotChannel = 0;
        public ulong discordAdminChannel = 0;
        public volatile string TwitchChannelID = string.Empty;
        // NOT DisplayName!!
        public volatile string TwitchChannelName = string.Empty;
        public volatile bool isTwitch = true;
        public volatile bool isLive = false;
        public volatile bool TwitchAutojoin = false;

        public volatile string pubsubOauth = string.Empty;
        [JsonIgnore]
        public string Key { get { return DataKey(); } }

        [JsonConstructor]
        BotChannel()
        {

        }

        public BotChannel(ulong guildID, string guildName="")
        {
            GuildID = guildID;
            GuildName = guildName;
            isTwitch = false;
        }

        public BotChannel(string channelName)
        {
            TwitchChannelID = "";
            TwitchChannelName = channelName;
            TwitchAutojoin = true;
        }

        private string DataKey()
        {
            string key = GuildID.ToString();

            if (!isLinked && GuildID == 0)
            {
                key = "tw" + TwitchChannelID;
            }

            if (isLinked)
            {
                key = GuildID.ToString();
            }
            return key;
        }
        
    }
}
