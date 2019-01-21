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

namespace MisfitBot2.Extensions.UserManager
{
    public class BotUsers
    {
        private readonly string PLUGINNAME = "BotUsers";

        #region DATA minapulation stuff
        public async Task<UserEntry> GetDBUserByTwitchUserName(string twitchUsername)
        {
            UserEntry user = new UserEntry();
            if (!TableExists())
            {
                TableCreate();
            }
            if (!await DBUserExistsTwitchName(twitchUsername))
            {
                await CreateNewTwitchUserFromName(twitchUsername, user);
            }
            if (await DBUserExistsTwitchName(twitchUsername))
            {
                return await DBReadTwitchUserByName(twitchUsername);
            }
            return null;
        }
        public async Task<UserEntry> GetDBUserByDiscordUID(ulong uid)
        {
            //Cacheable locally and return cached vertsion here TODO!!!
            UserEntry user = new UserEntry();
            if (!TableExists())
            {
                TableCreate();
            }
            if (!await DBUserExistsDiscordUID(uid))
            {
                CreateNewDiscordUser(uid, user);
            }
            return await DBReadDiscordUser(uid);
        }
        public async Task<UserEntry> GetDBUserByTwitchID(string uid)
        {
            UserEntry user = new UserEntry();
            if (!TableExists())
            {
                TableCreate();
            }
            if (!await DBUserExistsTwitchID(uid))
            {
                CreateNewTwitchUserFromID(uid, user);
            }
            if (await DBUserExistsTwitchID(uid))
            {
                return await DBReadTwitchUser(uid);
            }
            return null;
        }
        
        private bool TableExists()
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = "SELECT COUNT(*) AS QtRecords FROM sqlite_master WHERE type = 'table' AND name = @name";
                cmd.Parameters.AddWithValue("@name", PLUGINNAME);
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
        private void TableCreate()
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"CREATE TABLE BotUsers (" +
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
                cmd.CommandText = $"SELECT * FROM {PLUGINNAME} WHERE discordUID IS @discordUID";
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
                cmd.CommandText = $"SELECT * FROM {PLUGINNAME} WHERE twitchUID IS @twitchUID";
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
                cmd.CommandText = $"SELECT * FROM {PLUGINNAME} WHERE twitchUsername IS @name";
                cmd.Parameters.AddWithValue("@name", name);

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

