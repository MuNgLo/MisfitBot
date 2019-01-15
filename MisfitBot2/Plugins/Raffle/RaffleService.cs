using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MisfitBot2.Plugins.Raffle;
using TwitchLib.Client.Events;

namespace MisfitBot2.Services
{
    public class RaffleService : ServiceBase, IService
    {
        public readonly string PLUGINNAME = "Raffle";
        private RunningRaffles _raffles;
        // CONSTRUCTOR
        public RaffleService()
        {
            Core.Twitch._client.OnChatCommandReceived += TwitchOnChatCommandReceived;
            Core.Twitch._client.OnMessageReceived += TwitchOnMessageReceived;
            _raffles = new RunningRaffles();
            TimerStuff.OnSecondTick += OnSecondTick;
        }// END of Constructor
        #region Twitch command methods
        private async void TwitchOnChatCommandReceived(object sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.Command.ChatMessage.Channel);
            if (bChan == null) { return; }
            switch (e.Command.CommandText.ToLower())
            {
                case "clearraffle":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        if (HasRaffle(bChan))
                        {
                            ClearRaffle(bChan);
                        }
                    }
                    break;
                case "cancelraffle":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        if (HasRaffle(bChan))
                        {
                            CancelRaffle(bChan);
                        }
                    }
                    break;
                case "startraffle":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        if (HasRaffle(bChan) == false)
                        {
                            if (e.Command.ArgumentsAsList.Count == 0)
                            {
                                Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, "Use \"!startraffle <NumberOfTickets> <TicketPrice>\" to start a raffle.");
                                return;
                            }
                            if (e.Command.ArgumentsAsList.Count != 2)
                            {
                                return;
                            }
                            StartRaffle(bChan, e.Command.ArgumentsAsList);
                        }
                    }
                    break;
                case "stopticketsale":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        if (HasRaffle(bChan))
                        {
                            TicketSale(bChan, false);
                        }
                    }
                    break;
                case "startticketsale":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        if (HasRaffle(bChan))
                        {
                            TicketSale(bChan, true);
                        }
                    }
                    break;
                case "drawticket":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                    {
                        if (HasRaffle(bChan))
                        {
                            DrawTicket(bChan);
                        }
                    }
                    break;
                case "buyticket":
                    if (HasRaffle(bChan))
                    {
                        if (IsRaffleSelling(bChan))
                        {
                            _raffles.BuyTicket(await Core.UserMan.GetUserByTwitchUserName(e.Command.ChatMessage.Username), bChan);
                        }
                    }
                    else
                    {
                        Core.Twitch._client.SendMessage(
                            e.Command.ChatMessage.Channel,
                            "There is currently no raffle running here.");
                        return;
                    }
                    break;
            }
        }
        private async void TwitchOnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.ChatMessage.Channel);
            if (bChan == null) { return; }
            if (HasRaffle(bChan))
            {
                _raffles.GetRaffle(bChan)._msgSinceLast++;
            }
        }
        #endregion
        #region Discord command methods
        #region Interface default discord command methods
        public async Task SetDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            RaffleSettings settings = await Settings(bChan);
            settings._defaultDiscordChannel = discordChannelID;
            await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync("This is now the active channel for the PLC plugin.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);

        }
        public async Task ClearDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            RaffleSettings settings = await Settings(bChan);
            settings._defaultDiscordChannel = 0;
            await (Core.Discord.GetChannel(discordChannelID) as IMessageChannel).SendMessageAsync("The active channel for the PLC plugin is resetted. All channels now valid.");
            SaveBaseSettings(PLUGINNAME, bChan, settings);
        }
        #endregion
        public async Task DiscordClearRaffle(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            if (!HasRaffle(bChan)) { return; }
            ClearRaffle(bChan);
        }
        public async Task DiscordCancelRaffle(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            if (HasRaffle(bChan))
            {
                CancelRaffle(bChan);
            }
        }
        public async Task DiscordStartRaffle(ICommandContext context, List<string> arguments)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            if (HasRaffle(bChan)) { return; }
            if(arguments.Count != 2)
            {
                await context.Channel.SendMessageAsync("Use \"!startraffle <NumberOfTickets> <TicketPrice>\" to start a raffle.");
                return;
            }
            StartRaffle(bChan, arguments);
        }
        public async Task DiscordRaffleHelp(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            await context.Channel.SendMessageAsync("Use \"!startraffle <NumberOfTickets> <TicketPrice>\" to start a raffle.");
        }
        public async Task DiscordBuyTicket(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            if (HasRaffle(bChan))
            {
                if (IsRaffleSelling(bChan))
                {
                    _raffles.BuyTicket(await Core.UserMan.GetUserByDiscordID(context.User.Id), bChan);
                }
            }
        }
        public async Task DiscordDrawTicket(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan != null)
            {
                DrawTicket(bChan);
            }
        }
        public async Task DiscordStartTicketSale(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            if (HasRaffle(bChan))
            {
                TicketSale(bChan, true);
            }
            
        }
        public async Task DiscordStopTicketSale(ICommandContext context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            if (HasRaffle(bChan))
            {
                TicketSale(bChan, false);
            }
        }

        #endregion

        #region Internal supporting methods
        private void ClearRaffle(BotChannel bChan)
        {
            _raffles.ClearRaffle(bChan);
        }
        private void CancelRaffle(BotChannel bChan)
        {
            if (!_raffles.CancelRaffle(bChan))
            {
                Core.Twitch._client.SendMessage(
                            bChan.TwitchChannelName,
                            "Something went wrong. Raffle was not removed.");
            }

        }
        private void StartRaffle(BotChannel bChan, List<string> arguments)
        {
            int.TryParse(arguments[0], out int number);
            int.TryParse(arguments[1], out int price);
            if (number < 1 || price < 1) { return; }
            _raffles.StartRaffle(bChan, number, price);
        }
        private void TicketSale(BotChannel bChan, bool flag)
        {
            _raffles.SellingTickets(bChan, flag);
        }
        private void DrawTicket(BotChannel bChan)
        {
            _raffles.DrawTicket(bChan);
        }
        private bool HasRaffle(BotChannel bChan)
        {
            return _raffles.HasRaffle(bChan);
        }
        private bool IsRaffleSelling(BotChannel bChan)
        {
            Raffle raffle = _raffles.GetRaffle(bChan);
            if (raffle != null)
            {
                return raffle.isSelling;
            }
            return false;
        }

        #endregion

        #region Important base methods that can't be inherited
        private async Task<RaffleSettings> Settings(BotChannel bChan)
        {
            RaffleSettings settings = new RaffleSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as RaffleSettings;
        }
        #endregion
        #region Interface base methods
        public void OnSecondTick(int seconds)
        {
            if(seconds % 10 == 0)
            {
                _raffles.FireReminders();
            }
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
    }// END of RaffleService class
}
