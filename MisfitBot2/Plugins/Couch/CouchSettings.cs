using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Plugins.Couch
{
    class CouchSettings : PluginSettingsBase
    {
        public int couchsize = 8;
        public int failCount = 0;
        public int lastLiveEvent = Core.CurrentTime - 1000;
        public int lastOfflineEvent = Core.CurrentTime - 1000;

        public Dictionary<string, CouchEntry> _couches = new Dictionary<string, CouchEntry>();
    }
}
