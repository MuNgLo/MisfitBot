using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Plugins.MyPick
{
    class MyPickSettings : PluginSettingsBase
    {
        public List<string> nominees;
        /// <summary>
        /// key = userID
        /// </summary>
        [JsonProperty]
        public Dictionary<ulong, Vote> votes;
        public MyPickSettings()
        {
            nominees = new List<string>();
            votes = new Dictionary<ulong, Vote>();
        }
    }

    public struct Vote
    {
        public string game;
        public int timestamp;
        public Vote(string title)
        {
            game = title;
            timestamp = Core.CurrentTime;
        }
    }
    public class VoteCount
    {
        public string game;
        public int votes;
        public VoteCount(string title)
        {
            game = title;
            votes = 0;
        }
    }

    
}
