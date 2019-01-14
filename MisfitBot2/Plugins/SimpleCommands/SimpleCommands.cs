using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MisfitBot2.Services;
using MisfitBot2;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace MisfitBot2.Modules
{
    public class SimpleCommands : ModuleBase
    {
        // You *MUST* mark these commands with 'RunMode.Async'
        // otherwise the bot will not respond until the Task times out.

        [Command("juanage", RunMode = RunMode.Async)]
        [Summary("Returns the uptime of Juan")]
        public async Task JuanAgeCMD()
        {
            TimeSpan time = TimeSpan.FromSeconds(Core.UpTime);

            //here backslash is must to tell that colon is
            //not the part of format, it just a character that we want in output
            string str = time.ToString(@"d\ \d\a\y\s\ \a\n\d\ \ hh\:mm\:ss");

            //seconds -= minutes * 60;
            //minutes -= hours * 60;

            string msg = $"I have been running for {str}";
            await Context.Channel.SendMessageAsync(msg);
        }


        [Command("setup", RunMode = RunMode.Async)]
        [Summary("Link your Discord user with a Twitch username. Note Twitch username is all lowercase and visible in the address to your channel page.")]
        public async Task IsMeCMD()
        {
            await Core.UserMan.LinkTokenRequest(Context.User.Id, Context.Channel);
        }


        


    }
}
