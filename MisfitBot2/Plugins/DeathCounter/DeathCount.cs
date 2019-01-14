using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Plugins.DeathCounter
{
    struct DeathCount
    {
        public readonly ulong _guildID, _discordChannel;
        public int _deaths;
        public DeathCount(ulong guild, ulong discordChannel)
        {
            _guildID = guild;
            _discordChannel = discordChannel;
            _deaths = 0;
        }
    }
}
