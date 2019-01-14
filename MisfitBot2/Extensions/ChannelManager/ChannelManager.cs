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

namespace MisfitBot2.Services
{
    public class ChannelManager
    {
        public BotChannels _botChannels { get; private set; }
        private readonly string PLUGINNAME = "ChannelManager";
        //private readonly string FILENAME = "ChannelManager/BotChannels.json";
        public BotChannelMergeEvent OnBotChannelMerge;
        public BotChannelGoesOffline OnBotChannelGoesOffline;
        public Dictionary<string, TwPubSub> PubSubClients = new Dictionary<string, TwPubSub>();
        /// CONSTRUCTOR
        public ChannelManager()
        {
            _botChannels = new BotChannels();

            // Setup the database table if needed
            if (!TableExists(PLUGINNAME))
            {
                TableCreate(PLUGINNAME);
            }


            //if (!Load())
            //{
            //Save();// Creates a new file if we had none
            //}
            //Save(); // This makes sure we save the cleaned file. On start we clean old entries left over after linkning channels
            Core.Discord.GuildAvailable += DiscordGuildAvailable;
            Core.Channels = this;
            TimerStuff.OnMinuteTick += OnMinuteTick;
        }// EO Constructor

        public async Task<bool> SaveAsLinked(BotChannel channel)
        {
            // make sure to delete twitch botchannel from DB
            return false;
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
                BotChannel bChan = new BotChannel(GuildID);
                bChan.isLinked = result.GetBoolean(0);
                bChan.GuildID = (ulong)result.GetInt64(1);
                bChan.GuildName = result.GetString(2);
                bChan.discordDefaultBotChannel = (ulong)result.GetInt64(3);
                bChan.discordAdminChannel = (ulong)result.GetInt64(4);
                bChan.TwitchChannelID = result.GetString(5);
                bChan.TwitchChannelName = result.GetString(6);
                bChan.isTwitch = result.GetBoolean(7);
                bChan.isLive = result.GetBoolean(8);
                bChan.TwitchAutojoin = result.GetBoolean(9);
                bChan.pubsubOauth = result.GetString(10);
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
                BotChannel bChan = new BotChannel(TwitchChannelName);
                bChan.isLinked = result.GetBoolean(0);
                bChan.GuildID = (ulong)result.GetInt64(1);
                bChan.GuildName = result.GetString(2);
                bChan.discordDefaultBotChannel = (ulong)result.GetInt64(3);
                bChan.discordAdminChannel = (ulong)result.GetInt64(4);
                bChan.TwitchChannelID = result.GetString(5);
                bChan.TwitchChannelName = result.GetString(6);
                bChan.isTwitch = result.GetBoolean(7);
                bChan.isLive = result.GetBoolean(8);
                bChan.TwitchAutojoin = result.GetBoolean(9);
                bChan.pubsubOauth = result.GetString(10);
                return bChan;
            }
        }
        public async Task ChannelSave(BotChannel bChan)
        {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = Core.Data;
                if (bChan.isLinked || bChan.GuildID != 0)
                {
                    cmd.CommandText = $"UPDATE {PLUGINNAME} SET " +
                            $"isLinked = @isLinked, " +
                            $"GuildName = @GuildName, " +
                            $"discordDefaultBotChannel = @discordDefaultBotChannel, " +
                            $"discordAdminChannel = @discordAdminChannel, " +
                            $"TwitchChannelID = @TwitchChannelID, " +
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
                            $"GuildName = @GuildName, " +
                            $"discordDefaultBotChannel = @discordDefaultBotChannel, " +
                            $"discordAdminChannel = @discordAdminChannel, " +
                            $"TwitchChannelID = @TwitchChannelID, " +
                            $"isTwitch = @isTwitch, " +
                            $"isLive = @isLive, " +
                            $"TwitchAutojoin = @TwitchAutojoin, " +
                            $"pubsubOauth = @pubsubOauth " +
                            $" WHERE TwitchChannelName is @TwitchChannelName";
                    cmd.Parameters.AddWithValue("@TwitchChannelName", bChan.TwitchChannelName);
                }
                    cmd.Parameters.AddWithValue("@isLinked", bChan.isLinked);
                    cmd.Parameters.AddWithValue("@GuildName", bChan.GuildName);
                    cmd.Parameters.AddWithValue("@discordDefaultBotChannel", bChan.discordDefaultBotChannel);
                    cmd.Parameters.AddWithValue("@discordAdminChannel", bChan.discordAdminChannel);
                    cmd.Parameters.AddWithValue("@TwitchChannelID", bChan.TwitchChannelID);
                    cmd.Parameters.AddWithValue("@isTwitch", bChan.isTwitch);
                    cmd.Parameters.AddWithValue("@isLive", bChan.isLive);
                    cmd.Parameters.AddWithValue("@TwitchAutojoin", bChan.TwitchAutojoin);
                    cmd.Parameters.AddWithValue("@pubsubOauth", bChan.pubsubOauth);
                    cmd.ExecuteNonQuery();
                }
            
