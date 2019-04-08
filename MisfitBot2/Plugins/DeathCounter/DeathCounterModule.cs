using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MisfitBot2.Services;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace MisfitBot2.Modules
{
    class DeathCounterModule : ModuleBase
    {
        private readonly DeathCounterService _deathCounterService;
        DeathCounterModule(DeathCounterService deaths){_deathCounterService = deaths;}

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("dc", RunMode = RunMode.Async)]
        [Summary("Deathcounter base command.")]
        public async Task DeathCounter([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);
            await _deathCounterService.DiscordCommand(args, Context);
        }

        [Command("deaths", RunMode = RunMode.Async)]
        [Summary("Returns the current number of deaths.")]
        public async Task Deaths()
        {
            await _deathCounterService.Deaths(Context);
        }
    }
}
