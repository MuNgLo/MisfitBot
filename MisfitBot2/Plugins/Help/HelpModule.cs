using Discord.Commands;
using MisfitBot2.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MisfitBot2.Modules
{
    class HelpModule : ModuleBase<ICommandContext>
    {
        private readonly HelpService _service;
        HelpModule(HelpService service)
        {
            _service = service;
        }
        [Command("help", RunMode = RunMode.Async)]
        [Summary("You just used it. What did you you think it would be?")]
        public async Task HelpCMD()
        {
            await _service.DiscordHelp(Context);
        }
        [Command("help", RunMode = RunMode.Async)]
        public async Task HelpCMD(string arg)
        {
            int i = 0;
            if (int.TryParse(arg, out i))
            {
                if (i <= 0)
                {
                    return;
                }
                await _service.DiscordHelpPage(Context, i);
            }
            else
            {
                if(arg.Length < 3)
                {
                    await Context.Channel.SendMessageAsync("Search needs to be at least 3 characters.");
                }
                else
                {
                    await _service.DiscordHelpSearch(Context, arg);
                }
            }
        }
    }
}
