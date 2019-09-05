using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Plugins.Couch
{
    class CouchSettings : PluginSettingsBase
    {
        public int couchsize = 8;
        public int potatoGreeting = 10;
        public int failCount = 0;
        public int openTime = 600;
        public int maxFails = 3; // respond to this many tries after time has run out. then stay silent
        public int failcount = 0; // Current couch number of fails
        public int lastLiveEvent = Core.CurrentTime - 1000;
        public int lastOfflineEvent = Core.CurrentTime - 1000;
        public int reminderInterval = 280;
        public int reminderMessageInterval = 7;

        public Dictionary<string, CouchEntry> _couches = new Dictionary<string, CouchEntry>();
        public List<string> _greeted = new List<string>();

    }
}
