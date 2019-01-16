using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MisfitBot2.Plugins.Admin;
using MisfitBot2.Services;
namespace MisfitBot2.Modules
{
    class AdminModule : ModuleBase<ICommandContext>
    {
        private readonly AdminService _service;

        public AdminModule(AdminService service)
        {
            _service = service;
        }

        [Command("pubsub", RunMode = RunMode.Async)]
        [Summary("restart > Kills and relaunches the Twitch PubSub for the channel.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task PubSubCMD([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);
            if (arguments.Count < 1) { return; }
            switch (arguments[0].ToLower())
            {
                case "clear":
                    bChan.pubsubOauth = string.Empty;
                    await Core.Channels.ChannelSave(bChan);
                    await Context.Channel.SendMessageAsync("Current PubSub Token cleared.");
                    break;
                case "restart":
                    await Core.Channels.RestartPubSub(bChan);
                    break;
                case "setup":
                    // https://twitchtokengenerator.com/quick/sRgrU5bimJ
                    await Context.Channel.SendMessageAsync("To start your PubSub setup you need a token from this link https://twitchtokengenerator.com/quick/sRgrU5bimJ It is to generate a token specific for your Twitch channel. To later remove access through this token you remove it on Twitch under settings>Connections. It will be called \"Twitch Token Generator by swiftyspiffy\". Then run !pubsub set <TOKEN>");
                    break;
                case "set":
                    if (arguments.Count < 2) { return; }
                    arguments[1] = Crypto.Cipher.Encrypt(arguments[1]);
                    await Context.Message.DeleteAsync();
                    await _service.DiscordSetPubSubOauth(Context, arguments[1]);
                    break;
                case "start":
                    Core.Channels.StartPubSub(bChan);
                    break;
                default:
                    break;
            }
        }
        [Command("adminchan", RunMode = RunMode.Async)]
        [Summary("Declares the channel to be the one issue admin commands. This should not be a public channel. Use adminchan clear to clear the saved channel.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task AdminChanCMD()
        {
            if (Context.User.IsBot) { return; }
            await _service.SetAdminChannel(Context);
        }
        [Command("adminchan", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task AdminChanCMD(string arg)
        {
            if (Context.User.IsBot) { return; }
            if (arg.ToLower() == "clear")
            {
                await _service.ResetAdminChannel(Context);
            }
        }
        [Command("botchan", RunMode = RunMode.Async)]
        [Summary("Sets the default channel for bot output on the Discord Guild. Use botchan clear to clear the stored channel.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task BotChanCMD()
        {
            await _service.SetDefaultChannel(Context);
        }
        [Command("botchan", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task BotChanCMD(string arg)
        {
            if(arg.ToLower() == "clear")
            {
                await _service.ResetDefaultBotChannel(Context);
            }
        }
        [Command("admin", RunMode = RunMode.Async)]
        [Summary("Admin module information. Use admin on/off to turn the module on or off.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task BotAdminCMD()
        {
                await _service.DiscordAdminInfo(Context);
                return;
        }
        [Command("admin", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task BotAdminCMD(string arg)
        {
            if (arg.ToLower() == "on")
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
        [Command("link", RunMode = RunMode.Async)]
        [Summary("Links a Discord channel to a Twitch channel.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task LinkCMD(string arg)
        {
            await _service.DiscordLinkChannelCommand(Context, arg);
        }
        [Command("tw_join", RunMode = RunMode.Async)]
        [Summary("Tries to join a Twitch channel. Automatically creates a BotChannel entry for it. Autojoin is on by default.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task JoinTwitchChannelCMD(string arg)
        {
            await _service.DiscordJoinTwitchChannel(Context, arg);
        }
        [Command("reconnect", RunMode = RunMode.Async)]
        [Summary("Reinitialize the the Discord client. Note that Twitch runs under it as an extension.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task ReconnectCMD()
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, "AdminModule", "Reconnecting"));
            Program.DiscordReconnect();
        }







    }
}
