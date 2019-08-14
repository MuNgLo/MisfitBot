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
using MisfitBot2.Extensions.UserManager;

namespace MisfitBot2.Services
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
            Core.Discord.UserJoined += DiscordUserJoined;
            Core.Twitch._client.OnUserJoined += TwitchUserJoined;
            Core.Discord.UserLeft += DiscordUserLeft;
            Core.Twitch._client.OnUserLeft += TwitchUserLeft;
            Core.Twitch._client.OnMessageReceived += TwitchOnMessageReceived;
            Core.Discord.GuildMemberUpdated += GuildMemberUpdated;
            //Core.Discord._client.UserUpdated += ClientUserUpdated;
            //Core.Discord._client.CurrentUserUpdated += _client_CurrentUserUpdated;
            Core.Discord.GuildMembersDownloaded += GuildMembersDownloaded;
            Core.Discord.Ready += Ready;
            Core.UserMan = this;
            TimerStuff.OnMinuteTick += OnMinuteTick;
        }// EO CONSTRUCTOR

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
            if(twitchProfile == null || discordProfile == null)
            {
                return;
            }
            // Raise the link event so modules can listen and do whatever they need to do
            Core.RaiseUserLinkEvent(discordProfile, twitchProfile);
            // merge twitch user into discord user then elevate discord user to linked status
            discordProfile._twitchUID = twitchProfile._twitchUID;
            discordProfile._twitchUsername = twitchProfile._twitchUsername;
            discordProfile._twitchDisplayname = twitchProfile._twitchDisplayname;
            discordProfile._twitchLogo = twitchProfile._twitchLogo;
            discordProfile._twitchColour = twitchProfile._twitchColour;
            discordProfile.linked = true;
            discordProfile.lastSave = 0;
            UserList.SaveUser(discordProfile);
            SocketUser u = Core.Discord.GetUser(discordProfile._discordUID);
            await u.SendMessageAsync("Your userprofile is now the same one for both Twitch and Discord.");
            Core.Twitch._client.SendWhisper(discordProfile._twitchUsername, "Your userprofile is now the same one for both Twitch and Discord.");    
        }



        #region WTF!!! QUARANTINE THIS SHIT What is wroing here!

        /// <summary>
        /// Get UserEntry from twitch UserName. Create a new entry if none exist.
        /// </summary>
        /// <param name="twitchName"></param>
        /// <returns></returns>
        public async Task<UserEntry> GetUserByTwitchUserName(string twitchName)
        {
            if(twitchName != twitchName.ToLower())
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
        /// New user joins a guild
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task DiscordUserJoined(SocketGuildUser arg)
        {
                await Core.LOG(new LogMessage(LogSeverity.Info, "UserManagerService", $"UserJoined({arg.Username}) {arg.Guild.Name}"));
                await UpdateDiscordUserEntry(arg);
            UserEntry user = await Core.UserMan.GetUserByDiscordID(arg.Id);
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(arg.Guild.Id);
            Core.RaiseOnNewDiscordMember(bChan, user);

        }
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
        /// User leaves a guild
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task DiscordUserLeft(SocketGuildUser arg)
        {
                await Core.LOG(new LogMessage(LogSeverity.Info, "UserManagerService", $"UserLeft({arg.Username}) from {arg.Guild.Name}"));
                await UpdateDiscordUserEntry(arg);
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
        private async Task GuildMemberUpdated(SocketGuildUser arg1, SocketGuildUser arg2)
        {

            //JsonDumper.DumpObjectToJson(arg1, "GuildMemUpd");
            //JsonDumper.DumpObjectToJson(arg2, "GuildMemUpd");
            await UpdateDiscordUserEntry(arg2);
        }
        /// <summary>
        /// Fires when discord client is connected and ready.
        /// </summary>
        /// <returns></returns>
        private async Task Ready()
        {
            // When we are ready we request user lists for the guilds we are connected to.
            await Core.Discord.DownloadUsersAsync(Core.Discord.Guilds);
        }
        /// <summary>
        /// Fires as we get response from DownloadUsersAsync() (VERIFY)
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task GuildMembersDownloaded(SocketGuild arg)
        {
                await Core.LOG(new LogMessage(LogSeverity.Info, "UserManagerService", $"GuildMembersDownloaded({arg.Name})"));
                foreach (IGuildUser user in arg.Users)
                {
                    if (user.IsBot)
                    {
                        await Core.LOG(new LogMessage(LogSeverity.Info, "UserManagerService", $"GuildMembersDownloaded() ignoring bot {user.Username}."));
                    }
                    else if (user.Status == UserStatus.Online)
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
        private async Task UpdateDiscordUserEntry(SocketUser freshUser)
        {
            UserEntry storedUser = await UserList.GetDBUserByDiscordUID(freshUser.Id);
            UserStatus oldStatus = storedUser._discordStatus;
            if (freshUser.Status == UserStatus.Online)
            {
                storedUser._lastseen = Core.CurrentTime;
            }
            storedUser.lastChange = Core.CurrentTime;
            storedUser._discordStatus = freshUser.Status;
            UserList.SaveUser(storedUser);
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
