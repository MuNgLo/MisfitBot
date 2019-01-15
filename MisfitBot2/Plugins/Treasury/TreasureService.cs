using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using TwitchLib.Client.Interfaces;
using Newtonsoft.Json;
using TwitchLib.Client.Models;
using MisfitBot2.Extensions.ChannelManager;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;

namespace MisfitBot2.Services
{
    public class TreasureService : ServiceBase, IService
    {
        private readonly string PLUGINNAME = "Treasure";
        private Dictionary<string, List<string>> _goldResponseBuffer;
        private int _goldResponseBufferAge = 0;
        // CONSTRUCTOR
        public TreasureService()
        {
            _goldResponseBuffer = new Dictionary<string, List<string>>();
            Core.UserMan.OnUserEntryMerge += OnUserEntryMerge;
            Core.Channels.OnBotChannelMerge += OnBotChannelEntryMerge;
            Core.Discord.GuildAvailable += OnGuildAvailable;
            Core.Twitch._client.OnChatCommandReceived += TWITCH_OnChatCommandReceived;
            //Core.Twitch._client.OnJoinedChannel += _client_OnJoinedChannel;
            Core.Treasury = this;
            TimerStuff.OnSecondTick += OnSecondTick;
            //TimerStuff.OnMinuteTick += OnMinuteTick;
        }// END of Constructor

