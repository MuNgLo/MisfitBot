using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using Discord;
using Newtonsoft.Json;

namespace MisfitBot2
{
    /// <summary>
    /// This provides basic config support for servicebase class.
    /// </summary>
    public class ServiceBaseDB
    {
        #region Service Settings Data
        /// <summary>
        /// This base method should be overwritten by custom table creation
        /// </summary>
        /// <param name="plugin"></param>
        public void TableCreate(string plugin)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"CREATE TABLE {plugin} (configKey varchar(30), config TEXT(2048))";
                cmd.ExecuteNonQuery();
            }
        }
        public async void RowBaseCreate(String plugin, String key, string info)
        {
            using (SQLiteCommand c = new SQLiteCommand())
            {
                c.CommandType = CommandType.Text;
                c.Connection = Core.Data;
                c.CommandText = $"INSERT INTO {plugin} VALUES (@key, @info)";
                c.Parameters.AddWithValue("@key", key);
                c.Parameters.AddWithValue("@info", info);
                c.ExecuteNonQuery();
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, plugin, $"Created config entry ({plugin}::{key}) in DB."));
            }
        }
        /// <summary>
        /// Returns a string from the database that then should be deserialized to the right type.
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<string> RowBaseRead(string plugin, string key)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {plugin} WHERE configKey IS @key";
                cmd.Parameters.AddWithValue("@key", key);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, plugin, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                return result.GetString(1);
            }
        }
        /// <summary>
        /// This ONLY serializes and store an object as a string in table(plugin) and column(configKey)
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="bChan"></param>
        /// <param name="settings"></param>
        public void SaveBaseSettings(string plugin, BotChannel bChan, object settings)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"UPDATE {plugin} SET config = @data WHERE configKey is @key";
                cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(settings));
                cmd.Parameters.AddWithValue("@key", bChan.Key);
                cmd.ExecuteNonQuery();
            }
        }
        #endregion

        #region User data manipulation stuff
        /// <summary>
        /// Make sure the userkey column is named userkey. Otherwise userRow lookups will fail.
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="sqlstring"></param>
        public void UserTableCreate(string table)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"CREATE TABLE {table} (" +
                    $"userkey VACHAR(30), " +
                    $"timestamp INTEGER " +
                    $")"; ;
                cmd.ExecuteNonQuery();
            }
        }
        private async Task<UserValues> UserData(UserEntry user, BotChannel bChan, string table)
        {
            UserValues usrValues = new UserValues(Core.CurrentTime);
            if (!await TableExists(table))
            {
                UserTableCreate(table);
            }
            if (!await UserRowExists(table, user.Key))
            {
                UserRowCreate(table, user.Key, usrValues);
            }
            return await UserRowRead(table, user.Key);
        }
        /// <summary>
        /// Our custum row reader for the custom table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<UserValues> UserRowRead(string table, string userkey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {table} WHERE userkey IS @userkey";
                cmd.Parameters.AddWithValue("@userkey", userkey);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, table, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                UserValues usrValues = new UserValues(Core.CurrentTime);
                usrValues._timestamp = result.GetInt32(1);
                return usrValues;
            }
        }
        /// <summary>
        /// Creates a valid row in our custom table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="key"></param>
        /// <param name="usrValues"></param>
        public void UserRowCreate(String table, String userkey, UserValues usrValues)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"INSERT INTO {table} VALUES (" +
                    $"@userkey, " +
                    $"@timestamp " +
                    $")";
                cmd.Parameters.AddWithValue("@userkey", userkey);
                cmd.Parameters.AddWithValue("@timestamp", usrValues._timestamp);
                cmd.ExecuteNonQuery();
            }
        }
        public async void UserRowDelete(String table, String userkey, string pluginName)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"DELETE FROM {table} WHERE userkey IS @userkey";
                cmd.Parameters.AddWithValue("@userkey", userkey);
                cmd.ExecuteNonQuery();
            }
            if (await UserRowExists(table, userkey))
            {
                await Core.LOG(new LogMessage(LogSeverity.Warning, pluginName, $"Userdata deletion failed!"));
            }
            else
            {
                await Core.LOG(new LogMessage(LogSeverity.Info, pluginName, $"Userdata deleted."));
            }

        }
        /// <summary>
        /// This overrides the base table creation so we can do some magic stuff
        /// </summary>
        /// <param name="plugin"></param>
        public void SaveUserRow(string table, UserEntry user, UserValues usrValues)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"UPDATE {table} SET " +
                    $"timestamp = @timestamp " +
                    $" WHERE userkey is @userkey";
                cmd.Parameters.AddWithValue("@timestamp", usrValues._timestamp);
                cmd.Parameters.AddWithValue("@userkey", user.Key);
                cmd.ExecuteNonQuery();
            }
        }
        #endregion

        #region Simple queries
        public async Task<bool> TableExists(String tableName)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = "SELECT COUNT(*) AS QtRecords FROM sqlite_master WHERE type = 'table' AND name = @name";
                cmd.Parameters.AddWithValue("@name", tableName);
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                {
                    await Core.LOG(new LogMessage(LogSeverity.Warning, "ServiceBaseDB",
                        $"Table {tableName} not found."));
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        public async Task<bool> RowExists(String plugin, String key)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {plugin} WHERE configKey IS @key";
                cmd.Parameters.AddWithValue("@key", key);

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
        public async Task<bool> UserRowExists(String table, String userkey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {table} WHERE userkey IS @userkey";
                cmd.Parameters.AddWithValue("@userkey", userkey);

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

        #endregion
    }
}
