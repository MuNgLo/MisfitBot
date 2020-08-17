using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MisfitBot_MKII.MisfitBotEvents
{
    /// <summary>
    /// MKII v1.0
    /// </summary>
    internal class EventCatcherDiscord
    {
        internal EventCatcherDiscord(DiscordSocketClient client)
        {
            // Hook up all events
            //client.ChannelCreated += ChannelCreated;
            //client.ChannelDestroyed += ChannelDestroyed;
            //client.ChannelUpdated += ChannelUpdated;
            client.Connected += Connected;
            //client.CurrentUserUpdated += CurrentUserUpdated;
            client.Disconnected += Disconnected;
            client.GuildAvailable += Program.BotEvents.RaiseOnDiscordGuildAvailable;
            client.GuildMembersDownloaded += Program.BotEvents.RaiseOnDiscordMembersDownloaded;
            client.GuildMemberUpdated += GuildMemberUpdated;
            client.GuildUnavailable += (guild) => { Core.LOG(new LogMessage(LogSeverity.Warning, "EventCatcherDiscord", $"Discord guild \"{guild.Name}\" unavailable.")); return Task.CompletedTask; };
            //client.GuildUpdated += GuildUpdated;
            client.JoinedGuild += (guild) => { Core.LOG(new LogMessage(LogSeverity.Info, "EventCatcherDiscord", $"Joined Discord guild \"{guild.Name}\".")); return Task.CompletedTask; }; ;
            //client.LatencyUpdated += LatencyUpdated;
            //client.LeftGuild += LeftGuild;
            client.Log += LogAsync;
            //client.LoggedIn += LoggedIn;
            //client.LoggedOut += LoggedOut;
            //client.MessageDeleted += MessageDeleted;
            client.MessageReceived += HandleCommandAsync;
            //client.MessagesBulkDeleted += MessagesBulkDeleted;
            //client.MessageUpdated += MessageUpdated;
            //client.ReactionAdded += ReactionAdded;
            //client.ReactionRemoved += ReactionRemoved;
            //client.ReactionsCleared += ReactionsCleared;
            client.Ready += ReadyAsync;
            //client.RecipientAdded += RecipientAdded;
            //client.RecipientRemoved += RecipientRemoved;
            //client.RoleCreated += RoleCreated;
            //client.RoleDeleted += RoleDeleted;
            //client.RoleUpdated += RoleUpdated;
            //client.UserBanned += UserBanned;
            //client.UserIsTyping += UserIsTyping;
            client.UserJoined += UserJoined;
            client.UserLeft += UserLeft;
            //client.UserUnbanned += UserUnbanned;
            client.UserUpdated += Program.BotEvents.RaiseDiscordUserUpdated;
            //client.UserVoiceStateUpdated += UserVoiceStateUpdated;
            //client.VoiceServerUpdated += VoiceServerUpdated;
        }
        #region NEEDS WORK

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            SocketUserMessage msg = arg as SocketUserMessage;
            if (msg == null) return;

            UserEntry usr = await Program.Users.GetUserByDiscordID(msg.Author.Id);
            if (usr == null) return;

            Program.BotEvents.RaiseOnMessageReceived(new BotWideMessageArguments(){
                source = MESSAGESOURCE.DISCORD, 
                channel = msg.Channel.Id.ToString(),
                user = usr, 
                message = msg.Content
            });
            // TODO below

            // We don't want the bot to respond to itself or other bots.
            // NOTE: Selfbots should invert this first check and remove the second
            // as they should ONLY be allowed to respond to messages from the same account.
            if (msg.Author.Id == Program.DiscordClient.CurrentUser.Id || msg.Author.IsBot) return;

            // Create a number to track where the prefix ends and the command begins
            int pos = 0;
            // Replace the '!' with whatever character
            // you want to prefix your commands with.
            // Uncomment the second half if you also want
            // commands to be invoked by mentioning the bot instead.
            if (msg.HasCharPrefix(Program.CommandCharacter, ref pos) /* || msg.HasMentionPrefix(_client.CurrentUser, ref pos) */)
            {
                // Create a Command Context.
                var context = new SocketCommandContext(Program.DiscordClient, msg);

                // Execute the command. (result does not indicate a return value, 
                // rather an object stating if the command executed succesfully).
                //var result = await _commands.ExecuteAsync(context, pos, _services); // TODO Make your own command structure

                // Uncomment the following lines if you want the bot
                // to send a message if it failed (not advised for most situations).
                //if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                //    await msg.Channel.SendMessageAsync(result.ErrorReason);
            }
            return;
        }
        #endregion


        #region Passed first check. All here raise BotWide Events except the log grabber.
        /// <summary>
        /// When a guild memember gets updated we raise an event for each guild shared so plugins can listen for their specific guild
        /// </summary>
        /// <param name="current"></param>
        /// <param name="old"></param>
        /// <returns></returns>
        private async Task GuildMemberUpdated(SocketGuildUser current, SocketGuildUser old)
        {
            // TODO warning verify current and old is accurate
            foreach (SocketGuild guild in current.MutualGuilds)
            {
                BotChannel bChan = await Program.Channels.GetDiscordGuildbyID(guild.Id);
                UserEntry currentUser = await Program.Users.GetUserByDiscordID(current.Id);
                UserEntry oldUser = await Program.Users.GetUserByDiscordID(old.Id);
                Program.BotEvents.RaiseOnDiscordGuildMemberUpdated(bChan, currentUser, oldUser);
            }
        }
        /// <summary>
        /// New user joins a guild
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task UserJoined(SocketGuildUser arg)
        {
            UserEntry user = await Program.Users.GetUserByDiscordID(arg.Id);
            BotChannel bChan = await Program.Channels.GetDiscordGuildbyID(arg.Guild.Id);
            Program.BotEvents.RaiseOnDiscordNewMember(bChan, user);
        }
        /// <summary>
        /// User leaves a guild
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task UserLeft(SocketGuildUser arg)
        {
            UserEntry user = await Program.Users.GetUserByDiscordID(arg.Id);
            BotChannel bChan = await Program.Channels.GetDiscordGuildbyID(arg.Guild.Id);
            Program.BotEvents.RaiseOnDiscordMemberLeft(bChan, user);
        }
        /// <summary>
        /// This fires when Discord is fully connected
        /// </summary>
        /// <returns></returns>
        private async Task Connected()
        {
            await Task.Run(() =>
            {
                Program.BotEvents.RaiseOnDiscordConnected();
            });
        }
        /// <summary>
        /// When Discord client disconnects this fires.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static async Task Disconnected(Exception e)
        {
            await Program.BotEvents.RaiseOnDiscordDisconnected(e);
        }
        /// <summary>
        /// Fires when Discord client is ready.
        /// </summary>
        /// <returns></returns>
        private async Task ReadyAsync()
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, "EventCatcherDiscord", $"Connected to Discord as {Program.DiscordClient.CurrentUser}."));
            Program.BotEvents.RaiseOnDiscordReady();
        }
        /// <summary>
        /// This listens to the Discord log events. Doesn't raise a botwide event.! Just logs.
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        private async Task LogAsync(LogMessage log)
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, "EventCatcherDiscord", log.ToString()));
        }
        #endregion
    }// EOC
}//EON