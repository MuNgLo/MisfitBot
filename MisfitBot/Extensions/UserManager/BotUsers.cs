using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using MisfitBot_MKII.Statics;

namespace MisfitBot_MKII.Extensions.UserManager
{
    /// <summary>
    /// This class handles the caching of userentries and the database access
    /// Make sure nothing bypass this and access the user table
    /// </summary>
    internal class BotUsers
    {
        private readonly string EXTENSIONNAME = "BotUsers";
        private List<UserEntry> _UserCache;
        private int _lastCacheDrop = -1;
        internal int CachedUserCount {get {return _UserCache.Count;}}
        internal int LastCacheUserDropCount {get {return _lastCacheDrop;}}
        
        internal BotUsers(){
            TimerStuff.OnMinuteTick += OnMinuteTick;
            _UserCache = new List<UserEntry>();
            if (!TableExists())
            {
                TableCreate();
            }
        }
        private void OnMinuteTick(int minute){
            int preCount = _UserCache.Count;
            _UserCache.RemoveAll(p=>p.lastSeen < Core.CurrentTime - 600);
            if(preCount != _UserCache.Count){
                _lastCacheDrop = preCount - _UserCache.Count;
            }
        }
        #region User getters
        internal async Task<UserEntry> GetDBUserByTwitchUserName(string twitchUsername)
        {
            if(!_UserCache.Exists(p=>p.twitchUsername == twitchUsername))
            {
                await FetchDBUserByTwitchUserName(twitchUsername);
            }
            return _UserCache.Find(p=>p.twitchUsername == twitchUsername);
        }
        internal async Task<UserEntry> GetDBUserByTwitchDisplayName(string twitchDisplayName)
        {
            if(!_UserCache.Exists(p=>p.twitchDisplayName == twitchDisplayName))
            {
                return await FetchDBUserByTwitchDisplayName(twitchDisplayName);
            }
            return _UserCache.Find(p=>p.twitchDisplayName == twitchDisplayName);
        }
        internal async Task<UserEntry> GetDBUserByTwitchID(string uid)
        {
            if(!_UserCache.Exists(p=>p.twitchUID == uid))
            {
                await FetchDBUserByTwitchID(uid);
            }
            return _UserCache.Find(p=>p.twitchUID == uid);
        }
        
