using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using Discord.WebSocket;
using MisfitBot_MKII.MisfitBotEvents;
using TwitchLib.Client.Models;
using MisfitBot_MKII.Statics;
using System.Linq;

namespace MisfitBot_MKII.Extensions.ChannelManager
{
    /// <summary>
    /// Keeps track of bot channels and handles realtime functionality stuff.
    /// </summary>
    public class ChannelManager
    {
        private readonly string PLUGINNAME = "ChannelManager";
        public TwitchChannelGoesOfflineEvent OnBotChannelGoesOffline;
        private volatile bool apiQueryLock = false; // This is true when we have an active channel request towards Twitch API running
        /// CONSTRUCTOR
        public ChannelManager()
        {
            // Setup the database table if needed
            if (!TableExists(PLUGINNAME))
            {
                TableCreate(PLUGINNAME);
            }
            Program.BotEvents.OnDiscordGuildAvailable += OnDiscordGuildAvailable; // TODO fix with bot wide discord client connected event
            Program.BotEvents.OnTwitchConnected += OnTwitchConnected;
            TimerStuff.OnMinuteTick += OnMinuteTick;
        }// EO Constructor

        private async void OnTwitchConnected()
        {
            await JoinAllAutoJoinTwitchChannels();
        }

        /// <summary>
        /// Removes 1 bot channel with twitchID from DB and saves the passed bot channel to DB.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public async Task<bool> SaveAsLinked(BotChannel channel)
        {
            if(await ChannelDataDeleteTwitchID(channel.TwitchChannelID))
            {
                ChannelSave(channel);
            }
            return (await GetDiscordGuildByID(channel.GuildID)).isLinked;
        }
        /// <summary>
        /// Tries to connect to the channel after validation through Twitch.API.
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public async Task<bool> JoinTwitchChannel(string channelName)
        {
            try
            {
                TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse channelEntry = await Program.TwitchAPI.Helix.Users.GetUsersAsync(null, new List<string>(){ channelName.ToLower()});
                if (channelEntry.Users.Length < 1)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.ERROR, PLUGINNAME, $"Twitch channel lookup failed! Couldn't find channel. Not connecting to \"{channelName.ToLower()}\""));
                    return false;
                }
            }
            catch (Exception)
            {
                await Core.LOG(new LogEntry(LOGSEVERITY.ERROR, PLUGINNAME, $"Twitch channel lookup failed! Couldn't find channel. Not connecting to \"{channelName.ToLower()}\""));
                return false;
            }
            Program.TwitchClient.JoinChannel(channelName);
            return true;
        }

        public void OnMinuteTick(int minutes)
        {
            UpdateChannelStatuses();
        }
        /// <summary>
        /// Checks all connected Twitch channels and Discord Guilds so we have valid entries for them and updates the isLive flag on Twitch channels.
        /// </summary>
        private async void UpdateChannelStatuses()
        {
            try
            {
                //TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse channels;
                
                var fetchedStreams = await Program.TwitchAPI.Helix.Streams.GetStreamsAsync(
                    userLogins: Program.TwitchClient.JoinedChannels.ToList().Where(p=>p.Channel != string.Empty).Select(p=>p.Channel).ToList() 
                    );

                for (int i = 0; i < Program.TwitchClient.JoinedChannels.Count; i++)
                {
                    bool isLive = false;
                    BotChannel bChan = await GetTwitchChannelByName(Program.TwitchClient.JoinedChannels[i].Channel);
                    if (bChan == null)
                    {
                        continue;
                    }
                    isLive = fetchedStreams.Streams.ToList().Exists(p=>p.UserId == bChan.TwitchChannelID);
                    if (bChan.isLive != isLive)
                    {
                        if (isLive)
                        {
                            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, PLUGINNAME, $"Twitch channel \"{bChan.TwitchChannelName}\" went live!!"));
                            bChan.isLive = true;
                            ChannelSave(bChan);
                        }
                        else
                        {
                            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, PLUGINNAME, $"Twitch channel \"{bChan.TwitchChannelName}\" is now offline."));
                            bChan.isLive = false;
                            ChannelSave(bChan);
                        }
                    }
                }

            }
            catch (Exception)
            {
                return;
            }








            if (Program.DiscordClient.Guilds != null)
            {
                // Make sure discord guilds we are connected to exist in DB
                foreach (SocketGuild guild in Program.DiscordClient.Guilds)
                {
                    await GetDiscordGuildByID(guild.Id);
                }
            }
        }// END of UpdateChannelStatuses

        /// <summary>
        /// Returns true if we are connected to the channel
        /// </summary>
        /// <param name="twitchChannelName"></param>
        /// <returns></returns>
        public bool CheckIfInTwitchChannel(string twitchChannelName)
        {
            foreach (JoinedChannel channel in Program.TwitchClient.JoinedChannels){
                if(channel.Channel == twitchChannelName) {return true;}
            }
            return false;
        }

        /// <summary>
        /// Listens for when a Discord Guild is available so we can make sure we have a valid entry for it.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async void OnDiscordGuildAvailable(SocketGuild arg)
        {
            if (!await ChannelDataExists(arg.Id))
            {
                await ChannelDataWrite(new BotChannel(arg.Id, arg.Name));
            }
        }

        #region GetChannel methods
        /// <summary>
        /// Returns the BotChannel for the Discord Guild. Creates one if it doesn't exists.
        /// </summary>
        /// <param name="guildID"></param>
        /// <returns></returns>
        public async Task<BotChannel> GetDiscordGuildByID(ulong guildID)
        {
            if (!await ChannelDataExists(guildID))
            {
                SocketGuild guild = Program.DiscordClient.GetGuild(guildID);
                await ChannelDataWrite(new BotChannel(guild.Id, guild.Name));
            }
            return await ChannelDataRead(guildID);
        }
        /// <summary>
        /// Returns 1 match from DB. Creates one if needed and then resolves the Twitch name against Twitch.API to get the Twitch.ID
        /// Return Null if unknown user fails a lookup through Twitch API
        /// </summary>
        /// <param name="TwitchName"></param>
        /// <returns></returns>
        public async Task<BotChannel> GetTwitchChannelByName(string TwitchName)
        {

            if (!await ChannelDataExists(TwitchName))
            {
                TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse channelEntry = null;
                try
                {
                    while (apiQueryLock) { }
                    if (!await ChannelDataExists(TwitchName))
                    {
                        apiQueryLock = true;
                        channelEntry = await Program.TwitchAPI.Helix.Users.GetUsersAsync(null, new List<string>(){ TwitchName.ToLower()});
                    }
                    if (channelEntry == null || channelEntry.Users.Length < 1)
                    {
                        apiQueryLock = false;
                        return null;
                    }
                    await ChannelDataWrite(new BotChannel(channelEntry.Users[0].Login, channelEntry.Users[0].Id));
                    apiQueryLock = false;
                    return await ChannelDataRead(TwitchName);
                }
                catch (Exception)
                {
                    apiQueryLock = false;
                    return null;
                }
            }
            else
            {
                return await ChannelDataRead(TwitchName);
            }
        }
        /// <summary>
        /// Returns 1 match from DB. Creates one if needed and then resolves the TwitchID against Twitch.API to get the Twitch name.
        /// </summary>
        /// <param name="TwitchName"></param>
        /// <returns></returns>
        public async Task<BotChannel> GetTwitchChannelByID(string TwitchID)
        {
            if (!await ChannelDataExistsTwitchID(TwitchID))
            {
                TwitchLib.Api.Helix.Models.Users.GetUsers.User channel = null;
                try
                {
                    while (apiQueryLock) { }
                    if (!await ChannelDataExistsTwitchID(TwitchID))
                    {
                        apiQueryLock = true;
                        channel = await Program.Users.GetUserByTwitchIDFromAPI(TwitchID);
                    }
                    if (channel == null)
                    {
                        apiQueryLock = false;
                        return null;
                    }
                    await ChannelDataWrite(new BotChannel(channel.Login, channel.Id));
                    apiQueryLock = false;
                    return await ChannelDataReadTwitchID(TwitchID);
                }
                catch (Exception)
                {
                    apiQueryLock = false;
                    return null;
                }
            }
            else
            {
                return await ChannelDataReadTwitchID(TwitchID);
            }
        }
        /// <summary>
        /// Returns 1 match from DB. If no match can be found NULL is returned.
        /// </summary>
        /// <param name="TwitchName"></param>
        /// <returns></returns>
        public async Task<BotChannel> GetBotChannelByKey(string key)
        {
            // This could be rewritten for moke exclusive hits directly from DB (once key is added to DB)
            List<BotChannel> Channels = await GetChannels();
            if (Channels.FindAll(p => p.Key == key).Count == 1)
            {
                return Channels.Find(p => p.Key == key);
            }
            else if (Channels.FindAll(p => p.Key == key).Count > 1)
            {
                return Channels.FindAll(p => p.Key == key).Find(p => p.isLinked == true);
            }
            return null;
        }
        #endregion


        
        /// <summary>
        /// Gets all channels from DB. Looks up all flagged as auto join channels against Twitch.API. Then checks if we are in the valid channels.
        /// If not, we join them.
        /// </summary>
        /// <returns></returns>
        public async Task JoinAllAutoJoinTwitchChannels()
        {
            List<string> channelsToLookup = new List<string>();
            foreach (BotChannel chan in await GetChannels())
            {
                if (chan.TwitchChannelName != string.Empty && chan.TwitchAutoJoin)
                {
                    channelsToLookup.Add(chan.TwitchChannelName);
                }
            }
            if (channelsToLookup.Count < 1)
            {
                return;
            }
            TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse channelEntries = await Program.Users.GetUsersByTwitchUsernamesFromAPI(channelsToLookup);
            if (channelEntries.Users.Length < 1)
            {
                return;
            }
            foreach (TwitchLib.Api.Helix.Models.Users.GetUsers.User usr in channelEntries.Users)
            {
                var channel = Program.TwitchClient.GetJoinedChannel(usr.Login);
                if (channel == null)
                {
                    Program.TwitchClient.JoinChannel(usr.Login);
                }
            }
        }
        
        
        #region Database stuff
        private async Task<bool> ChannelDataExists(ulong GuildID)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINNAME} WHERE GuildID IS @GuildID";
                cmd.Parameters.AddWithValue("@GuildID", GuildID);

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
        /// <summary>
        /// Returns True if DB has channel entry
        /// </summary>
        /// <param name="TwitchName"></param>
        /// <returns></returns>
        private async Task<bool> ChannelDataExists(string TwitchName)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINNAME} WHERE TwitchChannelName IS @TwitchChannelName";
                cmd.Parameters.AddWithValue("@TwitchChannelName", TwitchName);

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
        private async Task<bool> ChannelDataExistsTwitchID(string TwitchID)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINNAME} WHERE TwitchChannelID IS @TwitchChannelID";
                cmd.Parameters.AddWithValue("@TwitchChannelID", TwitchID);

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
        private async Task<BotChannel> ChannelDataRead(ulong GuildID)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINNAME} WHERE GuildID IS @GuildID";
                cmd.Parameters.AddWithValue("@GuildID", GuildID);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                BotChannel bChan = SQLReaderToBotChannel(result);
                return bChan;
            }
        }
        private async Task<BotChannel> ChannelDataRead(string TwitchChannelName)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINNAME} WHERE TwitchChannelName IS @TwitchChannelName";
                cmd.Parameters.AddWithValue("@TwitchChannelName", TwitchChannelName);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                BotChannel bChan = SQLReaderToBotChannel(result);
                return bChan;
            }
        }
        private async Task<BotChannel> ChannelDataReadTwitchID(string TwitchID)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINNAME} WHERE TwitchChannelID IS @TwitchChannelID";
                cmd.Parameters.AddWithValue("@TwitchChannelID", TwitchID);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                BotChannel bChan = SQLReaderToBotChannel(result);
                return bChan;
            }
        }
        private async Task<bool> ChannelDataDeleteTwitchID(string TwitchID)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"DELETE FROM {PLUGINNAME} WHERE TwitchChannelID IS @TwitchChannelID";
                cmd.Parameters.AddWithValue("@TwitchChannelID", TwitchID);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();

            }
                return !(await ChannelDataExistsTwitchID(TwitchID));
        }
        public async Task<List<BotChannel>> GetChannels()
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                List<BotChannel> botChannels = new List<BotChannel>();

                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINNAME}";
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                while (result.Read())
                {
                    BotChannel bChan = new BotChannel("", "")
                    {
                        isLinked = result.GetBoolean(0),
                        Key = result.GetString(1),
                        GuildID = (ulong)result.GetInt64(2),
                        GuildName = result.GetString(3),
                        discordDefaultBotChannel = (ulong)result.GetInt64(4),
                        discordAdminChannel = (ulong)result.GetInt64(5),
                        TwitchChannelID = result.GetString(6),
                        TwitchChannelName = result.GetString(7),
                        isTwitch = result.GetBoolean(8),
                        isLive = result.GetBoolean(9),
                        TwitchAutoJoin = result.GetBoolean(10),
                        pubsubOauth = result.GetString(11)
                    };
                    botChannels.Add(bChan);
                }
                return botChannels;
            }
        }
        /// <summary>
        /// This saves the given BotChannel to the DB
        /// </summary>
        /// <param name="bChan"></param>
        public void ChannelSave(BotChannel bChan)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                bChan.UpdateKey();
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                if (bChan.isLinked || bChan.GuildID != 0)
                {
                    cmd.CommandText = $"UPDATE {PLUGINNAME} SET " +
                            $"isLinked = @isLinked, " +
                            $"Key = @Key, " + 
                            $"GuildName = @GuildName, " +
                            $"discordDefaultBotChannel = @discordDefaultBotChannel, " +
                            $"discordAdminChannel = @discordAdminChannel, " +
                            $"TwitchChannelID = @TwitchChannelID, " +
                            $"TwitchChannelName = @TwitchChannelName, " + 
                            $"isTwitch = @isTwitch, " +
                            $"isLive = @isLive, " +
                            $"TwitchAutojoin = @TwitchAutojoin, " +
                            $"pubsubOauth = @pubsubOauth " +
                            $" WHERE GuildID is @GuildID";
                    cmd.Parameters.AddWithValue("@GuildID", bChan.GuildID);
                }
                else
                {
                    cmd.CommandText = $"UPDATE {PLUGINNAME} SET " +
                            $"isLinked = @isLinked, " +
                            $"Key = @Key, " +
                            $"GuildName = @GuildName, " +
                            $"discordDefaultBotChannel = @discordDefaultBotChannel, " +
                            $"discordAdminChannel = @discordAdminChannel, " +
                            $"TwitchChannelID = @TwitchChannelID, " +
                            $"TwitchChannelName = @TwitchChannelName, " +
                            $"isTwitch = @isTwitch, " +
                            $"isLive = @isLive, " +
                            $"TwitchAutojoin = @TwitchAutojoin, " +
                            $"pubsubOauth = @pubsubOauth " +
                            $" WHERE TwitchChannelName is @TwitchChannelName";
                    cmd.Parameters.AddWithValue("@TwitchChannelName", bChan.TwitchChannelName);
                }
                cmd.Parameters.AddWithValue("@isLinked", bChan.isLinked);
                cmd.Parameters.AddWithValue("@Key", bChan.Key);
                cmd.Parameters.AddWithValue("@GuildName", bChan.GuildName);
                cmd.Parameters.AddWithValue("@discordDefaultBotChannel", bChan.discordDefaultBotChannel);
                cmd.Parameters.AddWithValue("@discordAdminChannel", bChan.discordAdminChannel);
                cmd.Parameters.AddWithValue("@TwitchChannelID", bChan.TwitchChannelID);
                cmd.Parameters.AddWithValue("@TwitchChannelName", bChan.TwitchChannelName);
                cmd.Parameters.AddWithValue("@isTwitch", bChan.isTwitch);
                cmd.Parameters.AddWithValue("@isLive", bChan.isLive);
                cmd.Parameters.AddWithValue("@TwitchAutojoin", bChan.TwitchAutoJoin);
                cmd.Parameters.AddWithValue("@pubsubOauth", bChan.pubsubOauth);
                cmd.ExecuteNonQuery();
            }

            //await Core.LOG(new LogMessage(LogSeverity.Warning, PLUGINNAME, $"Saving updated channeldata"));
        }
        /// <summary>
        /// This only takes a bot channel instance and write it into the DB. Not for saving or updating values
        /// </summary>
        /// <param name="bChan"></param>
        /// <returns></returns>
        private async Task ChannelDataWrite(BotChannel bChan)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                bChan.UpdateKey();
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"INSERT INTO {PLUGINNAME} VALUES (" +
                    $"@isLinked, " +
                    $"@Key, " + 
                    $"@GuildID, " +
                    $"@GuildName, " +
                    $"@discordDefaultBotChannel, " +
                    $"@discordAdminChannel, " +
                    $"@TwitchChannelID, " +
                    $"@TwitchChannelName, " +
                    $"@isTwitch, " +
                    $"@isLive, " +
                    $"@TwitchAutojoin, " +
                    $"@pubsubOauth " +
                    $")";
                cmd.Parameters.AddWithValue("@isLinked", bChan.isLinked);
                cmd.Parameters.AddWithValue("@Key", bChan.Key);
                cmd.Parameters.AddWithValue("@GuildID", bChan.GuildID);
                cmd.Parameters.AddWithValue("@GuildName", bChan.GuildName);
                cmd.Parameters.AddWithValue("@discordDefaultBotChannel", bChan.discordDefaultBotChannel);
                cmd.Parameters.AddWithValue("@discordAdminChannel", bChan.discordAdminChannel);
                cmd.Parameters.AddWithValue("@TwitchChannelID", bChan.TwitchChannelID);
                cmd.Parameters.AddWithValue("@TwitchChannelName", bChan.TwitchChannelName);
                cmd.Parameters.AddWithValue("@isTwitch", bChan.isTwitch);
                cmd.Parameters.AddWithValue("@isLive", bChan.isLive);
                cmd.Parameters.AddWithValue("@TwitchAutojoin", bChan.TwitchAutoJoin);
                cmd.Parameters.AddWithValue("@pubsubOauth", bChan.pubsubOauth);
                cmd.ExecuteNonQuery();
                if (bChan.isLinked)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, PLUGINNAME, $"Created linked entry for Discord Guild {bChan.GuildName} and TwitchChannel {bChan.TwitchChannelName}"));
                }
                else if (bChan.isTwitch)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, PLUGINNAME, $"Created entry for Twitch channel {bChan.TwitchChannelName}"));
                }
                else
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, PLUGINNAME, $"Created entry for Discord Guild {bChan.GuildName}"));
                }
            }
        }
        private void TableCreate(string plugin)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"CREATE TABLE {plugin} (" +
                    $"isLinked BOOL, " +
                    $"Key VACHAR(30), " +
                    $"GuildID INTEGER," +
                    $"GuildName VACHAR(30), " +
                    $"discordDefaultBotChannel INTEGER, " +
                    $"discordAdminChannel INTEGER, " +
                    $"TwitchChannelID VACHAR(30), " +
                    $"TwitchChannelName VACHAR(30), " +
                    $"isTwitch BOOL, " +
                    $"isLive BOOL, " +
                    $"TwitchAutoJoin BOOL, " +
                    $"pubsubOauth VACHAR(30)" +
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
        private BotChannel SQLReaderToBotChannel(SQLiteDataReader result)
        {
            BotChannel bChan = new BotChannel((ulong)result.GetInt64(2), result.GetString(3));
            bChan.isLinked = result.GetBoolean(0);
            bChan.Key = result.GetString(1);
            bChan.GuildID = (ulong)result.GetInt64(2);
            bChan.GuildName = result.GetString(3);
            bChan.discordDefaultBotChannel = (ulong)result.GetInt64(4);
            bChan.discordAdminChannel = (ulong)result.GetInt64(5);
            bChan.TwitchChannelID = result.GetString(6);
            bChan.TwitchChannelName = result.GetString(7);
            bChan.isTwitch = result.GetBoolean(8);
            bChan.isLive = result.GetBoolean(9);
            bChan.TwitchAutoJoin = result.GetBoolean(10);
            bChan.pubsubOauth = result.GetString(11);
            return bChan;
        }
        #endregion
    }
}
