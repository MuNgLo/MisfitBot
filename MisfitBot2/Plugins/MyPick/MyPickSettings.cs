using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Plugins.MyPick
{
    class MyPickSettings : PluginSettingsBase
    {
        public List<ListEntry> nominees;
        public String header = "Most liked clips here are....";
        public String category = "clips";
        /// <summary>
        /// key = userID
        /// </summary>
        [JsonProperty]
        public Dictionary<ulong, Vote> votes;
        public MyPickSettings()
        {
            nominees = new List<ListEntry>();
            votes = new Dictionary<ulong, Vote>();
        }
    }

    public struct ListEntry
    {
        public string Title;
        public string Link;
    }

    public struct Vote
    {
        public string title;
        public int timestamp;
        public Vote(string titleVoted)
        {
            title = titleVoted;
            timestamp = Core.CurrentTime;
        }
    }
    public class VoteCount
    {
        public string Title;
        public string Link;
        public int Votes;
        public VoteCount(string title, string link)
        {
            Title = title;
            Link = link;
            Votes = 0;
        }
    }

    
}
