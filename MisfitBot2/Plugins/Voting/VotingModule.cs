using System;
using System.Collections.Generic;
using System.Text;
using MisfitBot2.Plugins.Voting;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MisfitBot2.Services;

namespace MisfitBot2.Modules
{
    class VotingModule : ModuleBase<ICommandContext>
    {
        private readonly VotingService _service;

        public VotingModule (VotingService service)
        {
            _service = service;
        }

        [Command("votes", RunMode = RunMode.Async)]
        [Summary("Base command for the voting plugin.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task CommandMethod([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);
            await _service.DiscordCommand(Context, arguments);
        }

        [Command("vote", RunMode = RunMode.Async)]
        [Summary("Cast a vote in an active election.")]
        public async Task VoteCMD(string arg)
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordCastVote(Context, arg.ToLower());
        }
    }
}
