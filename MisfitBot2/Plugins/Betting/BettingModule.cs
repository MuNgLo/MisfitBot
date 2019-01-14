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

        [Command("bettingchan", RunMode = RunMode.Async)]
        [Summary("Declares the channel to be the one issue admin commands. This should not be a public channel.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task BettingChanCMD()
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordSetChannel(Context);
        }

        [Command("betting", RunMode = RunMode.Async)]
        [Summary("Sets the default channel for bot output on the Discord Guild.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task BettingCMD(string arg)
        {
            if (arg.ToLower() == "on")
            {
                await _service.DiscordSetActive(true, Context);
                return;
            }
            if (arg.ToLower() == "off")
            {
                await _service.DiscordSetActive(false, Context);
                return;
            }
            await Context.Channel.SendMessageAsync("This command only takes the arguments \"on\" or \"off\"");
        }

        [Command("openbets", RunMode = RunMode.Async)]
        [Summary("Starts betting between the given options.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task StartBetCMD([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);
            await _service.DiscordStartBetting(Context, arguments);
        }
        [Command("closebets", RunMode = RunMode.Async)]
        [Summary("Ends the current betting with the given option as winner.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task StopBetCMD(string arg)
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordStopBetting(Context, arg);
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
