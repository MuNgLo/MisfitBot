using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MisfitBot2.Services;

namespace MisfitBot2.Modules
{
    class BettingModule : ModuleBase<ICommandContext>
    {
        private readonly BettingService _service;

        public BettingModule(BettingService service)
        {
            _service = service;
        }

        [Command("bets", RunMode = RunMode.Async)]
        [Summary("Base command for the betting plugin.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task CommandMethod([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);
            await _service.DiscordBets(Context, arguments);
        }


        [Command("bet", RunMode = RunMode.Async)]
        [Summary("Place a bet in the current betting.")]
        public async Task BetCMD([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);
            await _service.DiscordBet(Context, arguments);
        }

    }
}
