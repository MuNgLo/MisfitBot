using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MisfitBot2.Extensions.ChannelManager;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using MisfitBot2.Plugins.DeathCounter;

namespace MisfitBot2.Services
{
    /// <summary>
    /// V1.0 Working as intended
    /// </summary>
    public class DeathCounterService : ServiceBase, IService
    {
        public readonly string PLUGINNAME = "DeathCounter";
        private Dictionary<string, DeathCount> _deathCounters = new Dictionary<string, DeathCount>();
        //private float _ts_twitch_lastResponse = -10.0f;
        //private float _ts_discord_lastResponse = -10.0f;
        public DeathCounterService()
        {
            Core.Twitch._client.OnChatCommandReceived += TWITCH_OnChatCommandReceived;
        }

        #region Handle Twitch Commands
        private async void TWITCH_OnChatCommandReceived(object sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.Command.ChatMessage.Channel);
            if (bChan == null) { return; }

            switch (e.Command.CommandText.ToLower())
            {
                case "dc_start":
                    if (!_deathCounters.ContainsKey(e.Command.ChatMessage.Channel))
                    {
                        if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                        {
                            await StartDeathCounter(bChan);
                        }
                    }
                    break;
                case "dc_stop":
                    if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                    {
                        if (_deathCounters.ContainsKey(e.Command.ChatMessage.Channel))
                        {
                            await StopDeathCounter(bChan);
                        }
                    }
                    break;
                case "dc_reset":
                    if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                    {
                        if (_deathCounters.ContainsKey(e.Command.ChatMessage.Channel))
                        {
                            await ResetDeathcCounter(bChan);
                        }
                    }
                    break;
                case "add":
                    if (_deathCounters.ContainsKey(e.Command.ChatMessage.Channel))
                    {
                        if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                        {
                            await AddToDeathCounter(bChan);
                        }
                    }
                    break;
                case "del":
                    if (_deathCounters.ContainsKey(e.Command.ChatMessage.Channel))
                    {
                        if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                        {
                            await DelFromDeathCounter(bChan);
                        }
                    }
                    break;
                case "deaths":

                    if (bChan == null)
                    {
                        return;
                    }
                    if (_deathCounters.ContainsKey(bChan.TwitchChannelName))
                    {
                        Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, $"{_deathCounters[bChan.TwitchChannelName]._deaths} Deaths.");
                    }

