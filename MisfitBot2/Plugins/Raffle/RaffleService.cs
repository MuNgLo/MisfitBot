using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MisfitBot2.Components;
using MisfitBot2.Plugins.Raffle;
using TwitchLib.Client.Events;

namespace MisfitBot2.Services
{
    public class RaffleService : ServiceBase, IService
    {
        public readonly string PLUGINNAME = "Raffle";
        private RunningRaffles _raffles;
        private DatabaseStrings dbStrings;

        // CONSTRUCTOR
        public RaffleService()
        {
            dbStrings = new DatabaseStrings(PLUGINNAME);
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
                case "raffle":
                    if( e.Command.ArgumentsAsList.Count < 1)
                    {
                        return;
                    }
                    if (!e.Command.ChatMessage.IsModerator && !e.Command.ChatMessage.IsBroadcaster)
                    {
                        return;
                    }
                    switch (e.Command.ArgumentsAsList[0])
                    {
                        case "clear":
                            if (HasRaffle(bChan))
                            {
                                ClearRaffle(bChan);
                            }
                            break;
                        case "cancel":
                            if (HasRaffle(bChan))
                            {
                                CancelRaffle(bChan);
                            }
                            break;
                        case "start":
                            if (HasRaffle(bChan) == false)
                            {
                                if (e.Command.ArgumentsAsList.Count == 1)
                                {
                                    Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, "Use \"!startraffle <NumberOfTickets> <TicketPrice>\" to start a raffle.");
                                    return;
                                }
                                if (e.Command.ArgumentsAsList.Count != 3)
                                {
                                    return;
                                }
                                StartRaffle(bChan, e.Command.ArgumentsAsList);
                            }
                            break;
                        case "stopsale":
                            if (HasRaffle(bChan))
                            {
                                TicketSale(bChan, false);
                            }
                            break;
                        case "startsale":
                            if (HasRaffle(bChan))
                            {
                                TicketSale(bChan, true);
                            }
                            break;
                        case "draw":
                            if (HasRaffle(bChan))
                            {
                                DrawTicket(bChan);
                            }
                            break;
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
        #region Discord command methods
        public async Task DiscordCommand(ICommandContext context)
        {
            if (context.User.IsBot) { return; }
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            await dbStrings.TableInit(bChan);
            RaffleSettings settings = await Settings(bChan);
            // Ugly bit coming here
            string helptext = $"```fix{Environment.NewLine}" +
                $"Admin/Broadcaster commands{Environment.NewLine}{Environment.NewLine}{Core._commandCharacter}raffle < Arguments >{Environment.NewLine}{Environment.NewLine}Arguments....{Environment.NewLine}" +
            $"on/off -> Turns plugin on or off.{Environment.NewLine}" +
            $"clear -> Removes any active raffle.{Environment.NewLine}" +
            $"cancel -> Cancels the raffle and refunds undrawn tickets.{Environment.NewLine}" +
            $"draw -> Draws one of the sold tickets.{Environment.NewLine}" +
            $"stopsale -> Halts the sale of tickets.{Environment.NewLine}" +
            $"startsale -> Allows the sale of tickets.{Environment.NewLine}" +
            $"start <NumberOfTickets> <TicketPrice> -> Starts a raffle with X amount of tickets for Y price each.{Environment.NewLine}" +
            $"{Environment.NewLine}{Environment.NewLine}User commands{Environment.NewLine}" +
            $"{Core._commandCharacter}buyticket -> Buys a ticket if the user has enough point.{Environment.NewLine}" +
            $"```";
            await SayOnDiscordAdmin(bChan, helptext);
            return;
        }
        public async Task DiscordCommand(ICommandContext Context, List<string> arguments)
        {
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info,
                PLUGINNAME,
                $"{Context.User.Username} used command \"couch\" in {Context.Channel.Name}."
                ));
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            await dbStrings.TableInit(bChan);
            RaffleSettings settings = await Settings(bChan);
            switch (arguments[0].ToLower())
            {
                case "on":
                    settings._active = true;
                    SaveBaseSettings(PLUGINNAME, bChan, settings);
                    await SayOnDiscordAdmin(bChan,
                    $"Raffle is active."
                    );
                    break;
                case "off":
                    settings._active = false;
                    SaveBaseSettings(PLUGINNAME, bChan, settings);
                    await SayOnDiscordAdmin(bChan,
                     $"Raffle is inactive."
                     );
                    break;
                case "clear":
                    if (!settings._active) { return; }
                    await DiscordClearRaffle(Context);
                    break;
                case "cancel":
                    if (!settings._active) { return; }
                    await DiscordCancelRaffle(Context);
                    break;
                case "draw":
                    if (!settings._active) { return; }
                    await DiscordDrawTicket(Context);
                    break;
                case "stopsale":
                    await DiscordStopTicketSale(Context);
                    break;
                case "starsale":
                    await DiscordStartTicketSale(Context);
                    break;
                case "start":
                    if (arguments.Count == 1)
                    {
                        await DiscordRaffleHelp(Context);
                        return;
                    }
                    await DiscordStartRaffle(Context, arguments);
                    break;
            }
        }
        #endregion

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
        /// <summary>
        /// Removes all active raffle entries from the botchannel
        /// </summary>
        /// <param name="bChan"></param>
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
            if(arguments.Count != 3)
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
            await context.Channel.SendMessageAsync($"Use \"{Core._commandCharacter}startraffle <NumberOfTickets> <TicketPrice>\" to start a raffle.");
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
        /// <summary>
        /// Removes all active raffle entries from the botchannel
        /// </summary>
        /// <param name="bChan"></param>
        private void ClearRaffle(BotChannel bChan)
        {
            _raffles.ClearRaffle(bChan);
        }
        /// <summary>
        /// Cancels the raffle and refunds undrawn tickets
        /// </summary>
        /// <param name="bChan"></param>
        /// <returns></returns>
        private void CancelRaffle(BotChannel bChan)
        {
            if (!_raffles.CancelRaffle(bChan))
            {
                Core.Twitch._client.SendMessage(
                            bChan.TwitchChannelName,
                            "Something went wrong. Raffle was not removed.");
                if (bChan.discordAdminChannel != 0)
                {
                    (Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                        $"Something went wrong. Raffle was not removed."
                        );
                }
            }

        }
        private void StartRaffle(BotChannel bChan, List<string> arguments)
        {
            arguments.RemoveAt(0);
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
