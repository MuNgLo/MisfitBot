using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisfitBot2.Services
{
    public class TreasureSettings : PluginSettingsBase
    {
        public int _cd_tick_discord = 60; 
        public int _g_per_tick_discord = 0;
        public int _ts_last_tick_discord = 0;
        public int _cd_tick_twitch = 60;
        public int _g_per_tick_twitch = 0;
        public int _ts_last_tick_twitch = 0;
    }
}
