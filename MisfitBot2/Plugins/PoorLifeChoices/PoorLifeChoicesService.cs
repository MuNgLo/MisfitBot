using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MisfitBot2.Plugins.PoorLifeChoices;
using Newtonsoft.Json;

namespace MisfitBot2.Services
{
    class PoorLifeChoicesService : ServiceBase, IService
    {
        private readonly string PLUGINNAME = "PoorLifeChoicesService";
        private Random _rng;
        private List<string> _choices = new List<string>();
        // CONSTRUCTOR
        public PoorLifeChoicesService()
        {
            _rng = new Random();
            Events.OnUserEntryMerge += OnUserEntryMergeEvent;
            Events.OnBotChannelMerge += OnBotChannelEntryMergeEvent;
            TimerStuff.OnSecondTick += OnSecondTick;
            TimerStuff.OnMinuteTick += OnMinuteTick;
            #region responses
            //_choices.Add("Subscribe to<relevant twitchchannel>.");
            _choices.Add("Stay up for another hour.");
            _choices.Add("Have another beer.");
            _choices.Add("Just one more.");
            _choices.Add("Stream for another hour. Just one more.");
            _choices.Add("Sleep ? Nah. Chug energy drinks");
            _choices.Add("Subscribe to Comcast");
            _choices.Add("Face Tattoos. Nuff Said.");
            _choices.Add("You should call your ex");
            _choices.Add("Shots!");
            _choices.Add("I bet you'd look good with a mullet");
            _choices.Add("Socks. Sandals. Sweatpants.");
            _choices.Add("Have you considered taking up smoking ?");
            _choices.Add("Multi - Level Marketing: Your ticket to financial freedom");
            #endregion
            Core.Twitch._client.OnChatCommandReceived += TwitchOnChatCommandRecieved;
        }// END of Constructor

        #region Twitch command methods 
        private async void TwitchOnChatCommandRecieved(object sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.Command.ChatMessage.Channel);
            switch (e.Command.CommandText.ToLower())
            {
                case "poorlifechoice":
                    await TwitchPoorLifeChoice(e);
                    break;
                case "plc":

                    if (e.Command.ArgumentsAsList.Count == 0)
                    {
                        await TwitchPoorLifeChoice(e);
                        break;
                    }
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        if (e.Command.ArgumentsAsList.Count == 1)
                        {
                            //TwitchLib.Api.V5.Models.Users.Users channels = await Core.Twitch._api.V5.Users.GetUserByNameAsync(Core.Twitch._client.GetJoinedChannel(e.Command.ChatMessage.Channel).Channel);
                            PoorLifeChoicesSettings settings = await Settings(bChan);
                            if (e.Command.ArgumentsAsList[0].ToLower() == "on")
                            {
                                if (e.Command.ArgumentsAsList.Count == 1)
                                {
                                    settings._active = true;
                                    SaveBaseSettings(PLUGINNAME, bChan, settings);
                                    Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, "PoorLifeChoices are now active in this channel.");
                                    return;
                                }
                            }
                            if (e.Command.ArgumentsAsList[0].ToLower() == "off")
                            {
                                if (e.Command.ArgumentsAsList.Count == 1)
                                {
                                    settings._active = false;
                                    SaveBaseSettings(PLUGINNAME, bChan, settings);
                                    Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, "PoorLifeChoices are now inactive in this channel.");
                                    return;
                                }
                            }
                            Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, "This command only takes the arguments \"on\" or \"off\"");
                            return;
                        }

                    }
                    break;
            }
        }

       

        public async Task TwitchPoorLifeChoice(TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.Command.ChatMessage.Channel);
            PoorLifeChoicesSettings settings = await Settings(bChan);
            if (settings._active == false) { return; }
            string picked = _choices[(_rng.Next(0, _choices.Count))];
            Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, picked);
        }
        #endregion

        #region Discord command methods 
        public async Task DiscordCommand(string[] args, ICommandContext context)
        {
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info,
                PLUGINNAME,
                $"{context.User.Username} used command \"plc\" in {context.Channel.Name}."
                ));
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            PoorLifeChoicesSettings settings = await Settings(bChan);
            switch (args[0].ToLower())
            {
                case "on":
                    await DiscordSetActive(true, context);
                    break;
                case "off":
                    await DiscordSetActive(false, context);
                    break;
                case "chan":
                    await SetDefaultDiscordChannel(bChan, context.Channel.Id);
                    break;
                case "chanreset":
                    await ClearDefaultDiscordChannel(bChan, context.Channel.Id);
                    break;
            }
        }
        #region Interface default discord command methods
        public async Task SetDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            if (bChan == null) { return; }
            PoorLifeChoicesSettings settings = await Settings(bChan);
            settings._defaultDiscordChannel = discordChannelID;
            await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync("This is now the active channel for the PLC plugin.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);
        }
        public async Task ClearDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            if (bChan == null) { return; }
            PoorLifeChoicesSettings settings = await Settings(bChan);
            settings._defaultDiscordChannel = 0;
            await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync("The active channel for the PLC plugin is resetted. All channels now valid.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);
        }
        #endregion


        public async Task DiscordPoorLifeChoice(ICommandContext Context)
        {
            PoorLifeChoicesSettings settings = await Settings(await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id));
            if (settings._defaultDiscordChannel != 0 && settings._defaultDiscordChannel != Context.Channel.Id)
            {
                return;
            }
            if (settings._active == false) { return; }
            string picked = _choices[(_rng.Next(0, _choices.Count))];
            await Context.Channel.SendMessageAsync(picked);
        }

        public async Task DiscordSetActive(bool flag, ICommandContext Context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            PoorLifeChoicesSettings settings = await Settings(bChan);
            settings._active = flag;
            SaveBaseSettings(PLUGINNAME, bChan, settings);
            if (settings._active)
            {
                await Context.Channel.SendMessageAsync($"PoorLifeChoices are now active in this channel ({settings._active})");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"PoorLifeChoices are now inactive in this channel ({settings._active})");
            }
        }
        #endregion



        #region Interface base methods
        public void OnSecondTick(int seconds)
        {

        }
        public void OnMinuteTick(int minutes)
        {

        }
        public void OnBotChannelEntryMergeEvent(BotChannel discordGuild, BotChannel twitchChannel)
        {

        }
        public void OnUserEntryMergeEvent(UserEntry discordUser, UserEntry twitchUser)
        {

        }
        #endregion




        private async Task<PoorLifeChoicesSettings> Settings(BotChannel bChan)
        {
            PoorLifeChoicesSettings settings = new PoorLifeChoicesSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as PoorLifeChoicesSettings;
        }
    }
}
