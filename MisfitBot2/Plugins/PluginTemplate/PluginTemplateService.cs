using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using MisfitBot2.Plugins.PluginTemplate;

namespace MisfitBot2.Services
{
    class PluginTemplateService : ServiceBase, IService
    {
        private readonly string PLUGINNAME = "PluginTemplateService";
        // CONSTRUCTOR
        public PluginTemplateService()
        {
            
        }// END of Constructor
        #region Twitch command methods
        #endregion
        #region Discord command methods
        #region Interface default discord command methods
        public async Task SetDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            PluginTemplateSettings settings = await Settings(bChan);
            settings._defaultDiscordChannel = discordChannelID;
            await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync("This is now the active channel for the PLC plugin.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);

        }
        public async Task ClearDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            PluginTemplateSettings settings = await Settings(bChan);
            settings._defaultDiscordChannel = 0;
            await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync("The active channel for the PLC plugin is resetted. All channels now valid.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);

        }
        #endregion
        #endregion
        #region Important base methods that can't be inherited
        private async Task<PluginTemplateSettings> Settings(BotChannel bChan)
        {
            PluginTemplateSettings settings = new PluginTemplateSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as PluginTemplateSettings;
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
        public void OnBotChannelEntryMerge(BotChannel discordGuild, BotChannel twitchChannel)
        {
            throw new NotImplementedException();
        }
        public void OnUserEntryMerge(UserEntry discordUser, UserEntry twitchUser)
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
        #endregion
    }
}