        private async void _client_OnJoinedChannel(object sender, TwitchLib.Client.Events.OnJoinedChannelArgs e)
        {
            TreasureSettings settings = await Settings(await Core.Channels.GetTwitchChannelByName(e.Channel));
        }
        private async Task<TreasureSettings> Settings(BotChannel bChan)
        {
            TreasureSettings settings = new TreasureSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as TreasureSettings;
        }
        #region DATA manipulation stuff
        /// <summary>
        /// Get access to the data related to the user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private async Task<TreasureUserDefinition> UserData(UserEntry user, BotChannel bChan)
        {
            
            TreasureUserDefinition userGold = new TreasureUserDefinition(Core.CurrentTime);
            string table = $"{PLUGINNAME}_Users_{bChan.Key}";
            if (!TableExists(table))
            {
                UserTableCreate(table);
            }
            if (!await UserRowExists(table, user.Key))
            {
                UserRowCreate(table, user.Key, userGold);
            }
            return await UserRowRead(table, user.Key);
        }
        new public async Task<bool> UserRowExists(String table, String userkey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {table} WHERE userkey IS @userkey";
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
        new public void UserTableCreate(string plugin)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"CREATE TABLE {plugin} (" +
                    $"userkey VACHAR(30), " +
                    $"created INTEGER, " +
                    $"gold INTEGER, " +
                    $"lastmessage INTEGER, " +
                    $"lastreaction INTEGER, " +
                    $"lasttick INTEGER " +
                    $")";
                cmd.ExecuteNonQuery();
            }
        }
        /// <summary>
        /// Our custum row reader for the custom table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        new public async Task<TreasureUserDefinition> UserRowRead(string table, string userkey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {table} WHERE userkey IS @userkey";
                cmd.Parameters.AddWithValue("@userkey", userkey);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, table, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                TreasureUserDefinition userGold = new TreasureUserDefinition(Core.CurrentTime)
                {
                    _created = result.GetInt32(1),
                    _gold = result.GetInt32(2),
                    _TS_LastMessage = result.GetInt32(3),
                    _TS_LastReaction = result.GetInt32(4),
                    _TS_LastTick = result.GetInt32(5)
                };
                return userGold;
            }
        }
        /// <summary>
        /// Creates a valid row in our custom table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="key"></param>
        /// <param name="userGold"></param>
        public void UserRowCreate(String table, String userkey, TreasureUserDefinition userGold)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"INSERT INTO {table} VALUES (" +
                    $"@userkey, " +
                    $"@created, " +
                    $"@gold, " +
                    $"@lastmessage, " +
                    $"@lastreaction, " +
                    $"@lasttick" +
                    $")";
                cmd.Parameters.AddWithValue("@userkey", userkey);
                cmd.Parameters.AddWithValue("@created", userGold._created);
                cmd.Parameters.AddWithValue("@gold", userGold._gold);
                cmd.Parameters.AddWithValue("@lastmessage", userGold._TS_LastMessage);
                cmd.Parameters.AddWithValue("@lastreaction", userGold._TS_LastReaction);
                cmd.Parameters.AddWithValue("@lasttick", userGold._TS_LastTick);
                cmd.ExecuteNonQuery();
            }
        }
        public async void UserRowDelete(String table, String userkey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"DELETE FROM {table} WHERE userkey IS @userkey";
                cmd.Parameters.AddWithValue("@userkey", userkey);
                cmd.ExecuteNonQuery();
            }
            if(await UserRowExists(table, userkey))
            {
                await Core.LOG(new LogMessage(LogSeverity.Warning, PLUGINNAME, $"Userdata deletion failed!"));
            }
            else
            {
                await Core.LOG(new LogMessage(LogSeverity.Info, PLUGINNAME, $"Userdata deleted."));
            }

        }
        public void SaveUserGold(BotChannel bChan, UserEntry user, TreasureUserDefinition userGold)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                string table = $"{PLUGINNAME}_Users_{bChan.Key}";
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"UPDATE {table} SET " +
                    $"gold = @gold, " +
                    $"lastmessage = @lastmessage, " +
                    $"lastreaction = @lastreaction, " +
                    $"lasttick = @lasttick " +
                    $" WHERE userkey is @userkey";
                cmd.Parameters.AddWithValue("@gold", userGold._gold);
                cmd.Parameters.AddWithValue("@lastmessage", userGold._TS_LastMessage);
                cmd.Parameters.AddWithValue("@lastreaction", userGold._TS_LastReaction);
                cmd.Parameters.AddWithValue("@lasttick", userGold._TS_LastTick);
                cmd.Parameters.AddWithValue("@userkey", user.Key);
                cmd.ExecuteNonQuery();
            }
        }
        #endregion


        #region Twitch command methods
        private async void TWITCH_OnChatCommandReceived(object sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {


            TreasureSettings settings = new TreasureSettings();
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.Command.ChatMessage.Channel);
            switch (e.Command.CommandText.ToLower())
            {
                case "gpm":

                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        TwitchLib.Api.V5.Models.Users.Users channels = await Core.Twitch._api.V5.Users.GetUserByNameAsync(Core.Twitch._client.GetJoinedChannel(e.Command.ChatMessage.Channel).Channel);


                        if (e.Command.ArgumentsAsList.Count == 0)
                        {
                            settings = await Settings(bChan);
                            Core.Twitch._client.SendMessage(
                           e.Command.ChatMessage.Channel,
                           $"In this channel you get {settings._g_per_tick_twitch} gold for each {settings._cd_tick_twitch} seconds while stream is live."
                           );
                        }
                        if (e.Command.ArgumentsAsList.Count == 1)
                        {
                            settings = await Settings(bChan);
                            int i = settings._g_per_tick_twitch;
                            int.TryParse(e.Command.ArgumentsAsList[0], out i);
                            if (i == settings._g_per_tick_twitch) { return; }
                            if (i < 0 || i > 100) { return; }
                            settings._g_per_tick_twitch = i;
                            Core.Configs.UpdateConfig(bChan, PLUGINNAME, settings);
                            Core.Twitch._client.SendMessage(
                          e.Command.ChatMessage.Channel,
                          $"Now you get {settings._g_per_tick_twitch} gold for each {settings._cd_tick_twitch} seconds while stream is live."
                          );
                        }
                    }



                    break;
                case "tickinterval":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        TwitchLib.Api.V5.Models.Users.Users channels = await Core.Twitch._api.V5.Users.GetUserByNameAsync(Core.Twitch._client.GetJoinedChannel(e.Command.ChatMessage.Channel).Channel);
                        if (e.Command.ArgumentsAsList.Count == 1)
                        {
                            settings = await Settings(bChan);
                            int i = settings._cd_tick_twitch;
                            int.TryParse(e.Command.ArgumentsAsList[0], out i);
                            if (i == settings._cd_tick_twitch) { return; }
                            if (i < 10 || i > 300) { return; }
                            settings._cd_tick_twitch = i;
                            Core.Configs.UpdateConfig(bChan, PLUGINNAME, settings);
                            Core.Twitch._client.SendMessage(
                           e.Command.ChatMessage.Channel,
                           $"Now you get {settings._g_per_tick_twitch} gold for each {settings._cd_tick_twitch} seconds while stream is live."
                           );
                        }
                    }
                    break;
                case "gold":
                    UserEntry user = await Core.UserMan.GetUserByTwitchUserName(e.Command.ChatMessage.Username);
                    BotChannel chan = await Core.Channels.GetTwitchChannelByName(e.Command.ChatMessage.Channel);
                    int gold = await GetUserGold(user, chan);
                    TwitchGoldCMDBufferedAnswer(chan, $"{e.Command.ChatMessage.DisplayName} has {gold} gold.");
                    break;

            }
        }
        private void TwitchGoldCMDBufferedAnswer(BotChannel channel, string msg)
        {
            //TreasureSettings settings = await Settings(channel);
            if (!_goldResponseBuffer.ContainsKey(channel.TwitchChannelName))
            {
                _goldResponseBuffer[channel.TwitchChannelName] = new List<string>();
            }
            if (_goldResponseBuffer.Keys.Count == 1)
            {
                _goldResponseBufferAge = Core.CurrentTime;
            }
            if (!_goldResponseBuffer[channel.TwitchChannelName].Contains(msg))
            {
                _goldResponseBuffer[channel.TwitchChannelName].Add(msg);
            }
            //SaveSettings(PLUGINNAME, channel, settings);
        }
        private void EmptyGoldResponseBuffer(int seconds)
        {
            if (Core.CurrentTime < _goldResponseBufferAge + 5) { return; }

            foreach (string key in _goldResponseBuffer.Keys)
            {
                string msg = string.Empty;
                foreach (string message in _goldResponseBuffer[key])
                {
                    msg += message + " ";
                }
                Core.Twitch._client.SendMessage(key, msg);
            }
            _goldResponseBuffer = new Dictionary<string, List<string>>();
        }
        #endregion
        #region Discord Command Methods
        #region Interface default discord command methods
        public async Task SetDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {

            TreasureSettings settings = await Settings(bChan);
            settings._defaultDiscordChannel = discordChannelID;
            await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync($"This is now the active channel for the {PLUGINNAME} plugin.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);

        }
        public async Task ClearDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            TreasureSettings settings = await Settings(bChan);
            settings._defaultDiscordChannel = 0;
            await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync($"The active channel for the {PLUGINNAME} plugin is resetted. All channels now valid.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);
        }
        #endregion
        private async Task OnGuildAvailable(SocketGuild arg)
        {
            TreasureSettings settings = await Settings(await Core.Channels.GetDiscordGuildbyID(arg.Id));
        }
        public async Task DiscordGoldCMD(ICommandContext Context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            UserEntry user = await Core.UserMan.GetUserByDiscordID(Context.User.Id);
            if(bChan == null || user == null) { return; }
            TreasureUserDefinition userGold = await UserData(user, bChan);
            if (userGold != null)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Username} has {userGold._gold} gold.");
            }
        }
        public async Task GoldPerIntervalCMD(ICommandContext Context)
        {
            TreasureSettings settings = await Settings(await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id));

            await Context.Channel.SendMessageAsync($"On this Discord online users get {settings._g_per_tick_discord} per {settings._cd_tick_discord} seconds.");
        }
        public async Task SetGoldPerIntervalCMD(ICommandContext Context, string arg)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            TreasureSettings settings = await Settings(bChan);

            int i = settings._g_per_tick_discord;
            int.TryParse(arg, out i);

            if (i != settings._g_per_tick_discord && i >= 0 && i <= 100)
            {
                settings._g_per_tick_discord = i;
                Core.Configs.UpdateConfig(bChan, PLUGINNAME, settings);
            }

            await Context.Channel.SendMessageAsync($"On this Discord online users get {settings._g_per_tick_discord} per {settings._cd_tick_discord} seconds.");
        }
        public async Task SetTickIntervalCMD(ICommandContext Context, string arg)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            TreasureSettings settings = await Settings(bChan);
            int i = settings._g_per_tick_discord;
            int.TryParse(arg, out i);
            if (i != settings._g_per_tick_discord && i >= 10 && i <= 300)
            {
                settings._cd_tick_discord = i;
                Core.Configs.UpdateConfig(bChan, PLUGINNAME, settings);
            }
            await Context.Channel.SendMessageAsync($"On this Discord online users get {settings._g_per_tick_discord} per {settings._cd_tick_discord} seconds.");
        }
        #endregion
        public async void GoldTick()
        {
            IReadOnlyList<JoinedChannel> joinedchannels = Core.Twitch._client.JoinedChannels;
            int channelCount = joinedchannels.Count;
            for (int i = 0; i < channelCount; i++)
            {
                string chanName = joinedchannels[i].Channel;
                BotChannel bChan = await Core.Channels.GetTwitchChannelByName(chanName);
                if (bChan == null) { continue; }
                if (bChan.isLive)
                {
                    // Give gold to all in chat but ignored ones
                    TreasureSettings cfg = new TreasureSettings();
                    cfg = await Settings(bChan);
                    if (cfg._g_per_tick_twitch > 0)
                    {
                        if (Core.CurrentTime > cfg._ts_last_tick_twitch + cfg._cd_tick_twitch)
                        {
                            cfg._ts_last_tick_twitch = Core.CurrentTime;
                            Core.Configs.UpdateConfig(bChan, PLUGINNAME, cfg);

                            foreach (string userName in Core.Twitch._twitchUsers.GetUsersInChannel(chanName))
                            {
                                UserEntry user = await Core.UserMan.GetUserByTwitchUserName(userName);
                                if (user == null)
                                {
                                    return;
                                }
                                await GiveGold(bChan, user, cfg._g_per_tick_twitch);
                            }

                        }
                    }
                }

            }


            foreach (SocketGuild guild in Core.Discord.Guilds)
            {
                //TreasureSettings settings = Program._configs._configs[guild.Id.ToString()][PLUGINNAME] as TreasureSettings;

                TreasureSettings settings = new TreasureSettings();
                //BotChannel bChan = Core.Channels._botChannels.GetDiscordGuildbyID(guild.Id);
                BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(guild.Id);
                settings = await Settings(bChan);
                if (settings == null || settings._g_per_tick_discord < 1) { continue; }
                if (Core.CurrentTime < settings._ts_last_tick_discord + settings._cd_tick_discord) { continue; }

                settings._ts_last_tick_discord = Core.CurrentTime;
                //Core.Configs.UpdateConfig(bChan, PLUGINNAME, settings);

                int online = guild.Users.Where((x) => x.Status == UserStatus.Online).Count();
                //Core.LOG(new LogMessage(LogSeverity.Info, PLUGINNAME, $"{guild.Name} User count: {guild.Users.Count()} Online: {online}"));
                foreach (SocketGuildUser gUser in guild.Users)
                {
                    if (gUser.Status == UserStatus.Online)
                    {
                        UserEntry user = await Core.UserMan.GetUserByDiscordID(gUser.Id);
                        if(user != null)
                        {
                            await  GiveGold(bChan, user, settings._g_per_tick_discord);
                        }
                    }
                }
            }
            

        }// END of GoldTick
        #region User related
        public async Task GiveGold(BotChannel bChan, UserEntry user, int value)
        {
            if(value <= 0) { return; }
            if (user == null)
            {
                return;
            }
            TreasureUserDefinition userGold = await UserData(user, bChan);
            userGold._gold += value;
            SaveUserGold(bChan, user, userGold);
        }

        public async Task<bool> MakePayment(UserEntry user, BotChannel bChan, int value)
        {
            int wallet = await GetUserGold(user, bChan);
            if (wallet >= value)
            {
                TakeGold(user, bChan, value);
                return true;
            }
            return false;

            /*// First determine what botchan it is. twitch or discord or both
            if (bChan.isLinked || bChan.isTwitch == false)
            {
                // Discord/Linked bChan so remove from discord/linked user
                if (user.linked || user._discordID != 0)
                {
                    int wallet = await GetUserGold(user, bChan);
                    if (wallet >= value)
                    {
                        TakeGold(user, bChan, value);
                        return true;
                    }
                    return false;
                }
                else if(user._twitchUID != string.Empty)
                {
                    int wallet = await GetUserGold(user, bChan);
                    if (wallet >= value)
                    {
                        TakeGold(user, bChan, value);
                        return true;
                    }
                    return false;
                }
            }
            else if (bChan.isTwitch == true)
            {
                // Unlinked twitch channel so we remove gold from user twitchID
                int wallet = await GetUserGold(user, bChan);
                if (wallet >= value)
                {
                    TakeGold(user, bChan, value);
                    return true;
                }
                return false;
            } else {
                return false;
            }
            return false;*/
        }

        public async void TakeGold(UserEntry user, BotChannel bChan, int value)
        {
            // make sure we are giving gold
            if (value <= 0)
            {
                return;
            }
            TreasureUserDefinition userGold = await UserData(user, bChan);
            userGold._gold -= value;
            SaveUserGold(bChan, user, userGold);
        }

        public async Task<TreasureUserDefinition> GetUserTreasure(UserEntry user, BotChannel bChan)
        {
            return await UserData(user, bChan);
        }

        public async Task<int> GetUserGold(UserEntry user, BotChannel bChan)
        {
            TreasureUserDefinition userGold = await UserData(user, bChan);
            return userGold._gold;
        }
        #endregion
       
        #region Important base methods that can't be inherited
        
        #endregion
        #region Interface base methods
        public void OnSecondTick(int seconds)
        {
            GoldTick();
            EmptyGoldResponseBuffer(seconds);
        }
        public void OnMinuteTick(int minutes)
        {
            throw new NotImplementedException();
        }
        public async void OnBotChannelEntryMerge(BotChannel discordGuild, BotChannel twitchChannel)
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, "TreasureService", $"Channel Merge!!! Warning this is not implemented yet!!"));
            // Maybe it is easier to do a user specific check on a linked channel instead of iterating over all users.
        }
        public async void OnUserEntryMerge(UserEntry discordUser, UserEntry twitchUser)
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, "TreasureService", $"Profile Merge!!! {discordUser._username} <> {twitchUser._twitchUsername}"));
            foreach (BotChannel bChan in await Core.Channels.GetChannels())
            {
                string table = $"{PLUGINNAME}_Users_{bChan.Key}";
                if (!TableExists(table))
                {
                    continue;
                }
                TreasureUserDefinition discordTreasure = new TreasureUserDefinition(Core.CurrentTime);
                TreasureUserDefinition twitchTreasure = new TreasureUserDefinition(Core.CurrentTime);

                if (await UserRowExists(table, discordUser.Key))
                {
                    discordTreasure = await UserData(discordUser, bChan);
                }
                if (await UserRowExists(table, twitchUser.Key))
                {
                    twitchTreasure = await UserData(twitchUser, bChan);
                }

                if(twitchTreasure._gold > discordTreasure._gold)
                {
                    discordTreasure._gold = twitchTreasure._gold;
                }

                UserRowDelete(table, twitchUser.Key);
                SaveUserGold(bChan, discordUser, discordTreasure);

         

          
            }

        }
        
        #endregion
    }
}
