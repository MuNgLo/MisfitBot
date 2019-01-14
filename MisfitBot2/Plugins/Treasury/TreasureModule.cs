using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MisfitBot2.Services;

namespace MisfitBot2.Modules
{
    public class TreasureModule : ModuleBase<ICommandContext>
    {


        private readonly TreasureService _service;

        // Remember to add an instance of the AudioService
        // to your IServiceCollection when you initialize your bot
        public TreasureModule(TreasureService service)
        {
            _service = service;
        }

        // You *MUST* mark these commands with 'RunMode.Async'
        // otherwise the bot will not respond until the Task times out.
        [Command("gold", RunMode = RunMode.Async)]
        [Summary("Returns the amount of gold the user has")]
        public async Task GoldCmd()
        {
            await _service.DiscordGoldCMD(Context);
        }

        [Command("gpm", RunMode = RunMode.Async)]
        [Summary("Returns or sets the gold per tick.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SetGoldPerMinute()
        {
            if (Context.User.IsBot) { return; }
            await _service.GoldPerIntervalCMD(Context);
        }

        [Command("gpm", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SetGoldPerMinute(string arg)
        {
            if (Context.User.IsBot) { return; }
            await _service.SetGoldPerIntervalCMD(Context, arg);
        }

        [Command("tickinterval", RunMode = RunMode.Async)]
        [Summary("Returns or sets the gold per tick.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SetTickInterval(string arg)
        {
            if (Context.User.IsBot) { return; }
            await _service.SetTickIntervalCMD(Context, arg);
        }

        //[Command("rob", RunMode = RunMode.Async), Ratelimit(5, 30, Measure.Minutes)]
        /*[Command("rob", RunMode = RunMode.Async)]
        [Summary("Tries to rob the user mentioned.")]
        public async Task RobCMD([Remainder] string song)
        {
            Console.WriteLine("RobCMD module");
            await _service.RobCMD(Context);
        }*/
    }
}
