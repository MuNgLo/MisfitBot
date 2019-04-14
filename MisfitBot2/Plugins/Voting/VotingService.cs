using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MisfitBot2.Plugins.Voting;
using Newtonsoft.Json;

namespace MisfitBot2.Services
{
    public class VotingService : ServiceBase, IService
    {
        private readonly string PLUGINNAME = "Voting";
        private List<RunningVote> _activeVotes = new List<RunningVote>();
        //Message buffers
        private Dictionary<string, List<string>> _twitchBuffer;
        private int _twitchBufferAge = 0;
        private Dictionary<ulong, List<string>> _discordBuffer;
        private int _discordBufferAge = 0;

        // CONSTRUCTOR
        public VotingService()
        {
            _twitchBuffer = new Dictionary<string, List<string>>();
            _discordBuffer = new Dictionary<ulong, List<string>>();
            Core.Twitch._client.OnChatCommandReceived += TwitchOnChatCommandReceived;
            TimerStuff.OnSecondTick += OnSecondTick;
        }// END of Constructor

        #region Twitch command methods
        private async void TwitchOnChatCommandReceived(object sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {
            switch (e.Command.CommandText.ToLower())
            {
                case "votes":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        switch (e.Command.ArgumentsAsList[0])
                        {
                            case "start":

                                if (e.Command.ArgumentsAsList.Count > 2)
                                {
                                    if (!_activeVotes.Exists(p => p.twitchChannelName == e.Command.ChatMessage.Channel))
                                    {
                                        await TwitchStartVote(e.Command.ChatMessage.Channel, e.Command.ArgumentsAsList);
                                    }
                                }
                                break;
                            case "stop":
                            case "close":
                                if (_activeVotes.Exists(p => p.twitchChannelName == e.Command.ChatMessage.Channel))
                                {
                                    await TwitchStopVote(e.Command.ChatMessage.Channel);
                                }
                                break;
                        }
                    }
                    break;
                case "vote":
                    if (!_activeVotes.Exists(p => p.twitchChannelName == e.Command.ChatMessage.Channel)) { return; }
                    if (e.Command.ArgumentsAsList.Count == 1)
                    {
                        await TwitchCastVote(e.Command.ChatMessage.UserId, e.Command.ChatMessage.Channel, e.Command.ArgumentsAsList[0]);
                    }
                    break;
            }
        }

        private async Task TwitchStopVote(string twitchChannelName)
        {
            await StopVote(await Core.Channels.GetTwitchChannelByName(twitchChannelName));
        }

