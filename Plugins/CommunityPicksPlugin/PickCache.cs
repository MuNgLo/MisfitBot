using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using MisfitBot_MKII;
using MisfitBot_MKII.DiscordWrap;

namespace CommunityPicksPlugin{
    /// <summary>
    /// This manages automatically the picks that are stored in memory as well as keeping the database entries updated
    /// </summary>
    internal class PickCache{

        private List<PickEntry> Picks;

        internal PickCache(){
            Picks = new List<PickEntry>();
        }

        internal bool HasPick(ulong dChan)
        {
            return Picks.Exists(p=>p.DiscordChannelID == dChan);
        }
        internal async void CreateNewPick(BotChannel bChan, ulong dChan, string title)
        {
             PickEntry pick = new PickEntry(bChan, dChan, title);
             Picks.Add(pick);

            Discord.Rest.RestUserMessage msg = await (DiscordClient.DiscordSayMessage(dChan, $"Community Pick started >>> {title}"));
            DiscordChannelMessage dMessage = await DiscordClient.DiscordGetMessage(dChan, msg.Id);
            await (msg as IUserMessage).PinAsync();

        }
        internal PickEntry GetPick(ulong dChan)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Calls teh relevent Nominate for the pick running in the discordchannel. If there is any
        /// </summary>
        /// <param name="dChan"></param>
        /// <param name="user"></param>
        /// <param name="nomination"></param>
        /// <returns></returns>
        internal async Task<string> Nominate(ulong dChan, BotChannel bChan, UserEntry user, string nomination)
        {
            // TODO check if running pick is now valid in timframe and all
            if(HasPick(dChan)){
                return await Picks.Find(p=>p.DiscordChannelID == dChan).Nominate(bChan, user, nomination);
            }
            return "Sorry but there is no pick running in this channel.";
        }

        


        #region DB methods
        private bool DBDoesNominationExist(ulong dChan, string nominatio){
            return false;
        }

        
        #endregion
    }
}