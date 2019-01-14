using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using MisfitBot2.Services;
using Discord;
using System.Threading.Tasks;

namespace MisfitBot2.Modules
{
    class RaffleModule : ModuleBase<ICommandContext>
    {
        private readonly RaffleService _service;

        // Remember to add an instance of the AudioService
        // to your IServiceCollection when you initialize your bot
        public RaffleModule(RaffleService service)
        {
            _service = service;
        }

        [Command("clearraffle", RunMode = RunMode.Async)]
        [Summary("Removes any running raffle")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task ClearRaffleCMD()
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordClearRaffle(Context);
        }
        [Command("cancelraffle", RunMode = RunMode.Async)]
        [Summary("Clears any running raffle and refunds undrawn tickets")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task CancelRaffleCMD()
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordCancelRaffle(Context);
        }
        [Command("startraffle", RunMode = RunMode.Async)]
        [Summary("Start a raffle in this channel and linked Twitch channel(if it exist).")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task StartRaffleCMD()
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordRaffleHelp(Context);
        }
        [Command("startraffle", RunMode = RunMode.Async)]
        [Summary("Start a raffle in this channel and linked Twitch channel(if it exist).")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task StartRaffleCMD([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);

            await _service.DiscordStartRaffle(Context, arguments);
        }
        [Command("buyticket", RunMode = RunMode.Async)]
        [Summary("Buys a ticket if possible and affordable")]
        public async Task BuyTicketCMD()
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordBuyTicket(Context);
        }
        [Command("drawticket", RunMode = RunMode.Async)]
        [Summary("Draws a ticket from the pool of sold tickets")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task DrawTicketCMD()
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordDrawTicket(Context);
        }
        [Command("stopticketsale", RunMode = RunMode.Async)]
        [Summary("Stops sellling tickets for current raffle")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task StopTicketSale()
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordStopTicketSale(Context);
        }
        [Command("startticketsale", RunMode = RunMode.Async)]
        [Summary("Starts sellling tickets for current raffle")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task StartTicketSale()
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordStartTicketSale(Context);
        }

    }
}
