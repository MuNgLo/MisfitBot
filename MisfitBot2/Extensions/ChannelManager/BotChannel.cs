namespace MisfitBot2
{
    /// <summary>
    /// This represents a channel for the bot. Can be Discord guild, Twitchchannel or both as a linked entry.
    /// </summary>
    public class BotChannel
    {
        public bool isLinked = false;
        public ulong GuildID = 0;
        public string GuildName = string.Empty;
        public ulong discordDefaultBotChannel = 0;
        public ulong discordAdminChannel = 0;
        public string TwitchChannelID = string.Empty;
        public string TwitchChannelName = string.Empty; // NOT DisplayName!!
        public bool isTwitch = true; // Phase this one out
        public bool isLive = false;
        public bool TwitchAutojoin = false;
        public string pubsubOauth = string.Empty;
        public string Key = string.Empty;
        // CONSTRUCTORS
        public BotChannel(ulong guildID, string guildName="")
        {
            GuildID = guildID;
            GuildName = guildName;
            isTwitch = false;
            Key = DataKey();
        }
        public BotChannel(string twitchChannelName, string twitchChannelID)
        {
            TwitchChannelID = twitchChannelID;
            TwitchChannelName = twitchChannelName;
            TwitchAutojoin = true;
            Key = DataKey();

        }
        public void UpdateKey()
        {
            Key = DataKey();
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
