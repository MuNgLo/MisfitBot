using Discord;
using Discord.Commands;
using MisfitBot2.Plugins.MatchMaking;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Discord.WebSocket;

namespace MisfitBot2.Services
{
    class MatchMakingService : ServiceBase, IService
    {
        public readonly string PLUGINNAME = "MatchMaking";
        // CONSTRUCTOR
        public MatchMakingService()
        {
            Core.OnUserEntryMerge += OnUserEntryMerge;
            Core.Channels.OnBotChannelMerge += OnBotChannelEntryMerge;
            TimerStuff.OnSecondTick += OnSecondTick;
            TimerStuff.OnMinuteTick += OnMinuteTick;
        }// END of Constructor
        #region Data stuff
        private async Task<MatchMakingSettings> Settings(BotChannel bChan)
        {
            MatchMakingSettings settings = new MatchMakingSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as MatchMakingSettings;
        }
        #region MMQueue data stuff
        private async Task<MMQueue> CreateQueue(ulong discordChannelID, int teamsize)
        {
            MMQueue queue = new MMQueue(discordChannelID, teamsize);
            string table = $"{PLUGINNAME}_QUEUES";
            if (!await TableExists(table))
            {
                QueueTableCreate(table);
            }
            if (!await QueueRowExists(table, discordChannelID))
            {
                QueueRowCreate(table, queue);
            }
            queue = await QueueRowRead(table, discordChannelID);
            if (queue.teamsize != teamsize)
            {
                queue.teamsize = teamsize;
                SaveQueue(queue);
            }
            return queue;
        }
        private async Task<MMQueue> GetQueueData(ulong discordChannelID)
        {
            MMQueue queue = new MMQueue(discordChannelID, -1);
            string table = $"{PLUGINNAME}_QUEUES";
            if (!await TableExists(table))
            {
                QueueTableCreate(table);
            }
            if (!await QueueRowExists(table, discordChannelID))
            {
                QueueRowCreate(table, queue);
            }
            return await QueueRowRead(table, discordChannelID);
        }
        public async Task<bool> QueueRowExists(String table, ulong discordChannelID)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {table} WHERE discordChannelID IS @discordChannelID";
                cmd.Parameters.AddWithValue("@discordChannelID", discordChannelID);

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
        public void QueueTableCreate(string tableName)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"CREATE TABLE {tableName} (" +
                    $"discordChannelID INTEGER, " +
                    $"currentstate INTEGER, " +
                    $"active BOOL, " +
                    $"game VACHAR(30), " +
                    $"teamsize INTEGER, " +
                    $"users TEXT, " +
                    $"teamA TEXT, " +
                    $"teamB TEXT, " +
                    $"winningteam INTEGER " +
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
        public async Task<MMQueue> QueueRowRead(string table, ulong discordChannelID)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {table} WHERE discordChannelID IS @discordChannelID";
                cmd.Parameters.AddWithValue("@discordChannelID", discordChannelID);
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
                MMQueue queue = new MMQueue((ulong)result.GetInt64(0), result.GetInt32(4));
                queue.currentstate = (QUEUESTATE)result.GetInt32(1);
                queue.active = result.GetBoolean(2);
                queue.game = result.GetString(3);
                queue.users = JsonConvert.DeserializeObject<List<ulong>>(result.GetString(5));
                queue.teamA = JsonConvert.DeserializeObject<List<ulong>>(result.GetString(6));
                queue.teamB = JsonConvert.DeserializeObject<List<ulong>>(result.GetString(7));
                queue.winningteam = (WINNER)result.GetInt32(8);
                return queue;
            }
        }
        /// <summary>
        /// Creates a valid row in our custom table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="key"></param>
        /// <param name="queue"></param>
        public void QueueRowCreate(String table, MMQueue queue)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"INSERT INTO {table} VALUES (" +
                    $"@discordChannelID, " +
                    $"@currentstate, " +
                    $"@active, " +
                    $"@game, " +
                    $"@teamsize, " +
                    $"@users, " +
                    $"@teamA, " +
                    $"@teamB, " +
                    $"@winningteam" +
                    $")";
                cmd.Parameters.AddWithValue("@discordChannelID", queue.DISCORDCHANNEL);
                cmd.Parameters.AddWithValue("@currentstate", queue.currentstate);
                cmd.Parameters.AddWithValue("@active", queue.active);
                cmd.Parameters.AddWithValue("@game", queue.game);
                cmd.Parameters.AddWithValue("@teamsize", queue.teamsize);
                cmd.Parameters.AddWithValue("@users", JsonConvert.SerializeObject(queue.users, Formatting.None));
                cmd.Parameters.AddWithValue("@teamA", JsonConvert.SerializeObject(queue.teamA, Formatting.None));
                cmd.Parameters.AddWithValue("@teamB", JsonConvert.SerializeObject(queue.teamB, Formatting.None));
                cmd.Parameters.AddWithValue("@winningteam", queue.winningteam);

                cmd.ExecuteNonQuery();
            }
        }
        public async Task QueueRowDelete(String table, ulong discordChannelID)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"DELETE FROM {table} WHERE discordChannelID IS @discordChannelID";
                cmd.Parameters.AddWithValue("@discordChannelID", discordChannelID);
                cmd.ExecuteNonQuery();
            }
            if (await QueueRowExists(table, discordChannelID))
            {
                await Core.LOG(new LogMessage(LogSeverity.Warning, PLUGINNAME, $"Queue deletion failed!"));
            }
            else
            {
                await Core.LOG(new LogMessage(LogSeverity.Info, PLUGINNAME, $"Queue data deleted."));
            }

        }
        /// <summary>
        /// Saves the queue instance to the database
        /// </summary>
        /// <param name="plugin"></param>
        public void SaveQueue(MMQueue queue)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                string table = $"{PLUGINNAME}_QUEUES";
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"UPDATE {table} SET " +
                    $"currentstate = @currentstate, " +
                    $"active = @active, " +
                    $"game = @game, " +
                    $"teamsize = @teamsize, " +
                    $"users = @users, " +
                    $"teamA = @teamA, " +
                    $"teamB = @teamB " +
                    $" WHERE discordChannelID is @discordChannelID";
                cmd.Parameters.AddWithValue("@currentstate", queue.currentstate);
                cmd.Parameters.AddWithValue("@active", queue.active);
                cmd.Parameters.AddWithValue("@game", queue.game);
                cmd.Parameters.AddWithValue("@teamsize", queue.teamsize);
                cmd.Parameters.AddWithValue("@users", JsonConvert.SerializeObject(queue.users, Formatting.None));
                cmd.Parameters.AddWithValue("@teamA", JsonConvert.SerializeObject(queue.teamA, Formatting.None));
                cmd.Parameters.AddWithValue("@teamB", JsonConvert.SerializeObject(queue.teamB, Formatting.None));
                cmd.Parameters.AddWithValue("@discordChannelID", queue.DISCORDCHANNEL);
                cmd.ExecuteNonQuery();
            }
        }
        #endregion // EO MMQueue data stuff
        #endregion // EO data stuff

        #region Discord commands
        /// <summary>
        /// Tries to sign the user up for the mm queue.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task DiscordSignup(ICommandContext context)
        {
            UserEntry user = await Core.UserMan.GetUserByDiscordID(context.User.Id);
            if (await AddUserToQueue(user, context.Channel.Id))
            {
                await SayOnDiscord($"{user._username} signed up.", context.Channel.Id);
                await CheckIfFull(context.Channel.Id);
            }
        }
        public async Task DiscordCommand(ICommandContext context)
        {
            await DiscordCommand(context, "help");
        }
        public async Task DiscordCommand(ICommandContext context, List<string> args)
        {

            await DiscordCommand(context, args[0], args);
        }
        public async Task DiscordCommand(ICommandContext context, string arg, List<string> args = null)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            MatchMakingSettings settings = await Settings(bChan);
            if (!settings._active)
            {
                if (arg.ToLower() == "on")
                {
                    await ActivateInGuild(bChan, context.Channel.Id);
                }
                if (arg.ToLower() == "info" || arg.ToLower() == "help")
                {
                    await PostInfo(bChan, context.Channel.Id);
                }
                if (arg.ToLower() == "commands")
                {
                    await PostCommandsHelp(context.Channel.Id);
                }
            }
            else
            {
                switch (arg.ToLower())
                {
                    case "info":
                    case "help":
                        await PostInfo(bChan, context.Channel.Id);
                        break;
                    case "commands":
                        await PostCommandsHelp(context.Channel.Id);
                        break;
                    case "off":
                        await DeactivateInGuild(bChan, context.Channel.Id);
                        break;
                    case "go":
                        await StartQueueInChannel(bChan, context.Channel.Id, args);
                        break;
                    case "stop":
                        await StopQueueInChannel(context.Channel.Id);
                        break;
                    case "game":
                        await SetQueueName(bChan, context.Channel.Id, args);
                        break;
                    case "start":
                        await RunQueue(context.Channel.Id);
                        break;
                    case "winner":
                        await DeclareWinner(bChan, context.Channel.Id, args);
                        break;
                    case "restart":
                        await RestartQueue(context.Channel.Id);
                        break;
                    case "runtest":
                        await RunDebugQueue(bChan, context.Channel.Id, context.User.Id, args);
                        break;
                }
            }
        }
        private async Task StopQueueInChannel(ulong discordChannelID)
        {
            MMQueue queue = await GetQueueData(discordChannelID);
            queue.currentstate = QUEUESTATE.INACTIVE;
            queue.users = new List<ulong>();
            queue.teamA = new List<ulong>();
            queue.teamB = new List<ulong>();
            queue.winningteam = WINNER.UNDECLARED;
            queue.active = false;
            SaveQueue(queue);
            await SayOnDiscord($"Queue stopped. Use \"{Core._commandCharacter}mm start\" or \"{Core._commandCharacter}mm go <teamsize>\" to start it up again.", discordChannelID);
        }
        private async Task RunDebugQueue(BotChannel bChan, ulong discordChannelID, ulong userDiscordID, List<string> args)
        {
            await SayOnDiscord($"Running a debug queue.....", discordChannelID);
            await RestartQueue(discordChannelID);
            MMQueue queue = await GetQueueData(discordChannelID);
            int teamsize = queue.teamsize;
            if (args != null)
            {
                if (args.Count > 1)
                {
                    int.TryParse(args[1], out teamsize);
                    if (teamsize < 1)
                    {
                        teamsize = 5;
                    }
                }
            }
            queue.teamsize = teamsize;
            for (int i = 0; i < queue.teamsize * 2; i++)
            {
                queue.users.Add(userDiscordID);
            }
            queue.currentstate = QUEUESTATE.RECRUITING;
            SaveQueue(queue);
            await CheckIfFull(discordChannelID);
        }
        /// <summary>
        /// Restarts any active queue
        /// </summary>
        /// <param name="discordChannelID"></param>
        /// <returns></returns>
        private async Task RestartQueue(ulong discordChannelID)
        {
            MMQueue queue = await GetQueueData(discordChannelID);
            if (queue.active == false)
            {
                await SayOnDiscord($"No queue to restart found.", discordChannelID);
                return;
            }
            queue.currentstate = QUEUESTATE.RECRUITING;
            queue.users = new List<ulong>();
            queue.teamA = new List<ulong>();
            queue.teamB = new List<ulong>();
            queue.winningteam = WINNER.UNDECLARED;
            SaveQueue(queue);
            await SayOnDiscord($"Queue restarted.", discordChannelID);
        }
        private async Task DeclareWinner(BotChannel bChan, ulong discordChannelID, List<string> args)
        {
            MMQueue queue = await GetQueueData(discordChannelID);
            if (queue.currentstate != QUEUESTATE.PENDING)
            {
                if (queue.currentstate == QUEUESTATE.RECRUITING)
                {
                    await SayOnDiscord($"This queue is ongoing. Use {Core._commandCharacter}mm restart to reset it or {Core._commandCharacter}mm help for more options.", discordChannelID);
                    return;
                }
                if (queue.currentstate == QUEUESTATE.INACTIVE | queue.currentstate == QUEUESTATE.ENDED)
                {
                    await SayOnDiscord($"This is no queue active here. Use {Core._commandCharacter}mm restart to reset it or {Core._commandCharacter}mm help for more options.", discordChannelID);
                    return;
                }
                return;
            }
            if (args.Count > 1)
            {
                if (args[0].ToLower() == "teama" || args[1].ToLower() == "a")
                {
                    queue.currentstate = QUEUESTATE.ENDED;
                    queue.winningteam = WINNER.TEAMA;
                    await PostWinner(queue);
                }
                if (args[0].ToLower() == "teamb" || args[1].ToLower() == "b")
                {
                    queue.currentstate = QUEUESTATE.ENDED;
                    queue.winningteam = WINNER.TEAMB;
                    await PostWinner(queue);
                }
            }
            if (queue.currentstate != QUEUESTATE.ENDED)
            {
                await SayOnDiscord($"To declare a winning team use {Core._commandCharacter}mm winner teama/teamb/a/b.", discordChannelID);
                return;
            }
        }
        private async Task PostWinner(MMQueue queue)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle($"{queue.game}   The match is over.");
            builder.WithColor(Color.Green);

            string message = string.Empty;
            string teamstring = string.Empty;
            EmbedFieldBuilder teamA = new EmbedFieldBuilder();
            foreach (ulong userID in queue.teamA)
            {
                SocketUser u = Core.Discord.GetUser(userID);
                message += $"{u.Mention} ";
                teamstring += $"{u.Username} ";
            }
            teamA.Name = teamstring;
            teamstring = string.Empty;
            EmbedFieldBuilder teamB = new EmbedFieldBuilder();
            foreach (ulong userID in queue.teamB)
            {
                SocketUser u = Core.Discord.GetUser(userID);
                message += $"{u.Mention} ";
                teamstring += $"{u.Username} ";
            }
            teamB.Name = teamstring;

            if (queue.winningteam == WINNER.TEAMA)
            {
                teamA.Value = 1000; teamB.Value = 100;
                builder.AddField("Winners   TeamA", teamA.Build());
                builder.AddField("Losers   TeamB", teamB.Build());
            }
            else if (queue.winningteam == WINNER.TEAMB)
            {
                teamA.Value = 100; teamB.Value = 1000;
                builder.AddField("Winners   TeamB", teamB.Build());
                builder.AddField("Losers   TeamA", teamA.Build());
            }
            else
            {
                return; // Winningteam match failed
            }
            Embed obj = builder.Build();
            await (Core.Discord.GetChannel(queue.DISCORDCHANNEL) as ISocketMessageChannel).SendMessageAsync(message, false, obj);
        }
        private async Task PostCommandsHelp(ulong discordChannelID)
        {
            await SayOnDiscord(
                $"```fix{Environment.NewLine}" +
                $"mm info > Alias for help.{Environment.NewLine}" +
                $"mm help > Dumps info back to channel.{Environment.NewLine}" +
                $"mm on / off > Turn the module off or on on a BotChannel level.{Environment.NewLine}" +
                $"mm go / stop > Turns queue sytem on or off in Discord channel.{Environment.NewLine}" +
                $"mm game > Sets the game for the Discord channel.{Environment.NewLine}" +
                $"mm start > Resets and starts a queue if there isn't currently one running.{Environment.NewLine}" +
                $"mm restart > Restarts any currently active queue.{Environment.NewLine}" +
                $"mm runtest > Debug function.Fills a queue and randomizes the teams.{Environment.NewLine}" +
                $"mm winner < team > > Declares the winning team.{Environment.NewLine}" +
                Environment.NewLine +
                $"signup > Signs the user up for the currently running queue for the Discord channel.{Environment.NewLine}" +
                $"```",
                discordChannelID
                );
        }
        private async Task PostInfo(BotChannel bChan, ulong discordChannelID)
        {
            MatchMakingSettings settings = await Settings(bChan);
            MMQueue queue = await GetQueueData(discordChannelID);
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle($"Matchmaking");
            builder.WithColor(Color.Green);
            // Settings section
            EmbedFieldBuilder pluginSettings = new EmbedFieldBuilder();
            pluginSettings.Value = 100;
            pluginSettings.Name = "Active:" + (settings._active ? "on" : "off");
            builder.AddField("Plugin wide settings", pluginSettings.Build());
            // Queue section
            EmbedFieldBuilder queueSettings = new EmbedFieldBuilder();
            queueSettings.Value = 110;
            queueSettings.Name = "Active:" + queue.active.ToString() + Environment.NewLine;
            queueSettings.Name += "State:" + queue.currentstate.ToString() + Environment.NewLine;
            queueSettings.Name += "Game:" + queue.game + Environment.NewLine;
            queueSettings.Name += "Team size:" + queue.teamsize.ToString() + Environment.NewLine;
            queueSettings.Name += "Winning team:" + queue.winningteam.ToString();
            builder.AddField("Queue settings", queueSettings.Build());

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.Text = $"Type \"{Core._commandCharacter}mm commands\" for a list of commands";
            builder.Footer = footer;
            // Build final Embedd and send it
            Embed obj = builder.Build();
            await (Core.Discord.GetChannel(queue.DISCORDCHANNEL) as ISocketMessageChannel).SendMessageAsync(" ", false, obj);
        }
        #endregion

        #region internal stuff
        private async Task CheckIfFull(ulong discordChannelID)
        {
            MMQueue queue = await GetQueueData(discordChannelID);
            if (queue.users.Count == queue.teamsize * 2)
            {
                queue.currentstate = QUEUESTATE.PENDING;
                SaveQueue(queue);
                await DrawTeams(queue);
            }
        }
        private async Task DrawTeams(MMQueue queue)
        {
            Random rng = new Random();
            List<ulong> users = new List<ulong>();
            users.AddRange(queue.users);
            List<ulong> teamA = new List<ulong>();
            List<ulong> teamB = new List<ulong>();
            for (int i = 0; i < queue.teamsize; i++)
            {
                int index = rng.Next(users.Count);
                teamA.Add(users[index]);
                users.RemoveAt(index);
            }
            foreach (ulong user in users)
            {
                teamB.Add(user);
            }
            queue.teamA = teamA;
            queue.teamB = teamB;
            SaveQueue(queue);
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle("Teams have been drawn. This is the result.");
            builder.WithColor(Color.Green);
            string message = string.Empty;
            string teamstring = string.Empty;
            EmbedFieldBuilder firstTeam = new EmbedFieldBuilder();
            foreach (ulong userID in queue.teamA)
            {
                SocketUser u = Core.Discord.GetUser(userID);
                message += $"{u.Mention} ";
                teamstring += $"{u.Username} ";
            }
            firstTeam.Name = teamstring;
            firstTeam.Value = 1000;
            builder.AddField("Team A", firstTeam.Build());

            teamstring = string.Empty;
            EmbedFieldBuilder secondTeam = new EmbedFieldBuilder();
            foreach (ulong userID in queue.teamB)
            {
                SocketUser u = Core.Discord.GetUser(userID);
                message += $"{u.Mention} ";
                teamstring += $"{u.Username} ";
            }
            secondTeam.Name = teamstring;
            secondTeam.Value = 100;
            builder.AddField("Team B", secondTeam.Build());
            Embed obj = builder.Build();
            await (Core.Discord.GetChannel(queue.DISCORDCHANNEL) as ISocketMessageChannel).SendMessageAsync(message, false, obj);
        }
        /// <summary>
        /// Tries to sign the user up for the mm queue. Returns boolean result.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="discordChannelID"></param>
        /// <returns></returns>
        private async Task<bool> AddUserToQueue(UserEntry user, ulong discordChannelID)
        {
            MMQueue queue = await GetQueueData(discordChannelID);
            if (!queue.active)
            {
                return false;
            }
            if (queue.currentstate != QUEUESTATE.RECRUITING)
            {
                await SayOnDiscord($"Queue is not open.", discordChannelID);
                return false;
            }
            if (user._discordUID == 0)
            {
                await SayOnDiscord($"User lookup failed.", discordChannelID);
                return false;
            }
            if (queue.users.Exists(p => p == user._discordUID))
            {
                await SayOnDiscord($"{user._username} is already signed up.", discordChannelID);
                return false;
            }
            queue.users.Add(user._discordUID);
            SaveQueue(queue);
            return queue.users.Exists(p => p == user._discordUID);
        }
        /// <summary>
        /// Works
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="discordChannelID"></param>
        /// <returns></returns>
        private async Task ActivateInGuild(BotChannel bChan, ulong discordChannelID)
        {
            MatchMakingSettings settings = await Settings(bChan);
            if (!settings._active)
            {
                settings._active = true;
                SaveBaseSettings(PLUGINNAME, bChan, settings);
                await SayOnDiscord($"Matchmaking is now available to run in channels on this Discord. It has to be activated per channel with !mm go", discordChannelID);
            }
        }
        /// <summary>
        /// Works
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="discordChannelID"></param>
        /// <returns></returns>
        private async Task DeactivateInGuild(BotChannel bChan, ulong discordChannelID)
        {
            MatchMakingSettings settings = await Settings(bChan);
            if (settings._active)
            {
                settings._active = false;
                SaveBaseSettings(PLUGINNAME, bChan, settings);
                await SayOnDiscord(bChan, "Deactivating Matchmaking for this guild.");
            }
        }
        private async Task StartQueueInChannel(BotChannel bChan, ulong discordChannelID, List<string> args)
        {
            int teamsize = -1;
            if (args != null)
            {
                if (args.Count > 1)
                {
                    int.TryParse(args[1], out teamsize);
                }
            }
            if (args == null || args.Count < 2 || teamsize < 1)
            {
                await SayOnDiscord($"Don't forget teamsize. Example: {Core._commandCharacter}mm go 5", discordChannelID);
                return;
            }
            await StartQueueInChannel(bChan, discordChannelID, teamsize);
        }
        private async Task StartQueueInChannel(BotChannel bChan, ulong discordChannelID, int teamsize)
        {
            MatchMakingSettings settings = await Settings(bChan);
            MMQueue queue = await CreateQueue(discordChannelID, teamsize);
            if (settings._active && !queue.active)
            {
                queue.active = true;
                SaveQueue(queue);
                if (queue.game != string.Empty)
                {
                    await SayOnDiscord($"Matchmaking is now running a queue in this channel for the game {queue.game}.", discordChannelID);
                }
                else
                {
                    await SayOnDiscord($"Matchmaking is now running a queue in this channel. Don't forget to set the game with {Core._commandCharacter}mm game <gamename>", discordChannelID);
                }
            }
            else if (settings._active && queue.active)
            {
                await SayOnDiscord($"Matchmaking is already running a queue in this channel for the game {queue.game}.", discordChannelID);
            }
        }
        private async Task SetQueueName(BotChannel bChan, ulong discordChannelID, List<string> args)
        {
            MatchMakingSettings settings = await Settings(bChan);
            if (!settings._active)
            {
                await SayOnDiscord($"This channels queue is not active.", discordChannelID);
                return;
            }
            args.RemoveAt(0);
            string game = args[0];
            if (args.Count > 1)
            {
                game = string.Empty;
                foreach (string arg in args)
                {
                    game += $"{arg} ";
                }
                game = game.Trim();
            }
            MMQueue queue = await GetQueueData(discordChannelID);
            if (queue.game != game)
            {
                queue.game = game;
                queue.currentstate = QUEUESTATE.RECRUITING;
                SaveQueue(queue);
                await SayOnDiscord($"This channel's queue is now for the game {queue.game}", discordChannelID);
                return;
            }
            await SayOnDiscord($"This channel's queue game setting is {queue.game}", discordChannelID);
        }
        private async Task RunQueue(ulong discordChannelID)
        {
            MMQueue queue = await GetQueueData(discordChannelID);
            if (queue.currentstate == QUEUESTATE.RECRUITING)
            {
                await SayOnDiscord($"Queue is already started.", discordChannelID);
                return;
            }
            if (queue.currentstate == QUEUESTATE.INACTIVE || queue.currentstate == QUEUESTATE.ENDED)
            {
                queue.active = true;
                queue.currentstate = QUEUESTATE.RECRUITING;
                queue.users = new List<ulong>();
                queue.teamA = new List<ulong>();
                queue.teamB = new List<ulong>();
                SaveQueue(queue);
                await SayOnDiscord($"Queue is now open. Type '{Core._commandCharacter}signup' to participate.", discordChannelID);
            }

        }
        #endregion

        #region Interface compliance
        public void OnBotChannelEntryMerge(BotChannel discordGuild, BotChannel twitchChannel)
        {
        }
        public void OnMinuteTick(int minutes)
        {
        }
        public void OnSecondTick(int seconds)
        {
        }
        public void OnUserEntryMerge(UserEntry discordUser, UserEntry twitchUser)
        {

        }
        #endregion

    }
}
