using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Plugins.MatchMaking
{
    public enum QUEUESTATE
    {
        INACTIVE, RECRUITING, PENDING, ENDED
    }
    public enum WINNER
    {
        UNDECLARED, TEAMA, TEAMB
    }

    class MMQueue
    {
        public readonly ulong DISCORDCHANNEL;
        public QUEUESTATE currentstate = QUEUESTATE.INACTIVE;
        public bool active = false;
        public string game = string.Empty;
        public int teamsize = 1;
        public List<ulong> users = new List<ulong>();
        public List<ulong> teamA = new List<ulong>();
        public List<ulong> teamB = new List<ulong>();
        public WINNER winningteam = WINNER.UNDECLARED;
        public MMQueue(ulong discordChannel, int size)
        {
            DISCORDCHANNEL = discordChannel;
            teamsize = size;
        }
    }
}
