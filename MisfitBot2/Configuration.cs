using System;
//using System.IO;
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

namespace MisfitBot2
{
    public class Configuration
    {
        public ulong GuildID { get; set; }
        public Configuration(ulong guildID)
        {
            GuildID = guildID;
        }
        
    }

    public class ConfigurationHandler
    {
        private readonly string PLUGINNAME = "Configuration";
        private Dictionary<string, Dictionary<string, Object>> _configs = new Dictionary<string, Dictionary<string, Object>>();
        private JsonSerializer serializer = new JsonSerializer();

        public ConfigurationHandler()
        {
            //Console.WriteLine("ConfigurationHandler::ConfigurationHandler()");
            serializer.Formatting = Formatting.Indented;
        }

        /// <summary>
        /// Returns the saved config or creates a new entry with the object passed then save it to file and return it.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="plugin"></param>
        /// <param name="objectType"></param>
        /// <param name="twitch"></param>
        /// <returns></returns>
        public async Task<dynamic> GetConfig(BotChannel bChan, string plugin, Object objectType)
        {
            if (!_configs.ContainsKey(bChan.Key))
            {
                _configs[bChan.Key] = new Dictionary<string, object>();
            }
            if (!_configs[bChan.Key].ContainsKey(plugin))
            {
                _configs[bChan.Key][plugin] = objectType;
            }
            await Load(bChan, plugin, objectType);
            return _configs[bChan.Key][plugin];
        }

        public async void UpdateConfig(BotChannel bChan, string plugin, Object data)
        {
            if (!_configs.ContainsKey(bChan.Key))
            {
                _configs[bChan.Key] = new Dictionary<string, object>();
            }
            if (!_configs[bChan.Key].ContainsKey(plugin))
            {
                await Load(bChan, plugin, data);
                return;
            }
            _configs[bChan.Key][plugin] = data;
            SQLiteCommand cmd = new SQLiteCommand();
            cmd.CommandType = CommandType.Text;
            cmd.Connection = Core.Data;
            cmd.CommandText = $"UPDATE {plugin} SET config = @data WHERE configKey is @key";
            cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(data));
            cmd.Parameters.AddWithValue("@key", bChan.Key);
            cmd.ExecuteNonQuery();
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Saved updated config for ({plugin}::{bChan.Key}) in DB."));
        }


        public async Task<bool> TableExists(String tableName, SQLiteConnection connection)
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

        private async Task Load(BotChannel bChan, string plugin, Object type)
        {
            if (!await TableExists(plugin, Core.Data))
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
                if (!_configs.ContainsKey(bChan.Key))
                {
                    _configs[bChan.Key] = new Dictionary<string, object>();
                }
                // Deserialize the data and remember it
                string debugfield = result.GetString(1);
                switch (plugin)
                {
                    case "Admin":
                        AdminSettings admin = JsonConvert.DeserializeObject<AdminSettings>(result.GetString(1));
                        _configs[bChan.Key][plugin] = admin;
                        break;
                    case "Betting":
                        BettingSettings betting = JsonConvert.DeserializeObject<BettingSettings>(result.GetString(1));
                        _configs[bChan.Key][plugin] = betting;
                        break;
                    case "Couch":
                        CouchSettings couch = JsonConvert.DeserializeObject<CouchSettings>(result.GetString(1));
                        _configs[bChan.Key][plugin] = couch;
                        break;
                    case "DeathCounter":
                        DeathCounterSettings deaths = JsonConvert.DeserializeObject<DeathCounterSettings>(result.GetString(1));
                        _configs[bChan.Key][plugin] = deaths;
                        break;
                    case "Greeter":
                        GreeterSettings greet = JsonConvert.DeserializeObject<GreeterSettings>(result.GetString(1));
                        _configs[bChan.Key][plugin] = greet;
                        break;
                    case "MyPick":
                        MyPickSettings result2 = JsonConvert.DeserializeObject<MyPickSettings>(result.GetString(1));
                        _configs[bChan.Key][plugin] = result2;
                        break;
                    case "PluginTemplateService":
                        PluginTemplateSettings temp = JsonConvert.DeserializeObject<PluginTemplateSettings>(result.GetString(1));
                        _configs[bChan.Key][plugin] = temp;
                        break;
                    case "PoorLifeChoicesService":
                        PoorLifeChoicesSettings plc = JsonConvert.DeserializeObject<PoorLifeChoicesSettings>(result.GetString(1));
                        _configs[bChan.Key][plugin] = plc;
                        break;
                    case "Raffle":
                        RaffleSettings raff = JsonConvert.DeserializeObject<RaffleSettings>(result.GetString(1));
                        _configs[bChan.Key][plugin] = raff;
                        break;
                    case "Treasure":
                        TreasureSettings result4 = JsonConvert.DeserializeObject<TreasureSettings>(result.GetString(1));
                        _configs[bChan.Key][plugin] = result4;
                        break;
                    case "Voting":
                        VotingSettings vote = JsonConvert.DeserializeObject<VotingSettings>(result.GetString(1));
                        _configs[bChan.Key][plugin] = vote;
                        break;
                    case "MatchMaking":
                        MatchMakingSettings mms = JsonConvert.DeserializeObject<MatchMakingSettings>(result.GetString(1));
                        _configs[bChan.Key][plugin] = mms;
                        break;
                    default:
                        Configuration result3 = JsonConvert.DeserializeObject<Configuration>(result.GetString(1));
                        _configs[bChan.Key][plugin] = result3;
                        break;
                }
            }

        }
    }
}
