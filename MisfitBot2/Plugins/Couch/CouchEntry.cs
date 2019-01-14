using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Plugins.Couch
{
    public class CouchEntry
    {
        public bool couchOpen = false;
        public int lastActivationTime = 0;
        //public int timestamp = 0;
        public List<string> TwitchUsernames = new List<string>();
    }
}
