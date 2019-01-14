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

        // Remember to add an instance of the AudioService
        // to your IServiceCollection when you initialize your bot
        public PoorLifeChoicesModule(PoorLifeChoicesService service)
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
        public async Task poorlifechoiceMethod(string arg)
        {
            if(arg.ToLower() == "on")
            {
                await _service.DiscordSetActive(true, Context);
                return;
            }
            if (arg.ToLower() == "off")
            {
                await _service.DiscordSetActive(false, Context);
                return;
            }
            await Context.Channel.SendMessageAsync("This command only takes the arguments \"on\" or \"off\"");
        }

        [Command("plc_channel", RunMode = RunMode.Async)]
        [Summary("The designated channel for the PLC plugin. Use plc_channel clear to clear the stored channel.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task clearPLCChannel(string arg)
        {
            if (arg.ToLower() == "clear")
            {
                BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
                if (bChan == null) { return; }
                await _service.ClearDefaultDiscordChannel(bChan, Context.Channel.Id);
                return;
            }
            await Context.Channel.SendMessageAsync("This command only takes the argument \"clear\".");
        }
        [Command("plc_channel", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task PLCChannel()
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            await _service.SetDefaultDiscordChannel(bChan, Context.Channel.Id);
                return;

        }
    }
}
