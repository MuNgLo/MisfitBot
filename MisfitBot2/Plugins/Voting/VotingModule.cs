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
        /* Example Commands
        
        [Command("commandtext", RunMode = RunMode.Async)]
        [Summary("Command description.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task CommandMethod(string arg)
        {
            if (Context.User.IsBot) { return; }
            await _service.SetTickIntervalCMD(Context, arg);
        }

        [Command("commandtext", RunMode = RunMode.Async)]
        [Summary("Command description.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task CommandMethod(string arg) <-- NOTE!! the signature have to be accurate
        {
            if (Context.User.IsBot) { return; }
            Console.WriteLine(Context.User.Username);
        }
        */
        [Command("startvote", RunMode = RunMode.Async)]
        [Summary("Starts a vote.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task StartVoteCMD([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);
            await _service.DiscordStartVote(Context, arguments);
        }
        [Command("stopvote", RunMode = RunMode.Async)]
        [Summary("Stops an active vote.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task StopVoteCMD()
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordStopVote(Context);
        }
        [Command("closevote", RunMode = RunMode.Async)]
        [Summary("Stops an active vote.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task CloseVoteCMD()
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordStopVote(Context);
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
