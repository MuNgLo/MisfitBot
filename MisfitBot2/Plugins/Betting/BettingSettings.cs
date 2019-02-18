using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Plugins.Betting
{
    class BettingSettings : PluginSettingsBase
    {
        public int _msgInterval = 60;
        public int _msgCheckInterval = 10;
        public int reminderMinMessageBetween = 8;
        public bool apexAutomation = true;
        public int apexOpenTimer = 60;
        public int apexRoundPause = 30;
    }
}
