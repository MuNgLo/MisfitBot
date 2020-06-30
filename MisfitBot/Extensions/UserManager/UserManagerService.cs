using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using TwitchLib.Api.V5.Models.Users;
using MisfitBot_MKII.Extensions.UserManager;

namespace MisfitBot_MKII.Services
{
    /// <summary>
    /// Handles the Userentries that represnets the users.
    /// </summary>
    public class UserManagerService
    {
        private readonly string PLUGINNAME = "UserManager";
        private BotUsers UserList = new BotUsers();
        private Userlinking userlinking = new Userlinking();

        // CONSTRUCTOR
        public UserManagerService()
        {
            Program.BotEvents.OnDiscordNewMember += DiscordNewMember;
            Program.BotEvents.OnDiscordMemberLeft += DiscordMemberLeft;
            Program.BotEvents.OnDiscordMemberUpdated += DiscordMemberUpdated;
            Program.BotEvents.OnDiscordMembersDownloaded += GuildMembersDownloaded;
            Program.BotEvents.OnDiscordReady += Ready;
            //Program.DiscordClient._client.UserUpdated += ClientUserUpdated;
            //Program.DiscordClient._client.CurrentUserUpdated += _client_CurrentUserUpdated;

            if (Program.TwitchClient != null)
            {
                Program.TwitchClient.OnUserJoined += TwitchUserJoined;
                Program.TwitchClient.OnUserLeft += TwitchUserLeft;
                Program.TwitchClient.OnMessageReceived += TwitchOnMessageReceived;
            }
            TimerStuff.OnMinuteTick += OnMinuteTick;
        }// EO CONSTRUCTOR

        /// <summary>
        /// New user joins a guild
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private void DiscordNewMember(BotChannel bChan, UserEntry user)
        {
            Core.LOG(new LogMessage(LogSeverity.Info, "UserManagerService", $"UserJoined({user._username}) {bChan.GuildName}"));
            UpdateDiscordUserEntry(user);

        }
        /// <summary>
        /// User leaves a guild
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private void DiscordMemberLeft(BotChannel bChan, UserEntry user)
        {
            Core.LOG(new LogMessage(LogSeverity.Info, "UserManagerService", $"UserLeft({user._username}) from {bChan.GuildName}"));
            UpdateDiscordUserEntry(user);
        }
        private void DiscordMemberUpdated(BotChannel botChannel, UserEntry currentUser, UserEntry oldUser){

        }
        
        
        
        
        private async void TwitchOnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            try
            {
                await UserList.UpdateTwitchUserColour(e); // TODO don't do this all the time
            }
            catch (Exception error)
            {
                await Core.LOG(new LogMessage(LogSeverity.Error, PLUGINNAME, error.Message));
            }
        }

        public async Task LinkTokenRequest(ulong discordID, IMessageChannel discordChannel)
        {
            UserEntry user = await GetUserByDiscordID(discordID);
            await userlinking.SetupAndInformLinkToken(user);
        }
        /// <summary>
        /// Links a Discord UserEnty and a Twitch UserEntry. Also raises the Core.OnUserEntryMerge event.
        /// </summary>
        /// <param name="discordProfile"></param>
        /// <param name="twitchProfile"></param>
        /// <returns></returns>
        public async Task LinkAccounts(UserEntry discordProfile, UserEntry twitchProfile)
        {
            // Null check the profiles
            if (twitchProfile == null || discordProfile == null)
            {
                return;
            }
            // Raise the link event so modules can listen and do whatever they need to do
            Program.BotEvents.RaiseUserLinkEvent(discordProfile, twitchProfile);
            // merge twitch user into discord user then elevate discord user to linked status
            discordProfile._twitchUID = twitchProfile._twitchUID;
            discordProfile._twitchUsername = twitchProfile._twitchUsername;
            discordProfile._twitchDisplayname = twitchProfile._twitchDisplayname;
            discordProfile._twitchLogo = twitchProfile._twitchLogo;
            discordProfile._twitchColour = twitchProfile._twitchColour;
            discordProfile.linked = true;
            discordProfile.lastSave = 0;
            UserList.SaveUser(discordProfile);
            SocketUser u = Program.DiscordClient.GetUser(discordProfile._discordUID);
            await u.SendMessageAsync("Your userprofile is now the same one for both Twitch and Discord.");
            Program.TwitchClient.SendWhisper(discordProfile._twitchUsername, "Your userprofile is now the same one for both Twitch and Discord.");
        }



        #region WTF!!! QUARANTINE THIS SHIT What is wroing here!

