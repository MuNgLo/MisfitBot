using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using MisfitBot2.Plugins.PluginTemplate;
using MisfitBot2.Plugins.Couch;

namespace MisfitBot2.Services
{
    class CouchService : ServiceBase, IService
    {
        public readonly string PLUGINNAME = "Couch";
        private Random rng = new Random();
        private List<string> _success = new List<string>();
        private List<string> _fail = new List<string>();
        private List<string> _incident = new List<string>();

        // CONSTRUCTOR
        public CouchService()
        {
            Core.Twitch._client.OnChatCommandReceived += TwitchOnChatCommandReceived;
            Core.OnBotChannelGoesLive += OnChannelGoesLive;
            // Successes
            _success.Add(" takes a seat on the couch.");
            _success.Add(" backflips onto the couch.");
            _success.Add(" manages to escape the restrains and takes a seat on the couch.");
            _success.Add(" suddenly materializes on the couch with a hint of a smirk.");
            _success.Add(" claws their way up from the void between the cushions.");
            _success.Add(" does an impressive herolanding then proceeds to stumble to the couch with intence knee pain.");
            _success.Add(" accepts their fate as a decoration on the couch.");

            // Fails
            _fail.Add(" is left standing.");
            _fail.Add(" rolls into fetal position as they don't fit on the couch.");
            _fail.Add(" creates a tsunami with their tears of despair.");
            _fail.Add(" hair catches fire from rage and others reach for the marshmallows.");
            _fail.Add(" is carried away by a flock of chairs to the land of standing space.");

            // Incidents
            _incident.Add(" got cought suckling a cushion in the couch and had to leave their spot.");
            _incident.Add(" by pure chance ends up quantum entangling with the couch and disappears back into the void.");
            _incident.Add(" gets bumped out of the couch as a new victim takes their seat.");
        }

        private string GetRNGSuccess()
        {
            return _success[rng.Next(_success.Count)];
        }
        private string GetRNGFail()
        {
            return _fail[rng.Next(_fail.Count)];
        }
        private string GetRNGIncident()
        {
            return _incident[rng.Next(_incident.Count)];
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


        private async void OnChannelGoesLive(BotChannel bChan)
        {
            CouchSettings settings = await Settings(bChan); 
            if (!settings._active)
            {
                return;
            }
            if (!settings._couches.ContainsKey(bChan.Key))
            {
                settings._couches[bChan.Key] = new CouchEntry();
            }

            if(Core.CurrentTime > settings._couches[bChan.Key].lastActivationTime + 60)
            {
                ResetCouch(bChan, settings);
            }
        }

        private bool RollIncident(int chance = 0)
        {
            return rng.Next(0, 100) + chance >= 95;
        }

        private async void ResetCouch(BotChannel bChan, CouchSettings settings)
        { 
            await Core.LOG(new LogMessage(LogSeverity.Info, PLUGINNAME, "Live event captured. Opening couch!"));
            if (!settings._couches.ContainsKey(bChan.Key))
            {
                settings._couches[bChan.Key] = new CouchEntry();
            }
            settings._couches[bChan.Key].couchOpen = true;
            settings._couches[bChan.Key].lastActivationTime = Core.CurrentTime;
            settings._couches[bChan.Key].TwitchUsernames = new List<string>();
            settings.failCount = 0;
            SaveBaseSettings(PLUGINNAME, bChan, settings);
            Core.Twitch._client.SendMessage(bChan.TwitchChannelName, "Couch is now open. Take a !seat.");
        }

        #region Twitch methods
        private async void TwitchOnChatCommandReceived(object sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.Command.ChatMessage.Channel);
            if (bChan == null) { return; }
            CouchSettings settings = await Settings(bChan);
            if (!settings._couches.ContainsKey(bChan.Key))
            {
                settings._couches[bChan.Key] = new CouchEntry();
            }
            switch (e.Command.CommandText.ToLower())
            {
                case "couch":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        if (e.Command.ArgumentsAsList.Count == 1)
                        {
                            if (e.Command.ArgumentsAsList[0].ToLower() == "on")
                            {
                                settings._active = true;
                            }
                            if (e.Command.ArgumentsAsList[0].ToLower() == "off")
                            {
                                settings._active = false;
                            }
                        }
                        if (e.Command.ArgumentsAsList.Count == 2)
                        {
                            if (e.Command.ArgumentsAsList[0].ToLower() == "seats")
                            {
                                int seats = settings.couchsize;
                                int.TryParse(e.Command.ArgumentsAsList[1], out seats);
                                if(seats > 0 && seats <= 20 && seats != settings.couchsize)
                                {
                                    settings.couchsize = seats;
                                }
                            }
                        }
                        if (settings._active)
                        {
                            Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                $"Couch plugin is active in this channel. {settings.couchsize} seats."
                                );
                        }
                        else
                        {
                            Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                $"Couch plugin is inactive in this channel. {settings.couchsize} seats."
                                );
                        }
                    }
                    break;
                case "seat":
                    if (!settings._couches[bChan.Key].couchOpen || !settings._active) { return; }
                    if(Core.CurrentTime - settings._couches[bChan.Key].lastActivationTime > 3600 && settings.failCount < 3)
                    {
                        Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                            $"{e.Command.ChatMessage.DisplayName} is late to get a seat in the couch. It isn't full but the people in it have spread out and refuse to move."
                            );
                        settings.failCount++;
                        SaveBaseSettings(PLUGINNAME, bChan, settings);
                        return;
                    }
                    if (Core.CurrentTime - settings._couches[bChan.Key].lastActivationTime > 600 && settings.failCount >= 3)
                    {
                        return;
                    }
                    if (settings._couches[bChan.Key].TwitchUsernames.Contains(e.Command.ChatMessage.DisplayName)) { return; }

                    if (settings._couches[bChan.Key].TwitchUsernames.Count < settings.couchsize)
                    {
                        if (RollIncident())
                        {
                            string mark = GetRNGSitter(bChan, settings);
                            if (mark != null)
                            {
                                Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                    $"{mark} {GetRNGIncident()}"
                                    );
                                settings._couches[bChan.Key].TwitchUsernames.RemoveAll(p => p == mark);
                                SaveBaseSettings(PLUGINNAME, bChan, settings);
                            }
                        }
                        settings._couches[bChan.Key].TwitchUsernames.Add(e.Command.ChatMessage.DisplayName);
                        Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, 
                            $"{e.Command.ChatMessage.DisplayName} {GetRNGSuccess()}"
                            );
                        SaveBaseSettings(PLUGINNAME, bChan, settings);
                    }
                    else
                    {
                        Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                            $"{e.Command.ChatMessage.DisplayName} {GetRNGFail()}"
                            );
                        settings.failCount++;
                        SaveBaseSettings(PLUGINNAME, bChan, settings);
                    }

                    break;
                case "opencouch":
                    if(!settings._active) { return; }
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        ResetCouch(bChan, settings);
                    }
                    break;
            }
        }
        #endregion

        #region Interface default discord command methods
        public async Task SetDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            CouchSettings settings = await Settings(bChan);
            settings._defaultDiscordChannel = discordChannelID;
            await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync(
                "This is now the active channel for the Couch plugin.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);
        }
        public async Task ClearDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            CouchSettings settings = await Settings(bChan);
            settings._defaultDiscordChannel = 0;
            await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync(
                "The active channel for the Couch plugin is resetted. All channels now valid.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);
        }
        #endregion
        #region Important base methods that can't be inherited
        private async Task<CouchSettings> Settings(BotChannel bChan)
        {
            CouchSettings settings = new CouchSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as CouchSettings;
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
