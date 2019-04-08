using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MisfitBot2.Services;

namespace MisfitBot2.Modules
{
    class PoorLifeChoicesModule : ModuleBase<ICommandContext>
    {
        private readonly PoorLifeChoicesService _service;
        public PoorLifeChoicesModule(PoorLifeChoicesService service)
        {
            _service = service;
        }

        [Command("poorlifechoice", RunMode = RunMode.Async)]
        [Summary("Returns a random PLC reply. Has plc as command alias.")]
        public async Task poorlifechoiceMethod()
        {
            await _service.DiscordPoorLifeChoice(Context);
        }

        [Command("plc", RunMode = RunMode.Async)]
        public async Task plcMethod()
        {
            await _service.DiscordPoorLifeChoice(Context);
        }

        [Command("plc", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task poorlifechoiceMethod([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);
            await _service.DiscordCommand(args, Context);

        }

    }
}