        private async Task<UserEntry> DBReadDiscordUser(ulong uid)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINNAME} WHERE discordUID IS @uid";
                cmd.Parameters.AddWithValue("@uid", uid);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                UserEntry user = new UserEntry
                {
                    linked = result.GetBoolean(0),
                    _username = result.GetString(1),
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
        private async Task<UserEntry> DBReadTwitchUser(string uid)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINNAME} WHERE twitchUID IS @uid";
                cmd.Parameters.AddWithValue("@uid", uid);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                UserEntry user = new UserEntry
                {
                    linked = result.GetBoolean(0),
                    _username = result.GetString(1),
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
        private async Task<UserEntry> DBReadTwitchUserByName(string twitchname)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINNAME} WHERE twitchUsername IS @twitchname";
                cmd.Parameters.AddWithValue("@twitchname", twitchname);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                UserEntry user = new UserEntry
                {
                    linked = result.GetBoolean(0),
                    _username = result.GetString(1),
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

        private void CreateNewDiscordUser(ulong uid, UserEntry user)
        {
            SocketUser userinfo = Core.Discord.GetUser(uid);
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"INSERT INTO {PLUGINNAME} VALUES (" +
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
        private async void CreateNewTwitchUserFromID(string uid, UserEntry user)
        {
            User userinfo = await Core.Twitch._api.V5.Users.GetUserByIDAsync(uid);
            if (user != null && userinfo != null)
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    try
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Connection = Core.Data;
                        cmd.CommandText = $"INSERT INTO {PLUGINNAME} VALUES (" +
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
                        cmd.Parameters.AddWithValue("@linked", user.linked);
                        cmd.Parameters.AddWithValue("@username", user._username);
                        cmd.Parameters.AddWithValue("@lastseen", Core.CurrentTime);
                        cmd.Parameters.AddWithValue("@lastseenOnTwitch", Core.CurrentTime);
                        cmd.Parameters.AddWithValue("@twitchUID", userinfo.Id);
                        cmd.Parameters.AddWithValue("@twitchUsername", userinfo.Name);
                        cmd.Parameters.AddWithValue("@twitchDisplayname", userinfo.DisplayName);
                        cmd.Parameters.AddWithValue("@twitchColour", user._twitchColour);
                        cmd.Parameters.AddWithValue("@twitchLogo", userinfo.Logo);
                        cmd.Parameters.AddWithValue("@twitchCreated", userinfo.CreatedAt);
                        cmd.Parameters.AddWithValue("@twitchLastUpdate", userinfo.UpdatedAt);
                        cmd.Parameters.AddWithValue("@discordUID", user._discordUID);
                        cmd.Parameters.AddWithValue("@discordStatus", user._discordStatus);
                        cmd.Parameters.AddWithValue("@lastChange", Core.CurrentTime);
                        cmd.Parameters.AddWithValue("@lastSave", Core.CurrentTime);
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception)
                    {
                        await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
                        throw;
                    }
                }
            }
            else
            {
                await Core.LOG(new LogMessage(LogSeverity.Warning, "BotUsers", "Twitch user lookup failed!"));
            }
        }
        private async Task CreateNewTwitchUserFromName(string twitchusername, UserEntry user)
        {
            try
            {
                Users users = await Core.Twitch._api.V5.Users.GetUserByNameAsync(twitchusername);


                Users userinfo = users;
                if (userinfo.Matches.Length > 0)
                {
                    using (SQLiteCommand cmd = new SQLiteCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Connection = Core.Data;
                        cmd.CommandText = $"INSERT INTO {PLUGINNAME} VALUES (" +
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
                        cmd.Parameters.AddWithValue("@username", string.Empty);
                        cmd.Parameters.AddWithValue("@lastseen", Core.CurrentTime);
                        cmd.Parameters.AddWithValue("@lastseenOnTwitch", Core.CurrentTime);
                        cmd.Parameters.AddWithValue("@twitchUID", userinfo.Matches[0].Id);
                        cmd.Parameters.AddWithValue("@twichUsername", userinfo.Matches[0].Name);
                        cmd.Parameters.AddWithValue("@twitchDisplayname", userinfo.Matches[0].DisplayName);
                        cmd.Parameters.AddWithValue("@twitchColour", string.Empty);
                        cmd.Parameters.AddWithValue("@twitchLogo", userinfo.Matches[0].Logo);
                        cmd.Parameters.AddWithValue("@twitchCreated", userinfo.Matches[0].CreatedAt);
                        cmd.Parameters.AddWithValue("@twitchLastUpdate", userinfo.Matches[0].UpdatedAt);
                        cmd.Parameters.AddWithValue("@discordUID", user._discordUID);
                        cmd.Parameters.AddWithValue("@discordStatus", user._discordStatus);
                        cmd.Parameters.AddWithValue("@lastChange", Core.CurrentTime);
                        cmd.Parameters.AddWithValue("@lastSave", Core.CurrentTime);

                        user._lastseen = Core.CurrentTime;
                        user._lastseenOnTwitch = Core.CurrentTime;
                        user._twitchUID = userinfo.Matches[0].Id;
                        user._twitchUsername = userinfo.Matches[0].Name;
                        user._twitchDisplayname = userinfo.Matches[0].DisplayName;
                        user._twitchLogo = userinfo.Matches[0].Logo;
                        user._twitchCreated = userinfo.Matches[0].CreatedAt;
                        user._twitchLastUpdate = userinfo.Matches[0].UpdatedAt;
                        user.lastChange = Core.CurrentTime;
                        user.lastSave = Core.CurrentTime;
                        if (await DBUserExistsTwitchName(twitchusername))
                        {
                            return;
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    await Core.LOG(new LogMessage(LogSeverity.Warning, "BotUsers", $"Twitch user lookup failed for {twitchusername}!"));
                }

            }
            catch (Exception)
            {
                await Core.LOG(new LogMessage(LogSeverity.Warning, "BotUsers", $"Twitch user lookup exception caught ({twitchusername})!"));
            }
        }

        public async void SaveUser(UserEntry user)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                if (user._twitchUID == string.Empty || user.linked)
                {
                    cmd.CommandText = $"UPDATE {PLUGINNAME} SET " +
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
                    cmd.CommandText = $"UPDATE {PLUGINNAME} SET " +
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
                cmd.Parameters.AddWithValue("@username", user._username);
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
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
            }
        }
        #endregion


        public async Task UpdateTwitchUserColour(OnMessageReceivedArgs e)
        {
            if (e == null) return;
            UserEntry user = await GetDBUserByTwitchID(e.ChatMessage.UserId);
            if (user == null) return;
            user._twitchColour = e.ChatMessage.ColorHex;
            user._lastseenOnTwitch = Core.CurrentTime;
            SaveUser(user);
        }


    }
}