        private async Task TwitchStartVote(string twitchChannelName, List<string> arguments)
        {
            await StartVote(await Core.Channels.GetTwitchChannelByName(twitchChannelName), arguments, true);
        }
        private async Task TwitchCastVote(string userID, string twitchChannelName, string option)
        {
            CastVote(
                await Core.Channels.GetTwitchChannelByName(twitchChannelName), 
                await Core.UserMan.GetUserByTwitchID(userID), 
                option);
        }
        #endregion
        #region Discord command methods
        public async Task DiscordCommand(ICommandContext context, List<string> arguments)
        {
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info,
                PLUGINNAME,
                $"{context.User.Username} used command \"bets\" in {context.Channel.Name}."
                ));
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            VotingSettings settings = await Settings(bChan);
            switch (arguments[0].ToLower())
            {
                case "start":
                    if (arguments.Count >= 3)
                    {
                        await DiscordStartVote(context, arguments);
                    }
                    break;
                case "stop":
                case "close":
                    await DiscordStopVote(context);
                    break;
            }
        }
        #region Interface default discord command methods
        public async Task SetDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
                VotingSettings settings = await Settings(bChan);
                settings._defaultDiscordChannel = discordChannelID;
                await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync("This is now the active channel for the PLC plugin.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);

        }
        public async Task ClearDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
                VotingSettings settings = await Settings(bChan);
                settings._defaultDiscordChannel = 0;
                await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync("The active channel for the PLC plugin is resetted. All channels now valid.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);

        }
        #endregion
        public async Task DiscordStartVote(ICommandContext context, List<string> arguments)
        {
            if (!_activeVotes.Exists(p => p.discordguild == context.Guild.Id))
            {
                BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
                if (bChan == null) return;
                await StartVote(bChan, arguments, false);
            }
        }
        public async Task DiscordCastVote(ICommandContext context, string option)
        {
            if (_activeVotes.Exists(p => p.discordguild == context.Guild.Id))
            {
                BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
                CastVote(
                    bChan, 
                    await Core.UserMan.GetUserByDiscordID(context.User.Id), 
                    option);
            }
        }
        public async Task DiscordStopVote(ICommandContext context)
        {
            if (_activeVotes.Exists(p => p.discordguild == context.Guild.Id))
            {
                BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
                await StopVote(bChan);
            }
        }
        #endregion
        #region Internal methods
        private async Task StopVote(BotChannel bChan)
        {
            RunningVote vote;
            if (bChan.isLinked || bChan.GuildID != 0)
            {
                vote = _activeVotes.Find(p => p.discordguild == bChan.GuildID);
            }
            else
            {
                vote = _activeVotes.Find(p => p.twitchChannelName == bChan.TwitchChannelName);
            }

            string result = vote.FinishVote();
            _activeVotes.Remove(vote);
            Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"Voting ended! {result}");
            await SayOnDiscord(bChan, $"Voting ended! {result}");
        }
        private async Task StartVote(BotChannel bChan, List<string> arguments, bool isTwitch)
        {
            arguments.RemoveAt(0);
            foreach(string arg in arguments)
            {
                if(arguments.FindAll(p=>p == arg).Count > 1)
                {
                    if (isTwitch)
                    {
                        Core.Twitch._client.SendMessage(bChan.TwitchChannelName, "There can be no duplicates in the voting options.");
                    }
                    else
                    {
                        await SayOnDiscord(bChan, "There can be no duplicates in the voting options.");
                    }
                    return;
                }
            }

            RunningVote vote = new RunningVote(bChan, arguments);
            _activeVotes.Add(vote);

            string msg = $"Voting started! use \"{Core._commandCharacter}vote #\" You can vote between ";
            foreach(string option in arguments)
            {
                msg += option + ", ";
            }

            await SayOnDiscord(bChan, msg);
            Core.Twitch._client.SendMessage(bChan.TwitchChannelName, msg);
        }
        private void CastVote(BotChannel bChan, UserEntry user, string option)
        {
            if (user == null) { return; }
            RunningVote vote;
            if(bChan.isLinked || bChan.GuildID != 0)
            {
                vote = _activeVotes.Find(p => p.discordguild == bChan.GuildID);
            }
            else
            {
                vote = _activeVotes.Find(p => p.twitchChannelName == bChan.TwitchChannelName);
            }


            if (vote.ValidateOption(option))
            {
                if (vote.AddVote(new Vote(user, option)))
                {
                    AddToTwitchBuffer(bChan, $"Vote accepted. {user._twitchDisplayname} voted for \"{option}\".");
                    AddToDiscordBuffer(bChan, $"Vote accepted. {user._twitchDisplayname} voted for \"{option}\".");
                }
            }
        }
        #region Buffer handling bits
        private async void AddToTwitchBuffer(BotChannel bChan, string msg)
        {
            VotingSettings settings = await Settings(bChan);
            if (!_twitchBuffer.ContainsKey(bChan.TwitchChannelName))
            {
                _twitchBuffer[bChan.TwitchChannelName] = new List<string>();
            }
            if (_twitchBuffer.Keys.Count == 1)
            {
                _twitchBufferAge = Core.CurrentTime;
            }
            if (!_twitchBuffer[bChan.TwitchChannelName].Contains(msg))
            {
                _twitchBuffer[bChan.TwitchChannelName].Add(msg);
            }
        }
        private async void AddToDiscordBuffer(BotChannel bChan, string msg)
        {
            VotingSettings settings = await Settings(bChan);
            if(bChan.discordDefaultBotChannel == 0)
            {
                return;
            }
            if (!_discordBuffer.ContainsKey(bChan.GuildID))
            {
                _discordBuffer[bChan.GuildID] = new List<string>();
            }
            if (_twitchBuffer.Keys.Count == 1)
            {
                _discordBufferAge = Core.CurrentTime;
            }
            if (!_discordBuffer[bChan.GuildID].Contains(msg))
            {
                _discordBuffer[bChan.GuildID].Add(msg);
            }
        }
        private void BufferCheckTwitch()
        {
            if (Core.CurrentTime < _twitchBufferAge + 5) { return; }

            foreach (string key in _twitchBuffer.Keys)
            {
                string msg = string.Empty;
                foreach (string message in _twitchBuffer[key])
                {
                    msg += message + " ";
                }
                Core.Twitch._client.SendMessage(key, msg);
            }
            _twitchBuffer = new Dictionary<string, List<string>>();
        }
        private async Task BufferCheckDiscord()
        {
            if (Core.CurrentTime < _discordBufferAge + 5) { return; }

            foreach (ulong key in _discordBuffer.Keys)
            {
                string msg = string.Empty;
                foreach (string message in _discordBuffer[key])
                {
                    msg += message + " ";
                }
                BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(key);
                if (bChan.discordDefaultBotChannel != 0)
                {
                    await SayOnDiscord(bChan, msg);
                }
            }
            _discordBuffer = new Dictionary<ulong, List<string>>();
        }
        #endregion
        #endregion
        #region Interface base methods
        public async void OnSecondTick(int seconds)
        {
            await BufferCheckDiscord();
            BufferCheckTwitch();
        }
        public void OnMinuteTick(int minutes)
        {
            
        }
        public void OnBotChannelEntryMerge(BotChannel discordGuild, BotChannel twitchChannel)
        {
            
        }
        public void OnUserEntryMerge(UserEntry discordUser, UserEntry twitchUser)
        {
            
        }

        /*public void NewUserValuesEntry(ulong userID, ulong guildID)
        {
            UserValues data = new UserValues(Core.CurrentTime);
            Core.UserMan.SetPluginUserValues(PLUGINNAME, userID, guildID, data);
        }
        public void NewUserValuesEntry(string twitchUserID, string twitchChannelID)
        {
            UserValues data = new UserValues(Core.CurrentTime);
            Core.UserMan.SetPluginUserValues(PLUGINNAME, twitchUserID, twitchChannelID, data);
        }*/
        #endregion



        private async Task<VotingSettings> Settings(BotChannel bChan)
        {
            VotingSettings settings = new VotingSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as VotingSettings;
        }
       

        


    }

    
}
