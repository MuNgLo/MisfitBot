using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Plugins.Raffle
{
    public class RaffleTicket
    {
        public readonly int _ticketNumber;
        public string _owner
        {
            get {
                if(_discordID != 0)
                {
                    return _discordID.ToString();
                }
                else
                {
                    return _twitchUsername;
                }
            }
            private set { }
        }
        private ulong _discordID = 0;
        private string _twitchUsername = string.Empty;
        private bool _isDiscordID = false;

        public RaffleTicket(int number)
        {
            _ticketNumber = number;
        }

        public void SetOwner(ulong discordID)
        {
            _discordID = discordID;
            _isDiscordID = true;
        }
        public void SetOwner(string twitchUserName)
        {
            _twitchUsername = twitchUserName;
        }
        public bool IsDicordUser()
        {
            return _isDiscordID;
        }
    }
}
