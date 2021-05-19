using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MisfitBot_MKII;
using MisfitBot_MKII.Components;
using MisfitBot_MKII.Statics;
using System.Data.SQLite;
using System.Data;

namespace Couch
{
    public class Couch : PluginBase
    {
        #region old fields
        public readonly string PLUGINNAME = "Couch";
        public readonly string PLUGINSTATS = "_Couch_Stats";
        private Random rng = new Random();
        private List<string> _success = new List<string>();
        private List<string> _fail = new List<string>();
        private List<string> _incident = new List<string>();
        private List<string> _tardy = new List<string>();
        private List<string> _shakeF = new List<string>();
        private List<string> _shakeS = new List<string>();
        private List<string> _greets = new List<string>();
        private DatabaseStrings dbStrings;
        private List<TimedMessage> _timedMessages = new List<TimedMessage>();
        #endregion

        public Couch():base("Couch", 1)
        {
            DBDefaultLines();
            Program.BotEvents.OnMessageReceived += OnMessageReceived;
            Program.BotEvents.OnCommandReceived += OnCommandRecieved;
            Program.BotEvents.OnTwitchChannelJoined += OnTwitchChannelJoined;
            Program.BotEvents.OnTwitchChannelGoesLive += OnTwitchChannelGoesLive;
            dbStrings = new DatabaseStrings(PLUGINNAME, "couch");
        }

        

        

        

        #region Command Stuff
        
