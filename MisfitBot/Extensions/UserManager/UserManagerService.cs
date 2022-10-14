using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using TwitchLib.Client.Events;
using MisfitBot_MKII.Statics;

namespace MisfitBot_MKII.Extensions.UserManager
{
    /// <summary>
    /// This handles all userentry load/saves. Returns userentriers as references so make sure to follow the lock in the entry.
    /// </summary>
    public class UserManagerService
    {
        private BotUsers UserList = new BotUsers();
        private Userlinking userlinking = new Userlinking();
        // CONSTRUCTOR
        internal UserManagerService()
        {
            Program.BotEvents.OnDiscordMembersDownloaded += OnGuildMembersDownloaded;
            Program.BotEvents.OnDiscordMemberLeft += OnDiscordMemberLeft;
            Program.BotEvents.OnDiscordMemberUpdated += OnDiscordMemberUpdated;
            Program.BotEvents.OnDiscordNewMember += OnDiscordNewMember;
            Program.BotEvents.OnDiscordReady += OnDiscordReady;
            Program.BotEvents.OnTwitchUserJoin += OnTwitchUserJoined;
            Program.BotEvents.OnTwitchUserLeave += OnTwitchUserLeft;
        }// EO CONSTRUCTOR

        internal async Task UpdateTwitchUserColour(OnMessageReceivedArgs e)
        {
            if (e == null) return;
            UserEntry user = await UserList.GetDBUserByTwitchUserName(e.ChatMessage.Username);
            if (user == null) return;
            user._twitchColour = e.ChatMessage.ColorHex;
            user._lastseenOnTwitch = Core.CurrentTime;
        }


        #region Discord Event Listeners
        /// <summary>
        /// New user joins a guild
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private void OnDiscordNewMember(BotChannel bChan, UserEntry user)
        {
            Core.LOG(new LogEntry(LOGSEVERITY.INFO, "UserManagerService", $"UserJoined({user._discordUsername}) {bChan.GuildName}"));
            UpdateDiscordUserEntry(user);

        }
        /// <summary>
        /// User leaves a guild
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private void OnDiscordMemberLeft(BotChannel bChan, UserEntry user)
        {
            Core.LOG(new LogEntry(LOGSEVERITY.INFO, "UserManagerService", $"UserLeft({user._discordUsername}) from {bChan.GuildName}"));
            UpdateDiscordUserEntry(user);
        }
        private void OnDiscordMemberUpdated(BotChannel botChannel, UserEntry currentUser, UserEntry oldUser){

        }
        /// <summary>
        /// Fires when discord client is connected and ready.
        /// </summary>
        /// <returns></returns>
        private async void OnDiscordReady()
        {
            // When we are ready we request user lists for the guilds we are connected to.
            await Program.DiscordClient.DownloadUsersAsync(Program.DiscordClient.Guilds);
        }
        /// <summary>
        /// Fires as we get response from DownloadUsersAsync() (VERIFY)
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async void OnGuildMembersDownloaded(SocketGuild arg)
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
        #region Twitch Event Listeners
        /// <summary>
        /// User leaves a twitch channel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnTwitchUserLeft(BotChannel botChannel, UserEntry user)
        {
            await Task.Run(()=>{
                while(user.locked){
                    Thread.Sleep(50);
                }
            user._lastseenOnTwitch = Core.CurrentTime;
            });
        }
        /// <summary>
        /// User join a twitch channel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnTwitchUserJoined(BotChannel botChannel, UserEntry user)
        {
            await Task.Run(()=>{
                while(user.locked){
                    Thread.Sleep(50);
                }
            user._lastseenOnTwitch = Core.CurrentTime;
            });
        }
        #endregion
        #region User Linking related
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
            SocketUser u = Program.DiscordClient.GetUser(discordProfile._discordUID);
            await u.SendMessageAsync("Your userprofile is now the same one for both Twitch and Discord.");
            Program.TwitchClient.SendWhisper(discordProfile._twitchUsername, "Your userprofile is now the same one for both Twitch and Discord.");
        }
        #endregion

        #region Get User Methods
        /// <summary>
        /// Get UserEntry from twitch UserName. Create a new entry if none exist.
        /// </summary>
        /// <param name="twitchName"></param>
        /// <returns></returns>
        public async Task<UserEntry> GetUserByTwitchUserName(string twitchName)
        {
            return await UserList.GetDBUserByTwitchUserName(twitchName);
        }
        /// <summary>
        /// Get UserEntry from twitch DisplayName. Return NULL if cant be found
        /// </summary>
        /// <param name="twitchDisplayName"></param>
        /// <returns></returns>
        public async Task<UserEntry> GetUserByTwitchDisplayName(string twitchDisplayName)
        {
            return await UserList.GetDBUserByTwitchDisplayName(twitchDisplayName);
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
        
        /// <summary>
        /// This can return null
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<UserEntry> GetUserByKey(string key)
        {
            return await UserList.GetDBUserByTwitchID(key);
        }

        public async Task<TwitchLib.Api.Helix.Models.Users.GetUsers.User> GetUserByTwitchIDFromAPI(string twitchID)
        {
            return await UserList.GetTwitchUserByIDFromAPI(twitchID);
        }
        public async Task<TwitchLib.Api.Helix.Models.Users.GetUsers.User> GetUserByTwitchUsernameFromAPI(string twUsername)
        {
            return await UserList.GetTwitchUserByUserNameFromAPI(twUsername);
        }
        public async Task<TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse> GetUsersByTwitchUsernamesFromAPI(List<string> twUsernames)
        {
            return await UserList.GetTwitchUsersByUserNamesFromAPI(twUsernames);
        }
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
        #endregion

        

        







        /// <summary>
        /// This also creates a new user if needed
        /// </summary>
        /// <param name="freshUser"></param>
        /// <returns></returns>
        private void UpdateDiscordUserEntry(UserEntry freshUser)
        {
            freshUser._lastseen = Core.CurrentTime;
            freshUser.lastChange = Core.CurrentTime;
        }
        private async Task UpdateDiscordUserEntry(SocketUser freshUser)
        {
            UserEntry user = await Program.Users.GetUserByDiscordID(freshUser.Id);
            UpdateDiscordUserEntry(user);
        }

        #region MISC
        public string UserStats() // TODO FIX later on
        {
            return $"UsersManagerStats: Cached Users[{UserList.CachedUserCount}] last Cache clean dropped [{UserList.LastCacheUserDropCount}] users.";
        }
        #endregion

        #region UNUSED
        /// <summary>
        /// Not entirely sure when this is called
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        /*private async Task ClientUserUpdated(SocketUser arg1, SocketUser arg2)
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, EXTENSIONNAME, "_client_UserUpdated")); // might be a staller
        }*/
        /// <summary>
        /// Duuno when this fires really
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        /*private async Task _client_CurrentUserUpdated(SocketSelfUser arg1, SocketSelfUser arg2)
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, EXTENSIONNAME, "CurrentUserUpdated"));
        }*/

        /*internal async Task<List<UserEntry>> SearchUserName(string search)
        {
            return await UserList.SearchDBUserByName(search);
        }*/
        #endregion
    }// EOF CLASS
}
