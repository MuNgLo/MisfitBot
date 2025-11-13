using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MisfitBot_MKII;
using MisfitBot_MKII.DiscordWrap;
using MisfitBot_MKII.Statics;
//using Discord;
//using Discord.WebSocket;

namespace CommunityPicksPlugin
{
    /// <summary>
    /// This represents a single pick structure for users to vote on. As in one ongoing vote in a channel.
    /// </summary>
    internal class PickEntry
    {
        public readonly ulong DiscordChannelID;
        public readonly string Name;
        //private ulong pinnedMessageID;
        private List<Nominee> nominated; // List of all the nominated entries for this pick
        private VotesCache votes;
        private int lastIDUsed = 0;
        public int LastUsedID {get => lastIDUsed; private set{} }
        public int NextID {get  {lastIDUsed++; return lastIDUsed; } private set{} }

        internal PickEntry(BotChannel bChan, ulong dChannelID, string title)
        {
            nominated = new List<Nominee>();
            votes = new VotesCache();
            DiscordChannelID = dChannelID;
            Name = title;
            //pinnedMessageID = 0;
        }
        /// <summary>
        /// Tries to add a candidate to the nominee list, Returns response
        /// </summary>
        /// <param name="nomination"></param>
        /// <param name="userKey"></param>
        /// <returns></returns>
        internal async Task<string> Nominate(BotChannel bChan, UserEntry user, string nomination)
        {
            string result = string.Empty;
            // If it exists
            if (nominated.Exists(p => p.Name == nomination))
            {
                Nominee nom = nominated.Find(p => p.Name == nomination);
                UserEntry submitter = await Program.Users.GetUserByDiscordID(nom.SubmitterDID);
                result = $"\"{nomination}\" is already nominated by {submitter.discordUsername}";
                if (!nom.InUse)
                {
                    UserEntry lastMod = await Program.Users.GetUserByDiscordID(nom.LastModActionByDID);
                    result += $" Nomination rejected by {lastMod.discordUsername}.";
                }
            }else{

                Discord.Rest.RestUserMessage msg = await (DiscordClient.DiscordSayMessage(bChan.discordAdminChannel, $"Nomination for {nomination} submitted."));
                DiscordChannelMessage dMessage = await DiscordClient.DiscordGetMessage(bChan.discordAdminChannel, msg.Id);

                nominated.Add(new Nominee(NextID, nomination, user.discordUID){messageID = msg.Id, Timestamp = Core.CurrentTime});
                await DiscordClient.ReactionAdd(dMessage as DiscordChannelMessage, "👍");
                await DiscordClient.ReactionAdd(dMessage as DiscordChannelMessage, "👎");
                result = $"Nominated \"{nomination}\".";
            }
            return result;
        }
        
    }
}