using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MisfitBot_MKII.Statics;

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
            client.GuildUnavailable += (guild) => { Core.LOG(new LogEntry(LOGSEVERITY.WARNING, "EventCatcherDiscord", $"Discord guild \"{guild.Name}\" unavailable.")); return Task.CompletedTask; };
            //client.GuildUpdated += GuildUpdated;
            client.JoinedGuild += (guild) => { Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EventCatcherDiscord", $"Joined Discord guild \"{guild.Name}\".")); return Task.CompletedTask; }; ;
            //client.LatencyUpdated += LatencyUpdated;
            //client.LeftGuild += LeftGuild;
            client.Log += LogAsync;
            //client.LoggedIn += LoggedIn;
            //client.LoggedOut += LoggedOut;
            //client.MessageDeleted += MessageDeleted;
            client.MessageReceived += HandleCommandAsync;
            //client.MessagesBulkDeleted += MessagesBulkDeleted;
            //client.MessageUpdated += MessageUpdated;
            client.ReactionAdded += ReactionAdded;
            client.ReactionRemoved += ReactionRemoved;
            client.ReactionsCleared += ReactionsCleared;
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

        private async Task ReactionsCleared(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EventCatcherDiscord", "ReactionCleared"));
            BotChannel bChan = await Program.Channels.GetDiscordGuildbyID((arg2 as SocketGuildChannel).Guild.Id);
            Program.BotEvents.RaiseDiscordReactionCleared(
                bChan,
                arg2.Id
            );
        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EventCatcherDiscord", "ReactionRemoved"));
            UserEntry user = await Program.Users.GetUserByDiscordID(arg3.UserId);
            BotChannel bChan = await Program.Channels.GetDiscordGuildbyID((arg2 as SocketGuildChannel).Guild.Id);
            Program.BotEvents.RaiseDiscordReactionRemoved(
                bChan,
                user,
                new DiscordReactionArgument(arg2.Id, arg3.MessageId, RESPONSEACTION.REMOVED, arg3.Emote.Name)
            );
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EventCatcherDiscord", "ReactionAdded"));
            UserEntry user = await Program.Users.GetUserByDiscordID(arg3.UserId);
            BotChannel bChan = await Program.Channels.GetDiscordGuildbyID((arg2 as SocketGuildChannel).Guild.Id);


            


            Program.BotEvents.RaiseDiscordReactionAdded(
                bChan,
                user,
                new DiscordReactionArgument(arg2.Id, arg3.MessageId, RESPONSEACTION.REMOVED, arg3.Emote.Name)
            );
        }
        #region NEEDS WORK









        private async Task HandleCommandAsync(SocketMessage arg)
        {
            SocketUserMessage msg = arg as SocketUserMessage;
            if (msg == null) return;
            if (msg.Content == string.Empty) return;
            if (msg.Author.Id == Program.DiscordClient.CurrentUser.Id || msg.Author.IsBot) return;

            UserEntry usr = await Program.Users.GetUserByDiscordID(msg.Author.Id);
            if (usr == null) return;

            // Create a command context
            SocketCommandContext context = new SocketCommandContext(Program.DiscordClient, msg);


            // Create permissions list
            ChannelPermissions asd = (context.User as SocketGuildUser).GetPermissions(arg.Channel as IGuildChannel);



            if (msg.Content[0] == Program.CommandCharacter)
            {
                List<string> args = new List<string>();
                args.AddRange(msg.Content.Split(' '));
                string cmd = args[0];
                cmd = cmd.Remove(0, 1);
                args.RemoveAt(0);
                Program.BotEvents.RaiseOnCommandRecieved(new BotWideCommandArguments()
                {
                    source = MESSAGESOURCE.DISCORD,
                    channel = msg.Channel.Id.ToString(),
                    guildID = context.Guild.Id,
                    messageID = context.Message.Id,
                    isBroadcaster = false,
                    isModerator = false,
                    canManageMessages = asd.ManageMessages,
                    user = usr,
                    userDisplayName = arg.Author.Username,
                    command = cmd.ToLower(),
                    message = msg.Content,
                    arguments = args
                });


            }
            else
            {
                Program.BotEvents.RaiseOnMessageReceived(new BotWideMessageArguments()
                {
                    source = MESSAGESOURCE.DISCORD,
                    channel = msg.Channel.Id.ToString(),
                    guildID = context.Guild.Id, 
                    isBroadcaster = false,
                    isModerator = false,
                    canManageMessages = asd.ManageMessages,
                    user = usr,
                    userDisplayName = arg.Author.Username,
                    message = msg.Content
                });
            }
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
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EventCatcherDiscord", $"Connected to Discord as {Program.DiscordClient.CurrentUser}."));
            Program.BotEvents.RaiseOnDiscordReady();
        }
        /// <summary>
        /// This listens to the Discord log events. Doesn't raise a botwide event.! Just logs.
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        private async Task LogAsync(LogMessage log)
        {
            if (Program.Debugmode)
            {
                await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "EventCatcherDiscord", log.ToString()));
            }
        }
        #endregion
    }// EOC
}//EON