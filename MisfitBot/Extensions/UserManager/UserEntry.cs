using Discord;
using System;
using System.Collections.Generic;

namespace MisfitBot_MKII
{
    /// <summary>
    /// This represents a Discord or Twitch User. A linked user represents both.
    /// </summary>
    public class UserEntry
    {
        public bool locked = false;
        public bool linked = false;
        public string discordUsername = string.Empty;
        public int lastSeen = 0;
        public int lastSeenOnTwitch = 0;
        public string twitchUID = string.Empty;
        public string twitchUsername = string.Empty;
        public string twitchDisplayName = string.Empty;
        public string twitchColour = string.Empty;
        public string twitchLogo = string.Empty;
        public DateTime twitchCreated = new DateTime();
        public DateTime twitchLastUpdate = new DateTime();
        public ulong discordUID = 0;
        public UserStatus discordStatus = UserStatus.Offline;
        public int lastChange = -1;
        public int lastSave = -1;
        public string Key { get { return DataKey(); } }
        public UserEntry()
        {

        }
        public UserEntry(string name, int lastSeen, ulong discordID)
        {
            discordUsername = name;
            this.lastSeen = lastSeen;
            twitchUID = string.Empty;
            discordUID = discordID;
        }

        public string ContextName(MESSAGESOURCE source)
        {
            if(source == MESSAGESOURCE.TWITCH){
                return twitchDisplayName;
            }
            return discordUsername;
        }

        public UserEntry(string name, int lastSeen, string twitchID)
        {
            discordUsername = name;
            this.lastSeen = lastSeen;
            twitchUID = twitchID;
            discordUID = 0;
        }
        private string DataKey()
        {
            string key = discordUID.ToString();
            if (!linked && discordUID == 0)
            {
                key = "tw" + twitchUID;
            }
            if (linked)
            {
                key = discordUID.ToString();
            }
            return key;
        }
    }
}
