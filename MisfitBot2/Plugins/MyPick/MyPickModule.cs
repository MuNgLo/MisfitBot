using System;
using System.Collections.Generic;
using System.Text;
using MisfitBot2.Plugins.MyPick;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using MisfitBot2.Services;
namespace MisfitBot2.Modules
{
    class MyPickModule : ModuleBase<ICommandContext>
    {
        private readonly MyPickService _service;

        // Remember to add an instance of the AudioService
        // to your IServiceCollection when you initialize your bot
        public MyPickModule(MyPickService service)
        {
            _service = service;
        }
        
        [Command("picks", RunMode = RunMode.Async)]
        [Summary("Shows the current picks. Admins use nominate/denominate to handle nominees.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task CommandMethod([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);
            await _service.DiscordPicks(Context, arguments);
        }
        [Command("picks", RunMode = RunMode.Async)]
        public async Task CommandMethod()
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordTopList(Context);
        }
        [Command("mypick", RunMode = RunMode.Async)]
        [Summary("Used to cast vote on a nominee.")]
        public async Task MyPickMethod(string text)
        {
            if (Context.User.IsBot) { return; }
            await _service.DiscordMyPick(Context, text);
        }
    }
}
