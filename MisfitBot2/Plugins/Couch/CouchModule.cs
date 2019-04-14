using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MisfitBot2.Services;

namespace MisfitBot2.Modules
{
    class CouchModule : ModuleBase<ICommandContext>
    {
        private readonly CouchService _service;

        public CouchModule(CouchService service)
        {
            _service = service;
        }

        [Command("couch", RunMode = RunMode.Async)]
        [Summary("Base command for the couch plugin.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task CommandMethod([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);
            await _service.DiscordCommand(Context, arguments);
        }

    }
}
