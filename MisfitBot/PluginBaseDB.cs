using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using Discord;
using Newtonsoft.Json;
using MisfitBot_MKII.Statics;

namespace MisfitBot_MKII
{
    /// <summary>
    /// This provides basic config support for servicebase class.
    /// </summary>
    public class PluginBaseDB
    {
        #region Service Settings Data
        /// <summary>
        /// This base method should be overwritten by custom table creation
        /// </summary>
        /// <param name="botChannel"></param>
        /// <param name="plugin"></param>
        public void TableCreate(BotChannel bChan, string plugin)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"CREATE TABLE \"{TableName(bChan.Key, plugin)}\" (configKey varchar(30), config TEXT(2048))";
                cmd.ExecuteNonQuery();
            }
        }
        public async void RowBaseCreate(BotChannel bChan, String plugin, String key, string info)
        {
            using (SQLiteCommand c = new SQLiteCommand())
            {
                c.CommandType = CommandType.Text;
                c.Connection = Core.Data;
                c.CommandText = $"INSERT INTO \"{TableName(bChan.Key, plugin)}\" VALUES (@key, @info)";
                c.Parameters.AddWithValue("@key", key);
                c.Parameters.AddWithValue("@info", info);
                c.ExecuteNonQuery();
                await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, plugin, $"Created config entry ({plugin}::{key}) in DB."));
            }
        }
        /// <summary>
        /// Returns a string from the database that then should be deserialized to the right type.
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<string> RowBaseRead(BotChannel bChan, string plugin, string key)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{TableName(bChan.Key, plugin)}\" WHERE configKey IS @key";
                cmd.Parameters.AddWithValue("@key", key);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, plugin, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                return result.GetString(1);
            }
        }
        /// <summary>
        /// This ONLY serializes and store an object as a string
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="plugin"></param>
        /// <param name="settings"></param>
        public void SaveBaseSettings(BotChannel bChan, string plugin, object settings)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"UPDATE \"{TableName(bChan.Key, plugin)}\" SET config = @data WHERE configKey is @key";
                cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(settings));
                cmd.Parameters.AddWithValue("@key", bChan.Key);
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
                    await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, "ServiceBaseDB",
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
                cmd.CommandText = $"SELECT * FROM \"{plugin}\" WHERE configKey IS @key";
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
                cmd.CommandText = $"SELECT * FROM \"{table}\" WHERE userkey IS @userkey";
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
        /// <summary>
        /// Constructs the tablename from the BotChannel Key and plugin
        /// also adds suffix
        /// </summary>
        /// <param name="chanKey"></param>
        /// <returns></returns>
        private string TableName(string chanKey, string plugin)
        {
            return chanKey + "_" + plugin + "_config";
        }
    }// EOF CLASS
}
