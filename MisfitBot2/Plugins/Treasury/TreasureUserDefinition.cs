using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisfitBot2.Services
{
    public class TreasureUserDefinition
    {
        [JsonProperty]
        public int _created;
        [JsonProperty]
        public int _gold;
        [JsonProperty]
        public int _TS_LastMessage;
        [JsonProperty]
        public int _TS_LastReaction;
        [JsonProperty]
        public int _TS_LastTick;

        public TreasureUserDefinition(int created, int gold = 0)
        {
            _gold = gold;
            _created = created;
            _TS_LastMessage = created - 600;
            _TS_LastReaction = created - 600;
            _TS_LastTick = created;
        }
    }
}
