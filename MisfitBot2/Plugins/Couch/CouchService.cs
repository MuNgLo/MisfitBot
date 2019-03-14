using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using MisfitBot2.Plugins.PluginTemplate;
using MisfitBot2.Plugins.Couch;
using System.Data.SQLite;
using System.Data;
using TwitchLib.Api.V5.Models.Users;					 							

namespace MisfitBot2.Services
{
    class CouchService : ServiceBase, IService
    {
        public readonly string PLUGINNAME = "Couch";
		public readonly string PLUGINSTATS = "Couch_Stats";								   
        private Random rng = new Random();
        private List<string> _success = new List<string>();
        private List<string> _fail = new List<string>();
        private List<string> _incident = new List<string>();

        // CONSTRUCTOR
        public CouchService()
        {
            Core.Twitch._client.OnChatCommandReceived += TwitchOnChatCommandReceived;
			Core.Twitch._client.OnUserJoined += TwitchInUserJoined;								   
            Core.OnBotChannelGoesLive += OnChannelGoesLive;
            Core.OnBotChannelGoesOffline += OnChannelGoesOffline;
            // Successes
            _success.Add(" takes a seat on the couch.");
            _success.Add(" backflips onto the couch.");
            _success.Add(" manages to escape the restrains and takes a seat on the couch.");
            _success.Add(" suddenly materializes on the couch with a hint of a smirk.");
            _success.Add(" claws their way up from the void between the cushions.");
            _success.Add(" does an impressive herolanding then proceeds to stumble to the couch with intence knee pain.");
            _success.Add(" accepts their fate as a decoration on the couch.");
            _success.Add(" stridently claims their seat on the couch and act very smug about it.");

            // Fails
            _fail.Add(" is left standing.");
            _fail.Add(" rolls into fetal position as they don't fit on the couch.");
            _fail.Add(" creates a tsunami with their tears of despair.");
            _fail.Add(" hair catches fire from rage and others reach for the marshmallows.");
            _fail.Add(" is carried away by a flock of chairs to the land of standing space.");
            _fail.Add(" lacks the basic understanding of how to couch so they end up on the table.");
            _fail.Add(" storms in with a cavelry, but misses the couch.");
            _fail.Add(" eagerly runs towards the couch but trips and slides under it only to come out on the other side covered in dustbunnies.");

            // Incidents
            _incident.Add(" got cought suckling a cushion in the couch and had to leave their spot.");
            _incident.Add(" by pure chance ends up quantum entangling with the couch and disappears back into the void.");
            _incident.Add(" gets bumped out of the couch as a new victim takes their seat.");
            _incident.Add(" becomes the victim of EjectorZeat 3000™. Who is playing with the buttons?");
            _incident.Add(" leaves the couch mumbling something about bathroom just as a distict smell envelops the whole couch.");
            // Database checks
            if (!StatsTableExists()) { StatsTableCreate(PLUGINSTATS); }
        }


        private string GetRNGSuccess()
        {
            return _success[rng.Next(_success.Count)];
        }
        private string GetRNGFail()
        {
            return _fail[rng.Next(_fail.Count)];
        }
        private string GetRNGIncident()
        {
            return _incident[rng.Next(_incident.Count)];
        }
        private string GetRNGSitter(BotChannel bChan, CouchSettings settings)
        {
            int i = rng.Next(settings._couches[bChan.Key].TwitchUsernames.Count);
            if (i < settings._couches[bChan.Key].TwitchUsernames.Count && i >= 0)
            {
                return settings._couches[bChan.Key].TwitchUsernames[i];
            }
            return null;
        }


        private async void OnChannelGoesOffline(BotChannel bChan)
        {
            CouchSettings settings = await Settings(bChan);
            settings.lastOfflineEvent = Core.CurrentTime;
            SaveBaseSettings(PLUGINNAME, bChan, settings);
        }
        private async void OnChannelGoesLive(BotChannel bChan, int delay)
        {
            CouchSettings settings = await Settings(bChan); 
            if (!settings._active)
            {
                return;
            }
            if (!settings._couches.ContainsKey(bChan.Key))
            {
                settings._couches[bChan.Key] = new CouchEntry();
            }

            if(Core.CurrentTime > settings.lastOfflineEvent + 180)
            {
                ResetCouch(bChan, settings);
            }
            else
            {
                await Core.LOG(new LogMessage(LogSeverity.Info, PLUGINNAME, $"Channel {bChan.TwitchChannelName} went live. Using existing counch."));
                await SayOnDiscordAdmin(bChan, $"Channel {bChan.TwitchChannelName} went live. Using existing counch.");
            }
        }

        private bool RollIncident(int chance = 0)
        {
            return rng.Next(0, 100) + chance >= 95;
        }