        private async void CommandResolve(BotWideCommandArguments args)
        {
            if (args.command.ToLower() != "couch" && args.command.ToLower() != "seat" && args.command.ToLower() != "seats"){return;}
            BotChannel bChan = await GetBotChannel(args);
            if (bChan == null) { return; }
            if (args.user == null) { return; }
            // Prep response instance
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            // For Couch we don't go further if we don't have a twitch channel tied to the discord guild
            if (bChan.TwitchChannelName == string.Empty && args.arguments.Count == 0)
            {
                response.message = "There is no twitch channel to run a couch in.";
                Respond(bChan, response);
                return;
            }

            CouchSettings settings = await Settings<CouchSettings>(bChan, PLUGINNAME);
            if (!settings._couches.ContainsKey(bChan.Key))
            {
                settings._couches[bChan.Key] = new CouchEntry();
            }
            switch (args.command.ToLower())
            {
                case "couch":
                    // Broadcaster and Moderator commands
                    if (args.isModerator || args.isBroadcaster || args.canManageMessages)
                    {
                        // Check so we are connected to the twitch channel
                        if (!Program.Channels.CheckIfInTwitchChannel(bChan.TwitchChannelName))
                        {
                            response.message = "Couch needs a twitch channel connection. See \"twitch\" command.";
                            Respond(bChan, response);
                            return;
                        }
                        if (args.arguments.Count == 0)
                        {
                            if (args.source == MESSAGESOURCE.DISCORD)
                            {
                                string message = $"```fix{System.Environment.NewLine}Admin/Broadcaster commands {System.Environment.NewLine}" +
                                $"{Program.CommandCharacter}couch < Arguments >{System.Environment.NewLine}{System.Environment.NewLine}" +
                                $"Arguments....{System.Environment.NewLine}" +
                                $"< none > ->responds current settings{System.Environment.NewLine}" +
                                $"open -> Manually resets and open the couch.{System.Environment.NewLine}" +
                                $"on/off -> Turns plugin on or off for the channel.{System.Environment.NewLine}" +
                                $"size # -> Sets the number of seats between 1 and 40.{System.Environment.NewLine}" +
                                $"greet # -> Sets the number of seated needed in stats for a greeting when a user joins the twitch channel.{System.Environment.NewLine}" +
                                $"time # -> Sets the time in seconds the couch will stay open.{System.Environment.NewLine}{System.Environment.NewLine}" +
                                $"Discord only arguments(make sure adminchannel is set in adminplugin){System.Environment.NewLine}" +
                                $"addsuccess < text > Text being the line returned. Use [USER] in text where username should be.{System.Environment.NewLine}" +
                                $"addfail < text >{System.Environment.NewLine}" +
                                $"addgreet < text >{System.Environment.NewLine}" +
                                $"addincident < text >{System.Environment.NewLine}" +
                                $"list / list # -> Shows stored lines by page.{System.Environment.NewLine}" +
                                $"use # -> Toggles the inuse flag for the line with given ID.{System.Environment.NewLine}" +
                                $"delete # -> Deletes the line with the ID if inuse flag is false. As in not in use.{System.Environment.NewLine}" +
                                System.Environment.NewLine + System.Environment.NewLine +
                                $"User commands{System.Environment.NewLine}" +
                                $"{Program.CommandCharacter}seat -> When couch open it responds with success of fail message.{System.Environment.NewLine}" +
                                $"{Program.CommandCharacter}seats -> User stats rundown.{System.Environment.NewLine}```";
                                await SayOnDiscord(message, args.channelID);
                            }
                            if (settings._active)
                            {
                                response.message = $"Couch is active. {settings.couchsize} seats. Greetlimit is {settings.potatoGreeting}. Open time is {settings.openTime}";
                                Respond(bChan, response);
                            }
                            else
                            {
                                response.message = $"Couch is inactive. {settings.couchsize} seats. Greetlimit is {settings.potatoGreeting}. Open time is {settings.openTime}";
                                Respond(bChan, response);
                            }
                            return;
                        }
                        switch (args.arguments[0].ToLower())
                        {
                            case "addfail":
                                if (args.arguments.Count >= 2)
                                {
                                    await AddLine(bChan, "FAIL", args.arguments);
                                }
                                break;
                            case "addgreet":
                                if (args.arguments.Count >= 2)
                                {
                                    await AddLine(bChan, "GREET", args.arguments);
                                }
                                break;
                            case "addincident":
                                if (args.arguments.Count >= 2)
                                {
                                    await AddLine(bChan, "INCIDENT", args.arguments);
                                }
                                break;
                            case "addsuccess":
                                if (args.arguments.Count >= 2)
                                {
                                    await AddLine(bChan, "SUCCESS", args.arguments);
                                }
                                break;
                            case "addtardy":
                                if (args.arguments.Count >= 2)
                                {
                                    await AddLine(bChan, "TARDY", args.arguments);
                                }
                                break;
                            case "addshakes":
                                if (args.arguments.Count >= 2)
                                {
                                    await AddLine(bChan, "SHAKES", args.arguments);
                                }
                                break;
                            case "addshakef":
                                if (args.arguments.Count >= 2)
                                {
                                    await AddLine(bChan, "SHAKEF", args.arguments);
                                }
                                break;
                            case "close":
                                CloseCouch(bChan, settings);
                                break;
                            case "dbgreet":
                                response.message = dbStrings.GetRandomLine(bChan, "GREET");
                                response.parseMessage = true;
                                Respond(bChan, response);
                                break;
                            case "delete":
                                if (args.arguments.Count == 1)
                                {
                                    return;
                                }
                                int id = -1;
                                int.TryParse(args.arguments[1], out id);
                                if (id <= 0) { return; }
                                await DeleteEntry(bChan, args.channelID, id);
                                break;
                            case "greet":
                                if (args.arguments.Count == 2)
                                {
                                    int greet = settings.potatoGreeting;
                                    int.TryParse(args.arguments[1], out greet);
                                    if (greet > 0 && greet <= 100 && greet != settings.potatoGreeting)
                                    {
                                        settings.potatoGreeting = greet;
                                        response.message = $"Couch greeting limit is now {settings.potatoGreeting}.";
                                        Respond(bChan, response);
                                        await SayOnDiscordAdmin(bChan, $"{args.userDisplayName} changed the Couch Greetlimit setting to {settings.potatoGreeting}.");
                                        SaveBaseSettings(bChan, PLUGINNAME, settings);
                                    }
                                    else
                                    {
                                        response.message = $"Couch greeting limit has to be more than 0 and max 100.";
                                        Respond(bChan, response);
                                    }
                                }
                                break;
                            case "off":
                                settings._active = false;
                                SaveBaseSettings(bChan, PLUGINNAME, settings);
                                response.message = $"Couch is inactive. {settings.couchsize} seats. Greetlimit is {settings.potatoGreeting}. Open time is {settings.openTime}";
                                Respond(bChan, response);
                                break;
                            case "on":
                                settings._active = true;
                                SaveBaseSettings(bChan, PLUGINNAME, settings);
                                response.message = $"Couch is active. {settings.couchsize} seats. Greetlimit is {settings.potatoGreeting}. Open time is {settings.openTime}";
                                Respond(bChan, response);
                                break;
                            case "open":
                                if (!settings._active) { return; }
                                OpenCouch(bChan, settings);
                                break;
                            /*  TODO never look at this again!!
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
                                break;*/
                            case "rock":
                            case "shake":
                                await ShakeCouch(bChan, settings);
                                break;
                            case "size":
                                if (args.arguments.Count == 2)
                                {
                                    int seats = settings.couchsize;
                                    int.TryParse(args.arguments[1], out seats);
                                    if (seats > 0 && seats <= 100 && seats != settings.couchsize)
                                    {
                                        settings.couchsize = seats;
                                        response.message = $"Couch now has {settings.couchsize} seats.";
                                        Respond(bChan, response);
                                        await SayOnDiscordAdmin(bChan, $"{args.userDisplayName} changed the Couch size to {settings.couchsize}.");
                                        SaveBaseSettings(bChan, PLUGINNAME, settings);
                                    }
                                    else
                                    {
                                        response.message = $"Couch size limit has to be more than 0 and max 100.";
                                        Respond(bChan, response);
                                    }
                                }
                                break;
                            case "time":
                                if (args.arguments.Count == 2)
                                {
                                    int timer = settings.openTime;
                                    int.TryParse(args.arguments[1], out timer);
                                    if (timer > 0 && timer <= 10000 && timer != settings.openTime)
                                    {
                                        settings.openTime = timer;
                                        response.message = $"Couch time limit is now {settings.openTime} seconds.";
                                        Respond(bChan, response);
                                        await SayOnDiscordAdmin(bChan, $"{args.user} changed the Couch open time limit setting to {settings.openTime} seconds.");
                                        SaveBaseSettings(bChan, PLUGINNAME, settings);
                                    }
                                    else
                                    {
                                        response.message = $"Couch time limit has to be more than 0 and max 10000 seconds.";
                                        Respond(bChan, response);
                                    }
                                }
                                break;
                            case "use":
                                if (args.arguments.Count == 1)
                                {
                                    return;
                                }
                                id = -1;
                                int.TryParse(args.arguments[1], out id);
                                if (id <= 0) { return; }
                                await ToggleInUse(bChan, id);
                                break;
                            case "who":
                                if (!settings._active || args.source != MESSAGESOURCE.DISCORD) { return; }
                                response.message = GetAllSittersAsString(bChan, settings);
                                Respond(bChan, response);
                                break;
                            case "list":
                                if (args.source != MESSAGESOURCE.DISCORD) { return; }
                                if (args.arguments.Count == 1)
                                {
                                    await ListLinesFromDB(bChan, args.channelID, 0);
                                    return;
                                }
                                int page = 0;
                                int.TryParse(args.arguments[1], out page);
                                if (page <= 0) { page = 1; }

                                await ListLinesFromDB(bChan, args.channelID, page - 1);
                                break;

                        }
                    }
                    break;
                // User Commands
                case "seat":
                    if (!settings._couches[bChan.Key].couchOpen || !settings._active) { return; }
                    // To late
                    if (Core.CurrentTime > settings._couches[bChan.Key].lastActivationTime + settings.openTime)
                    {
                        // only give feedback a specified count on fails
                        if (settings.failCount <= settings.maxFails)
                        {
                            Respond(bChan, new BotWideResponseArguments(args)
                            {
                                source = args.source,
                                twitchChannel = bChan.TwitchChannelName,
                                discordChannel = bChan.discordAdminChannel,
                                user = args.user,
                                victim = null,
                                message = dbStrings.GetRandomLine(bChan, "TARDY"),
                                parseMessage = true
                            });
                            settings.failCount++;
                            SaveBaseSettings(bChan, PLUGINNAME, settings);
                        }
                        return;
                    }
                    if (settings._couches[bChan.Key].TwitchUsernames.Contains(args.userDisplayName)) { return; }

                    if (settings._couches[bChan.Key].TwitchUsernames.Count < settings.couchsize)
                    {
                        if (settings._couches[bChan.Key].TwitchUsernames.Count != 0)
                        {
                            if (RollIncident(15))
                            {
                                string rngSitter = GetRNGSitter(bChan, settings);
                                if (rngSitter != null)
                                {
                                    UserEntry victim = await Program.Users.GetUserByTwitchDisplayName(rngSitter);
                                    if (victim != null)
                                    {
                                        settings._couches[bChan.Key].TwitchUsernames.RemoveAll(p => p == victim._twitchUsername);
                                        if (!await UserStatsExists(bChan.Key, args.user.Key))
                                        {
                                            UserStatsCreate(bChan.Key, args.user.Key);
                                        }
                                        CouchUserStats userStats1 = await UserStatsRead(bChan.Key, args.user.Key);
                                        userStats1.CountSeated++;
                                        UserStatsSave(bChan, userStats1);

                                        if (!await UserStatsExists(bChan.Key, victim.Key))
                                        {
                                            UserStatsCreate(bChan.Key, args.user.Key);
                                        }
                                        CouchUserStats markUserStats = await UserStatsRead(bChan.Key, victim.Key);
                                        markUserStats.CountBooted++;
                                        UserStatsSave(bChan, markUserStats);

                                        response.victim = victim;
                                        response.message = dbStrings.GetRandomLine(bChan, "INCIDENT");
                                        response.parseMessage = true;
                                        Respond(bChan, response);

                                        SaveBaseSettings(bChan, PLUGINNAME, settings);
                                    }
                                }
                            }
                        }
                        if (!await UserStatsExists(bChan.Key, args.user.Key))
                        {
                            UserStatsCreate(bChan.Key, args.user.Key);
                        }
                        CouchUserStats userStats = await UserStatsRead(bChan.Key, args.user.Key);
                        userStats.CountSeated++;
                        UserStatsSave(bChan, userStats);
                        settings._couches[bChan.Key].TwitchUsernames.Add(args.userDisplayName);

                        Respond(bChan, new BotWideResponseArguments(args)
                        {
                            message = dbStrings.GetRandomLine(bChan, "SUCCESS"),
                            parseMessage = true
                        });

                        SaveBaseSettings(bChan, PLUGINNAME, settings);

                    }
                    else
                    {
                        Program.TwitchSayMessage(args.channel,
                            dbStrings.GetRandomLine(bChan, "FAIL").Replace("[USER]", args.user._twitchDisplayname)
                            );
                        settings.failCount++;
                        SaveBaseSettings(bChan, PLUGINNAME, settings);
                    }
                    break;
                case "seats":
                    CouchUserStats cStats = await GetUserCouchStats(bChan.Key, args.user.Key);
                    Program.TwitchSayMessage(args.channel,
                                $"{args.user._twitchDisplayname}, you have sat in couch {cStats.CountSeated} times. {cStats.CountBooted} times you fell off."
                                );
                    break;
            }
        }
        private async void TwitchInUserJoined(object sender, TwitchLib.Client.Events.OnUserJoinedArgs e)
        {
            if (TimerStuff.Uptime < 300)
            {
                return;
            }
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(e.Channel);
            if (!bChan.isLive)
            {
                return;
            }
            UserEntry user = await Program.Users.GetUserByTwitchUserName(e.Username);
            CouchSettings settings = await Settings<CouchSettings>(bChan, PLUGINNAME);
            if (user == null || settings == null) { return; }
            if (settings._greeted.Exists(p => p == user._twitchUsername)) { return; }
            if (!settings._couches.ContainsKey(bChan.Key))
            {
                settings._couches[bChan.Key] = new CouchEntry();
            }
            if (user != null && bChan != null && settings._active)
            {
                CouchUserStats uStats = await GetUserCouchStats(bChan.Key, user.Key);
                if (uStats.CountSeated >= settings.potatoGreeting)
                {
                    if (!await dbStrings.TableInit(bChan))
                    {
                        await DBStringsFirstSetup(bChan);
                    }
                    Program.TwitchSayMessage(e.Channel, dbStrings.GetRandomLine(bChan, "GREET").Replace("[USER]", user._twitchDisplayname));
                    settings._greeted.Add(user._twitchUsername);
                    SaveBaseSettings(bChan, PLUGINNAME, settings);
                }
            }
        }
        #endregion
        #region Internal stuff
        
