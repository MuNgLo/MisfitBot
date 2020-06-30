using System.Collections.Generic;

namespace MisfitBot_MKII.Plugins.Admin
{
    class AdminSettings : PluginSettingsBase
    {
        public List<string> _bannedTwitchIDs = new List<string>();
        public List<ulong> _bannedDiscordIDs = new List<ulong>();
        public bool _announceStreamEvents = false;
    }
}
