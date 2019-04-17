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
using TwitchLib.Api.V5.Models.Streams;

namespace MisfitBot2.Modules
{
    /// <summary>
    /// This is simple Discord commands that can be resolved without a service
    /// </summary>
    public class SimpleCommands : ModuleBase
    {
        //https://discordapp.com/api/oauth2/authorize?client_id=295257486708047882&scope=bot&permissions=1

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

        [Command("invitejuan", RunMode = RunMode.Async)]
        [Summary("Gives you the link to invite Juan yo your own Discord Guild")]
        public async Task JuanInviteCMD()
        {
            string msg = "Make sure you are logged in on this link for your server to show up https://discordapp.com/api/oauth2/authorize?client_id=295257486708047882&scope=bot&permissions=1";
            SocketUser u = Core.Discord.GetUser(Context.User.Id);
            await Discord.UserExtensions.SendMessageAsync(u, msg );
        }

        [Command("uptime", RunMode = RunMode.Async)]
        [Summary("How long the current stream has been live.")]
        public async Task GameCMD()
        {
            if (Context.User.IsBot) { return; }
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            if (bChan.isLinked == false) { return; }
            StreamByUser stream = await Core.Twitch._api.V5.Streams.GetStreamByUserAsync(bChan.TwitchChannelID);
            string msg = $"{bChan.TwitchChannelName} has been streaming for {(DateTime.Now - stream.Stream.CreatedAt).ToString()} and they've been playing {stream.Stream.Game}.";
            await Context.Channel.SendMessageAsync(msg);
        }





    }
}
