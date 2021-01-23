
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace MisfitBot_MKII.Extensions.PubSub
{
    internal class PubSubManager
    {
        private readonly string PLUGINNAME = "PubSubManager";
        private bool debug = false;
        public Dictionary<string, PubSubConnection> PubSubClients = new Dictionary<string, PubSubConnection>();

        /// <summary>
        /// Tries to restart PubSub listener for given Botchannel if there is one.
        /// </summary>
        /// <param name="bChan"></param>
        /// <returns></returns>
        internal async Task RestartPubSub(BotChannel bChan)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, PLUGINNAME, "RestartPubSub"));
            if (bChan.TwitchChannelID == null || bChan.TwitchChannelID == string.Empty)
            {
                return;
            }
            if (PubSubClients.ContainsKey(bChan.TwitchChannelID))
            {
                PubSubStop(bChan);
                StartPubSub(bChan, debug);
            }
            else
            {
                await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, PLUGINNAME, "RestartPubSub::No pubsub running on that channelID."));
            }
        }
        /// <summary>
        /// Tries to start a PubSub listener for every botchannel in DB.
        /// </summary>
        /// <returns></returns>
        internal async Task LaunchAllPubSubs()
        {
            foreach (BotChannel bChan in await Program.Channels.GetChannels())
            {
                StartPubSub(bChan, debug);
            }

        }
        /// <summary>
        
        /// Launches individual PubSub for given Botchannel if it has a token.
        /// </summary>
        /// <param name="bChan"></param>
        internal async void StartPubSub(BotChannel bChan, bool verbose = false)
        {
            // Debug end
            if (bChan.pubsubOauth != string.Empty)
            {
                if (!PubSubClients.ContainsKey(bChan.TwitchChannelID))
                {
                    //await Core.LOG(new LogEntry(LOGSEVERITY.INFO, PLUGINNAME, $"Pubsub starting for {bChan.TwitchChannelName}."));
                    PubSubClients[bChan.TwitchChannelID] = new PubSubConnection(bChan.pubsubOauth, bChan.TwitchChannelID, bChan.TwitchChannelName, verbose);
                }
                else
                {
                    if (bChan.discordAdminChannel != 0)
                    {
                        await (Program.DiscordClient.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                        $"A pubsub client for that channel already exists."
                        );
                    }
                }
            }
        }
        internal void PubSubStop(BotChannel bChan) {
            if(PubSubClients[bChan.TwitchChannelID] != null){
                PubSubClients[bChan.TwitchChannelID].Close();
                PubSubClients.Remove(bChan.TwitchChannelID);
            }
        }
        internal string PubSubStatus(BotChannel bChan) {
            if(PubSubClients[bChan.TwitchChannelID] != null){
                return "PubSub is running";
            }
            return "No pubsub instance running.";
        }
    }// EOF CLASS
}