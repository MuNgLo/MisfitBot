using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Threading.Tasks;
using MisfitBot_MKII.Statics;

namespace MisfitBot_MKII.Components
{
    /// <summary>
    /// Create an instanse of this to store/grab strings from DB.
    /// The pluginname fed to constructor is used as part of the table name
    /// Seperation of string groups for a plugin is by using different Topics.
    /// </summary>
    public class DatabaseStrings
    {
        readonly string PLUGINNAME; // Is used as tablename, then each row is treated as a
        readonly string BASECOMMAND; // Is used as command when making help text. Example "couch list"
        /// <summary>
        /// pluginName is used as part of tablename
        /// baseCMD is used as command when making help text. Example "couch list"
        /// Note the lack of commanidentifier
        /// </summary>
        /// <param name="pluginName"></param>
        /// <param name="baseCMD"></param>
        public DatabaseStrings(string pluginName, string baseCMD)
        {
            PLUGINNAME = pluginName;
            BASECOMMAND = baseCMD;
        }

        #region WiP
        /// <summary>
        /// Call this to get a list string wrapped in code flags for monospace layout returned with the fitting results.
        /// NOTE! Topic not implemneted yet TODO
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="page"></param>
        /// <param name="topic"></param>
        /// <returns></returns>
        internal async Task<string> GetPage(BotChannel bChan, int page, string topic="") // TODO make Topic work
        {
            StringBuilder text = new StringBuilder();
            List<DBString> lines = GetRowsByTen(bChan, page);
            await Task.Run(()=>{
                text.Append($"```fix{Environment.NewLine}");
                text.Append($"<ID> <TOPIC> <INUSE> <TEXT>        Page {page + 1}{Environment.NewLine}");
                if (lines.Count == 0)
                {
                    text.Append("No hits. Try a lower page number.");
                }
                else
                {
                    foreach (DBString entry in lines)
                    {
                        text.Append(String.Format("{0,4}", entry._id));
                        text.Append(String.Format("{0,8}", entry._topic));
                        text.Append(String.Format("{0,7}", entry._inuse));
                        text.Append(" ");
                        text.Append(entry._text);
                        text.Append(Environment.NewLine);
                    }
                }
            });
            text.Append(Environment.NewLine);
            text.Append($"Use command {Program.CommandCharacter}{BASECOMMAND} <page> to list a page. Those marked with an X for INUSE are in rotation. Topic is what the text is used for.");
            text.Append($"```");
            return text.ToString();
        }

        #endregion