        internal async Task<UserEntry> GetDBUserByDiscordUID(ulong uid)
        {
            if(!_UserCache.Exists(p=>p.discordUID == uid))
            {
                await FetchDBUserByDiscordUID(uid);
            }
            return _UserCache.Find(p=>p.discordUID == uid);
        }
        #endregion
        #region Database user getters
        /// <summary>
        /// Gets the user matching by twitch username. Looks user up and creates a new entry when unknown user.
        /// Fails rarely
        /// </summary>
        /// <param name="twitchUsername"></param>
        /// <returns></returns>
        private async Task FetchDBUserByTwitchUserName(string twitchUsername)
        {
            UserEntry user = new UserEntry();
            if (!await DBUserExistsTwitchName(twitchUsername))
            {
                await CreateNewTwitchUserFromName(twitchUsername);
            }
            if (await DBUserExistsTwitchName(twitchUsername))
            {
                user = DBReadTwitchUserByName(twitchUsername);
                if(user == null){
                    await Core.LOG(new LogEntry(LOGSEVERITY.CRITICAL, "BotUsers", "FetchDBUserByTwitchUserName() User created in db. Exist returned True but getting the user returned NULL"));
                    return;
                }
                user.lastSeen = Core.CurrentTime;
                _UserCache.Add(user);
            }else{
                await Core.LOG(new LogEntry(LOGSEVERITY.CRITICAL, "BotUsers", "DB Tw User lookup failed when we should have just made one!"));
            }
        }
        private async Task<UserEntry> FetchDBUserByTwitchDisplayName(string twitchDisplayName)
        {
            if (await DBUserExistsTwitchName(twitchDisplayName))
            {
                return DBReadTwitchUserByName(twitchDisplayName);
            }
            return null;
        }
        private async Task FetchDBUserByTwitchID(string uid)
        {
            if(uid=="0"){
                throw new ArgumentException($"Twitch userID to lookup is 0 ?? WTF!");
            }
            UserEntry user = new UserEntry();
            if (!await DBUserExistsTwitchID(uid))
            {
                CreateNewTwitchUserFromID(uid);
            }
            if (await DBUserExistsTwitchID(uid))
            {
                user = DBReadTwitchUser(uid);
            }
            user.lastSeen = Core.CurrentTime;
            _UserCache.Add(user);
        }
        private async Task FetchDBUserByDiscordUID(ulong uid)
        {
            UserEntry user = new UserEntry();
            if (!await DBUserExistsDiscordUID(uid))
            {
                CreateNewDiscordUser(uid, user);
            }
            user = DBReadDiscordUser(uid);
            user.lastSeen = Core.CurrentTime;
            _UserCache.Add(user);
        }
        #endregion
        #region DATABASE manipulation stuff
        /// <summary>
        /// Checks if User table exists and returns result
        /// </summary>
        /// <returns></returns>
        private bool TableExists()
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = "SELECT COUNT(*) AS QtRecords FROM sqlite_master WHERE type = 'table' AND name = @name";
                cmd.Parameters.AddWithValue("@name", EXTENSIONNAME);
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        /// <summary>
        /// Creates the User table in the database
        /// </summary>
        private void TableCreate()
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"CREATE TABLE \"{EXTENSIONNAME}\" (" +
                    $"linked BOOLEAN, " +
                    $"username VACHAR(30)," +
                    $"lastseen INTEGER, " +
                    $"lastseenOnTwitch INTEGER, " +
                    $"twitchUID VACHAR(30)," +
                    $"twitchUsername VACHAR(30)," +
                    $"twitchDisplayname VACHAR(30)," +
                    $"twitchColour VACHAR(30)," +
                    $"twitchLogo VACHAR(100)," +
                    $"twitchCreated VACHAR(30)," +
                    $"twitchLastUpdate VACHAR(30)," +
                    $"discordUID INTEGER, " +
                    $"discordStatus INTEGER, " +
                    $"lastChange INTEGER, " +
                    $"lastSave INTEGER" +
                    $")";
                cmd.ExecuteNonQuery();
            }
        }

        private async Task<bool> DBUserExistsDiscordUID(ulong discordUID)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{EXTENSIONNAME}\" WHERE discordUID IS @discordUID";
                cmd.Parameters.AddWithValue("@discordUID", discordUID);

                if (await cmd.ExecuteScalarAsync() == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        private async Task<bool> DBUserExistsTwitchID(string twitchUID)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{EXTENSIONNAME}\" WHERE twitchUID IS @twitchUID";
                cmd.Parameters.AddWithValue("@twitchUID", twitchUID);

                if (await cmd.ExecuteScalarAsync() == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        private async Task<bool> DBUserExistsTwitchName(string name)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{EXTENSIONNAME}\" WHERE twitchUsername IS @name";
                cmd.Parameters.AddWithValue("@name", name);

                var asd = await cmd.ExecuteScalarAsync();
                if (asd == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        #region DB entries readers 
        private UserEntry DBReadDiscordUser(ulong uid)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{EXTENSIONNAME}\" WHERE discordUID IS @uid";
                cmd.Parameters.AddWithValue("@uid", uid);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    throw;
                }
                result.Read();
                UserEntry user = new UserEntry
                {
                    linked = result.GetBoolean(0),
                    discordUsername = result.GetString(1),
                    lastSeen = result.GetInt32(2),
                    lastSeenOnTwitch = result.GetInt32(3),
                    twitchUID = result.GetString(4),
                    twitchUsername = result.GetString(5),
                    twitchDisplayName = result.GetString(6),
                    twitchColour = result.GetString(7),
                    twitchLogo = result.GetString(8),
                    discordUID = (ulong)result.GetInt64(11),
                    lastChange = (int)result.GetInt64(13),
                    lastSave = (int)result.GetInt64(14)
                };
                return user;
            }
        }
        private UserEntry DBReadTwitchUser(string uid)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{EXTENSIONNAME}\" WHERE twitchUID IS @uid";
                cmd.Parameters.AddWithValue("@uid", uid);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    throw;
                }
                result.Read();
                UserEntry user = new UserEntry
                {
                    linked = result.GetBoolean(0),
                    discordUsername = result.GetString(1),
                    lastSeen = result.GetInt32(2),
                    lastSeenOnTwitch = result.GetInt32(3),
                    twitchUID = result.GetString(4),
                    twitchUsername = result.GetString(5),
                    twitchDisplayName = result.GetString(6),
                    twitchColour = result.GetString(7),
                    twitchLogo = result.GetString(8),
                    discordUID = (ulong)result.GetInt64(11),
                    lastChange = (int)result.GetInt64(13),
                    lastSave = (int)result.GetInt64(14)
                };
                return user;
            }
        }
        /// <summary>
        /// Returns match from DB or NULL
        /// </summary>
        /// <param name="twitchname"></param>
        /// <returns></returns>
        private UserEntry DBReadTwitchUserByName(string twitchname)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{EXTENSIONNAME}\" WHERE twitchUsername IS @twitchname";
                cmd.Parameters.AddWithValue("@twitchname", twitchname);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    throw;
                }
                result.Read();
                UserEntry user = new UserEntry
                {
                    linked = result.GetBoolean(0),
                    discordUsername = result.GetString(1),
                    lastSeen = result.GetInt32(2),
                    lastSeenOnTwitch = result.GetInt32(3),
                    twitchUID = result.GetString(4),
                    twitchUsername = result.GetString(5),
                    twitchDisplayName = result.GetString(6),
                    twitchColour = result.GetString(7),
                    twitchLogo = result.GetString(8),
                    discordUID = (ulong)result.GetInt64(11),
                    lastChange = (int)result.GetInt64(13),
                    lastSave = (int)result.GetInt64(14)
                };
                _UserCache.Add(user);

                if(_UserCache[_UserCache.Count - 1].twitchUsername != twitchname){
                    Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "BotUsers", $"Found DB username {twitchname}, read it and added to cache but last cache entry did not match!"));
                    return null;
                }

                return _UserCache[_UserCache.Count - 1];
            }
        }
        private UserEntry DBReadTwitchUserByDisplayName(string twitchDisplayName)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{EXTENSIONNAME}\" WHERE twitchDisplayname IS @twitchDisplayname";
                cmd.Parameters.AddWithValue("@twitchDisplayname", twitchDisplayName);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    throw;
                }
                result.Read();
                UserEntry user = new UserEntry
                {
                    linked = result.GetBoolean(0),
                    discordUsername = result.GetString(1),
                    lastSeen = result.GetInt32(2),
                    lastSeenOnTwitch = result.GetInt32(3),
                    twitchUID = result.GetString(4),
                    twitchUsername = result.GetString(5),
                    twitchDisplayName = result.GetString(6),
                    twitchColour = result.GetString(7),
                    twitchLogo = result.GetString(8),
                    discordUID = (ulong)result.GetInt64(11),
                    lastChange = (int)result.GetInt64(13),
                    lastSave = (int)result.GetInt64(14)
                };
                return user;
            }
        }
        #endregion
        private async Task<List<UserEntry>> SearchDBUserByName(string pattern)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{EXTENSIONNAME}\" WHERE twitchUsername LIKE @pattern";
                cmd.Parameters.AddWithValue("@pattern", "%"+pattern+"%");
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, EXTENSIONNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                List<UserEntry> hits = new List<UserEntry>();
                while (result.Read())
                {
                    UserEntry user = new UserEntry
                    {
                        linked = result.GetBoolean(0),
                        discordUsername = result.GetString(1),
                        lastSeen = result.GetInt32(2),
                        lastSeenOnTwitch = result.GetInt32(3),
                        twitchUID = result.GetString(4),
                        twitchUsername = result.GetString(5),
                        twitchDisplayName = result.GetString(6),
                        twitchColour = result.GetString(7),
                        twitchLogo = result.GetString(8),
                        discordUID = (ulong)result.GetInt64(11),
                        lastChange = (int)result.GetInt64(13),
                        lastSave = (int)result.GetInt64(14)
                    };
                    hits.Add(user);
                }
                return hits;
            }
        }
        #region New User Creation
        private void CreateNewDiscordUser(ulong uid, UserEntry user)
        {
            SocketUser userinfo = Program.DiscordClient.GetUser(uid);
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"INSERT INTO \"{EXTENSIONNAME}\" VALUES (" +
                    $"@linked, " +
                    $"@username, " +
                    $"@lastseen, " +
                    $"@lastseenOnTwitch, " +
                    $"@twitchUID, " +
                    $"@twichUsername, " +
                    $"@twitchDisplayname, " +
                    $"@twitchColour, " +
                    $"@twitchLogo, " +
                    $"@twitchCreated, " +
                    $"@twitchLastUpdate, " +
                    $"@discordUID, " +
                    $"@discordStatus, " +
                    $"@lastChange, " +
                    $"@lastSave" +
                    $")";
                cmd.Parameters.AddWithValue("@linked", user.linked);
                cmd.Parameters.AddWithValue("@username", userinfo.Username);
                cmd.Parameters.AddWithValue("@lastseen", Core.CurrentTime);
                cmd.Parameters.AddWithValue("@lastseenOnTwitch", user.lastSeenOnTwitch);
                cmd.Parameters.AddWithValue("@twitchUID", user.twitchUID);
                cmd.Parameters.AddWithValue("@twichUsername", user.twitchUsername);
                cmd.Parameters.AddWithValue("@twitchDisplayname", user.twitchDisplayName);
                cmd.Parameters.AddWithValue("@twitchColour", user.twitchColour);
                cmd.Parameters.AddWithValue("@twitchLogo", user.twitchLogo);
                cmd.Parameters.AddWithValue("@twitchCreated", user.twitchCreated);
                cmd.Parameters.AddWithValue("@twitchLastUpdate", user.twitchLastUpdate);
                cmd.Parameters.AddWithValue("@discordUID", uid);
                cmd.Parameters.AddWithValue("@discordStatus", userinfo.Status);
                cmd.Parameters.AddWithValue("@lastChange", Core.CurrentTime);
                cmd.Parameters.AddWithValue("@lastSave", Core.CurrentTime);
                cmd.ExecuteNonQuery();
                //await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Created entry for Discord user {userinfo.Username}"));
            }
        }

        internal async Task<TwitchLib.Api.Helix.Models.Users.GetUsers.User> GetTwitchUserByIDFromAPI(string uid){
            TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse response = await Program.TwitchAPI.Helix.Users.GetUsersAsync(new List<string>() { uid }, null, null);
            if(response.Users.Length == 1){
                return response.Users[0];
            }
            return null;
        }
        internal async Task<TwitchLib.Api.Helix.Models.Users.GetUsers.User> GetTwitchUserByUserNameFromAPI(string username){
            TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse response = await Program.TwitchAPI.Helix.Users.GetUsersAsync(
                null, 
                new List<string>() { username }, 
                null);
            if(response.Users.Length == 1){
                return response.Users[0];
            }
            return null;
        }
        internal async Task<TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse> GetTwitchUsersByUserNamesFromAPI(List<string> usernames){
            TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse response = await Program.TwitchAPI.Helix.Users.GetUsersAsync(
                null, 
                usernames, 
                null);
            if(response.Users.Length > 0){
                return response;
            }
            return null;
        }
        private async void CreateNewTwitchUserFromID(string uid)
        {
            TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse userinfo = await Program.TwitchAPI.Helix.Users.GetUsersAsync(
                new List<string>() { uid }, null, null
                );

            if (userinfo != null)
            {
if (userinfo.Users.Length == 1)
            {

await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "BotUsers", $"{userinfo.Users[0].DisplayName} is {userinfo.Users[0].BroadcasterType} vc {userinfo.Users[0].ViewCount}"));

                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    try
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Connection = Core.Data;
                        cmd.CommandText = $"INSERT INTO \"{EXTENSIONNAME}\" VALUES (" +
                            $"@linked, " +
                            $"@username, " +
                            $"@lastseen, " +
                            $"@lastseenOnTwitch, " +
                            $"@twitchUID, " +
                            $"@twitchUsername, " +
                            $"@twitchDisplayname, " +
                            $"@twitchColour, " +
                            $"@twitchLogo, " +
                            $"@twitchCreated, " +
                            $"@twitchLastUpdate, " +
                            $"@discordUID, " +
                            $"@discordStatus, " +
                            $"@lastChange, " +
                            $"@lastSave" +
                            $")";
                        cmd.Parameters.AddWithValue("@linked", false);
                        cmd.Parameters.AddWithValue("@username", string.Empty);
                        cmd.Parameters.AddWithValue("@lastseen", Core.CurrentTime);
                        cmd.Parameters.AddWithValue("@lastseenOnTwitch", Core.CurrentTime);
                        cmd.Parameters.AddWithValue("@twitchUID", userinfo.Users[0].Id);
                        cmd.Parameters.AddWithValue("@twitchUsername", userinfo.Users[0].Login);
                        cmd.Parameters.AddWithValue("@twitchDisplayname", userinfo.Users[0].DisplayName);
                        cmd.Parameters.AddWithValue("@twitchColour", string.Empty);
                        cmd.Parameters.AddWithValue("@twitchLogo", userinfo.Users[0].ProfileImageUrl);
                        cmd.Parameters.AddWithValue("@twitchCreated", userinfo.Users[0].CreatedAt);
                        cmd.Parameters.AddWithValue("@twitchLastUpdate", userinfo.Users[0].CreatedAt);
                        cmd.Parameters.AddWithValue("@discordUID", 0);
                        cmd.Parameters.AddWithValue("@discordStatus", Discord.UserStatus.Offline);
                        cmd.Parameters.AddWithValue("@lastChange", Core.CurrentTime);
                        cmd.Parameters.AddWithValue("@lastSave", Core.CurrentTime);
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception)
                    {
                        await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, EXTENSIONNAME, $"Database query failed hard. ({cmd.CommandText})"));
                        throw;
                    }
                }
            }
            }
            else
            {
                await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, "BotUsers", "Twitch user lookup failed!"));
            }
            // always wait a bit so DB changes really work
            await Task.Delay(300);
        }
        
        private async Task CreateNewTwitchUserFromName(string twitchusername)
        {
            try
            {
                TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse users = await Program.TwitchAPI.Helix.Users.GetUsersAsync(null, new List<string>(){ twitchusername });
                if (users.Users.Length > 0)
                {
                    using (SQLiteCommand cmd = new SQLiteCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Connection = Core.Data;
                        cmd.CommandText = $"INSERT INTO \"{EXTENSIONNAME}\" VALUES (" +
                            $"@linked, " +
                            $"@username, " +
                            $"@lastseen, " +
                            $"@lastseenOnTwitch, " +
                            $"@twitchUID, " +
                            $"@twichUsername, " +
                            $"@twitchDisplayname, " +
                            $"@twitchColour, " +
                            $"@twitchLogo, " +
                            $"@twitchCreated, " +
                            $"@twitchLastUpdate, " +
                            $"@discordUID, " +
                            $"@discordStatus, " +
                            $"@lastChange, " +
                            $"@lastSave" +
                            $")";
                        cmd.Parameters.AddWithValue("@linked", false);
                        cmd.Parameters.AddWithValue("@username", string.Empty);
                        cmd.Parameters.AddWithValue("@lastseen", Core.CurrentTime);
                        cmd.Parameters.AddWithValue("@lastseenOnTwitch", Core.CurrentTime);
                        cmd.Parameters.AddWithValue("@twitchUID", users.Users[0].Id);
                        cmd.Parameters.AddWithValue("@twichUsername", users.Users[0].Login);
                        cmd.Parameters.AddWithValue("@twitchDisplayname", users.Users[0].DisplayName);
                        cmd.Parameters.AddWithValue("@twitchColour", string.Empty);
                        cmd.Parameters.AddWithValue("@twitchLogo", users.Users[0].ProfileImageUrl);
                        cmd.Parameters.AddWithValue("@twitchCreated", users.Users[0].CreatedAt);
                        cmd.Parameters.AddWithValue("@twitchLastUpdate", users.Users[0].CreatedAt);
                        cmd.Parameters.AddWithValue("@discordUID", 0);
                        cmd.Parameters.AddWithValue("@discordStatus", UserStatus.Offline);
                        cmd.Parameters.AddWithValue("@lastChange", Core.CurrentTime);
                        cmd.Parameters.AddWithValue("@lastSave", Core.CurrentTime);
                        if (await DBUserExistsTwitchName(twitchusername))
                        {
                            return;
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, "BotUsers", $"Twitch user lookup failed for {twitchusername}!"));
                }
            }
            catch (Exception)
            {
                await Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "BotUsers", $"Twitch user lookup exception caught ({twitchusername})!"));
            }
            // always wait a bit so DB changes really work
            await Task.Delay(300);
        }
        #endregion
        private async void SaveUser(UserEntry user)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                if (user.twitchUID == string.Empty || user.linked)
                {
                    cmd.CommandText = $"UPDATE \"{EXTENSIONNAME}\" SET " +
                        $"linked = @linked, " +
                        $"username = @username, " +
                        $"lastseen = @lastseen, " +
                        $"lastseenOnTwitch = @lastseenOnTwitch, " +
                        $"twitchUID = @twitchUID, " +
                        $"twitchUsername = @twitchUsername, " +
                        $"twitchDisplayname = @twitchDisplayname, " +
                        $"twitchColour = @twitchColour, " +
                        $"twitchLogo = @twitchLogo, " +
                        $"twitchCreated = @twitchCreated, " +
                        $"twitchLastUpdate = @twitchLastUpdate, " +
                        $"discordUID = @discordUID, " +
                        $"discordStatus = @discordStatus, " +
                        $"lastChange = @lastChange, " +
                        $"lastSave = @lastSave " +
                        $" WHERE discordUID is @uid";
                    cmd.Parameters.AddWithValue("@uid", user.discordUID);
                }
                else
                {
                    cmd.CommandText = $"UPDATE {EXTENSIONNAME} SET " +
                        $"linked = @linked, " +
                        $"username = @username, " +
                        $"lastseen = @lastseen, " +
                        $"lastseenOnTwitch = @lastseenOnTwitch, " +
                        $"twitchUID = @twitchUID, " +
                        $"twitchUsername = @twitchUsername, " +
                        $"twitchDisplayname = @twitchDisplayname, " +
                        $"twitchColour = @twitchColour, " +
                        $"twitchLogo = @twitchLogo, " +
                        $"twitchCreated = @twitchCreated, " +
                        $"twitchLastUpdate = @twitchLastUpdate, " +
                        $"discordUID = @discordUID, " +
                        $"discordStatus = @discordStatus, " +
                        $"lastChange = @lastChange, " +
                        $"lastSave = @lastSave " +
                        $" WHERE twitchUID is @uid";
                    cmd.Parameters.AddWithValue("@uid", user.twitchUID);
                }

                cmd.Parameters.AddWithValue("@linked", user.linked);
                cmd.Parameters.AddWithValue("@username", user.discordUsername);
                cmd.Parameters.AddWithValue("@lastseen", user.lastSeen);
                cmd.Parameters.AddWithValue("@lastseenOnTwitch", user.lastSeenOnTwitch);
                cmd.Parameters.AddWithValue("@twitchUID", user.twitchUID);
                cmd.Parameters.AddWithValue("@twitchUsername", user.twitchUsername);
                cmd.Parameters.AddWithValue("@twitchDisplayname", user.twitchDisplayName);
                cmd.Parameters.AddWithValue("@twitchColour", user.twitchColour);
                cmd.Parameters.AddWithValue("@twitchLogo", user.twitchLogo);
                cmd.Parameters.AddWithValue("@twitchCreated", user.twitchCreated);
                cmd.Parameters.AddWithValue("@twitchLastUpdate", user.twitchLastUpdate);
                cmd.Parameters.AddWithValue("@discordUID", user.discordUID);
                cmd.Parameters.AddWithValue("@discordStatus", user.discordStatus);
                cmd.Parameters.AddWithValue("@lastChange", user.lastChange);
                cmd.Parameters.AddWithValue("@lastSave", user.lastSave);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.ERROR, EXTENSIONNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
            }
        }
        #endregion
    }// EOF CLASS
}
