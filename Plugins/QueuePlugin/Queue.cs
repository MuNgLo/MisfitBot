using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MisfitBot_MKII;
using MisfitBot_MKII.Components;
using MisfitBot_MKII.Statics;
using System.Data.SQLite;
using System.Data;
using TwitchStream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;

namespace QueuePlugin
{
    public class QueuePlugin : PluginBase
    {
        private Dictionary<string, QueueObject> _queues;
        public QueuePlugin() : base("queue", "QueuePlugin", 3, "Allows users to get into a queue")
        {
            Program.BotEvents.OnMessageReceived += OnMessageRecieved;
            _queues = new Dictionary<string, QueueObject>();
            TimerStuff.OnSecondTick += OnSecondTick;
            Program.BotEvents.OnTwitchChannelGoesOffline += OnBotChannelGoesOffline;
        }

        #region Command Methods
        [SingleCommand("aq"), CommandHelp("Join active queue."), CommandSourceAccess(MESSAGESOURCE.TWITCH), CommandVerified(3)]
        public void JQueue(BotChannel bChan, BotWideCommandArguments args)
        {
            JoinQueue(bChan, args);
        }
        [SingleCommand("addqueue"), CommandHelp("Join active queue."), CommandSourceAccess(MESSAGESOURCE.TWITCH), CommandVerified(3)]
        public async void JoinQueue(BotChannel bChan, BotWideCommandArguments args)
        {
            QueueSettings settings = await Settings<QueueSettings>(bChan, PluginName);
            if (settings._active && _queues.ContainsKey(bChan.Key))
            {
                if (args.source == MESSAGESOURCE.TWITCH)
                {
                    QueueUp(bChan, args.userDisplayName);
                }
            }
        }
        [SingleCommand("lq"), CommandHelp("Leave active queue."), CommandSourceAccess(MESSAGESOURCE.TWITCH), CommandVerified(3)]
        public void LQueue(BotChannel bChan, BotWideCommandArguments args)
        {
            LeaveQueue(bChan, args);
        }
        [SingleCommand("leavequeue"), CommandHelp("Leave active queue."), CommandSourceAccess(MESSAGESOURCE.TWITCH), CommandVerified(3)]
        public async void LeaveQueue(BotChannel bChan, BotWideCommandArguments args)
        {
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            QueueSettings settings = await Settings<QueueSettings>(bChan, PluginName);
            if (settings._active && _queues.ContainsKey(bChan.Key))
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
        }
        [SingleCommand("queueinfo"), CommandHelp("Gives info about the queue."), CommandVerified(3)]
        public async void Queue(BotChannel bChan, BotWideCommandArguments args)
        {
            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages)
            {
                // No access below
                return;
            }
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            QueueSettings settings = await Settings<QueueSettings>(bChan, PluginName);
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

                    // add a field listinbg all the users in the queue
                    if (settings._active && _queues.ContainsKey(bChan.Key))
                    {
                        if (_queues[bChan.Key].Count > 0)
                        {
                            embedded.AddField(name: "Queued up users", _queues[bChan.Key].ToString(), false);
                        }
                    }

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
        }
        [SubCommand("start", 0), CommandHelp("Start running a queue in the twitch channel"), CommandVerified(3)]
        public async void StartQueue(BotChannel bChan, BotWideCommandArguments args)
        {
            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages)
            {
                // No access below
                return;
            }
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            QueueSettings settings = await Settings<QueueSettings>(bChan, PluginName);
            if (settings._active)
            {
                CreateQueue(bChan, settings);
                response.message = $"Queue started";
                Respond(bChan, response);
                if (response.source == MESSAGESOURCE.DISCORD && bChan.TwitchChannelName != string.Empty)
                {
                    Program.TwitchSayMessage(bChan.TwitchChannelName, response.message);
                }
            }
        }
        [SubCommand("stop", 0), CommandHelp("Stop a queue that is running in the twitch channel"), CommandVerified(3)]
        public async void StopQueue(BotChannel bChan, BotWideCommandArguments args)
        {
            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages)
            {
                // No access below
                return;
            }
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            QueueSettings settings = await Settings<QueueSettings>(bChan, PluginName);
            if (settings._active)
            {
                StopQueue(bChan);
                response.message = $"Queue stopped.";
                Respond(bChan, response);
                if (response.source == MESSAGESOURCE.DISCORD && bChan.TwitchChannelName != string.Empty)
                {
                    Program.TwitchSayMessage(bChan.TwitchChannelName, response.message);
                }
            }
        }
        [SubCommand("reset", 0), CommandHelp("Resets the active queue"), CommandVerified(3)]
        public async void ResetQueue(BotChannel bChan, BotWideCommandArguments args)
        {
            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages)
            {
                // No access below
                return;
            }
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            QueueSettings settings = await Settings<QueueSettings>(bChan, PluginName);
            if (settings._active && _queues.ContainsKey(bChan.Key))
            {
                CreateQueue(bChan, settings);
                response.message = "Queue reset";
                Respond(bChan, response);
                if (response.source == MESSAGESOURCE.DISCORD && bChan.TwitchChannelName != string.Empty)
                {
                    Program.TwitchSayMessage(bChan.TwitchChannelName, response.message);
                }
            }
        }
        [SubCommand("next", 0), CommandHelp("Stop a queue that is running in the twitch channel"), CommandVerified(3)]
        public async void NextInQueue(BotChannel bChan, BotWideCommandArguments args)
        {
            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages)
            {
                // No access below
                return;
            }
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            QueueSettings settings = await Settings<QueueSettings>(bChan, PluginName);
            if (settings._active)
            {
                response.message = NextInQueue(bChan);
                Respond(bChan, response);
                if (response.source == MESSAGESOURCE.DISCORD && bChan.TwitchChannelName != string.Empty)
                {
                    Program.TwitchSayMessage(bChan.TwitchChannelName, response.message);
                }
            }
        }
        #endregion

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

        #region Interface adherance 
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
                BotChannel bChan = await Program.Channels.GetBotChannelByKey(key);
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
        private async void OnBotChannelGoesOffline(TwitchStreamGoOfflineEventArguments arg)
        {
            QueueSettings settings = await Settings<QueueSettings>(arg.bChan, PluginName);
            if (settings._active)
            {
                StopQueue(arg.bChan);
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
        #endregion
    }// EOF CLASS
}