        #region DB access
        /// <summary>
        /// Deletes entry with ID from table TableName(bChan.Key).
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool DeleteEntry(BotChannel bChan, int id)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"DELETE FROM \"{TableName(bChan.Key)}\" WHERE ROWID IS @id";
                cmd.Parameters.AddWithValue("@id", id);
                if (cmd.ExecuteNonQuery() > 0)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Retrieves string from DB table TableName(bChan.Key) by ID.
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<DBString> GetStringByID(BotChannel bChan, int id)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{TableName(bChan.Key)}\" WHERE ROWID IS @id";
                cmd.Parameters.AddWithValue("@id", id);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, PLUGINNAME + "DatabseStrings", $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                if (!result.HasRows)
                {
                    return null;
                }
                return new DBString((int)result.GetInt64(0), result.GetBoolean(1), result.GetString(2), result.GetString(3));
            }
        }
        /// <summary>
        /// Inserts new string into DB table TableName(bChan.Key) with INUSE=True and TOPIC=topic.
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="topic"></param>
        /// <param name="line"></param>
        public void SaveNewLine(BotChannel bChan, string topic, string line)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"INSERT INTO \"{TableName(bChan.Key)}\" (inuse, topic, text) VALUES (" +
                    $"@inuse, " +
                    $"@topic, " +
                    $"@text)";
                cmd.Parameters.AddWithValue("@inuse", true);
                cmd.Parameters.AddWithValue("@topic", topic);
                cmd.Parameters.AddWithValue("@text", line);
                cmd.ExecuteNonQuery();
            }
        }
        /// <summary>
        /// Overwrites existing string in DB table TableName(bChan.Key) by ID.
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool SaveEditedLineByID(BotChannel bChan, DBString entry)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"UPDATE \"{TableName(bChan.Key)}\" SET inuse=@inuse WHERE ROWID IS @id";
                cmd.Parameters.AddWithValue("@id", entry._id);
                cmd.Parameters.AddWithValue("@inuse", entry._inuse);
                if (cmd.ExecuteNonQuery() == 1)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Returns a List<string> from DB table TableName(bChan.Key) by topic where INUSE=True.
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="topic"></param>
        /// <returns></returns>
        public List<DBString> GetRowsInUse(BotChannel bChan, string topic)
        {
            List<DBString> inuseLines = new List<DBString>();
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{TableName(bChan.Key)}\" WHERE topic=@topic AND inuse=@inuse";
                cmd.Parameters.AddWithValue("@topic", topic);
                cmd.Parameters.AddWithValue("@inuse", true);
                using (SQLiteDataReader result = cmd.ExecuteReader())
                {
                    while (result.Read())
                    {
                        DBString entry = new DBString((int)result.GetInt64(0), result.GetBoolean(1), result.GetString(2), result.GetString(3));
                        inuseLines.Add(entry);
                    }
                }
                return inuseLines;
            }
        }
        /// <summary>
        /// Gets a List of up to 10 entries from DB table TableName(bChan.Key). Use page to offset.
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public List<DBString> GetRowsByTen(BotChannel bChan, int page = 0)
        {
            List<DBString> inuseLines = new List<DBString>();
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{TableName(bChan.Key)}\" LIMIT {page * 10}, 10";
                using (SQLiteDataReader result = cmd.ExecuteReader())
                {
                    while (result.Read())
                    {
                        DBString entry = new DBString((int)result.GetInt64(0), result.GetBoolean(1), result.GetString(2), result.GetString(3));
                        inuseLines.Add(entry);
                    }
                }
                return inuseLines;
            }
        }
        /// <summary>
        /// This gets a random line from the DB and replaces the [REPLACE] with replace string.
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="topic"></param>
        /// <param name="victims"></param>
        /// <returns></returns>
        public string GetRandomLine(BotChannel bChan, string topic, string victims){
            string msg = GetRandomLine(bChan, topic);
            if(msg.Contains("[REPLACE]")){
                return msg.Replace("[REPLACE]", victims);
            }
            return msg + " " + victims;
        }


        /// <summary>
        /// Gets a random string from DB table TableName(bChan.Key) under topic that are INUSE.
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="topic"></param>
        /// <returns></returns>
        public string GetRandomLine(BotChannel bChan, string topic)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{TableName(bChan.Key)}\" WHERE inuse IS @inuse AND topic IS @topic ORDER BY RANDOM() LIMIT 1";
                cmd.Parameters.AddWithValue("@inuse", true);
                cmd.Parameters.AddWithValue("@topic", topic);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    Core.LOG(new LogEntry(LOGSEVERITY.ERROR, PLUGINNAME+"DatabseStrings", $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                if (!result.HasRows) { return "No string found in DB. Make sure to add lines and that they are inuse."; }
                return result.GetString(3);
            }

        }
        /// <summary>
        /// This will return TRUE if table already has been initiated
        /// </summary>
        /// <param name="bChan"></param>
        /// <returns></returns>
        public async Task<bool> TableInit(BotChannel bChan)
        {
            bool result = false;
            await Task.Run(() => {
                if (TableExists(TableName(bChan.Key)))
                {
                    result = true;
                }
                else
                {
                    TableCreate(bChan.Key);
                    result = false;
                }
            });
            return result;
        }
        /// <summary>
        /// Drops the DB table TableName(bChan.Key)
        /// </summary>
        /// <param name="bChan"></param>
        /// <returns></returns>
        public async Task<bool> TableDrop(BotChannel bChan)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                /*cmd.CommandText = $"PRAGMA foreign_keys = OFF" +
                    $" DROP TABLE IF EXISTS \"{TableName(bChan.Key)}\"" +
                    $" PRAGMA foreign_keys = ON";
                */
                //cmd.CommandText = $"PRAGMA foreign_keys = OFF DROP TABLE [IF EXISTS] \"{TableName(bChan.Key)}\" PRAGMA foreign_keys = ON";

                cmd.CommandText = $"PRAGMA foreign_keys = OFF, DROP TABLE \"{TableName(bChan.Key)}\", PRAGMA foreign_keys = ON";
                
                try
                {
                   int i = await cmd.ExecuteNonQueryAsync();
                    i = 123;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    throw;
                }
                return TableExists(TableName(bChan.Key));
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
        private void TableCreate(string chanKey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                //ID int primary key IDENTITY(1,1) NOT NULL
                cmd.CommandText = $"CREATE TABLE \"{TableName(chanKey)}\" (" +
                    $"ROWID INTEGER PRIMARY KEY AUTOINCREMENT," +
                    $"inuse BOOLEAN, " +
                    $"topic VACHAR(30), " +
                    $"text VACHAR(255)" +
                    $")";
                cmd.ExecuteNonQuery();
            }
        }
        #endregion
        #region Internals
        /// <summary>
        /// Constructs the tablename from the BotChannel Key and PLUGINNAME fed through constructor.
        /// </summary>
        /// <param name="chanKey"></param>
        /// <returns></returns>
        private string TableName(string chanKey)
        {
            return chanKey + "_" + PLUGINNAME + "_strings";
        }
        #endregion
    }//END of DatabaseStrings
}