                    break;
            }
        }
        #endregion

        #region Handling discord commands
        public async Task SetDefaultDiscordChannel(ICommandContext Context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            DeathCounterSettings settings = await Core.Configs.GetConfig(bChan, PLUGINNAME, new DeathCounterSettings()) as DeathCounterSettings;
            if (settings._defaultDiscordChannel != Context.Channel.Id)
            {
                settings._defaultDiscordChannel = Context.Channel.Id;
                Core.Configs.UpdateConfig(bChan, PLUGINNAME, settings);
                await Context.Message.Channel.SendMessageAsync($"This channel is now the default channel for the deathcounter module.");
                return;
            }
            await Context.Message.Channel.SendMessageAsync($"This channel is already the default channel for the deathcounter module.");
        }
        public async Task ClearDefaultDiscordChannel(ICommandContext Context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            DeathCounterSettings settings = await Core.Configs.GetConfig(bChan, PLUGINNAME, new DeathCounterSettings()) as DeathCounterSettings;
            if (settings._defaultDiscordChannel != 0)
            {
                settings._defaultDiscordChannel = 0;
                Core.Configs.UpdateConfig(bChan, PLUGINNAME, settings);
                await Context.Message.Channel.SendMessageAsync($"Resseting the default channel for the deathcounter module.");
            }
            await Context.Message.Channel.SendMessageAsync($"There was no default channel for the deathcounter module to reset.");
        }
        public async Task StartCounter(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            if (!_deathCounters.ContainsKey(bChan.TwitchChannelName))
            {
                await StartDeathCounter(bChan, context.Channel.Id);
            }

        }
        public async Task StopCounter(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            await StopDeathCounter(bChan);
        }
        public async Task ResetCounter(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            await ResetDeathcCounter(bChan);
        }
        public async Task AddCounter(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            await AddToDeathCounter(bChan);
        }
        public async Task DelCounter(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            await DelFromDeathCounter(bChan);
        }
        public async Task Deaths(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan==null)
            {
                return;
            }
            if(context.Channel.Id != _deathCounters[bChan.TwitchChannelName]._discordChannel)
            {
                return;
            }

            if (_deathCounters.ContainsKey(bChan.TwitchChannelName))
            {
                await (Core.Discord.GetChannel(_deathCounters[bChan.TwitchChannelName]._discordChannel) as ISocketMessageChannel).SendMessageAsync($"{_deathCounters[bChan.TwitchChannelName]._deaths} Deaths.");
            }
        }
        #endregion

        #region DeathCounter specific methods
        private async Task<DeathCounterSettings> Settings(BotChannel bChan)
        {
            DeathCounterSettings settings = new DeathCounterSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as DeathCounterSettings;
        }

        public async Task DiscordSetActive(bool flag, ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            DeathCounterSettings settings = await Settings(bChan);
            if (settings._active == flag) { return; }
            settings._active = flag;
            SaveBaseSettings(PLUGINNAME, bChan, settings);
            if (settings._active)
            {
                await context.Channel.SendMessageAsync($"DeathCounter module are now active.");
            }
            else
            {
                await context.Channel.SendMessageAsync($"DeathCounter module are now inactive.");
            }
        }
        #endregion

        #region Backbone methods
        private async Task StartDeathCounter(BotChannel bChan, ulong channel=0)
        {
            if (!bChan.isLinked)
            {
                return;
            }
            DeathCounterSettings settings = await Settings(bChan);
            ulong channelForCounter = 0;


            if (settings._defaultDiscordChannel != 0) {
                channelForCounter = settings._defaultDiscordChannel;
            }else if(channelForCounter == 0 && bChan.discordDefaultBotChannel != 0)
            {
                channelForCounter = bChan.discordDefaultBotChannel;
            }
            else if(channel!=0)
            {
                channelForCounter = channel;
            }
            if(channelForCounter == 0)
            {
                await Core.LOG(new LogMessage(LogSeverity.Warning, PLUGINNAME, $"Deathcounter couldn't resolve a Discord channel to use."));
                return;
            }
            _deathCounters[bChan.TwitchChannelName] = new DeathCount(bChan.GuildID, channelForCounter);
            await Core.LOG(new LogMessage(LogSeverity.Info, PLUGINNAME, $"Deathcounter started for {bChan.TwitchChannelName}"));
            Core.Twitch._client.SendMessage(bChan.TwitchChannelName, "Death counter started. Good Luck BloodTrail");
            await (Core.Discord.GetChannel(channelForCounter) as ISocketMessageChannel).SendMessageAsync("Death counter started. Good Luck!");
        }
        private async Task StopDeathCounter(BotChannel bChan)
        {


            if (_deathCounters.ContainsKey(bChan.TwitchChannelName))
            {
                string msg = $"Death counter stopped. It stopped with a total of {_deathCounters[bChan.TwitchChannelName]._deaths} deaths.";
                Core.Twitch._client.SendMessage(bChan.TwitchChannelName, msg);
                await (Core.Discord.GetChannel(_deathCounters[bChan.TwitchChannelName]._discordChannel) as ISocketMessageChannel).SendMessageAsync(msg);
                _deathCounters.Remove(bChan.TwitchChannelName);
            }

        }
        private async Task ResetDeathcCounter(BotChannel bChan)
        {

            if (_deathCounters.ContainsKey(bChan.TwitchChannelName))
            {
                string msg = "Death counter reset.";
                Core.Twitch._client.SendMessage(bChan.TwitchChannelName, msg);
                await (Core.Discord.GetChannel(_deathCounters[bChan.TwitchChannelName]._discordChannel) as ISocketMessageChannel).SendMessageAsync(msg);
                DeathCount count = _deathCounters[bChan.TwitchChannelName];
                count._deaths = 0;
                _deathCounters[bChan.TwitchChannelName] = count;
            }

        }
        private async Task AddToDeathCounter(BotChannel bChan)
        {
            if (_deathCounters.ContainsKey(bChan.TwitchChannelName))
            {
                DeathCount count = _deathCounters[bChan.TwitchChannelName];
                count._deaths++;
                _deathCounters[bChan.TwitchChannelName] = count;
                string msg = $"Death added. Total is {_deathCounters[bChan.TwitchChannelName]._deaths}";
                Core.Twitch._client.SendMessage(bChan.TwitchChannelName, msg);

                await (Core.Discord.GetChannel(count._discordChannel) as ISocketMessageChannel).SendMessageAsync(msg);
            }

        }
        private async Task DelFromDeathCounter(BotChannel bChan)
        {

            if (_deathCounters.ContainsKey(bChan.TwitchChannelName))
            {
                if (_deathCounters[bChan.TwitchChannelName]._deaths < 1)
                {
                    return;
                }
                DeathCount count =  _deathCounters[bChan.TwitchChannelName];
                count._deaths--;
                _deathCounters[bChan.TwitchChannelName] = count;
                string msg = $"Death subtracted. Total is {_deathCounters[bChan.TwitchChannelName]._deaths}";
                Core.Twitch._client.SendMessage(bChan.TwitchChannelName, msg);
                await (Core.Discord.GetChannel(_deathCounters[bChan.TwitchChannelName]._discordChannel) as ISocketMessageChannel).SendMessageAsync(msg);
            }

        }
        #endregion

        #region Interface methods
        public void OnSecondTick(int seconds)
        {
            
        }

        public void OnMinuteTick(int minutes)
        {
            
        }

        public void OnUserEntryMerge(UserEntry discordUser, UserEntry twitchUser)
        {
            
        }

        public void OnBotChannelEntryMerge(BotChannel discordGuild, BotChannel twitchChannel)
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

        public Task SetDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            throw new NotImplementedException();
        }

        public Task ClearDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            throw new NotImplementedException();
        }



        #endregion

    }// END OF DeathCounterService class
}
