using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot_MKII.Extensions.CommandInterpreter
{
    internal struct Helpmenu
    {
        public readonly ulong ChannelId;
        public readonly ulong MessageID;
        public int Timestamp;
        public Helpmenu(ulong cID, ulong mID)
        {
            ChannelId = cID;
            MessageID = mID;
            Timestamp = TimerStuff.Uptime;
        }
    }
}
