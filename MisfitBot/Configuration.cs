using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;

namespace MisfitBot_MKII
{
    /// <summary>
    /// Handles the creation and loading of the modules settings in the DB.
    /// </summary>
    public class ConfigurationHandler
    {
        private readonly string PLUGINNAME = "Configuration";
        //private Dictionary<string, Dictionary<string, Object>> _configs = new Dictionary<string, Dictionary<string, Object>>();
        //private JsonSerializer serializer = new JsonSerializer();
        // CONSTRUCTOR
        public ConfigurationHandler()
        {
            //Console.WriteLine("ConfigurationHandler::ConfigurationHandler()");
            //Core.serializer.Formatting = Formatting.Indented;
        }
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
        /// <summary>
        /// Returns the saved config or creates a new entry with the object passed then save it to file and return it.
        /// Remember to use the result in an AS statement.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="plugin"></param>
        /// <param name="objectType"></param>
        /// <param name="twitch"></param>
        /// <returns></returns>
        public async Task<dynamic> GetConfig<T>(BotChannel bChan, string plugin)
        {
            T var = await Load<T>(bChan, plugin);
            return var;
        }
        /// <summary>
        /// Takes the passed Object, serialize the object and store it as a string under the plugin's DB Table using the Botchannel Key as identifier.
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="plugin"></param>
        /// <param name="data"></param>
        public async void UpdateConfig<T>(BotChannel bChan, string plugin, Object data)
        {
            SQLiteCommand cmd = new SQLiteCommand
            {
                CommandType = CommandType.Text,
                Connection = Core.Data,
                CommandText = $"UPDATE \"{TableName(bChan.Key,plugin)}\" SET config = @data WHERE configKey is @key"
            };
            cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(data));
            cmd.Parameters.AddWithValue("@key", bChan.Key);
            cmd.ExecuteNonQuery();
            await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, PLUGINNAME, $"Saved updated config for ({plugin}::{bChan.Key}) in DB."));
        }
        /// <summary>
        /// USed to check if the given table exists in the DB.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        private bool TableExists(String tableName, SQLiteConnection connection)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = connection;
                cmd.CommandText = "SELECT COUNT(*) AS QtRecords FROM sqlite_master WHERE type = 'table' AND name = @name";
                cmd.Parameters.AddWithValue("@name", tableName);
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0) {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        /// <summary>
        /// Run this to make sure a config table exists in the database
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="plugin"></param>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task ConfigSetup<T>(BotChannel bChan, string plugin, T obj){
            string tableName = TableName(bChan.Key,plugin);
            // Check if missing the table If so then make it
            if (!TableExists(tableName, Core.Data))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = Core.Data;
                    cmd.CommandText = $"CREATE TABLE \"{tableName}\" (configKey varchar(30), config TEXT(2048))";
                    cmd.ExecuteNonQuery();
                    await Core.LOG(new LogEntry(LOGSEVERITY.INFO, PLUGINNAME, $"Table {tableName} created!!"));
                }
            }
            // Get the data stored or save a blank set
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE configKey IS @key";
                cmd.Parameters.AddWithValue("@key", bChan.Key);
                // Serialize the data to store if we dont find any entry
                string info = JsonConvert.SerializeObject(obj, Formatting.None);
                // Check if we already have an entry
                var i = await cmd.ExecuteScalarAsync();
                // If no entry add what we got
                if (i == null)
                {
                    using (SQLiteCommand c = new SQLiteCommand())
                    {
                        c.CommandType = CommandType.Text;
                        c.Connection = Core.Data;
                        c.CommandText = $"INSERT INTO \"{tableName}\" VALUES (@key, @info)";
                        c.Parameters.AddWithValue("@key", bChan.Key);
                        c.Parameters.AddWithValue("@info", info);
                        c.ExecuteNonQuery();
                        await Core.LOG(new LogEntry(LOGSEVERITY.INFO, PLUGINNAME, $"Created config entry ({plugin}::{bChan.Key}) in DB."));
                    }
                }
            }
        }
        private async Task<dynamic> Load<T>(BotChannel bChan, string plugin)
        {
            
            // Get the data stored or save a blank set
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{TableName(bChan.Key,plugin)}\" WHERE configKey IS @key";
                cmd.Parameters.AddWithValue("@key", bChan.Key);

                var i = await cmd.ExecuteScalarAsync();

                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.ERROR, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }

                result.Read();
                // Deserialize the data
                //string debugfield = result.GetString(1);
                T result3 = JsonConvert.DeserializeObject<T>(result.GetString(1));
                return result3;
            }

        }
    }
}
