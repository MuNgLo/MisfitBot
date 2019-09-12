using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Plugins.Couch
{
    public class TimedMessage
    {
        public string twitchChannelName;
        public int interval;
        public int msgInterval;
        public int lastused;
        public int msgSinceLastused;
        public bool done;
    }

    public class CouchUserStats
    {
        public readonly string BotChannelKey;
        public readonly string UserKey;
        public int CountSeated;
        public int CountBooted;
        public CouchUserStats(string bKey, string uKey, int cSeated, int cBooted)
        {
            BotChannelKey = bKey;
            UserKey = uKey;
            CountSeated = cSeated;
            CountBooted = cBooted;
        }
    }
}