        private async void ResetCouch(BotChannel bChan, CouchSettings settings)
        { 
            await Core.LOG(new LogMessage(LogSeverity.Info, PLUGINNAME, "Live event captured. Opening couch!"));
            if (!settings._couches.ContainsKey(bChan.Key))
            {
                settings._couches[bChan.Key] = new CouchEntry();
            }
            settings._couches[bChan.Key].couchOpen = true;
            settings._couches[bChan.Key].lastActivationTime = Core.CurrentTime;
            settings._couches[bChan.Key].TwitchUsernames = new List<string>();
            settings.failCount = 0;
            SaveBaseSettings(PLUGINNAME, bChan, settings);
            Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"Couch is now open. Take a {Core._commandCharacter}seat.");
        }

        #region Twitch methods
        private async void TwitchOnChatCommandReceived(object sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.Command.ChatMessage.Channel);
            if (bChan == null) { return; }
            CouchSettings settings = await Settings(bChan);
            if (!settings._couches.ContainsKey(bChan.Key))
            {
                settings._couches[bChan.Key] = new CouchEntry();
            }
            switch (e.Command.CommandText.ToLower())
            {
                case "couch":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        if (e.Command.ArgumentsAsList.Count == 1)
                        {
                            if (e.Command.ArgumentsAsList[0].ToLower() == "on")
                            {
                                settings._active = true;
                            }
                            if (e.Command.ArgumentsAsList[0].ToLower() == "off")
                            {
                                settings._active = false;
                            }
                        }
                        if (e.Command.ArgumentsAsList.Count == 2)
                        {
                            if (e.Command.ArgumentsAsList[0].ToLower() == "seats")
                            {
                                int seats = settings.couchsize;
                                int.TryParse(e.Command.ArgumentsAsList[1], out seats);
                                if(seats > 0 && seats <= 20 && seats != settings.couchsize)
                                {
                                    settings.couchsize = seats;
                                }
                            }
                        }
                        if (settings._active)
                        {
                            Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                $"Couch plugin is active in this channel. {settings.couchsize} seats."
                                );
                        }
                        else
                        {
                            Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                $"Couch plugin is inactive in this channel. {settings.couchsize} seats."
                                );
                        }
                        SaveBaseSettings(PLUGINNAME, bChan, settings);
                    }
                    break;
                case "seat":
                    if (!settings._couches[bChan.Key].couchOpen || !settings._active) { return; }
                    if(Core.CurrentTime - settings._couches[bChan.Key].lastActivationTime > 3600 && settings.failCount < 3)
                    {
                        Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                            $"{e.Command.ChatMessage.DisplayName} is late to get a seat in the couch. It isn't full but the people in it have spread out and refuse to move."
                            );
                        settings.failCount++;
                        SaveBaseSettings(PLUGINNAME, bChan, settings);
                        return;
                    }
                    if (Core.CurrentTime - settings._couches[bChan.Key].lastActivationTime > 600 && settings.failCount >= 3)
                    {
                        return;
                    }
                    if (settings._couches[bChan.Key].TwitchUsernames.Contains(e.Command.ChatMessage.DisplayName)) { return; }

                    if (settings._couches[bChan.Key].TwitchUsernames.Count < settings.couchsize)
                    {
                        if (RollIncident())
                        {
                            string mark = GetRNGSitter(bChan, settings);
                            if (mark != null)
                            {
                                UserEntry failuser = await Core.UserMan.GetUserByTwitchUserName(e.Command.ChatMessage.Username);
                                if (failuser != null)
                                {
                                    if (!await UserStatsExists(failuser.Key))
                                    {
                                        UserStatsCreate(failuser.Key);
                                    }
                                    CouchUserStats failUserStats = await UserStatsRead(failuser.Key);
                                    failUserStats.CountSeated++;
                                    UserStatsSave(failUserStats);
                                }
                                Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                    $"{mark} {GetRNGIncident()}"
                                    );
                                settings._couches[bChan.Key].TwitchUsernames.RemoveAll(p => p == mark);
                                SaveBaseSettings(PLUGINNAME, bChan, settings);
                            }
                        }
                        UserEntry user = await Core.UserMan.GetUserByTwitchUserName(e.Command.ChatMessage.Username);
                        if (user != null)
                        {
                            if (!await UserStatsExists(user.Key))
                            {
                                UserStatsCreate(user.Key);
                            }
                            CouchUserStats userStats = await UserStatsRead(user.Key);
                            userStats.CountSeated++;
                            UserStatsSave(userStats);
                        }
                        settings._couches[bChan.Key].TwitchUsernames.Add(e.Command.ChatMessage.DisplayName);
                        Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                            $"{e.Command.ChatMessage.DisplayName} {GetRNGSuccess()}"
                            );
                        SaveBaseSettings(PLUGINNAME, bChan, settings);

                    }
                    else
                    {
                        Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                            $"{e.Command.ChatMessage.DisplayName} {GetRNGFail()}"
                            );
                        settings.failCount++;
                        SaveBaseSettings(PLUGINNAME, bChan, settings);
                    }
                    
                    break;
                case "opencouch":
                    if(!settings._active) { return; }
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        ResetCouch(bChan, settings);
                    }
                    break;
            }
        }
		private async void TwitchInUserJoined(object sender, TwitchLib.Client.Events.OnUserJoinedArgs e)
        {
            UserEntry user = await Core.UserMan.GetUserByTwitchUserName(e.Username);
            if(user != null)
            {
                CouchUserStats uStats = await GetUserCouchStats(user.Key);
                if(uStats.CountSeated >= 10)
                {
                    Core.Twitch._client.SendMessage(e.Channel,
                            $"Welcome back {user._twitchDisplayname}. You truly are a proper couch potato. BloodTrail"
                            );
                }
            }
        }
        #endregion

        #region Database stuff
        public bool StatsTableExists()
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = "SELECT COUNT(*) AS QtRecords FROM sqlite_master WHERE type = 'table' AND name = @name";
                cmd.Parameters.AddWithValue("@name", PLUGINSTATS);
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
        private async Task<CouchUserStats> GetUserCouchStats(string uKey)
        {
            if (!await UserStatsExists(uKey))
                {
                    UserStatsCreate(uKey);
                }
                return await UserStatsRead(uKey);
        }
        private void StatsTableCreate(string tablename)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"CREATE TABLE {tablename} (" +
                    $"UserKey VACHAR(30)," +
                    $"CountSeated INTEGER, " +
                    $"CountBooted INTEGER " +
                    $")";
                cmd.ExecuteNonQuery();
            }
        }
        private async Task<CouchUserStats> UserStatsRead(string uKey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINSTATS} WHERE UserKey IS @uKey";
                cmd.Parameters.AddWithValue("@uKey", uKey);
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
                CouchUserStats user = new CouchUserStats(result.GetString(0), result.GetInt32(1), result.GetInt32(2));
                return user;
            }
        }
        private async void UserStatsCreate(string uKey)
        {
            CouchUserStats userStats = new CouchUserStats(uKey, 0, 0);
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"INSERT INTO {PLUGINSTATS} VALUES (" +
                    $"@UserKey, " +
                    $"@CountSeated, " +
                    $"@CountBooted " +
                    $")";
                cmd.Parameters.AddWithValue("@UserKey", userStats.UserKey);
                cmd.Parameters.AddWithValue("@CountSeated", userStats.CountSeated);
                cmd.Parameters.AddWithValue("@CountBooted", userStats.CountBooted);
                cmd.ExecuteNonQuery();
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, $"Created new entry for UserKey {userStats.UserKey}"));
            }
        }
        public async void UserStatsSave(CouchUserStats userStats)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;

                cmd.CommandText = $"UPDATE {PLUGINSTATS} SET " +
                    $"CountSeated = @CountSeated, " +
                    $"CountBooted = @CountBooted " +
                    $" WHERE UserKey is @UserKey";
                cmd.Parameters.AddWithValue("@UserKey", userStats.UserKey);
                cmd.Parameters.AddWithValue("@CountSeated", userStats.CountSeated);
                cmd.Parameters.AddWithValue("@CountBooted", userStats.CountBooted);
                cmd.ExecuteNonQuery();
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, "Saved UserStats in DB."));
            }
        }
        private async Task<bool> UserStatsExists(string uKey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINSTATS} WHERE UserKey IS @uKey";
                cmd.Parameters.AddWithValue("@uKey", uKey);

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
        #endregion	  
        #region Interface default discord command methods
        public async Task SetDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            CouchSettings settings = await Settings(bChan);
            settings._defaultDiscordChannel = discordChannelID;
            await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync(
                "This is now the active channel for the Couch plugin.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);
        }
        public async Task ClearDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            CouchSettings settings = await Settings(bChan);
            settings._defaultDiscordChannel = 0;
            await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync(
                "The active channel for the Couch plugin is resetted. All channels now valid.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);
        }
        #endregion
        #region Important base methods that can't be inherited
        private async Task<CouchSettings> Settings(BotChannel bChan)
        {
            CouchSettings settings = new CouchSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as CouchSettings;
        }
        #endregion
        #region Interface base methods
        public void OnSecondTick(int seconds)
        {
            throw new NotImplementedException();
        }
        public void OnMinuteTick(int minutes)
        {
            throw new NotImplementedException();
        }
        public void OnBotChannelEntryMerge(BotChannel discordGuild, BotChannel twitchChannel)
        {
            throw new NotImplementedException();
        }
        public void OnUserEntryMerge(UserEntry discordUser, UserEntry twitchUser)
        {
            throw new NotImplementedException();
        }
        public void NewUserValuesEntry(ulong userID, ulong guildID)
        {
            throw new NotImplementedException();
        }
        public void NewUserValuesEntry(string twitchUserID, string twitchChannelID)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
