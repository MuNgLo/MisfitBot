using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Plugins.Couch
{
    public class CouchUserStats
    {
        public readonly string UserKey;
        public int CountSeated;
        public int CountBooted;
        public CouchUserStats(string uKey, int cSeated, int cBooted)
        {
            UserKey = uKey;
            CountSeated = cSeated;
            CountBooted = cBooted;
        }
    }
}