        /// <summary>
        /// Get UserEntry from twitch UserName. Create a new entry if none exist.
        /// </summary>
        /// <param name="twitchName"></param>
        /// <returns></returns>
        public async Task<UserEntry> GetUserByTwitchUserName(string twitchName)
        {
            if (twitchName != twitchName.ToLower())
            {
                return null;
            }

            return await UserList.GetDBUserByTwitchUserName(twitchName);
        }
        /// <summary>
        /// This can return null
        /// </summary>
        /// <param name="twitchID"></param>
        /// <returns></returns>
        public async Task<UserEntry> GetUserByTwitchID(string twitchID)
        {
            return await UserList.GetDBUserByTwitchID(twitchID);
        }

        #endregion

        /// <summary>
        /// Get UserEntry from Discord user ID. Create a new entry if none exist.
        /// </summary>
        /// <param name="discordID"></param>
        /// <returns></returns>
        public async Task<UserEntry> GetUserByDiscordID(ulong discordID)
        {
            return await UserList.GetDBUserByDiscordUID(discordID);
            //return await UserList.GetUserByDiscordID(discordID);
        }
        private void OnMinuteTick(int minute)
        {
            //if (minute % FLUSHINTERVAL == 0) FlushUsers();
        }
        #region User Join/Left
        
        /// <summary>
        /// User join a twitch channel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TwitchUserJoined(object sender, TwitchLib.Client.Events.OnUserJoinedArgs e)
        {

            UserEntry user = await UserList.GetDBUserByTwitchUserName(e.Username);
            if (user == null)
            {
                return;
            }
            user._lastseenOnTwitch = Core.CurrentTime;
            UserList.SaveUser(user);
        }
        
        /// <summary>
        /// User leaves a twitch channel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TwitchUserLeft(object sender, TwitchLib.Client.Events.OnUserLeftArgs e)
        {
            UserEntry user = await UserList.GetDBUserByTwitchUserName(e.Username);
            if (user != null)
            {
                user._lastseenOnTwitch = Core.CurrentTime;
                //user.RemoveTwitchChannel(e.Channel);
            }
        }
        #endregion

        #region Discord Events
        /// <summary>
        /// Unsure. Betting it is firing when a user has their guild access changed.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        
        /// <summary>
        /// Fires when discord client is connected and ready.
        /// </summary>
        /// <returns></returns>
        private async void Ready()
        {
            // When we are ready we request user lists for the guilds we are connected to.
            await Program.DiscordClient.DownloadUsersAsync(Program.DiscordClient.Guilds);
        }
        /// <summary>
        /// Fires as we get response from DownloadUsersAsync() (VERIFY)
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async void GuildMembersDownloaded(SocketGuild arg)
        {
            foreach (IGuildUser user in arg.Users)
            {
                if (user.Status == UserStatus.Online && !user.IsBot)
                {
                    await UpdateDiscordUserEntry(user as SocketUser);
                }
            }
        }
        #endregion Discord Events







        /// <summary>
        /// This also creates a new user if needed
        /// </summary>
        /// <param name="freshUser"></param>
        /// <returns></returns>
        private void UpdateDiscordUserEntry(UserEntry freshUser)
        {
            freshUser._lastseen = Core.CurrentTime;
            freshUser.lastChange = Core.CurrentTime;
            UserList.SaveUser(freshUser);
        }
        private async Task UpdateDiscordUserEntry(SocketUser freshUser)
        {
            UserEntry user = await Program.Users.GetUserByDiscordID(freshUser.Id);
            UpdateDiscordUserEntry(user);
        }

        #region MISC
        public string GetUSerStats() // TODO FIX later on
        {
            int linked = 0, discord = 0, twitch = 0;
            return $"   Users in memory: [Discord]{discord}  [Twitch]{twitch}  [Linked]{linked}";
        }

        /*
       internal void SetPluginUserValues(string pLUGINNAME, string userID, string twitchchannelID, object entry)
        {
            
        }
        internal void SetPluginUserValues(string pLUGINNAME, ulong userID, ulong twitchchannelID, object entry)
        {

        }
        internal void SetPluginUserValues(string pLUGINNAME, UserEntry userID, BotChannel twitchchannelID, object entry)
        {

        }
        */
        #endregion
        /// <summary>
        /// Not entirely sure when this is called
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        private async Task ClientUserUpdated(SocketUser arg1, SocketUser arg2)
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, PLUGINNAME, "_client_UserUpdated")); // might be a staller
        }
        /// <summary>
        /// Duuno when this fires really
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        private async Task _client_CurrentUserUpdated(SocketSelfUser arg1, SocketSelfUser arg2)
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, PLUGINNAME, "CurrentUserUpdated"));
        }

        internal async Task<List<UserEntry>> SearchUserName(string search)
        {
            return await UserList.SearchDBUserByName(search);
        }
    }// END of UserManagerService
}
