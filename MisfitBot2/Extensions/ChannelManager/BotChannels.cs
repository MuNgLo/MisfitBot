using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MisfitBot2.Extensions.ChannelManager
{
    public class BotChannels
    {
        //public Dictionary<string, BotChannel> Channels { get; private set; } = new Dictionary<string, BotChannel>();
        private List<BotChannel> Channels = new List<BotChannel>();

        public async Task JoinAllAutoJoinTwitchChannels()
        {
            List<string> chansToLookup = new List<string>();
            foreach(BotChannel chan in Channels)
            {
                if (chan.isTwitch && chan.TwitchAutojoin) {
                    chansToLookup.Add(chan.TwitchChannelName);
                    }
            }
            if(chansToLookup.Count < 1)
            {
                return;
            }
            TwitchLib.Api.V5.Models.Users.Users channelEntry = await Core.Twitch._api.V5.Users.GetUsersByNameAsync(chansToLookup);
            if (channelEntry.Matches.Length < 1)
            {
                return;
            }

            foreach (TwitchLib.Api.V5.Models.Users.User usr in channelEntry.Matches)
            {

                    var channel = Core.Twitch._client.GetJoinedChannel(usr.Name);
                    if (channel == null)
                    {

                        Core.Twitch._client.JoinChannel(usr.Name);
                    }
                
            }
        }

        public List<BotChannel> GetChannels()
        {
            return Channels;
        }


        public BotChannel GetBotchannelByKey(string key)
        {
            if (Channels.FindAll(p => p.Key == key).Count == 1)
            {
                return Channels.Find(p => p.Key == key);
            }
            else if (Channels.FindAll(p => p.Key == key).Count > 1)
            {
                return Channels.FindAll(p => p.Key == key).Find(p => p.isLinked == true);
            }
            return null;
        }
        /// <summary>
        /// Returns the BotChannel for the Twitch Channel. Proritizes linked if more then one is found. Can return NULL
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BotChannel GetTwitchChannelByID(string id)
        {
            
            if (Channels.FindAll(p=>p.TwitchChannelID == id).Count == 1) {
                return Channels.Find(p => p.TwitchChannelID == id);
            }
            else if(Channels.FindAll(p => p.TwitchChannelID == id).Count > 1)
            {
                return Channels.FindAll(p => p.TwitchChannelID == id).Find(p => p.isLinked == true);
            }
            return null;
        }
        /*public async Task<BotChannel> GetTwitchChannelByName(string name)
        {
            if (Channels.FindAll(p => p.TwitchChannelName == name).Count == 1)
            {
                return Channels.Find(p => p.TwitchChannelName == name);
            }
            else if (Channels.FindAll(p => p.TwitchChannelName == name).Count > 1)
            {
                return Channels.FindAll(p => p.TwitchChannelName == name).Find(p => p.isLinked == true);
            }
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, "BotChannels", "Couldn't match the Twitch channelname. Returning NULL."));
            return null;
        }
        /// <summary>
        /// Tries to add a channel. Linked channels needs unlinked guildid and twitchchannelname. Unlinked needs unique guildid and twitchID. returns false if it fails;
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public async Task<bool> AddChannel(BotChannel channel)
        {
            if (channel.isLinked)
            {

                if (Channels.FindAll(p=>p.isLinked).Exists(p => p.GuildID == channel.GuildID))
                {
                    return false;
                }
                if (Channels.FindAll(p => p.isLinked).Exists(p => p.TwitchChannelName == channel.TwitchChannelName))
                {
                    return false;
                }
                Channels.Add(channel);
                return true;
            }
            else
            {
                if (channel.TwitchChannelID != string.Empty)
                {
                    if (Channels.Exists(p => p.TwitchChannelID == channel.TwitchChannelID))
                    {
                        return false;
                    }
                }
                if (channel.GuildID != 0)
                {
                    if (Channels.Exists(p => p.GuildID == channel.GuildID))
                    {
                        return false;
                    }
                }
                Channels.Add(channel);
                return true;
            }
        }*/
    }
}
