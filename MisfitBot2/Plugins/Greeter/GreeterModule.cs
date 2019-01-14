using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MisfitBot2.Services;

namespace MisfitBot2.Modules
{
    public class GreeterModule : ModuleBase
    {
        private GreeterService gService;

        public GreeterModule(GreeterService service) { gService = service; }

        [Command("greetchannel", RunMode = RunMode.Async)]
        [Summary("Assign the Discord server's default channel for greeting messages.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task GreetCMD()
        {
            await gService.AssignGreetChannel(Context);
        }

        [Command("db_greetchannel", RunMode = RunMode.Async)]
        [Summary("Debug the greetchannel command.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task DebugGreetCMD()
        {
            await gService.DebugAssignGreetChannel(Context);
        }
    }
}
