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

        [Command("raffle", RunMode = RunMode.Async)]
        [Summary("Base command for the Raffle plugin.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task CommandMethod()
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordCommand(Context);
        }
        [Command("raffle", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task CommandMethod([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);
            await _service.DiscordCommand(Context, arguments);
        }


        [Command("buyticket", RunMode = RunMode.Async)]
        [Summary("Buys a ticket if possible and affordable")]
        public async Task BuyTicketCMD()
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordBuyTicket(Context);
        }

       

    }
}
