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
            _success.Add("[USER] takes a seat on the couch.");
            _success.Add("[USER] backflips onto the couch.");
            _success.Add("[USER] manages to escape the restraints and takes a seat on the couch.");
            _success.Add("[USER] suddenly materializes on the couch with a hint of a smirk.");
            _success.Add("[USER] claws their way up from the void between the cushions.");
            _success.Add("[USER] does an impressive herolanding then proceeds to stumble to the couch with intence knee pain.");
            _success.Add("[USER] accepts their fate as a decoration on the couch.");
            _success.Add("[USER] stridently claims their seat on the couch and act very smug about it.");

            // Fails
            _fail.Add("[USER] is left standing.");
            _fail.Add("[USER] rolls into fetal position as they don't fit on the couch.");
            _fail.Add("[USER] creates a tsunami with their tears of despair.");
            _fail.Add("[USER] hair catches fire from rage and others reach for the marshmallows.");
            _fail.Add("[USER] is carried away by a flock of chairs to the land of standing space.");
            _fail.Add("[USER] lacks the basic understanding of how to couch so they end up on the table.");
            _fail.Add("[USER] storms in with a cavelry, but misses the couch.");
            _fail.Add("[USER] eagerly runs towards the couch but trips and slides under it only to come out on the other side covered in dustbunnies.");

            // Incidents
            _incident.Add("[USER] got cought suckling a cushion in the couch and had to leave their spot.");
            _incident.Add("[USER] by pure chance ends up quantum entangling with the couch and disappears back into the void.");
            _incident.Add("[USER] gets bumped out of the couch as a new victim takes their seat.");
            _incident.Add("[USER] becomes the victim of EjectorZeat 3000™. Who is playing with the buttons?");
            _incident.Add("[USER] leaves the couch mumbling something about bathroom just as a distict smell envelops the whole couch.");
            // Database checks
            if (!StatsTableExists()) { StatsTableCreate(PLUGINSTATS); }
            // DB Strings setup
            dbStrings = new DatabaseStrings(PLUGINNAME);
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

        #region Twitch methods
        private async void TwitchOnChatCommandReceived(object sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {

            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.Command.ChatMessage.Channel);
            if (bChan == null) { return; }
            if (!await dbStrings.TableInit(bChan))
            {
                await DBStringsFirstSetup(bChan);
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
                                 $"Couch is inactive. {settings.couchsize} seats. Greetlimit is {settings.potatoGreeting}."
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
                                        $"{dbStrings.GetRandomLine(bChan, "INCIDENT").Replace("[USER]", mark)}"
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
                            $"{ dbStrings.GetRandomLine(bChan, "SUCCESS").Replace("[USER]", user._twitchDisplayname)}"
                            );
                        SaveBaseSettings(PLUGINNAME, bChan, settings);

                    }
                    else
                    {
                        Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                            dbStrings.GetRandomLine(bChan, "FAIL").Replace("[USER]", user._twitchDisplayname)
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
        private async void TwitchInUserJoined(object sender, TwitchLib.Client.Events.OnUserJoinedArgs e)
        {
            if(TimerStuff.Uptime < 300)
            {
                return;
            }
            UserEntry user = await Core.UserMan.GetUserByTwitchUserName(e.Username);
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.Channel);
            CouchSettings settings = await Settings(bChan);
            if (!settings._couches.ContainsKey(bChan.Key))
            {
                settings._couches[bChan.Key] = new CouchEntry();
            }
            if (user != null && bChan != null && settings._active)
            {
                CouchUserStats uStats = await GetUserCouchStats(bChan.Key, user.Key);
                if(uStats.CountSeated >= settings.potatoGreeting)
                {
                    if (!await dbStrings.TableInit(bChan))
                    {
                        await DBStringsFirstSetup(bChan);
                    }
                    Core.Twitch._client.SendMessage(e.Channel, dbStrings.GetRandomLine(bChan, "GREET").Replace("[USER]", user._twitchDisplayname));
                }
            }
        }
        #endregion

        #region Discord command methods
        public async Task DiscordCommand(ICommandContext context)
        {
            if (context.User.IsBot) { return; }

            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            if (!await dbStrings.TableInit(bChan))
            {
                await DBStringsFirstSetup(bChan);
            }
            CouchSettings settings = await Settings(bChan);
            await SayOnDiscordAdmin(bChan, $"Couch is active. {settings.couchsize} seats. Greetlimit is {settings.potatoGreeting}.");
            // Ugly bit coming here
            string helptext = $"```fix{Environment.NewLine}" +
                $"Admin/Broadcaster commands{Environment.NewLine}{Environment.NewLine}{Core._commandCharacter}couch < Arguments >{Environment.NewLine}{Environment.NewLine}Arguments....{Environment.NewLine}< none > ->responds current settings{Environment.NewLine}" +
            $"open -> Manually resets and open the couch.{Environment.NewLine}on/off -> Turns plugin on or off for the channel.{Environment.NewLine}size # -> Sets the number of seats between 1 and 40.{Environment.NewLine}" +
            $"greet # -> Sets the number of seated needed in stats for a greeting when a user joins the twitch channel.{Environment.NewLine}{Environment.NewLine}Discord only arguments(make sure adminchannel is set in adminplugin){Environment.NewLine}" +
            $"addsuccess < text > Text being the line returned. Use [USER] in text where username should be.{Environment.NewLine}addfail < text >{Environment.NewLine}addgreet < text >{Environment.NewLine}addincident < text >{Environment.NewLine}" +
            $"list / list # -> Shows stored lines by page.{Environment.NewLine}use # -> Toggles the inuse flag for the line with given ID.{Environment.NewLine}delete # -> Deletes the line with the ID if inuse flag is false. As in not in use.{Environment.NewLine}" +
            $"{Environment.NewLine}{Environment.NewLine}User commands{Environment.NewLine}" +
            $"{Core._commandCharacter}seat -> When couch open it responds with success of fail message.{Environment.NewLine}" +
            $"{Core._commandCharacter}seats -> User stats rundown." +
            $"```";
            await SayOnDiscordAdmin(bChan, helptext);
            return;
        }
        public async Task DiscordCommand(ICommandContext context, List<string> arguments)
        {
            if (context.User.IsBot) { return; }
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info,
                PLUGINNAME,
                $"{context.User.Username} used command \"couch\" in {context.Channel.Name}."
                ));
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            if (!await dbStrings.TableInit(bChan))
            {
                await DBStringsFirstSetup(bChan);
            }
            CouchSettings settings = await Settings(bChan);
            switch (arguments[0].ToLower())
            {
                case "on":
                    settings._active = true;
                    SaveBaseSettings(PLUGINNAME, bChan, settings);
                    await SayOnDiscordAdmin(bChan,
                    $"Couch is active. {settings.couchsize} seats. Greetlimit is {settings.potatoGreeting}."
                    );
                    break;
                case "off":
                    settings._active = false;
                    SaveBaseSettings(PLUGINNAME, bChan, settings);
                    await SayOnDiscordAdmin(bChan,
                     $"Couch is inactive. {settings.couchsize} seats. Greetlimit is {settings.potatoGreeting}."
                     );
                    break;
                case "open":
                    if (!settings._active) { return; }
                    ResetCouch(bChan, settings);
                    break;
                case "size":
                    if (arguments.Count == 2)
                    {
                        int seats = settings.couchsize;
                        int.TryParse(arguments[1], out seats);
                        if (seats > 0 && seats <= 40 && seats != settings.couchsize)
                        {
                            settings.couchsize = seats;
                            await SayOnDiscordAdmin(bChan, $"Changed the Couch size to {settings.couchsize}.");
                            SaveBaseSettings(PLUGINNAME, bChan, settings);
                        }
                    }
                    break;
                case "greet":
                        int greet = settings.potatoGreeting;
                        int.TryParse(arguments[1], out greet);
                        if (greet > 0 && greet <= 999 && greet != settings.potatoGreeting)
                        {
                            settings.potatoGreeting = greet;
                            await SayOnDiscordAdmin(bChan,$"Couch greeting limit is now {settings.potatoGreeting}.");
                            SaveBaseSettings(PLUGINNAME, bChan, settings);
                        }
                    break;
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
                case "addgreet":
                    if (arguments.Count >= 2)
                    {
                        await AddLine(bChan, "GREET", arguments);
                    }
                    break;
                case "list":
                    if (arguments.Count == 1)
                    {
                        await ListLinesFromDB(bChan, 0);
                        return;
                    }
                    int page = 0;
                    int.TryParse(arguments[1], out page);
                    if (page <= 0) { page = 1; }

                    await ListLinesFromDB(bChan, page - 1);
                    break;
                case "use":
                    if (arguments.Count == 1)
                    {
                        return;
                    }
                    int id = -1;
                    int.TryParse(arguments[1], out id);
                    if (id <= 0) { return; }
                    await ToggleInUse(bChan, id);
                    break;
                case "delete":
                    if (arguments.Count == 1)
                    {
                        return;
                    }
                    id = -1;
                    int.TryParse(arguments[1], out id);
                    if (id <= 0) { return; }
                    await DeleteEntry(bChan, id);
                    break;
                case "restore":
                    bool removed = await dbStrings.TableDrop(bChan);
                    if (removed)
                    {
                        await SayOnDiscordAdmin(bChan, "Removed all current line data from the database.");
                    }
                    else
                    {
                        await SayOnDiscordAdmin(bChan, "Couldn't remove anything from the database.");
                    }
                    break;
            }
        }
        #endregion
        #region Internal stuff
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
            await SayOnDiscordAdmin(bChan, $"Couch is now open. Click https://twitch.tv/{bChan.TwitchChannelName} and take a {Core._commandCharacter}seat.");
        }
        private async Task DBStringsFirstSetup(BotChannel bChan)
        {
            await Task.Run(() =>
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
            });
        }
        private bool RollIncident(int chance = 0)
        {
            return rng.Next(0, 100) + chance >= 95;
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
        #endregion
        #region Database stuff
        #region DB Strings stuff
        private async Task DeleteEntry(BotChannel bChan, int id)
        {
            CouchDBString entry = await dbStrings.GetStringByID(bChan, id);
            if (entry == null)
            {
                await SayOnDiscordAdmin(bChan, $"Could not match the given ID.");
                return;
            }
            if (entry._inuse)
            {
                await SayOnDiscordAdmin(bChan, $"Only entries that is not in use can be deleted. Use \"{Core._commandCharacter}couch use <ID>\" to toggle the inuse flag on entries.");
                return;
            }
            if(dbStrings.DeleteEntry(bChan, id))
            {
                await SayOnDiscordAdmin(bChan, $"Entry {id} deleted.");
            }
        }
        private async Task ToggleInUse(BotChannel bChan, int id)
        {
            CouchDBString entry = await dbStrings.GetStringByID(bChan, id);
            CouchDBString edited = new CouchDBString(entry._id, !entry._inuse, entry._topic, entry._text);
            if(dbStrings.SaveEditedLineByID(bChan, edited))
            {
                await SayOnDiscordAdmin(bChan, "Entry updated.");
            }
            else
            {
                await SayOnDiscordAdmin(bChan, "Failed to update entry.");
            }
        }
        private async Task ListLinesFromDB(BotChannel bChan, int page)
        {
            // LINES IN USE
            string inuseText = $"Currently stored lines...```fix{Environment.NewLine}" +
                $"These are lines stored in the database that the Couch plugin will use based on topic if they are marked as inuse.{Environment.NewLine}{Environment.NewLine}" +
                $"<ID> <TOPIC> <INUSE> <TEXT>        Page {page + 1}{Environment.NewLine}";
            List<CouchDBString> lines = dbStrings.GetRowsByTen(bChan, page);
            if (lines.Count == 0)
            {
                inuseText += "No hits. Try a lower page number.";
            }
            else
            {
                foreach(CouchDBString entry in lines)
                {
                    inuseText += String.Format("{0,4}", entry._id);
                    inuseText += String.Format("{0,8}", entry._topic);
                    inuseText += String.Format("{0,7}", entry._inuse);
                    inuseText += " " + entry._text + Environment.NewLine;
                }
            }

            inuseText += $"```Use command {Core._commandCharacter}couch list <page> to list a page. Those marked with an X for INUSE are in rotation. Topic is what the text is used for.";
            await SayOnDiscordAdmin(bChan, inuseText);
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
