using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Plugins.Couch
{
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
        public class CouchDBString
    {
        public readonly int _id;
        public readonly bool _inuse;
        public readonly string _topic;
        public readonly string _text;
        public CouchDBString(int id, bool inuse, string topic, string text)
        {
            _id = id; _inuse = inuse; _topic = topic; _text = text;
        }
    }
}
