using Discord;
using System;
using System.Collections.Generic;

namespace MisfitBot2
{
    /// <summary>
    /// This represents a Discord or Twitch User. A linked user represents both.
    /// </summary>
    public class UserEntry
    {
        public bool linked = false;
        public string _username = string.Empty;
        public int _lastseen = 0;
        public int _lastseenOnTwitch = 0;
        public string _twitchUID = string.Empty;
        public string _twitchUsername = string.Empty;
        public string _twitchDisplayname = string.Empty;
        public string _twitchColour = string.Empty;
        public string _twitchLogo = string.Empty;
        public DateTime _twitchCreated = new DateTime();
        public DateTime _twitchLastUpdate = new DateTime();
        public ulong _discordUID = 0;
        public UserStatus _discordStatus = UserStatus.Offline;
        public int lastChange = -1;
        public int lastSave = -1;
        public string Key { get { return DataKey(); } }
        public UserEntry()
        {

        }
        public UserEntry(string name, int lastseen, ulong discordID)
        {
            _username = name;
            _lastseen = lastseen;
            _twitchUID = string.Empty;
            _discordUID = discordID;
        }
        public UserEntry(string name, int lastseen, string twitchID)
        {
            _username = name;
            _lastseen = lastseen;
            _twitchUID = twitchID;
            _discordUID = 0;
        }
        private string DataKey()
        {
            string key = _discordUID.ToString();
            if (!linked && _discordUID == 0)
            {
                key = "tw" + _twitchUID;
            }
            if (linked)
            {
                key = _discordUID.ToString();
            }
            return key;
        }
    }
}