        private async Task ShakeCouch(BotChannel bChan, CouchSettings settings)
        {
            if (!settings._active || !settings._couches[bChan.Key].couchOpen) { return; }

            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, PLUGINNAME, $"Shaking couch in {bChan.TwitchChannelName}."));
            Random rng = new Random();
            List<string> victims = new List<string>();
            foreach (string twitchUserName in settings._couches[bChan.Key].TwitchUsernames)
            {
                if (rng.Next(1, 100) <= 20)
                {
                    victims.Add(twitchUserName);
                }
            }
            if (victims.Count < 1)
            {
                Program.TwitchSayMessage(bChan.TwitchChannelName,
                                dbStrings.GetRandomLine(bChan, "SHAKEF")
                                );
                return;
            }
            string msg = string.Empty;
            foreach (string victim in victims)
            {
                settings._couches[bChan.Key].TwitchUsernames.Remove(victim);
                msg += victim + ",";
                UserEntry usr = await Program.Users.GetUserByTwitchDisplayName(victim);
                if (usr != null)
                {
                    CouchUserStats markUserStats = await UserStatsRead(bChan.Key, usr.Key);
                    markUserStats.CountBooted++;
                    UserStatsSave(bChan, markUserStats);
                }
            }
            Program.TwitchSayMessage(bChan.TwitchChannelName,
                                dbStrings.GetRandomLine(bChan, "SHAKES", msg)
                                );
            settings._couches[bChan.Key].lastActivationTime = Core.CurrentTime;
            SaveBaseSettings(bChan, PLUGINNAME, settings);
        }
        private async void OpenCouch(BotChannel bChan, CouchSettings settings)
        {
            if (!settings._couches.ContainsKey(bChan.Key))
            {
                settings._couches[bChan.Key] = new CouchEntry();
            }
            settings._couches[bChan.Key].couchOpen = true;
            settings._couches[bChan.Key].lastActivationTime = Core.CurrentTime;
            settings._couches[bChan.Key].TwitchUsernames = new List<string>();
            settings.failCount = 0;
            SaveBaseSettings(bChan, PLUGINNAME, settings);
            RegisterTimedMessage(bChan, settings);
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, PLUGINNAME, $"Opening couch in twitchchannel {bChan.TwitchChannelName}."));
            Program.TwitchSayMessage(bChan.TwitchChannelName, $"Couch is now open. Take a {Program.CommandCharacter}seat.");
            await SayOnDiscordAdmin(bChan, $"Couch is now open. Click https://twitch.tv/{bChan.TwitchChannelName} and take a {Program.CommandCharacter}seat.");
        }
        private async void CloseCouch(BotChannel bChan, CouchSettings settings)
        {
            if (!settings._couches.ContainsKey(bChan.Key))
            {
                settings._couches[bChan.Key] = new CouchEntry();
            }
            settings._couches[bChan.Key].couchOpen = false;
            SaveBaseSettings(bChan, PLUGINNAME, settings);
            Program.TwitchSayMessage(bChan.TwitchChannelName, $"Couch is now closed.");
            await SayOnDiscordAdmin(bChan, $"Couch is now closed.");
        }
        private void RegisterTimedMessage(BotChannel bChan, CouchSettings settings)
        {
            _timedMessages.RemoveAll(p => p.twitchChannelName == bChan.TwitchChannelName);
            _timedMessages.Add(
                new TimedMessage()
                {
                    twitchChannelName = bChan.TwitchChannelName,
                    interval = settings.reminderInterval,
                    msgInterval = settings.reminderMessageInterval,
                    lastused = Core.CurrentTime,
                    msgSinceLastused = 0,
                    done = false
                }
                );
        }
        private async void ReminderText(string twitchChannelName)
        {
            BotChannel bChan = await Program.Channels.GetTwitchChannelByName(twitchChannelName);
            CouchSettings settings = await Settings<CouchSettings>(bChan, PLUGINNAME);
            if (settings._active == false) { RemoveTimedMessage(bChan.TwitchChannelName); return; }
            if (!settings._couches[bChan.Key].couchOpen) { RemoveTimedMessage(bChan.TwitchChannelName); return; }
            if (settings._couches[bChan.Key].Count >= settings.couchsize)
            {
                Program.TwitchSayMessage(bChan.TwitchChannelName, $"Couch is now full.");
                RemoveTimedMessage(bChan.TwitchChannelName);
                return;
            }
            if (Core.CurrentTime > settings._couches[bChan.Key].lastActivationTime + settings.openTime) { RemoveTimedMessage(bChan.TwitchChannelName); return; }
            Program.TwitchSayMessage(bChan.TwitchChannelName, $"Couch is still open. Take a {Program.CommandCharacter}seat.");
        }
        private void RemoveTimedMessage(string twitchChannel)
        {
            if (_timedMessages.Exists(p => p.twitchChannelName == twitchChannel))
            {
                _timedMessages.Find(p => p.twitchChannelName == twitchChannel).done = true;
            }
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
                foreach (string line in _tardy)
                {
                    dbStrings.SaveNewLine(bChan, "TARDY", line);
                }
                foreach (string line in _shakeF)
                {
                    dbStrings.SaveNewLine(bChan, "SHAKEF", line);
                }
                foreach (string line in _shakeS)
                {
                    dbStrings.SaveNewLine(bChan, "SHAKES", line);
                }
                foreach (string line in _greets)
                {
                    dbStrings.SaveNewLine(bChan, "GREET", line);
                }
            });
        }
        private bool RollIncident(int chance)
        {
            return rng.Next(0, 100) < chance;
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
        private string GetAllSittersAsString(BotChannel bChan, CouchSettings settings)
        {
            string txt = string.Empty;
            foreach (string name in settings._couches[bChan.Key].TwitchUsernames)
            {
                txt += $"{name}, ";
            }
            if (txt == string.Empty)
            {
                txt = "There is nobody in the couch. Feel the anguish of failure puny human.";
            }
            return txt;
        }
        #endregion
        #region Event Listeners
        private void OnCommandRecieved(BotWideCommandArguments args)
        {
            if (args.source == MESSAGESOURCE.TWITCH)
            {
                if (_timedMessages.Exists(p => p.twitchChannelName == args.channel))
                {
                    _timedMessages.Find(p => p.twitchChannelName == args.channel).msgSinceLastused++;
                }
            }
            CommandResolve(args);
        }
        /// <summary>
        /// Count messages for reminder functionality
        /// </summary>
        /// <param name="args"></param>
        private void OnMessageReceived(BotWideMessageArguments args)
        {
            if (args.source == MESSAGESOURCE.TWITCH)
            {
                if (_timedMessages.Exists(p => p.twitchChannelName == args.channel))
                {
                    _timedMessages.Find(p => p.twitchChannelName == args.channel).msgSinceLastused++;
                }
            }
        }
        public override void OnMinuteTick(int minutes) { }
        public override void OnSecondTick(int seconds) { }
        private async void OnTwitchChannelJoined(string channel, string botname)
        {
            if (channel == botname) { return; }
            // Make sure we have all couch database tables for any twitch channel we join
            BotChannel bChan = null;
            while (bChan == null)
            {
                bChan = await Program.Channels.GetTwitchChannelByName(channel);
            }
            if (bChan == null) { return; }
            if (!await dbStrings.TableInit(bChan))
            {
                await DBStringsFirstSetup(bChan);
            }
            if (!StatsTableExists(bChan)) { StatsTableCreate(bChan, PLUGINSTATS); }
            await Core.Configs.ConfigSetup<CouchSettings>(bChan, PLUGINNAME, new CouchSettings());
        }
        public override void OnUserEntryMergeEvent(MisfitBot_MKII.UserEntry discordUser, MisfitBot_MKII.UserEntry twitchUser) { }
        public override void OnBotChannelEntryMergeEvent(MisfitBot_MKII.BotChannel discordGuild, MisfitBot_MKII.BotChannel twitchChannel) { }
        private async void OnTwitchChannelGoesLive(BotChannel bChan, int delay)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.DEBUG, PLUGINNAME,
            $"OnTwitchChannelGoesLive({bChan.TwitchChannelName}) in it? ({Program.Channels.CheckIfInTwitchChannel(bChan.TwitchChannelName)})"));
            CouchSettings settings = await Settings<CouchSettings>(bChan, PLUGINNAME);
            // Check so we are connected to the twitch channel
            if (Program.Channels.CheckIfInTwitchChannel(bChan.TwitchChannelName) && settings._active)
            {
                OpenCouch(bChan, settings);
            }
        }
        #endregion
        #region DATABSE METHODS
        #region DB Strings stuff
        /// <summary>
        /// Builds the default DB string lists. Does not add them to Database
        /// </summary>
        private void DBDefaultLines()
        {
            // Successes
            _success.Add("[USER] takes a seat on the couch.");
            _success.Add("[USER] backflips onto the couch.");
            _success.Add("[USER] manages to escape the restraints and takes a seat on the couch.");
            _success.Add("[USER] suddenly materializes on the couch with a hint of a smirk.");
            _success.Add("[USER] claws their way up from the void between the cushions.");
            _success.Add("[USER] does an impressive herolanding then proceeds to stumble to the couch with intense knee pain.");
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
            _incident.Add("[USER] catches [VICTIM] a couch cushion and leave their spot from embarrassment.");
            _incident.Add("[VICTIM] by pure chance ends up quantum entangling with the couch and disappears back into the void. [USER] Materializes in the same spot.");
            _incident.Add("[VICTIM] gets bumped out of the couch as [USER] take their seat.");
            _incident.Add("[VICTIM] becomes the victim of EjectorZeat 3000™. Who is playing with the buttons? [USER]?");
            _incident.Add("[USER] feeds the couch beans and [VICTIM] gets launched by a rocket fart. [USER] smugly takes the spot.");
            /// EOF OLD STUFF

            _tardy.Add("[USER] is late to get a seat in the couch. It isn't full but the people in it have spread out and refuse to move.");
            _tardy.Add("Sorry [USER]. Couch is closed for business.");
            _tardy.Add("As the couch is racing towards the horizon, [USER] gets left in the dust.");

            _shakeF.Add($"The couch is shaking! Luckily everybody on the couch manages to bite the pillows and hold on for dear life.");
            _shakeF.Add($"A tremor travels through the couch. Watch out for grabboids.");
            _shakeF.Add($"The couch frolic in the green pasture. Everyone on it is overcome by bliss.");

            _shakeS.Add($"The couch is shaking! [REPLACE] couldn't hold on.");
            _shakeS.Add($"A stampede of angry coasters make the couch flip! [REPLACE] couldn't hold on.");
            _shakeS.Add($"A cheesewheel falls of the couch! [REPLACE] starts chasing it.");

            _greets.Add("Welcome back [USER]. You truly are a proper couch potato. BloodTrail");
            _greets.Add("❤️❤️❤️ Senpai [USER] ❤️❤️❤️");
            _greets.Add("/me twitches nervously as [USER] enters the room.");
        }
        private async Task DeleteEntry(BotChannel bChan, ulong discordChannel, int id)
        {
            DBString entry = await dbStrings.GetStringByID(bChan, id);
            if (entry == null)
            {
                await SayOnDiscord($"Could not match the given ID.", discordChannel);
                return;
            }
            if (entry._inuse)
            {
                await SayOnDiscord($"Only entries that is not in use can be deleted. Use \"{Program.CommandCharacter}couch use <ID>\" to toggle the inuse flag on entries.", discordChannel);
                return;
            }
            if (dbStrings.DeleteEntry(bChan, id))
            {
                await SayOnDiscord($"Entry {id} deleted.", discordChannel);
            }
        }
        private async Task ToggleInUse(BotChannel bChan, int id)
        {
            DBString entry = await dbStrings.GetStringByID(bChan, id);
            DBString edited = new DBString(entry._id, !entry._inuse, entry._topic, entry._text);
            if (dbStrings.SaveEditedLineByID(bChan, edited))
            {
                await SayOnDiscordAdmin(bChan, "Entry updated.");
            }
            else
            {
                await SayOnDiscordAdmin(bChan, "Failed to update entry.");
            }
        }
        private async Task ListLinesFromDB(BotChannel bChan, ulong channelID, int page)
        {
            await ListLinesFromDB(bChan, channelID.ToString(), page);
        }
        private async Task ListLinesFromDB(BotChannel bChan, string channelID, int page)
        {
            // LINES IN USE
            string inuseText = $"Currently stored lines...```fix{Environment.NewLine}" +
                $"These are lines stored in the database that the Couch plugin will use based on topic if they are marked as inuse.{Environment.NewLine}{Environment.NewLine}" +
                $"<ID> <TOPIC> <INUSE> <TEXT>        Page {page + 1}{Environment.NewLine}";
            List<DBString> lines = dbStrings.GetRowsByTen(bChan, page);
            if (lines.Count == 0)
            {
                inuseText += "No hits. Try a lower page number.";
            }
            else
            {
                foreach (DBString entry in lines)
                {
                    inuseText += String.Format("{0,4}", entry._id);
                    inuseText += String.Format("{0,8}", entry._topic);
                    inuseText += String.Format("{0,7}", entry._inuse);
                    inuseText += " " + entry._text + Environment.NewLine;
                }
            }

            inuseText += $"```Use command {Program.CommandCharacter}couch list <page> to list a page. Those marked with an X for INUSE are in rotation. Topic is what the text is used for.";
            await SayOnDiscord(inuseText, channelID);
        }
        private async Task AddLine(BotChannel bChan, string topic, List<string> arguments)
        {
            arguments.RemoveAt(0);
            string line = string.Empty;
            foreach (string part in arguments) { line += " " + part; }
            line.Trim();
            dbStrings.SaveNewLine(bChan, topic, line);
            await SayOnDiscordAdmin(bChan, $"Added one more \"{topic}\" line for Couch plugin.");
        }
        #endregion
        public bool StatsTableExists(BotChannel bChan)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = "SELECT COUNT(*) AS QtRecords FROM sqlite_master WHERE type = 'table' AND name = @name";
                cmd.Parameters.AddWithValue("@name", bChan.Key + PLUGINSTATS);
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
        private void StatsTableCreate(BotChannel bChan, string tablename)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"CREATE TABLE \"{bChan.Key + tablename}\" (" +
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
                cmd.CommandText = $"SELECT * FROM \"{bKey + PLUGINSTATS}\" WHERE UserKey IS @uKey AND BotChannelKey IS @bKey";
                cmd.Parameters.AddWithValue("@uKey", uKey);
                cmd.Parameters.AddWithValue("@bKey", bKey);
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
                cmd.CommandText = $"INSERT INTO \"{bKey + PLUGINSTATS}\" VALUES (" +
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
        public void UserStatsSave(BotChannel bChan, CouchUserStats userStats)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;

                cmd.CommandText = $"UPDATE \"{bChan.Key + PLUGINSTATS}\" SET " +
                    $"BotChannelKey = @BotChannelKey, " +
                    $"CountSeated = @CountSeated, " +
                    $"CountBooted = @CountBooted " +
                    $" WHERE BotChannelKey is @BotChannelKey AND UserKey is @UserKey";
                cmd.Parameters.AddWithValue("@BotChannelKey", userStats.BotChannelKey);
                cmd.Parameters.AddWithValue("@UserKey", userStats.UserKey);
                cmd.Parameters.AddWithValue("@CountSeated", userStats.CountSeated);
                cmd.Parameters.AddWithValue("@CountBooted", userStats.CountBooted);
                cmd.ExecuteNonQuery();
            }
        }
        private async Task<bool> UserStatsExists(string bKey, string uKey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{bKey + PLUGINSTATS}\" WHERE UserKey IS @uKey AND BotChannelKey IS @bKey";
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
        private async Task ReplaceChannelKey(BotChannel bChan, string newKey, string oldKey)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM \"{bChan + PLUGINSTATS}\" WHERE BotChannelKey IS @oldKey";
                cmd.Parameters.AddWithValue("@oldKey", oldKey);
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
                    using (SQLiteCommand cmd2 = new SQLiteCommand())
                    {
                        cmd2.CommandType = CommandType.Text;
                        cmd2.Connection = Core.Data;

                        cmd2.CommandText = $"UPDATE \"{bChan.Key + PLUGINSTATS}\" SET " +
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
                await Core.LOG(new LogEntry(LOGSEVERITY.WARNING, PLUGINNAME, "Channelmerge detected. DB updated."));
            }
        }

        
        // END OF DB things
        #endregion
    }// EOF CLASS
}
