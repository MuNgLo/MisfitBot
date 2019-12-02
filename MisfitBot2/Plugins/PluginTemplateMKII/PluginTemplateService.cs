using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using MisfitBot2.Plugins.PluginTemplate;

namespace MisfitBot2.Services
{
    class PluginTemplateService : ServiceBaseMKII, IService
    {
        private readonly string PLUGINNAME = "PluginTemplateService";
        // CONSTRUCTOR
        public PluginTemplateService() : base("PluginTemplateService")
        {

        }// END of Constructor
        #region Twitch command methods
        #endregion
        #region Discord command methods
        public async Task SetDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            PluginTemplateSettings settings = (await Settings(bChan)) as PluginTemplateSettings;
            settings._defaultDiscordChannel = discordChannelID;
            await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync("This is now the active channel for the PLC plugin.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);

        }
        public async Task ClearDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            PluginTemplateSettings settings = (await Settings(bChan)) as PluginTemplateSettings;
            settings._defaultDiscordChannel = 0;
            await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync("The active channel for the PLC plugin is resetted. All channels now valid.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);

        }
        #endregion
        #region Abstracted methods that needs to be implemented. Often with custom code.
        override public async Task<Object> Settings(BotChannel bChan)
        {
            PluginTemplateSettings settings = new PluginTemplateSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as PluginTemplateSettings;
        }
        /// <summary>
        /// This should be called from the constructor and register all relevant listeners
        /// </summary>
        public override void RegisterEventListeners()
        {
            // At the least these should be hooked up
            TimerStuff.OnSecondTick += OnSecondTick;
            TimerStuff.OnMinuteTick += OnMinuteTick;
            Events.OnBotChannelMerge += OnBotChannelEntryMergeEvent;
            Events.OnUserEntryMerge += OnUserEntryMergeEvent;
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
        public void OnBotChannelEntryMergeEvent(BotChannel discordGuild, BotChannel twitchChannel)
        {
            throw new NotImplementedException();
        }
        public void OnUserEntryMergeEvent(UserEntry discordUser, UserEntry twitchUser)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
