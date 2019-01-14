using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MisfitBot2.Plugins.MatchMaking;
using MisfitBot2.Services;

namespace MisfitBot2.Modules
{
    class MatchMakingModule : ModuleBase<ICommandContext>
    {
        private readonly MatchMakingService _service;
        public MatchMakingModule(MatchMakingService service)
        {
            _service = service;
        }
        [Command("mm", RunMode = RunMode.Async)]
        [Summary("Manage the Matchmaking stuff. on/off")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task mmCMD(string arg)
        {
            await _service.DiscordCommand(Context, arg);
        }
        [Command("mm", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task mm2CMD([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);
            if (arguments.Count > 1)
            {
                await _service.DiscordCommand(Context, arguments);
            }
        }
        [Command("signup", RunMode = RunMode.Async)]
        [Summary("Sign up to an ongoing queue.")]
        public async Task signupCMD()
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordSignup(Context);
        }
    }
}
