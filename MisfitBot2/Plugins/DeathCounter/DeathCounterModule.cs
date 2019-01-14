using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MisfitBot2.Services;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace MisfitBot2.Modules
{
    class DeathCounterModule : ModuleBase
    {
        private readonly DeathCounterService _deathCounterService;
        DeathCounterModule(DeathCounterService deaths){_deathCounterService = deaths;}

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("dc_channel", RunMode = RunMode.Async)]
        [Summary("Declares used channel as the default deathcounterchannel.")]
        public async Task SetDefChannel()
        {
            await _deathCounterService.SetDefaultDiscordChannel(Context);
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("dc_channelreset", RunMode = RunMode.Async)]
        [Summary("Resets the default deathcounterchannel.")]
        public async Task ResetDefChannel()
        {
            await _deathCounterService.ClearDefaultDiscordChannel(Context);
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("dc_start", RunMode = RunMode.Async)]
        [Summary("Starts a death counter for the linked Twitchchannel.")]
        public async Task StartCounter()
        {
            await _deathCounterService.StartCounter(Context);
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("dc_stop", RunMode = RunMode.Async)]
        [Summary("Stops the running deathcounter for the linked Twitchchannel.")]
        public async Task StopCounter()
        {
            await _deathCounterService.StopCounter(Context);
        }

        [Command("dc_reset", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Resets the running deathcounter for the linked Twitchchannel.")]
        public async Task ResetCounter()
        {
            await _deathCounterService.ResetCounter(Context);
        }

        [Command("add", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Adds a death to the running deathcounter for the linked Twitchchannel.")]
        public async Task AddCounter()
        {
            await _deathCounterService.AddCounter(Context);
        }

        [Command("del", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Removes a death to the running deathcounter for the linked Twitchchannel.")]
        public async Task DelCounter()
        {
            await _deathCounterService.DelCounter(Context);
        }

        [Command("deaths", RunMode = RunMode.Async)]
        [Summary("Returns the current number of deaths.")]
        public async Task Deaths()
        {
            await _deathCounterService.Deaths(Context);
        }
    }
}
