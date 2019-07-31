using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MisfitBot2.Plugins.Admin;
using Newtonsoft.Json;

namespace MisfitBot2.Services
{
    class AdminService : ServiceBase, IService
    {
        readonly string PLUGINNAME = "Admin";
        // CONSTRUCTOR
        public AdminService()
        {
            Core.OnBanEvent += OnBanEvent;
            Core.OnBitEvent += OnBitEvent;
            Core.OnBotChannelGoesLive += OnChannelGoesLive;
            Core.OnBotChannelGoesOffline += OnChannelGoesOffline;
            Core.OnHostEvent += OnHostEvent;
            Core.OnNewDiscordMember += OnNewDiscordMember;
            Core.OnUnBanEvent += OnUnBanEvent;
            Core.OnTwitchSubEvent += OnTwitchSub;
            Core.OnUserEntryMerge += OnUserEntryMerge;
        }

        private async void OnTwitchSub(BotChannel bChan, TwitchSubEventArguments e)
        {
            string message = string.Empty;
            switch (e.subContext)
            {
                case TWSUBCONTEXT.SUB:
                    message = $"{e.userDisplayname} subscribed. ({e.subscriptionplan.ToString()})";
                    break;
                case TWSUBCONTEXT.RESUB:
                    message = $"{e.userDisplayname} resubscribed for {e.months} months. ({e.subscriptionplan.ToString()})";
                    break;
                case TWSUBCONTEXT.GIFTSUB:
                    message = $"{e.userDisplayname} gifted {e.recipientDisplayname} asub. {e.recipientDisplayname} subscribed for {e.months} months. ({e.subscriptionplan.ToString()})";
                    break;
            }
            if (bChan.discordAdminChannel != 0 && message != string.Empty)
            {
            await SayOnDiscordAdmin(bChan, message);
            }
        }

        private async void OnHostEvent(BotChannel bChan, HostEventArguments e)
        {
            await SayOnDiscordAdmin(bChan, $"{e.Moderator} hosted {e.Hostchannel} https://www.twitch.tv/{e.Hostchannel}");
        }

