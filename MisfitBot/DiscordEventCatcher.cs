using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MisfitBot_MKII
{

    internal class DiscordEventCatcher
    {

        internal DiscordEventCatcher(DiscordSocketClient client)
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
            client.GuildUnavailable += (guild) => { Core.LOG(new LogMessage(LogSeverity.Warning, "Misfitbot", $"Discord guild \"{guild.Name}\" unavailable.")); return Task.CompletedTask; };
            //client.GuildUpdated += GuildUpdated;
            client.JoinedGuild += (guild) => { Core.LOG(new LogMessage(LogSeverity.Info, "Misfitbot", $"Joined Discord guild \"{guild.Name}\".")); return Task.CompletedTask; }; ;
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

        private async Task GuildMemberUpdated(SocketGuildUser arg1, SocketGuildUser arg2)
        {
            foreach (SocketGuild guild in arg1.MutualGuilds)
            {
                BotChannel bChan = await Program.Channels.GetDiscordGuildbyID(guild.Id);
                UserEntry userA = await Program.Users.GetUserByDiscordID(arg1.Id);
                UserEntry userB = await Program.Users.GetUserByDiscordID(arg2.Id);
                Program.BotEvents.RaiseOnDiscordGuildMemberUpdated(bChan, userA, userB);
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
        private Task Connected()
        {
            Console.WriteLine($"Discord Connected.");
            return Task.CompletedTask;
        }

        private Task HandleCommandAsync(SocketMessage arg)
        {
            //Console.WriteLine($":DW:[HandleCommandAsync]");
            // Bail out if it's a System Message.
            var msg = arg as SocketUserMessage;
            if (msg == null) return Task.CompletedTask;

            // We don't want the bot to respond to itself or other bots.
            // NOTE: Selfbots should invert this first check and remove the second
            // as they should ONLY be allowed to respond to messages from the same account.
            if (msg.Author.Id == Program.DiscordClient.CurrentUser.Id || msg.Author.IsBot) return Task.CompletedTask;

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
            return Task.CompletedTask;
        }
        private static async Task Disconnected(Exception arg)
        {
            await Core.LOG(new LogMessage(LogSeverity.Error, "DiscordNET", $"ClientDisconnected:{arg.Message}"));
        }
        private Task ReadyAsync()
        {
            Core.LOG(new LogMessage(LogSeverity.Info, "Program", $"Connected to Discord as {Program.DiscordClient.CurrentUser}."));
            Program.BotEvents.RaiseOnDiscordReady();
            return Task.CompletedTask;
        }
        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}