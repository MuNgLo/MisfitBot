using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MisfitBot_MKII;
using MisfitBot_MKII.Components;
using MisfitBot_MKII.Statics;
using System.Data.SQLite;
using System.Data;

namespace QueuePlugin
{
    public class QueuePlugin : PluginBase
    {

        private Dictionary<string, QueueObject> _queues;

        public QueuePlugin() : base("QueuePlugin", 1)
        {
            Program.BotEvents.OnCommandReceived += OnCommandRecieved;
            Program.BotEvents.OnMessageReceived += OnMessageRecieved;
            _queues = new Dictionary<string, QueueObject>();
            TimerStuff.OnSecondTick += OnSecondTick;
            Program.BotEvents.OnTwitchChannelGoesOffline += OnBotChannelGoesOffline;
        }

        private async void OnBotChannelGoesOffline(BotChannel bChan)
        {
            QueueSettings settings = await Settings<QueueSettings>(bChan, PluginName);
            if (settings._active)
            {
                StopQueue(bChan);
            }
        }

        private async void OnMessageRecieved(BotWideMessageArguments args)
        {
            BotChannel bChan = await GetBotChannel(args);
            QueueSettings settings = await Settings<QueueSettings>(bChan, PluginName);
            if (settings._active)
            {
                if (_queues.ContainsKey(bChan.Key))
                {
                    _queues[bChan.Key].AddMessage();
                }
            }
        }

        private async void OnCommandRecieved(BotWideCommandArguments args)
        {
            BotChannel bChan = await GetBotChannel(args);
            if (bChan == null) { return; }
            QueueSettings settings = await Settings<QueueSettings>(bChan, PluginName);
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            if (settings._active && args.source == MESSAGESOURCE.TWITCH && (args.command.ToLower() == "addqueue" || args.command.ToLower() == "aq"))
            {
                QueueUp(bChan, args.userDisplayName);
                return;
            }
            if (settings._active && args.source == MESSAGESOURCE.TWITCH && (args.command.ToLower() == "leavequeue" || args.command.ToLower() == "lq"))
            {
                if (QueueLeave(bChan, args.userDisplayName))
                {
                    response.message = $"{args.userDisplayName} left the queue";
                    response.parseMessage = true;
                    response.victim = args.user;
                    Respond(bChan, response);
                }
                return;
            }

            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages)
            {
                // No access below
                return;
            }
            if (args.command.ToLower() == "queue")
            {
                // Blank queue response here
                if (args.arguments.Count == 0)
                {
                    if (args.source == MESSAGESOURCE.DISCORD)
                    {
                        Discord.EmbedFooterBuilder footer = new Discord.EmbedFooterBuilder
                        {
                            Text = $"The plugin is currently {(settings._active ? "active" : "inactive")} here.{(HasActiveQueue(bChan) ? $" {QueuedUserCount(bChan)} in queue." : "")}"
                        };

                        Discord.EmbedBuilder embedded = new Discord.EmbedBuilder
                        {
                            Title = "Plugin: Queue ",
                            Description = HelpText(settings),
                            Color = Discord.Color.DarkOrange,
                            Footer = footer
                        };

                        await SayEmbedOnDiscord(args.channelID, embedded.Build());
                        return;
                    }
                    if (args.source == MESSAGESOURCE.TWITCH)
                    {
                        response.message = $"The plugin is currently {(settings._active ? "active" : "inactive")} here.{(HasActiveQueue(bChan) ? $" {QueuedUserCount(bChan)} in queue." : "")}";
                        Respond(bChan, response);
                        return;
                    }
                }
                // resolve subcommands
                switch (args.arguments[0])
                {
                    case "off":
                        settings._active = false;
                        SaveBaseSettings(bChan, PluginName, settings);
                        response.message = $"Queue is inactive.";
                        Respond(bChan, response);
                        break;
                    case "on":
                        settings._active = true;
                        SaveBaseSettings(bChan, PluginName, settings);
                        response.message = $"Queue is active.";
                        Respond(bChan, response);
                        break;

                    case "next":
                        if (settings._active)
                        {
                            response.message = NextInQueue(bChan);
                            Respond(bChan, response);
                            if(response.source == MESSAGESOURCE.DISCORD && bChan.TwitchChannelName != string.Empty){
                                Program.TwitchSayMessage(bChan.TwitchChannelName, response.message);
                            }
                        }
                        break;
                    case "reset":
                        if (settings._active)
                        {
                            CreateQueue(bChan, settings);
                            response.message = "Queue reset";
                            Respond(bChan, response);
                            if(response.source == MESSAGESOURCE.DISCORD && bChan.TwitchChannelName != string.Empty){
                                Program.TwitchSayMessage(bChan.TwitchChannelName, response.message);
                            }
                        }
                        break;
                    case "start":
                        if (settings._active)
                        {
                            CreateQueue(bChan, settings);
                            response.message = $"Queue started";
                            Respond(bChan, response);
                            if(response.source == MESSAGESOURCE.DISCORD && bChan.TwitchChannelName != string.Empty){
                                Program.TwitchSayMessage(bChan.TwitchChannelName, response.message);
                            }
                        }
                        break;
                    case "stop":
                        if (settings._active)
                        {
                            StopQueue(bChan);
                            response.message = $"Queue stopped.";
                            Respond(bChan, response);
                            if(response.source == MESSAGESOURCE.DISCORD && bChan.TwitchChannelName != string.Empty){
                                Program.TwitchSayMessage(bChan.TwitchChannelName, response.message);
                            }
                        }
                        break;


                }

            }
        }

