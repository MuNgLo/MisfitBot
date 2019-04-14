using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Threading.Tasks;

namespace MisfitBot2.Components
{
    public class DatabaseStrings
    {
        readonly string PLUGINNAME;
        public DatabaseStrings(string pluginName)
        {
            PLUGINNAME = pluginName;
        }

        #region DB access

        public void SaveNewLine(BotChannel bChan, string topic, string line)
        {
            if (!TableExists(TableName(bChan.Key))) { TableCreate(bChan.Key); }
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"INSERT INTO {TableName(bChan.Key)} VALUES (" +
                    $"@chanKey, " +
                    $"@inuse, " +
                    $"@topic, " +
                    $"@text)";
                cmd.Parameters.AddWithValue("@chanKey", bChan.Key);
                cmd.Parameters.AddWithValue("@inuse", true);
                cmd.Parameters.AddWithValue("@topic", topic);
                cmd.Parameters.AddWithValue("@text", line);
                cmd.ExecuteNonQuery();
            }
        }
        public async Task<List<string>> GetAllInUse(BotChannel bChan, string topic)
        {
            List<string> inuseLines = new List<string>();
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                //cmd.CommandText = $"SELECT * FROM {TableName(bChan.Key)} WHERE inuse IS @inuse AND topic IS @topic";
                cmd.CommandText = $"SELECT * FROM {TableName(bChan.Key)} WHERE inuse IS @inuse";
                cmd.Parameters.AddWithValue("@inuse", true);
                //cmd.Parameters.AddWithValue("@topic", topic);
                using (SQLiteDataReader result = cmd.ExecuteReader())
                {
                    while (result.Read())
                    {
                        inuseLines.Add(result.GetString(3));
                    }
                }

                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, PLUGINNAME, "aasdasdasd"));

                /*settings._active = result.GetBoolean(1);
                settings._defaultCooldown = result.GetInt32(2);
                settings._defaultDiscordChannel = (ulong)result.GetInt64(3);
                settings._defaultTwitchRoom = result.GetString(4);*/
                return inuseLines;
            }
        }
            
        public string GetRandomLine(BotChannel bChan, string topic)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {TableName(bChan.Key)} WHERE chanKey IS @chanKey AND inuse IS @inuse AND topic IS @topic";
                cmd.Parameters.AddWithValue("@chanKey", bChan.Key);
                cmd.Parameters.AddWithValue("@inuse", true);
                cmd.Parameters.AddWithValue("@topic", topic);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME+"DatabseStrings", $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                /*settings._active = result.GetBoolean(1);
                settings._defaultCooldown = result.GetInt32(2);
                settings._defaultDiscordChannel = (ulong)result.GetInt64(3);
                settings._defaultTwitchRoom = result.GetString(4);*/
                return result.GetString(3);
            }

        }
        private void TableCreate(string chanKey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"CREATE TABLE {TableName(chanKey)} (" +
                    $"chanKey VACHAR(30)," +
                    $"inuse BOOLEAN, " +
                    $"topic VACHAR(30), " +
                    $"text VACHAR(255)" +
                    $")";
                cmd.ExecuteNonQuery();
            }
        }
        private bool TableExists(String tableName)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = "SELECT COUNT(*) AS QtRecords FROM sqlite_master WHERE type = 'table' AND name = @name";
                cmd.Parameters.AddWithValue("@name", tableName);
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
        #endregion
        #region Internals
        private string TableName(string chanKey)
        {
            return PLUGINNAME + "_" + chanKey;
        }
        #endregion
    }//END of DatabaseStrings
}