        private async void OnChannelGoesLive(BotChannel bChan, int delay)
        {
            if (bChan.discordAdminChannel != 0)
            {
                await (Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                    $"{bChan.TwitchChannelName} went live. {delay}s delay."
                    );
            }
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, PLUGINNAME,
               $"{bChan.TwitchChannelName} went live. {delay}s delay."
                ));
        }

        private async void OnChannelGoesOffline(BotChannel bChan)
        {
            if (bChan.discordAdminChannel != 0)
            {
                // This has caused 1 error ???
                await(Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                    $"{bChan.TwitchChannelName} went offline."
                    );
                return;
            }
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, PLUGINNAME,
                $"{bChan.TwitchChannelName} went offline."
                ));
        }

        private async void OnBitEvent(BitEventArguments e)
        {
            await SayOnDiscordAdmin(e.bChan,
                $"{e.user._twitchDisplayname} just cheered with {e.bitsGiven} bits with a total of {e.bitsTotal} bits."
                );
        }
        private async void OnNewDiscordMember(BotChannel bChan, UserEntry user)
        {
            await SayOnDiscordAdmin(bChan, $"{user._username} just joined the Discord.");
        }

        #region Discord Command Methods
        public async Task DiscordCommand(ICommandContext Context, List<string> args)
        {
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info,
                PLUGINNAME,
                $"{Context.User.Username} used command \"admin\" in {Context.Channel.Name}."
                ));
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            AdminSettings settings = await Settings(bChan);
            switch (args[0].ToLower())
            {
                case "on":
                    await DiscordSetActive(true, Context);
                    break;
                case "off":
                    await DiscordSetActive(false, Context);
                    break;
                case "user":
                    if(Context.Channel.Id != bChan.discordAdminChannel || settings._active == false)
                    {
                        await Context.Channel.SendMessageAsync($"Make sure the Admin plugin is active and has a adminchannel set.");
                        return;
                    }
                    if(args.Count < 2)
                    {
                        await Context.Channel.SendMessageAsync($"You need to give a user related command.");
                        return;
                    }
                    switch (args[1].ToLower()){
                        case "search":
                            if(args.Count < 3)
                            {
                                await Context.Channel.SendMessageAsync($"Nothing to search for. The search has to be at least 2 characters long.");
                                return;
                            }
                            if(args[2].Length < 2)
                            {
                                await Context.Channel.SendMessageAsync($"The search has to be at least 2 characters long.");
                                return;
                            }

                            await UserSearch(bChan, args[2]);
                            break;
                    }


                    break;
            }
        }

        

        public async Task DiscordLinkChannelCommand(ICommandContext context, string twitchChannelName)
        {
            List<BotChannel> channels = await Core.Channels.GetChannels();
            if (!channels.Exists(p => p.GuildID == context.Guild.Id) && !channels.Exists(p => p.TwitchChannelName == twitchChannelName))
            {
                return;
            }
            BotChannel discordProfile = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (discordProfile.isLinked || (await Core.Channels.GetTwitchChannelByName(twitchChannelName)).isLinked)
            {
                return;
            }
            BotChannel linkedProfile = new BotChannel(context.Guild.Id,  context.Guild.Name);
            BotChannel twitchProfile = await Core.Channels.GetTwitchChannelByName(twitchChannelName);
            linkedProfile.isLinked = true;
            linkedProfile.TwitchChannelID = twitchProfile.TwitchChannelID;
            linkedProfile.TwitchChannelName = twitchProfile.TwitchChannelName;
            linkedProfile.TwitchAutojoin = twitchProfile.TwitchAutojoin;
            linkedProfile.isTwitch = twitchProfile.isTwitch;
            linkedProfile.isLive = twitchProfile.isLive;
            bool result = await Core.Channels.SaveAsLinked(linkedProfile);
            // Trigger OnLinkingChannel event if channel was added
            if (result)
            {
                Core.Channels.OnBotChannelMerge?.Invoke(discordProfile, twitchProfile);
                await Core.LOG(new LogMessage(LogSeverity.Info, PLUGINNAME, $"Linking Discord Guild {linkedProfile.GuildID} to Twitch channel {linkedProfile.TwitchChannelName}."));
                await context.Message.Channel.SendMessageAsync($"This Discord guild linked to Twitchchannel \"{linkedProfile.TwitchChannelName}\" ");
                Core.Channels.ChannelSave(linkedProfile);
            }else
            {
                await Core.LOG(new LogMessage(LogSeverity.Info, PLUGINNAME, $"Failed to link Discord Guild {linkedProfile.GuildID} to Twitch channel {linkedProfile.TwitchChannelName}."));
                await context.Message.Channel.SendMessageAsync($"Failed to link Discord Guild to Twitchchannel \"{linkedProfile.TwitchChannelName}\" ");
            }
            return;
        }

        

        public async Task SetAdminChannel(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null){return;}
            bChan.discordAdminChannel = context.Channel.Id;
            Core.Channels.ChannelSave(bChan);
            await SayOnDiscordAdmin(bChan, "This is now the dedicated admin command channel. Admin commands are accepted in this channel only.");
        }
        public async Task ResetAdminChannel(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            bChan.discordAdminChannel = 0;
            Core.Channels.ChannelSave(bChan);
            await SayOnDiscord("Admin channel is cleared. This should really be set. Remember not to make it a public channel.", context.Channel.Id);
        }
        public async Task SetDefaultChannel(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            bChan.discordDefaultBotChannel = context.Channel.Id;
            Core.Channels.ChannelSave(bChan);
            await SayOnDiscord(bChan, "This is now the default channel for the bot. A lot of general purpose messages will be sent here.");

        }
        public async Task ResetDefaultBotChannel(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            bChan.discordDefaultBotChannel = 0;
            Core.Channels.ChannelSave(bChan);
            await SayOnDiscord("Default channel is cleared. This means a lot of general messages will not show up.", context.Channel.Id);
        }
        public async Task DiscordJoinTwitchChannel(ICommandContext context, string twitchChannelName)
        {
            if(await Core.Channels.JoinTwitchChannel(twitchChannelName))
            {
                await context.Message.Channel.SendMessageAsync($"Joining Twitch channel {twitchChannelName}.");
            }
            else
            {
                await context.Message.Channel.SendMessageAsync($"Failed to join the Twitch channel {twitchChannelName}. Check spelling.");
            }
        }
        #endregion

        #region AdminService specific methods
        public async Task DiscordSetPubSubOauth(ICommandContext Context, string encryptedoauth)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            bChan.pubsubOauth = encryptedoauth;
            Core.Channels.ChannelSave(bChan);
            await Context.Channel.SendMessageAsync("PubSub OAUTH key has been updated. Use \"!pubsub start\" to launch it.");
        }
        public async Task DiscordAdminInfo(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            AdminSettings settings = await Settings(bChan);
            string message = string.Empty;
            if (settings._active)
            {
                message += "Admin module active.";
            }
            else
            {
                message += "Admin module inactive.";
            }
            await (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(message);
        }
        public async Task DiscordSetActive(bool flag, ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            AdminSettings settings = await Settings(bChan);
            settings._active = flag;
            SaveSettings(PLUGINNAME, bChan, settings);
            if (settings._active)
            {
                await context.Channel.SendMessageAsync($"Admin module are now active.");
            }
            else
            {
                await context.Channel.SendMessageAsync($"Admin module are now inactive.");
            }
        }
        private async void OnUnBanEvent(UnBanEventArguments e)
        {
            AdminSettings settings = await Settings(e.bChan);
            if (e.isDiscord)
            {
                if (e.bannedUser._discordUID != 0)
                {
                    if (settings._bannedDiscordIDs.Contains(e.bannedUser._discordUID))
                    {
                        settings._bannedDiscordIDs.RemoveAll(p=>p == e.bannedUser._discordUID);
                        await SayOnDiscordAdmin(
                            e.bChan,
                            $"{e.enforcingUser._username} unbanned {e.bannedUser._username}."
                            );
                    }

                }
            }
            else
            {
                if (e.bannedUser._twitchUID != string.Empty)
                {
                    if (settings._bannedTwitchIDs.Contains(e.bannedUser._twitchUID))
                    {
                        settings._bannedTwitchIDs.RemoveAll(p=>p == e.bannedUser._twitchUID);

                        if (e.enforcingUser == null)
                        {
                            await SayOnDiscordAdmin(
                                e.bChan,
                                $"{e.bannedUser._twitchDisplayname} was unbanned on Twitch."
                                );
                        }
                        else
                        {
                            await SayOnDiscordAdmin(
                                e.bChan,
                                $"{e.enforcingUser?._twitchDisplayname} unbanned {e.bannedUser._twitchDisplayname} on Twitch."
                                );
                        }
                    }


                }
            }
            Core.Configs.UpdateConfig(e.bChan, PLUGINNAME, settings);
        }
        private async void OnBanEvent(BanEventArguments e)
        {
            AdminSettings settings = await Settings(e.bChan);
            if (e.isDiscord)
            {
                if (e.bannedUser._discordUID != 0)
                {
                    if (!settings._bannedDiscordIDs.Contains(e.bannedUser._discordUID))
                    {
                        settings._bannedDiscordIDs.Add(e.bannedUser._discordUID);

                        // This should be a setting to sync bans or not
                        /*if (!settings._bannedTwitchIDs.Contains(e.bannedUser._twitchUID))
                        {
                            settings._bannedTwitchIDs.Add(e.bannedUser._twitchUID);
                        }*/
                        await SayOnDiscordAdmin(
                            e.bChan,
                            $"{e.enforcingUser._username} banned {e.bannedUser._username} for {e.duration}. \"{e.reason}\"."
                            );
                    }

                }
            }
            else
            {
                if (e.bannedUser._twitchUID != string.Empty)
                {
                    // This should be a setting to sync bans or not
                    /*if (!settings._bannedDiscordIDs.Contains(e.bannedUser._discordID))
                    {
                        settings._bannedDiscordIDs.Add(e.bannedUser._discordID);
                    }*/
                    if (!settings._bannedTwitchIDs.Contains(e.bannedUser._twitchUID))
                    {
                        settings._bannedTwitchIDs.Add(e.bannedUser._twitchUID);
                        
                        if (e.enforcingUser == null)
                        {
                            await SayOnDiscordAdmin(
                                e.bChan,
                                $"{e.bannedUser._twitchDisplayname} was banned on Twitch."
                                );
                        }
                        else
                        {
                            await SayOnDiscordAdmin(
                                e.bChan,
                                $"{e.enforcingUser?._twitchDisplayname} banned {e.bannedUser._twitchDisplayname}. \"{e.reason}\" on Twitch."
                                );
                        }
                    }


                }
            }
        }

        private async Task UserSearch(BotChannel bChan, string search)
        {
            List<UserEntry> matches = await Core.UserMan.SearchUserName(search);

            string msg = "```" + Environment.NewLine+
                $"Search results.  {matches.Count} hits.{Environment.NewLine}{Environment.NewLine}";

            msg += String.Format("{0,15}", "D-Username");
            msg += "    ";
            msg += String.Format("{0,15}", "Tw-Username");
            msg += "    ";
            msg += String.Format("{0,15}", "Tw-Displayname");
            msg += Environment.NewLine;

            foreach (UserEntry user in matches)
            {
                msg += String.Format("{0,15}", user._username);
                msg += "    ";
                msg += String.Format("{0,15}", user._twitchUsername);
                msg += "    ";
                msg += String.Format("{0,15}", user._twitchDisplayname);
                msg += Environment.NewLine;
                if(msg.Length > 1500)
                {
                    msg += "More hits than shown. Try a more precise search.";
                    msg += Environment.NewLine;
                    break;
                }
            }
            msg += "```";

            await SayOnDiscordAdmin(bChan, msg);
        }

        #region DATA manipulation stuff
        /// <summary>
        /// Get access to the data related to the botchannel
        /// </summary>
        /// <param name="bChan"></param>
        /// <returns></returns>
        private async Task<AdminSettings> Settings(BotChannel bChan)
        {
            AdminSettings settings = new AdminSettings();
            if (!await TableExists(PLUGINNAME))
            {
                TableCreate(PLUGINNAME);
            }
            if (!await RowExists(PLUGINNAME, bChan.Key))
            {
                RowCreate(PLUGINNAME, bChan.Key, settings);
            }
            return await RowRead(PLUGINNAME, bChan.Key);
        }
        /// <summary>
        /// This overrides the base table creation so we can do some magic stuff
        /// </summary>
        /// <param name="plugin"></param>
        new public void TableCreate(string plugin)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"CREATE TABLE {plugin} (" +
                    $"configKey VACHAR(30)," +
                    $"active BOOLEAN, " +
                    $"defaultCooldown INTEGER, " +
                    $"defaultDiscordChannel INTEGER, " +
                    $"defaultTwitchRoom VACHAR(30)" +
                    $")";
                cmd.ExecuteNonQuery();
            }
        }
        /// <summary>
        /// Our custum row reader for the custom table
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<AdminSettings> RowRead(string plugin, string key)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"SELECT * FROM {plugin} WHERE configKey IS @key";
                cmd.Parameters.AddWithValue("@key", key);
                SQLiteDataReader result;
                try
                {
                    result = cmd.ExecuteReader();
                }
                catch (Exception)
                {
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, plugin, $"Database query failed hard. ({cmd.CommandText})"));
                    throw;
                }
                result.Read();
                AdminSettings settings = new AdminSettings();
                settings._active = result.GetBoolean(1);
                settings._defaultCooldown = result.GetInt32(2);
                settings._defaultDiscordChannel = (ulong)result.GetInt64(3);
                settings._defaultTwitchRoom = result.GetString(4);
                return settings;
            }
        }
        /// <summary>
        /// Creates a valid row in our custom table
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="key"></param>
        /// <param name="settings"></param>
        public async void RowCreate(String plugin, String key, AdminSettings settings)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"INSERT INTO {plugin} VALUES (" +
                    $"@key, " +
                    $"@active, " +
                    $"@defaultCooldown, " +
                    $"@defaultDiscordChannel, " +
                    $"@defaultTwitchRoom" +
                    $")";
                cmd.Parameters.AddWithValue("@key", key);
                cmd.Parameters.AddWithValue("@active", settings._active);
                cmd.Parameters.AddWithValue("@defaultCooldown", settings._defaultCooldown);
                cmd.Parameters.AddWithValue("@defaultDiscordChannel", settings._defaultDiscordChannel);
                cmd.Parameters.AddWithValue("@defaultTwitchRoom", settings._defaultTwitchRoom);
                cmd.ExecuteNonQuery();
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, plugin, $"Created config entry ({plugin}::{key}) in DB."));
            }
        }
        /// <summary>
        /// This overrides the base table creation so we can do some magic stuff
        /// </summary>
        /// <param name="plugin"></param>
        public async void SaveSettings(string plugin, BotChannel bChan, AdminSettings settings)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = Core.Data;
                cmd.CommandText = $"UPDATE {plugin} SET " +
                    $"active = @active, " +
                    $"defaultCooldown = @defaultCooldown, " +
                    $"defaultDiscordChannel = @defaultDiscordChannel, " +
                    $"defaultTwitchRoom = @defaultTwitchRoom " +
                    $" WHERE configKey is @key";
                cmd.Parameters.AddWithValue("@active", settings._active);
                cmd.Parameters.AddWithValue("@defaultCooldown", settings._defaultCooldown);
                cmd.Parameters.AddWithValue("@defaultDiscordChannel", settings._defaultDiscordChannel);
                cmd.Parameters.AddWithValue("@defaultTwitchRoom", settings._defaultTwitchRoom);
                cmd.Parameters.AddWithValue("@key", bChan.Key);
                cmd.ExecuteNonQuery();
                await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Warning, plugin, $"Saved updated config for ({plugin}::{bChan.Key}) in DB."));
            }
        }
        #endregion

        #endregion
        #region Interface compliance
        public Task ClearDefaultDiscordChannel(BotChannel bChan, ulong guildID)
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
        public void OnBotChannelEntryMerge(BotChannel discordGuild, BotChannel twitchChannel)
        {
            throw new NotImplementedException();
        }
        public void OnMinuteTick(int minutes)
        {
            throw new NotImplementedException();
        }
        public void OnSecondTick(int seconds)
        {
            throw new NotImplementedException();
        }
        public void OnUserEntryMerge(UserEntry discordUser, UserEntry twitchUser)
        {
            
        }
        public Task SetDefaultDiscordChannel(BotChannel bChan, ulong guildID)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
