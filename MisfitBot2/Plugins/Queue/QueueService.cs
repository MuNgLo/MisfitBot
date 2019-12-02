using MisfitBot2.Plugins.Queue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MisfitBot2.Services
{
    public class QueueService : ServiceBase, IService
    {
        readonly string PLUGINNAME = "Queue";
        private Dictionary<string, Queue> _queues;
        // CONSTRUCTOR
        public QueueService()
        {
            _queues = new Dictionary<string, Queue>();
            Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info, PLUGINNAME, "Loaded"));
            Core.Twitch._client.OnChatCommandReceived += TwitchOnChatCommandReceived;
            Core.Twitch._client.OnMessageReceived += TwitchOnMessageReceived;
            TimerStuff.OnSecondTick += OnSecondTick;
            Events.OnTwitchChannelGoesOffline += OnBotChannelGoesOffline;
        }

        private async void OnBotChannelGoesOffline(BotChannel bChan)
        {
            QueueSettings settings = await Settings(bChan);
            if (settings._active)
            {
                StopQueue(bChan);
            }
        }
        #region Twitch methods
        private async void TwitchOnChatCommandReceived(object sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.Command.ChatMessage.Channel);
            QueueSettings settings = await Settings(bChan);
            switch (e.Command.CommandText.ToLower())
            {
                case "queue":
                case "q":
                    if (e.Command.ArgumentsAsList.Count > 0 && (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster))
                    {
                        // Admincommands
                        switch (e.Command.ArgumentsAsList[0].ToLower())
                        {
                            case "on":
                                settings._active = true;
                                SaveBaseSettings(PLUGINNAME, bChan, settings);
                                Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                        "Queue is active.");
                                await SayOnDiscordAdmin(bChan,
                                $"Queue is active."
                                );
                                break;
                            case "off":
                                settings._active = false;
                                SaveBaseSettings(PLUGINNAME, bChan, settings);
                                Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                        "Queue is inactive.");
                                await SayOnDiscordAdmin(bChan,
                                 $"Queue is inactive."
                                 );
                                break;
                            case "next":
                                if (settings._active)
                                {
                                    NextInQueue(bChan);
                                }
                                break;
                            case "reset":
                                if (settings._active)
                                {
                                    CreateQueue(bChan, settings);
                                }
                                break;
                            case "start":
                                if (settings._active)
                                {
                                    CreateQueue(bChan, settings);
                                    Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                        "Queue started");
                                }
                                break;
                            case "stop":
                                if (settings._active)
                                {
                                    StopQueue(bChan);
                                    Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel,
                                        "Queue stopped");
                                }
                                break;
                        }
                    }
                    if(e.Command.ArgumentsAsList.Count == 0)
                    {
                        if (settings._active)
                        {
                            QueueUp(bChan, e.Command.ChatMessage.DisplayName);
                        }
                    }
                    break;
            }
        }
        private async void TwitchOnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.ChatMessage.Channel);
            QueueSettings settings = await Settings(bChan);
            if (settings._active)
            {
                if (_queues.ContainsKey(bChan.Key))
                {
                    _queues[bChan.Key].AddMessage();
                }
            }
        }
        #endregion

        #region internal Queue methods
        private void NextInQueue(BotChannel bChan)
        {
            if (_queues.ContainsKey(bChan.Key))
            {
                _queues[bChan.Key].PullnextInQueue();
            }
        }
        private void CreateQueue(BotChannel bChan, QueueSettings settings)
        {
            if (_queues.ContainsKey(bChan.Key))
            {
                _queues[bChan.Key].Reset();
            }
            else
            {
                _queues[bChan.Key] = new Queue(bChan, settings._announceTimer);
            }
        }
        private void StopQueue(BotChannel bChan)
        {
            if (_queues.ContainsKey(bChan.Key))
            {
                _queues.Remove(bChan.Key);
            }
        }
        private void QueueUp(BotChannel bChan, string twitchDisplayName)
        {
            if (_queues.ContainsKey(bChan.Key))
            {
                _queues[bChan.Key].AddUser(twitchDisplayName);
            }
        }
        #endregion

        #region non interface base methods
        private async Task<QueueSettings> Settings(BotChannel bChan)
        {
            QueueSettings settings = new QueueSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as QueueSettings;
        }
        #endregion


        #region IService methods
        public void OnBotChannelEntryMergeEvent(BotChannel discordGuild, BotChannel twitchChannel)
        {
            throw new NotImplementedException();
        }

        public void OnMinuteTick(int minutes)
        {
            throw new NotImplementedException();
        }

        public async void OnSecondTick(int seconds)
        {

            foreach (string key in _queues.Keys)
            {
                BotChannel bChan = await Core.Channels.GetBotchannelByKey(key);
                QueueSettings settings = await Settings(bChan);
                if (settings._active) { _queues[key].CheckAnnounce(); }
            }
        }

        public void OnUserEntryMergeEvent(UserEntry discordUser, UserEntry twitchUser)
        {
            throw new NotImplementedException();
        }
        #endregion
    }// End of class
}