            await Core.LOG(new LogMessage(LogSeverity.Warning, PLUGINNAME, $"Saving updated channeldata"));
        }
        private void ChannelDataWrite(BotChannel bChan)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                    cmd.CommandText = $"INSERT INTO {PLUGINNAME} VALUES (" +
                        $"@isLinked, " +
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
                Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Created entry for Discord Guild {bChan.GuildName}"));
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
        #endregion

        /// <summary>
        /// Returns the BotChannel for the Discord Guild. Proritizes linked if more then one is found. Can return NULL
        /// </summary>
        /// <param name="guildID"></param>
        /// <returns></returns>
        public async Task<BotChannel> GetDiscordGuildbyID(ulong guildID)
        {
            if (!await ChannelDataExists(guildID))
            {
                SocketGuild guild = Core.Discord.GetGuild(guildID);
                ChannelDataWrite(new BotChannel(guild.Id, guild.Name));
            }
            return await ChannelDataRead(guildID);
        }

        public async Task<BotChannel> GetTwitchChannelByName(string TwitchName)
        {
            if (!await ChannelDataExists(TwitchName))
            {
                JoinedChannel twitchChannel = Core.Twitch._client.GetJoinedChannel(TwitchName);
                ChannelDataWrite(new BotChannel(twitchChannel.Channel));
            }
            return await ChannelDataRead(TwitchName);
        }


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
                PubSubClients[bChan.TwitchChannelID].Connect();
            }
            else
            {
                await Core.LOG(new LogMessage(LogSeverity.Warning, PLUGINNAME, "RestartPubSub::Not a valid TwitchID."));
            }
        }

        public void OnMinuteTick(int minutes)
        {
            UpdateChannelStatuses();
        }

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
                    channels = await Core.Twitch._api.V5.Users.GetUserByNameAsync(Core.Twitch._client.JoinedChannels[i].Channel);
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

        private async Task DiscordGuildAvailable(SocketGuild arg)
        {
            if (!await ChannelDataExists(arg.Id))
            {
                ChannelDataWrite(new BotChannel(arg.Id, arg.Name));
            }

           /* if (_botChannels.GetDiscordGuildbyID(arg.Id) == null)
            {
                await _botChannels.AddChannel(new BotChannel(arg.Id, arg.Name));
            }
            */

        }

        public async Task<bool> JoinTwitchChannel(string channelName)
        {
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, PLUGINNAME, $"Trying to join twitch channel \"{channelName}\""));
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
                        BotChannel newChannel = new BotChannel(c.Name);
                        newChannel.TwitchChannelID = c.Id;
                        newChannel.isTwitch = true;
                        newChannel.TwitchAutojoin = true;
                        //await _botChannels.AddChannel(newChannel);
                        await ChannelSave(newChannel);
                    }

                }
            }


            await _botChannels.JoinAllAutoJoinTwitchChannels();

            foreach (BotChannel bChan in _botChannels.GetChannels())
            {
                // Debug end
                if (bChan.pubsubOauth != string.Empty)
                {
                    if (!PubSubClients.ContainsKey(bChan.TwitchChannelID))
                    {
                        PubSubClients[bChan.TwitchChannelID] = new TwPubSub(bChan.pubsubOauth, bChan.TwitchChannelID, bChan.TwitchChannelName);
                    }
                }
            }
        }

        
    }
}
