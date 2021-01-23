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
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Client.Events;

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
            _UserCache.RemoveAll(p=>p._lastseen < Core.CurrentTime - 600);
            if(preCount != _UserCache.Count){
                //Core.LOG(new LogEntry(LOGSEVERITY.INFO, EXTENSIONNAME, $"UserCache Flushed of {preCount - _UserCache.Count} users. {_UserCache.Count} left."));
            }
        }
        #region User getters
        internal async Task<UserEntry> GetDBUserByTwitchUserName(string twitchUsername)
        {
            if(!_UserCache.Exists(p=>p._twitchUsername == twitchUsername))
            {
                await FetchDBUserByTwitchUserName(twitchUsername);
            }
            return _UserCache.Find(p=>p._twitchUsername == twitchUsername);
        }
        internal async Task<UserEntry> GetDBUserByTwitchDisplayName(string twitchDisplayName)
        {
            if(!_UserCache.Exists(p=>p._twitchDisplayname == twitchDisplayName))
            {
                return await FetchDBUserByTwitchDisplayName(twitchDisplayName);
            }
            return _UserCache.Find(p=>p._twitchDisplayname == twitchDisplayName);
        }
        internal async Task<UserEntry> GetDBUserByTwitchID(string uid)
        {
            if(!_UserCache.Exists(p=>p._twitchUID == uid))
            {
                await FetchDBUserByTwitchID(uid);
            }
            return _UserCache.Find(p=>p._twitchUID == uid);
        }
        internal async Task<UserEntry> GetDBUserByDiscordUID(ulong uid)
        {
            if(!_UserCache.Exists(p=>p._discordUID == uid))
            {
                await FetchDBUserByDiscordUID(uid);
            }
            return _UserCache.Find(p=>p._discordUID == uid);
        }
        #endregion
        #region Database user getters
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
                user._lastseen = Core.CurrentTime;
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
            user._lastseen = Core.CurrentTime;
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
            user._lastseen = Core.CurrentTime;
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
                    _discordUsername = result.GetString(1),
                    _lastseen = result.GetInt32(2),
                    _lastseenOnTwitch = result.GetInt32(3),
                    _twitchUID = result.GetString(4),
                    _twitchUsername = result.GetString(5),
                    _twitchDisplayname = result.GetString(6),
                    _twitchColour = result.GetString(7),
                    _twitchLogo = result.GetString(8),
                    _discordUID = (ulong)result.GetInt64(11),
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
                    _discordUsername = result.GetString(1),
                    _lastseen = result.GetInt32(2),
                    _lastseenOnTwitch = result.GetInt32(3),
                    _twitchUID = result.GetString(4),
                    _twitchUsername = result.GetString(5),
                    _twitchDisplayname = result.GetString(6),
                    _twitchColour = result.GetString(7),
                    _twitchLogo = result.GetString(8),
                    _discordUID = (ulong)result.GetInt64(11),
                    lastChange = (int)result.GetInt64(13),
                    lastSave = (int)result.GetInt64(14)
                };
                return user;
            }
        }
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
                    _discordUsername = result.GetString(1),
                    _lastseen = result.GetInt32(2),
                    _lastseenOnTwitch = result.GetInt32(3),
                    _twitchUID = result.GetString(4),
                    _twitchUsername = result.GetString(5),
                    _twitchDisplayname = result.GetString(6),
                    _twitchColour = result.GetString(7),
                    _twitchLogo = result.GetString(8),
                    _discordUID = (ulong)result.GetInt64(11),
                    lastChange = (int)result.GetInt64(13),
                    lastSave = (int)result.GetInt64(14)
                };
                _UserCache.Add(user);

                if(_UserCache[_UserCache.Count - 1]._twitchUsername != twitchname){
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
                    _discordUsername = result.GetString(1),
                    _lastseen = result.GetInt32(2),
                    _lastseenOnTwitch = result.GetInt32(3),
                    _twitchUID = result.GetString(4),
                    _twitchUsername = result.GetString(5),
                    _twitchDisplayname = result.GetString(6),
                    _twitchColour = result.GetString(7),
                    _twitchLogo = result.GetString(8),
                    _discordUID = (ulong)result.GetInt64(11),
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
                        _discordUsername = result.GetString(1),
                        _lastseen = result.GetInt32(2),
                        _lastseenOnTwitch = result.GetInt32(3),
                        _twitchUID = result.GetString(4),
                        _twitchUsername = result.GetString(5),
                        _twitchDisplayname = result.GetString(6),
                        _twitchColour = result.GetString(7),
                        _twitchLogo = result.GetString(8),
                        _discordUID = (ulong)result.GetInt64(11),
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
                cmd.Parameters.AddWithValue("@lastseenOnTwitch", user._lastseenOnTwitch);
                cmd.Parameters.AddWithValue("@twitchUID", user._twitchUID);
                cmd.Parameters.AddWithValue("@twichUsername", user._twitchUsername);
                cmd.Parameters.AddWithValue("@twitchDisplayname", user._twitchDisplayname);
                cmd.Parameters.AddWithValue("@twitchColour", user._twitchColour);
                cmd.Parameters.AddWithValue("@twitchLogo", user._twitchLogo);
                cmd.Parameters.AddWithValue("@twitchCreated", user._twitchCreated);
                cmd.Parameters.AddWithValue("@twitchLastUpdate", user._twitchLastUpdate);
                cmd.Parameters.AddWithValue("@discordUID", uid);
                cmd.Parameters.AddWithValue("@discordStatus", userinfo.Status);
                cmd.Parameters.AddWithValue("@lastChange", Core.CurrentTime);
                cmd.Parameters.AddWithValue("@lastSave", Core.CurrentTime);
                cmd.ExecuteNonQuery();
                //await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Created entry for Discord user {userinfo.Username}"));
            }
        }
        private async void CreateNewTwitchUserFromID(string uid)
        {
            User userinfo = await Program.TwitchAPI.V5.Users.GetUserByIDAsync(uid);
            if (userinfo != null)
            {
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
                        cmd.Parameters.AddWithValue("@twitchUID", userinfo.Id);
                        cmd.Parameters.AddWithValue("@twitchUsername", userinfo.Name);
                        cmd.Parameters.AddWithValue("@twitchDisplayname", userinfo.DisplayName);
                        cmd.Parameters.AddWithValue("@twitchColour", string.Empty);
                        cmd.Parameters.AddWithValue("@twitchLogo", userinfo.Logo);
                        cmd.Parameters.AddWithValue("@twitchCreated", userinfo.CreatedAt);
                        cmd.Parameters.AddWithValue("@twitchLastUpdate", userinfo.UpdatedAt);
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
            else
            {
                await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, "BotUsers", "Twitch user lookup failed!"));
            }
        }
        private async Task CreateNewTwitchUserFromName(string twitchusername)
        {
            try
            {
                Users users = await Program.TwitchAPI.V5.Users.GetUserByNameAsync(twitchusername.ToLower());
                if (users.Matches.Length > 0)
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
                        cmd.Parameters.AddWithValue("@twitchUID", users.Matches[0].Id);
                        cmd.Parameters.AddWithValue("@twichUsername", users.Matches[0].Name);
                        cmd.Parameters.AddWithValue("@twitchDisplayname", users.Matches[0].DisplayName);
                        cmd.Parameters.AddWithValue("@twitchColour", string.Empty);
                        cmd.Parameters.AddWithValue("@twitchLogo", users.Matches[0].Logo);
                        cmd.Parameters.AddWithValue("@twitchCreated", users.Matches[0].CreatedAt);
                        cmd.Parameters.AddWithValue("@twitchLastUpdate", users.Matches[0].UpdatedAt);
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
        }
        #endregion
        private async void SaveUser(UserEntry user)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                if (user._twitchUID == string.Empty || user.linked)
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
                    cmd.Parameters.AddWithValue("@uid", user._discordUID);
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
                    cmd.Parameters.AddWithValue("@uid", user._twitchUID);
                }

                cmd.Parameters.AddWithValue("@linked", user.linked);
                cmd.Parameters.AddWithValue("@username", user._discordUsername);
                cmd.Parameters.AddWithValue("@lastseen", user._lastseen);
                cmd.Parameters.AddWithValue("@lastseenOnTwitch", user._lastseenOnTwitch);
                cmd.Parameters.AddWithValue("@twitchUID", user._twitchUID);
                cmd.Parameters.AddWithValue("@twitchUsername", user._twitchUsername);
                cmd.Parameters.AddWithValue("@twitchDisplayname", user._twitchDisplayname);
                cmd.Parameters.AddWithValue("@twitchColour", user._twitchColour);
                cmd.Parameters.AddWithValue("@twitchLogo", user._twitchLogo);
                cmd.Parameters.AddWithValue("@twitchCreated", user._twitchCreated);
                cmd.Parameters.AddWithValue("@twitchLastUpdate", user._twitchLastUpdate);
                cmd.Parameters.AddWithValue("@discordUID", user._discordUID);
                cmd.Parameters.AddWithValue("@discordStatus", user._discordStatus);
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