        #region internal Queue methods
        private bool HasActiveQueue(BotChannel bChan)
        {
            return _queues.ContainsKey(bChan.Key);
        }
        private int QueuedUserCount(BotChannel bChan)
        {
            if (HasActiveQueue(bChan))
            {
                return _queues[bChan.Key].Count;
            }
            return 0;
        }
        private string NextInQueue(BotChannel bChan)
        {
            if (_queues.ContainsKey(bChan.Key))
            {
                return $"{_queues[bChan.Key].PullnextInQueue()} you are up!";
            }
            return $"Queue is empty. Use {CMC}aq to join queue and {CMC}lq to leave queue.";
        }
        private void CreateQueue(BotChannel bChan, QueueSettings settings)
        {
            if (_queues.ContainsKey(bChan.Key))
            {
                _queues[bChan.Key].Reset();
            }
            else
            {
                _queues[bChan.Key] = new QueueObject(bChan, settings._announceTimer);
            }
        }
        private void StopQueue(BotChannel bChan)
        {
            if (_queues.ContainsKey(bChan.Key))
            {
                _queues.Remove(bChan.Key);
            }
        }
        private bool QueueUp(BotChannel bChan, string twitchDisplayName)
        {
            if (_queues.ContainsKey(bChan.Key))
            {
                _queues[bChan.Key].AddUser(twitchDisplayName);
                return _queues[bChan.Key].HasUser(twitchDisplayName);
            }
            return false;
        }
        private bool QueueLeave(BotChannel bChan, string twitchDisplayName)
        {
            if (_queues.ContainsKey(bChan.Key))
            {
                _queues[bChan.Key].RemoveUser(twitchDisplayName);
                return true;
            }
            return false;
        }
        #endregion

        public override void OnBotChannelEntryMergeEvent(BotChannel discordGuild, BotChannel twitchChannel)
        {
            throw new NotImplementedException();
        }

        public override void OnMinuteTick(int minutes)
        {
            throw new NotImplementedException();
        }

        public override async void OnSecondTick(int seconds)
        {
            foreach (string key in _queues.Keys)
            {
                BotChannel bChan = await Program.Channels.GetBotchannelByKey(key);
                QueueSettings settings = await Settings<QueueSettings>(bChan, PluginName);
                if (settings._active)
                {
                    if (_queues[key].CheckAnnounce())
                    {
                        Program.TwitchSayMessage(bChan.TwitchChannelName, _queues[key].Announce(CMC));
                    }
                }
            }
        }

        public override void OnUserEntryMergeEvent(UserEntry discordUser, UserEntry twitchUser)
        {
            throw new NotImplementedException();
        }
        private string HelpText(QueueSettings settings)
        {
            string message = $"This plugin lets users get into a queue with the {CMC}addqueue command." +
            System.Environment.NewLine + System.Environment.NewLine +
            $"**{CMC}queue on/off** to turn the plugin on or off.";
            return message;
        }
    }// EOF CLASS
}
