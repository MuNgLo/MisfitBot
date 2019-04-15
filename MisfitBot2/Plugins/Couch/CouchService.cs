﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using MisfitBot2.Plugins.PluginTemplate;
using MisfitBot2.Plugins.Couch;
using System.Data.SQLite;
using System.Data;
using TwitchLib.Api.V5.Models.Users;
using MisfitBot2.Components;
using Discord.Commands;

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
        private DatabaseStrings dbStrings;
        // CONSTRUCTOR
        public CouchService()
        {
            
            Core.Twitch._client.OnChatCommandReceived += TwitchOnChatCommandReceived;
			Core.Twitch._client.OnUserJoined += TwitchInUserJoined;								   
            Core.OnBotChannelGoesLive += OnChannelGoesLive;
            Core.OnBotChannelGoesOffline += OnChannelGoesOffline;
            ///Core.OnUserEntryMerge += OnUserEntryMerge; FIX THIS NEXT
            Core.Channels.OnBotChannelMerge += OnBotChannelEntryMerge;
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
            // DB Strings setup
            dbStrings = new DatabaseStrings(PLUGINNAME);
        }



        /*private string GetRNGFail()
        {
            return _fail[rng.Next(_fail.Count)];
        }
        private string GetRNGIncident()
        {
            return _incident[rng.Next(_incident.Count)];
        }*/

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
            if (!dbStrings.TableInit(bChan))
            {
                DBStringsFirstSetup(bChan);
            }
            UserEntry user = await Core.UserMan.GetUserByTwitchID(e.Command.ChatMessage.UserId);
            if (user == null) { return; }
            CouchSettings settings = await Settings(bChan);
            if (!settings._couches.ContainsKey(bChan.Key))
            {
                settings._couches[bChan.Key] = new CouchEntry();
            }
            switch (e.Command.CommandText.ToLower())
            {
                case "couch":
                    // Broadcaster and Moderator commands
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        if(e.Command.ArgumentsAsList.Count == 0)
                        {
                            Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                $"Couch is active. {settings.couchsize} seats. Greetlimit is {settings.potatoGreeting}."
                                );
                            return;
                        }
                        switch (e.Command.ArgumentsAsList[0].ToLower())
                        {
                            case "on":
                                settings._active = true;
                                SaveBaseSettings(PLUGINNAME, bChan, settings);
                                Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                $"Couch is active. {settings.couchsize} seats. Greetlimit is {settings.potatoGreeting}."
                                );
                                break;
                            case "off":
                                settings._active = false;
                                SaveBaseSettings(PLUGINNAME, bChan, settings);
                                Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                 $"Couch is active. {settings.couchsize} seats. Greetlimit is {settings.potatoGreeting}."
                                 );
                                break;
                            case "size":
                                if (e.Command.ArgumentsAsList.Count == 2)
                                {
                                    int seats = settings.couchsize;
                                    int.TryParse(e.Command.ArgumentsAsList[1], out seats);
                                    if (seats > 0 && seats <= 40 && seats != settings.couchsize)
                                    {
                                        settings.couchsize = seats;
                                        Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                        $"Couch now has {settings.couchsize} seats."
                                        );
                                        await SayOnDiscordAdmin(bChan, $"{e.Command.ChatMessage.DisplayName} changed the Couch size to {settings.couchsize}.");
                                        SaveBaseSettings(PLUGINNAME, bChan, settings);
                                    }
                                }
                                break;
                            case "greet":
                                if (e.Command.ArgumentsAsList.Count == 2)
                                {
                                    int greet = settings.potatoGreeting;
                                    int.TryParse(e.Command.ArgumentsAsList[1], out greet);
                                    if (greet > 0 && greet <= 40 && greet != settings.potatoGreeting)
                                    {
                                        settings.potatoGreeting = greet;
                                        Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                        $"Couch greeting limit is now {settings.potatoGreeting}."
                                        );
                                        await SayOnDiscordAdmin(bChan, $"{e.Command.ChatMessage.DisplayName} changed the Couch Greetlimit setting to {settings.potatoGreeting}.");
                                        SaveBaseSettings(PLUGINNAME, bChan, settings);
                                    }
                                }
                                break;
                            case "open":
                                if (!settings._active) { return; }
                                if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                                {
                                    ResetCouch(bChan, settings);
                                }
                                break;
                        }
                    }
                    break;
                    // User Commands
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
                        if (settings._couches[bChan.Key].TwitchUsernames.Count != 0)
                        {
                            if (RollIncident())
                            {
                                string mark = GetRNGSitter(bChan, settings);
                                if (mark != null)
                                {
                                    UserEntry failuser = await Core.UserMan.GetUserByTwitchUserName(e.Command.ChatMessage.Username);
                                    if (failuser != null)
                                    {
                                        if (!await UserStatsExists(bChan.Key, failuser.Key))
                                        {
                                            UserStatsCreate(bChan.Key, failuser.Key);
                                        }
                                        CouchUserStats failUserStats = await UserStatsRead(bChan.Key, failuser.Key);
                                        failUserStats.CountSeated++;
                                        UserStatsSave(failUserStats);
                                    }
                                    Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                        $"{mark} {await dbStrings.GetRNGFromTopic(bChan, "INCIDENT")}"
                                        );
                                    settings._couches[bChan.Key].TwitchUsernames.RemoveAll(p => p == mark);
                                    SaveBaseSettings(PLUGINNAME, bChan, settings);
                                }
                            }
                        }
                        if (!await UserStatsExists(bChan.Key, user.Key))
                        {
                            UserStatsCreate(bChan.Key, user.Key);
                        }
                        CouchUserStats userStats = await UserStatsRead(bChan.Key, user.Key);
                        userStats.CountSeated++;
                        UserStatsSave(userStats);
                        settings._couches[bChan.Key].TwitchUsernames.Add(e.Command.ChatMessage.DisplayName);
                        Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                            $"{e.Command.ChatMessage.DisplayName} {await dbStrings.GetRNGFromTopic(bChan, "SUCCESS")}"
                            );
                        SaveBaseSettings(PLUGINNAME, bChan, settings);

                    }
                    else
                    {
                        Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                            $"{e.Command.ChatMessage.DisplayName} {await dbStrings.GetRNGFromTopic(bChan, "FAIL")}"
                            );
                        settings.failCount++;
                        SaveBaseSettings(PLUGINNAME, bChan, settings);
                    }
                    break;
                case "seats":
                    CouchUserStats cStats = await GetUserCouchStats(bChan.Key, user.Key);
                    Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                $"{user._twitchDisplayname}, you have sat in couch {cStats.CountSeated} times. {cStats.CountBooted} times you fell off."
                                );
                    break;
            }
        }

        private void DBStringsFirstSetup(BotChannel bChan)
        {
            foreach (string line in _success)
            {
                dbStrings.SaveNewLine(bChan, "SUCCESS", line);
            }
            foreach (string line in _fail)
            {
                dbStrings.SaveNewLine(bChan, "FAIL", line);
            }
            foreach (string line in _incident)
            {
                dbStrings.SaveNewLine(bChan, "INCIDENT", line);
            }
            dbStrings.SaveNewLine(bChan, "GREET", "Welcome back [USER]. You truly are a proper couch potato. BloodTrail");
        }

        private async void TwitchInUserJoined(object sender, TwitchLib.Client.Events.OnUserJoinedArgs e)
        {
            UserEntry user = await Core.UserMan.GetUserByTwitchUserName(e.Username);
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.Channel);
            CouchSettings settings = await Settings(bChan);
            if (!settings._couches.ContainsKey(bChan.Key))
            {
                settings._couches[bChan.Key] = new CouchEntry();
            }
            if (user != null && bChan != null)
            {
                CouchUserStats uStats = await GetUserCouchStats(bChan.Key, user.Key);
                if(uStats.CountSeated >= settings.potatoGreeting)
                {
                    Core.Twitch._client.SendMessage(e.Channel,
                            $"Welcome back {user._twitchDisplayname}. You truly are a proper couch potato. BloodTrail"
                            );
                }
            }
        }
        #endregion

        #region Discord command methods
        public async Task DiscordCommand(ICommandContext context, List<string> arguments)
        {
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info,
                PLUGINNAME,
                $"{context.User.Username} used command \"couch\" in {context.Channel.Name}."
                ));
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            if (!dbStrings.TableInit(bChan))
            {
                DBStringsFirstSetup(bChan);
            }
            CouchSettings settings = await Settings(bChan);
            switch (arguments[0].ToLower())
            {
                case "addsuccess":
                    if (arguments.Count >= 2)
                    {
                        await AddLine(bChan, "SUCCESS", arguments);
                    }
                    break;
                case "addfail":
                    if (arguments.Count >= 2)
                    {
                        await AddLine(bChan, "FAIL", arguments);
                    }
                    break;
                case "addincident":
                    if (arguments.Count >= 2)
                    {
                        await AddLine(bChan, "INCIDENT", arguments);
                    }
                    break;
                case "rngsuccess":
                    await SayOnDiscordAdmin(bChan, dbStrings.GetRandomLine(bChan, "SUCCESS"));
                    break;
                case "rngfail":
                    await SayOnDiscordAdmin(bChan, dbStrings.GetRandomLine(bChan, "FAIL"));
                    break;
                case "rngincident":
                    await SayOnDiscordAdmin(bChan, dbStrings.GetRandomLine(bChan, "INCIDENT"));
                    break;
                case "list":
                    await ListLinesFromDB(bChan, 0);
                    break;
            }
        }
        #endregion

        #region Database stuff
        #region DB Strings stuff 
        private async Task ListLinesFromDB(BotChannel bChan, int page)
        {
            // LINES IN USE
            string inuseText = "These are lines stored in the database that the Couch plugin will use based on topic if they are marked as inuse." + Environment.NewLine + Environment.NewLine;
            List<string> inuseLines = await dbStrings.GetTenInUse(bChan, "SUCCESS", page);
            if (inuseLines.Count == 0)
            {
                inuseText += "There is no lines inuse. This is probably a bad thing.";
            }
            else
            {
                foreach(string line in inuseLines)
                {
                    inuseText += line +Environment.NewLine;
                }
            }
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle("Currently stored lines...");
            builder.WithDescription(inuseText);
            builder.WithColor(Color.Purple);


            EmbedFieldBuilder field = new EmbedFieldBuilder();
            field.Name = $"qweqweqweqweqweq";
            field.Value = "kjahsdkjhaskdjh";
            builder.AddField($"In use Success strings. Page {page}", field.Build());
            
            // LINES NOT IN USE
            //EmbedFieldBuilder field2 = new EmbedFieldBuilder();
            //field2.Name = help;
            //field2.Value = 100;
            //builder.AddField("How to participate", field2.Build());

            Embed obj = builder.Build();
            await SayEmbedOnDiscordAdmin(bChan, obj);
        }

        private async Task AddLine(BotChannel bChan, string topic, List<string> arguments)
        {
            arguments.RemoveAt(0);
            string line = string.Empty;
            foreach(string part in arguments) { line += " " + part; }
            line.Trim();
            dbStrings.SaveNewLine(bChan, topic, line);
            await SayOnDiscordAdmin(bChan, $"Added one more \"{topic}\" line for Couch plugin.");
        }
        #endregion
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
        private async Task<CouchUserStats> GetUserCouchStats(string bKey, string uKey)
        {
            if (!await UserStatsExists(bKey, uKey))
                {
                    UserStatsCreate(bKey, uKey);
                }
                return await UserStatsRead(bKey, uKey);
        }
        private void StatsTableCreate(string tablename)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"CREATE TABLE {tablename} (" +
                    $"BotChannelKey VACHAR(30)," +
                    $"UserKey VACHAR(30)," +
                    $"CountSeated INTEGER, " +
                    $"CountBooted INTEGER " +
                    $")";
                cmd.ExecuteNonQuery();
            }
        }
        private async Task<CouchUserStats> UserStatsRead(string bKey, string uKey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINSTATS} WHERE UserKey IS @uKey AND BotChannelKey IS @bKey";
                cmd.Parameters.AddWithValue("@uKey", uKey);
                cmd.Parameters.AddWithValue("@bKey", bKey);
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
                CouchUserStats user = new CouchUserStats(result.GetString(0), result.GetString(1), result.GetInt32(2), result.GetInt32(3));
                return user;
            }
        }
        private void UserStatsCreate(string bKey, string uKey)
        {
            CouchUserStats userStats = new CouchUserStats(bKey, uKey, 0, 0);
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"INSERT INTO {PLUGINSTATS} VALUES (" +
                    $"@BotChannelKey, " +
                    $"@UserKey, " +
                    $"@CountSeated, " +
                    $"@CountBooted " +
                    $")";
                cmd.Parameters.AddWithValue("@BotChannelKey", bKey);
                cmd.Parameters.AddWithValue("@UserKey", userStats.UserKey);
                cmd.Parameters.AddWithValue("@CountSeated", userStats.CountSeated);
                cmd.Parameters.AddWithValue("@CountBooted", userStats.CountBooted);
                cmd.ExecuteNonQuery();
            }
        }
        public async void UserStatsSave(CouchUserStats userStats)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;

                cmd.CommandText = $"UPDATE {PLUGINSTATS} SET " +
                    $"BotChannelKey = @BotChannelKey, " +
                    $"CountSeated = @CountSeated, " +
                    $"CountBooted = @CountBooted " +
                    $" WHERE BotChannelKey is @BotChannelKey AND UserKey is @UserKey";
                cmd.Parameters.AddWithValue("@BotChannelKey", userStats.BotChannelKey);
                cmd.Parameters.AddWithValue("@UserKey", userStats.UserKey);
                cmd.Parameters.AddWithValue("@CountSeated", userStats.CountSeated);
                cmd.Parameters.AddWithValue("@CountBooted", userStats.CountBooted);
                cmd.ExecuteNonQuery();
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, "Saved UserStats in DB."));
            }
        }
        private async Task<bool> UserStatsExists(string bKey, string uKey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINSTATS} WHERE UserKey IS @uKey AND BotChannelKey IS @bKey";
                cmd.Parameters.AddWithValue("@uKey", uKey);
                cmd.Parameters.AddWithValue("@bKey", bKey);

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
        private async Task ReplaceChannelKey(string newKey, string oldKey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {PLUGINSTATS} WHERE BotChannelKey IS @oldKey";
                cmd.Parameters.AddWithValue("@oldKey", oldKey);
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
                    using (SQLiteCommand cmd2 = new SQLiteCommand())
                    {
                        cmd2.CommandType = CommandType.Text;
                        cmd2.Connection = Core.Data;

                        cmd2.CommandText = $"UPDATE {PLUGINSTATS} SET " +
                            $"BotChannelKey = @newKey, " +
                            $"CountSeated = @CountSeated, " +
                            $"CountBooted = @CountBooted " +
                            $" WHERE UserKey is @UserKey AND BotChannelKey is @oldKey";
                        cmd2.Parameters.AddWithValue("@oldKey", oldKey);
                        cmd2.Parameters.AddWithValue("@newKey", newKey);
                        cmd2.Parameters.AddWithValue("@UserKey", result.GetString(1));
                        cmd2.Parameters.AddWithValue("@CountSeated", result.GetInt32(2));
                        cmd2.Parameters.AddWithValue("@CountBooted", result.GetInt32(3));
                        cmd2.ExecuteNonQuery();
                    }
                }
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, PLUGINNAME, "Channelmerge detected. DB updated."));
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
        public async void OnBotChannelEntryMerge(BotChannel discordGuild, BotChannel twitchChannel)
        {
            string keyToReplace = twitchChannel.Key;
            await ReplaceChannelKey(discordGuild.Key, twitchChannel.Key);
        }
        public void OnUserEntryMerge(UserEntry discordUser, UserEntry twitchUser)
        {
           // TODO make this fix db when a user merges
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
