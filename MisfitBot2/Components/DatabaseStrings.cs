using MisfitBot2.Plugins.Couch;
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
        public bool DeleteEntry(BotChannel bChan, int id)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"DELETE FROM {TableName(bChan.Key)} WHERE ROWID IS @id";
                cmd.Parameters.AddWithValue("@id", id);
                if (cmd.ExecuteNonQuery() > 0)
                {
                    return true;
                }
            }
            return false;
        }
        public async Task<CouchDBString> GetStringByID(BotChannel bChan, int id)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {TableName(bChan.Key)} WHERE ROWID IS @id";
                cmd.Parameters.AddWithValue("@id", id);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME + "DatabseStrings", $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                return new CouchDBString((int)result.GetInt64(0), result.GetBoolean(1), result.GetString(2), result.GetString(3));
            }
        }
        public void SaveNewLine(BotChannel bChan, string topic, string line)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"INSERT INTO {TableName(bChan.Key)} (inuse, topic, text) VALUES (" +
                    $"@inuse, " +
                    $"@topic, " +
                    $"@text)";
                cmd.Parameters.AddWithValue("@inuse", true);
                cmd.Parameters.AddWithValue("@topic", topic);
                cmd.Parameters.AddWithValue("@text", line);
                cmd.ExecuteNonQuery();
            }
        }
        public  bool SaveEditedLineByID(BotChannel bChan, CouchDBString entry)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"UPDATE {TableName(bChan.Key)} SET inuse=@inuse WHERE ROWID IS @id";
                cmd.Parameters.AddWithValue("@id", entry._id);
                cmd.Parameters.AddWithValue("@inuse", entry._inuse);
                if (cmd.ExecuteNonQuery() == 1)
                {
                    return true;
                }
            }
            return false;
        }
        public async Task<string> GetRNGFromTopic(BotChannel bChan, string topic)
        {
            List<CouchDBString> candidates = await GetRowsInUse(bChan, topic);
            if (candidates.Count == 0) { return null; }
            Random rng = new Random();
            return candidates[rng.Next(candidates.Count)]._text;
        }
        public async Task<List<CouchDBString>> GetRowsInUse(BotChannel bChan, string topic)
        {
            List<CouchDBString> inuseLines = new List<CouchDBString>();
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {TableName(bChan.Key)} WHERE topic=@topic AND inuse=@inuse";
                cmd.Parameters.AddWithValue("@topic", topic);
                cmd.Parameters.AddWithValue("@inuse", true);
                using (SQLiteDataReader result = cmd.ExecuteReader())
                {
                    while (result.Read())
                    {
                        CouchDBString entry = new CouchDBString((int)result.GetInt64(0), result.GetBoolean(1), result.GetString(2), result.GetString(3));
                        inuseLines.Add(entry);
                    }
                }
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, PLUGINNAME, "aasdasdasd"));
                return inuseLines;
            }
        }
        public async Task<List<CouchDBString>> GetRowsByTen(BotChannel bChan, int page = 0)
        {
            List<CouchDBString> inuseLines = new List<CouchDBString>();
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {TableName(bChan.Key)} LIMIT {page * 10}, 10";
                using (SQLiteDataReader result = cmd.ExecuteReader())
                {
                    while (result.Read())
                    {
                        CouchDBString entry = new CouchDBString((int)result.GetInt64(0), result.GetBoolean(1), result.GetString(2), result.GetString(3));
                        inuseLines.Add(entry);
                    }
                }
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, PLUGINNAME, "aasdasdasd"));
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
        /// <summary>
        /// This will return TRUE if table already has been initiated
        /// </summary>
        /// <param name="bChan"></param>
        /// <returns></returns>
        public bool TableInit(BotChannel bChan)
        {
            if (TableExists(TableName(bChan.Key)))
            { return true; }
            else
            {
                TableCreate(bChan.Key);
                return false;
            }
        }
        private void TableCreate(string chanKey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;

                //ID int primary key IDENTITY(1,1) NOT NULL

                cmd.CommandText = $"CREATE TABLE {TableName(chanKey)} (" +
                    $"ROWID INTEGER PRIMARY KEY AUTOINCREMENT," +
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
