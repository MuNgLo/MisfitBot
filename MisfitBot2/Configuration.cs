using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using MisfitBot2.Services;
using Discord.WebSocket;
using System.Threading.Tasks;
using MisfitBot2.Extensions.ChannelManager;
using MisfitBot2.Plugins.MyPick;
using System.Data.SQLite;
using System.Data;
using MisfitBot2.Plugins.Couch;
using MisfitBot2.Plugins.Admin;
using MisfitBot2.Plugins.Betting;
using MisfitBot2.Plugins.DeathCounter;
using MisfitBot2.Plugins.PoorLifeChoices;
using MisfitBot2.Plugins.Greeter;
using MisfitBot2.Plugins.PluginTemplate;
using MisfitBot2.Plugins.Raffle;
using MisfitBot2.Plugins.Voting;
using MisfitBot2.Plugins.MatchMaking;
using MisfitBot2.Plugins.Queue;

namespace MisfitBot2
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
        /// Returns the saved config or creates a new entry with the object passed then save it to file and return it.
        /// Remember to use the result in an AS statement.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="plugin"></param>
        /// <param name="objectType"></param>
        /// <param name="twitch"></param>
        /// <returns></returns>
        public async Task<dynamic> GetConfig(BotChannel bChan, string plugin, Object objectType)
        {
            /*if (!_configs.ContainsKey(bChan.Key))
            {
                _configs[bChan.Key] = new Dictionary<string, object>();
            }
            if (!_configs[bChan.Key].ContainsKey(plugin))
            {
                _configs[bChan.Key][plugin] = objectType;
            }*/
            return await Load(bChan, plugin, objectType);
            //return _configs[bChan.Key][plugin];
        }
        /// <summary>
        /// Takes the passed Object, serialize the object and store it as a string under the plugin's DB Table using the Botchannel Key as identifier.
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="plugin"></param>
        /// <param name="data"></param>
        public async void UpdateConfig(BotChannel bChan, string plugin, Object data)
        {
            /*if (!_configs.ContainsKey(bChan.Key))
            {
                _configs[bChan.Key] = new Dictionary<string, object>();
            }*/
            //if (!_configs[bChan.Key].ContainsKey(plugin))
            //{
                await Load(bChan, plugin, data);
                //return;
            //}
            //_configs[bChan.Key][plugin] = data;
            SQLiteCommand cmd = new SQLiteCommand
            {
                CommandType = CommandType.Text,
                Connection = Core.Data,
                CommandText = $"UPDATE {plugin} SET config = @data WHERE configKey is @key"
            };
            cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(data));
            cmd.Parameters.AddWithValue("@key", bChan.Key);
            cmd.ExecuteNonQuery();
            //await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Saved updated config for ({plugin}::{bChan.Key}) in DB."));
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

        private async Task<dynamic> Load(BotChannel bChan, string plugin, Object type)
        {
            if (!TableExists(plugin, Core.Data))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = Core.Data;
                    cmd.CommandText = $"CREATE TABLE {plugin} (configKey varchar(30), config TEXT(2048))";
                    cmd.ExecuteNonQuery();
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Table {plugin} created!!"));
                }
            }
            // Serialize the data to store if we dont find any entry
            string info = JsonConvert.SerializeObject(type, Formatting.None);
            // Get the data stored or save a blank set
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {plugin} WHERE configKey IS @key";
                cmd.Parameters.AddWithValue("@key", bChan.Key);

                var i = await cmd.ExecuteScalarAsync();
                if (i == null)
                {
                    using (SQLiteCommand c = new SQLiteCommand())
                    {
                        c.CommandType = CommandType.Text;
                        c.Connection = Core.Data;
                        c.CommandText = $"INSERT INTO {plugin} VALUES (@key, @info)";
                        c.Parameters.AddWithValue("@key", bChan.Key);
                        c.Parameters.AddWithValue("@info", info);
                        c.ExecuteNonQuery();
                        await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Created config entry ({plugin}::{bChan.Key}) in DB."));
                    }
                }

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
                // Make room for the config in memory
               /* if (!_configs.ContainsKey(bChan.Key))
                {
                    _configs[bChan.Key] = new Dictionary<string, object>();
                }*/
                // Deserialize the data and remember it
                string debugfield = result.GetString(1);
                switch (plugin)
                {
                    case "Admin":
                        AdminSettings admin = JsonConvert.DeserializeObject<AdminSettings>(result.GetString(1));
                        //_configs[bChan.Key][plugin] = admin;
                        return admin;
                    case "Betting":
                        BettingSettings betting = JsonConvert.DeserializeObject<BettingSettings>(result.GetString(1));
                        //_configs[bChan.Key][plugin] = betting;
                        return betting;
                    case "Couch":
                        CouchSettings couch = JsonConvert.DeserializeObject<CouchSettings>(result.GetString(1));
                        //_configs[bChan.Key][plugin] = couch;
                        return couch;
                    case "DeathCounter":
                        DeathCounterSettings deaths = JsonConvert.DeserializeObject<DeathCounterSettings>(result.GetString(1));
                        //_configs[bChan.Key][plugin] = deaths;
                        return deaths;
                    case "Greeter":
                        GreeterSettings greet = JsonConvert.DeserializeObject<GreeterSettings>(result.GetString(1));
                        //_configs[bChan.Key][plugin] = greet;
                        return greet;
                    case "MatchMaking":
                        MatchMakingSettings mms = JsonConvert.DeserializeObject<MatchMakingSettings>(result.GetString(1));
                        //_configs[bChan.Key][plugin] = mms;
                        return mms;
                    case "MyPick":
                        MyPickSettings result2 = JsonConvert.DeserializeObject<MyPickSettings>(result.GetString(1));
                        //_configs[bChan.Key][plugin] = result2;
                        return result2;
                    case "PluginTemplateService":
                        PluginTemplateSettings temp = JsonConvert.DeserializeObject<PluginTemplateSettings>(result.GetString(1));
                        //_configs[bChan.Key][plugin] = temp;
                        return temp;
                    case "PoorLifeChoicesService":
                        PoorLifeChoicesSettings plc = JsonConvert.DeserializeObject<PoorLifeChoicesSettings>(result.GetString(1));
                        //_configs[bChan.Key][plugin] = plc;
                        return plc;
                    case "Raffle":
                        RaffleSettings raff = JsonConvert.DeserializeObject<RaffleSettings>(result.GetString(1));
                        //_configs[bChan.Key][plugin] = raff;
                        return raff;
                    case "Treasure":
                        TreasureSettings result4 = JsonConvert.DeserializeObject<TreasureSettings>(result.GetString(1));
                        //_configs[bChan.Key][plugin] = result4;
                        return result4;
                    case "Voting":
                        VotingSettings vote = JsonConvert.DeserializeObject<VotingSettings>(result.GetString(1));
                        //_configs[bChan.Key][plugin] = vote;
                        return vote;
                    case "Queue":
                        QueueSettings queue = JsonConvert.DeserializeObject<QueueSettings>(result.GetString(1));
                        //_configs[bChan.Key][plugin] = vote;
                        return queue;
                    default:
                        PluginSettingsBase result3 = JsonConvert.DeserializeObject<PluginSettingsBase>(result.GetString(1));
                        //_configs[bChan.Key][plugin] = result3;
                        return result3;
                }
            }

        }
    }
}
