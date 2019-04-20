using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MisfitBot2;
using MisfitBot2.Extensions.ChannelManager;
using TwitchLib.Client.Models;

namespace MisfitBot2.Extensions.ChannelManager
{
    /// <summary>
    /// Keeps track of botchannels and handles realtime functionality stuff.
    /// </summary>
    public class ChannelManager
    {
        private readonly string PLUGINNAME = "ChannelManager";
        private List<BotChannel> channelsToSave = new List<BotChannel>(); // Memory cache to avoid race condition when saving new botchannels to DB
        public BotChannelMergeEvent OnBotChannelMerge;
        public BotChannelGoesOffline OnBotChannelGoesOffline;
        public Dictionary<string, TwPubSub> PubSubClients = new Dictionary<string, TwPubSub>();
        /// CONSTRUCTOR
        public ChannelManager()
        {
            // Setup the database table if needed
            if (!TableExists(PLUGINNAME))
            {
                TableCreate(PLUGINNAME);
            }
            Core.Discord.GuildAvailable += OnDiscordGuildAvailable;
            Core.Channels = this;
            TimerStuff.OnMinuteTick += OnMinuteTick;
        }// EO Constructor
        /// <summary>
        /// Removes 1 botchannel with twitchID from DB and saves the passed botchannel to DB.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public async Task<bool> SaveAsLinked(BotChannel channel)
        {
            if(await ChannelDataDeleteTwitchID(channel.TwitchChannelID))
            {
                await ChannelSave(channel);
            }
            return (await GetDiscordGuildbyID(channel.GuildID)).isLinked;
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
                TwitchLib.Api.V5.Models.Users.Users channelEntry = await Core.Twitch._api.V5.Users.GetUserByNameAsync(channelName);
                if (channelEntry.Matches.Length < 1)
                {
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Error, PLUGINNAME, $"Twitch channel lookup failed! Couldn't find channel. Not connecting to \"{channelName}\""));
                    return false;
                }
            }
            catch (Exception)
            {
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Error, PLUGINNAME, $"Twitch channel lookup failed! Couldn't find channel. Not connecting to \"{channelName}\""));
                return false;
            }
            Core.Twitch._client.JoinChannel(channelName);
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
            if (Core.Twitch == null)
            {
                return;
            }
            for (int i = 0; i < Core.Twitch._client.JoinedChannels.Count; i++)
            {
                TwitchLib.Api.V5.Models.Users.Users channels;
                try
                {
                    channels = await Core.Twitch._api.V5.Users.GetUserByNameAsync(Core.Twitch._client.JoinedChannels[i].Channel);// Maybe rewrite to bulk get channels
                }
                catch (Exception)
                {
                    return;
                }
                if (channels.Matches.Length > 0)
                {
                    bool isLive = false;
                    try
                    {
                        isLive = await Core.Twitch._api.V5.Streams.BroadcasterOnlineAsync(channels.Matches[0].Id);
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    BotChannel bChan = await GetTwitchChannelByName(channels.Matches[0].Name);
                    if (bChan == null)
                    {
                        continue;
                    }
                    if (bChan.isLive != isLive)
                    {
                        if (isLive)
                        {
                            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, PLUGINNAME, $"Twitch channel \"{channels.Matches[0].Name}\" went live!!"));
                            bChan.isLive = true;
                            await ChannelSave(bChan);
                        }
                        else
                        {
                            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, PLUGINNAME, $"Twitch channel \"{channels.Matches[0].Name}\" is now offline."));
                            bChan.isLive = false;
                            await ChannelSave(bChan);
                        }
                    }
                }
            }
            foreach (SocketGuild guild in Core.Discord.Guilds)
            {
                await GetDiscordGuildbyID(guild.Id);
            }
        }// END of UpdateChannelStatuses
        /// <summary>
        /// Listens for when a Discord Guild is available so we can make sure we have a valid entry for it.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task OnDiscordGuildAvailable(SocketGuild arg)
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
        public async Task<BotChannel> GetDiscordGuildbyID(ulong guildID)
        {
            if (!await ChannelDataExists(guildID))
            {
                SocketGuild guild = Core.Discord.GetGuild(guildID);
                await ChannelDataWrite(new BotChannel(guild.Id, guild.Name));
            }
            return await ChannelDataRead(guildID);
        }
        /// <summary>
        /// Returns 1 match from DB. Creates one if needed and then resolves the Twitchname against Twitch.API to get the Twitch.ID
        /// Return Null if unkown user fails a lookup through Twitch API
        /// </summary>
        /// <param name="TwitchName"></param>
        /// <returns></returns>
        public async Task<BotChannel> GetTwitchChannelByName(string TwitchName)
        {
            if (!await ChannelDataExists(TwitchName))
            {
                TwitchLib.Api.V5.Models.Users.Users channelEntry;
                try
                {
                    channelEntry = await Core.Twitch._api.V5.Users.GetUserByNameAsync(TwitchName);
                }
                catch (Exception)
                {
                    return null;
                }
                if (channelEntry.Matches.Length < 1)
                {
                    return null;
                }
                if (!channelsToSave.Exists(p => p.TwitchChannelName == TwitchName))
                {
                    channelsToSave.Add(new BotChannel(TwitchName, channelEntry.Matches[0].Id));
                    await ChannelDataWrite(channelsToSave.Find(p => p.TwitchChannelName == TwitchName));
                    channelsToSave.RemoveAll(p => p.TwitchChannelName == TwitchName);
                }
                else
                {
                    return channelsToSave.Find(p => p.TwitchChannelName == TwitchName);
                }
            }
            return await ChannelDataRead(TwitchName);
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
                TwitchLib.Api.V5.Models.Users.User channel = await Core.Twitch._api.V5.Users.GetUserByIDAsync(TwitchID);
                if (channel == null)
                {
                    return null;
                }
                if (!channelsToSave.Exists(p => p.TwitchChannelID == TwitchID))
                {
                    channelsToSave.Add(new BotChannel(channel.Name, channel.Id));
                    await ChannelDataWrite(channelsToSave.Find(p => p.TwitchChannelID == TwitchID));
                    channelsToSave.RemoveAll(p => p.TwitchChannelID == TwitchID);
                }
                else
                {
                    return channelsToSave.Find(p => p.TwitchChannelID == TwitchID);
                }
            }
            return await ChannelDataReadTwitchID(TwitchID);
        }
        /// <summary>
        /// Returns 1 match from DB. If no match can be found NULL is returned.
        /// </summary>
        /// <param name="TwitchName"></param>
        /// <returns></returns>
        public async Task<BotChannel> GetBotchannelByKey(string key)
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
        /// Makes sure we create a valid botchannel entry for the autojoin channel we gave when connecting to Twitch. Then calls JoinAllAutoJoinTwitchChannels().
        /// </summary>
        /// <returns></returns>
        public async Task JoinAutojoinChannels()
        {
            foreach (JoinedChannel chan in Core.Twitch._client.JoinedChannels)
            {
                TwitchLib.Api.V5.Models.Users.Users channelEntry = await Core.Twitch._api.V5.Users.GetUserByNameAsync(chan.Channel);
                if (channelEntry.Matches.Length > 0)
                {
                    TwitchLib.Api.V5.Models.Channels.Channel c = await Core.Twitch._api.V5.Channels.GetChannelByIDAsync(channelEntry.Matches[0].Id);
                    if (!await ChannelDataExists(c.Name))
                    {
                        BotChannel newChannel = new BotChannel(c.Name, c.Id)
                        {
                            TwitchChannelID = c.Id,
                            isTwitch = true,
                            TwitchAutojoin = true
                        };
                        await ChannelSave(newChannel);
                    }
                }
            }
            await JoinAllAutoJoinTwitchChannels();
        }
        /// <summary>
        /// Gets all channels from DB. Looksup all flagged as autojoin channels against Twitch.API. Then checks if we are in the valid channels. If not we join them.
        /// </summary>
        /// <returns></returns>
        public async Task JoinAllAutoJoinTwitchChannels()
        {
            List<string> chansToLookup = new List<string>();
            foreach (BotChannel chan in await Core.Channels.GetChannels())
            {
                if (chan.TwitchChannelName != string.Empty && chan.TwitchAutojoin)
                {
                    chansToLookup.Add(chan.TwitchChannelName);
                }
            }
            if (chansToLookup.Count < 1)
            {
                return;
            }
            TwitchLib.Api.V5.Models.Users.Users channelEntries = await Core.Twitch._api.V5.Users.GetUsersByNameAsync(chansToLookup);
            if (channelEntries.Matches.Length < 1)
            {
                return;
            }
            foreach (TwitchLib.Api.V5.Models.Users.User usr in channelEntries.Matches)
            {
                var channel = Core.Twitch._client.GetJoinedChannel(usr.Name);
                if (channel == null)
                {
                    Core.Twitch._client.JoinChannel(usr.Name);
                }
            }
            await LaunchAllPubSubs();
        }
        #region pubsub stuff
        /// <summary>
        /// Tries to restart PubSub listener for given Botchannel if there is one.
        /// </summary>
        /// <param name="bChan"></param>
        /// <returns></returns>
        public async Task RestartPubSub(BotChannel bChan)
        {
            await Core.LOG(new LogMessage(LogSeverity.Warning, PLUGINNAME, "RestartPubSub"));
            if (bChan.TwitchChannelID == null || bChan.TwitchChannelID == string.Empty)
            {
                return;
            }
            if (PubSubClients.ContainsKey(bChan.TwitchChannelID))
            {
                PubSubClients[bChan.TwitchChannelID].Close();
                PubSubClients.Remove(bChan.TwitchChannelID);
                StartPubSub(bChan);
            }
            else
            {
                await Core.LOG(new LogMessage(LogSeverity.Warning, PLUGINNAME, "RestartPubSub::Not a valid TwitchID."));
            }
        }
        /// <summary>
        /// Tries to start a PubSub listener for every botchannel in DB.
        /// </summary>
        /// <returns></returns>
        private async Task LaunchAllPubSubs()
        {
            foreach (BotChannel bChan in await GetChannels())
            {
                StartPubSub(bChan, true);
            }
        }
        /// <summary>
        /// Launches individual PubSub for given Botchannel if it has a token.
        /// </summary>
        /// <param name="bChan"></param>
        public async void StartPubSub(BotChannel bChan, bool silent=false)
        {
            // Debug end
            if (bChan.pubsubOauth != string.Empty)
            {
                if (!PubSubClients.ContainsKey(bChan.TwitchChannelID))
                {
                    PubSubClients[bChan.TwitchChannelID] = new TwPubSub(bChan.pubsubOauth, bChan.TwitchChannelID, bChan.TwitchChannelName, silent);
                }
                else
                {
                    if (bChan.discordAdminChannel != 0)
                    {
                        await (Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                            $"A pubsub client for that channel already exists. Use \"{Core._commandCharacter}pubsub restart\" to restart it with updated token."
                            );
                    }
                }
            }
        }
        #endregion
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
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                BotChannel bChan = SQLReaderToBchan(result);
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
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                BotChannel bChan = SQLReaderToBchan(result);
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
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                BotChannel bChan = SQLReaderToBchan(result);
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
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
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
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Database query failed hard. ({cmd.CommandText})"));
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
                        TwitchAutojoin = result.GetBoolean(10),
                        pubsubOauth = result.GetString(11)
                    };
                    botChannels.Add(bChan);
                }
                return botChannels;
            }
        }
        public async Task ChannelSave(BotChannel bChan)
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
                cmd.Parameters.AddWithValue("@TwitchAutojoin", bChan.TwitchAutojoin);
                cmd.Parameters.AddWithValue("@pubsubOauth", bChan.pubsubOauth);
                cmd.ExecuteNonQuery();
            }

            await Core.LOG(new LogMessage(LogSeverity.Warning, PLUGINNAME, $"Saving updated channeldata"));
        }
        /// <summary>
        /// This only takes a botchannel instance and write it into the DB. Not for saving or udpating values
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
                cmd.Parameters.AddWithValue("@TwitchAutojoin", bChan.TwitchAutojoin);
                cmd.Parameters.AddWithValue("@pubsubOauth", bChan.pubsubOauth);
                cmd.ExecuteNonQuery();
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Created entry for Discord Guild {bChan.GuildName}"));
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
        private BotChannel SQLReaderToBchan(SQLiteDataReader result)
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
            bChan.TwitchAutojoin = result.GetBoolean(10);
            bChan.pubsubOauth = result.GetString(11);
            return bChan;
        }
        #endregion
    }
}
