﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MisfitBot_MKII.Services;
using TwitchLib.Api.V5.Models.Channels;
using TwitchLib.Api.V5.Models.Streams;

namespace MisfitBot_MKII.Modules
{
    class AdminModule : ModuleBase<ICommandContext>
    {
        private readonly AdminService _service;

        public AdminModule(AdminService service)
        {
            _service = service;
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
        public async Task BotAdminCMD([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);
            await _service.DiscordCommand(Context, arguments);
        }




        [Command("usersetup", RunMode = RunMode.Async)]
        [Summary("Link your Discord user with a Twitch username. Note Twitch username is all lowercase and visible in the address to your channel page.")]
        public async Task IsMeCMD()
        {
            await Program.Users.LinkTokenRequest(Context.User.Id, Context.Channel);
        }

        [Command("pubsub", RunMode = RunMode.Async)]
        [Summary("clear > Removes current token." +
            "restart > Kills and relaunches the Twitch PubSub for the channel." +
            "setup > Instructions on how to get and use token." +
            "set <TOKEN> > Sets the token. Also deletes the message to hide token." +
            "start > Tries to launch the PubSub listener.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task PubSubCMD()
        {
            if (Context.User.IsBot) { return; }
            BotChannel bChan = await Program.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            await Context.Channel.SendMessageAsync("restart > Kills and relaunches the Twitch PubSub for the channel." +
            "setup > Instructions on how to get and use token." +
            "set <TOKEN> > Sets the token. Also deletes the message to hide token." +
            "start > Tries to launch the PubSub listener.");
        }
        [Command("pubsub", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task PubSubCMD([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            BotChannel bChan = await Program.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            string[] args = text.Split(" ");
            List<string> arguments = new List<string>(args);
            if (arguments.Count < 1) { return; }
            switch (arguments[0].ToLower())
            {
                case "clear":
                    bChan.pubsubOauth = string.Empty;
                    Program.Channels.ChannelSave(bChan);
                    await Context.Channel.SendMessageAsync("Current PubSub Token cleared.");
                    break;
                case "restart":
                    await Program.Channels.RestartPubSub(bChan);
                    break;
                case "setup":
                    // https://twitchtokengenerator.com/quick/YfuRoOx9WW
                    await Context.Channel.SendMessageAsync($"To start your PubSub setup you need a token from this link https://twitchtokengenerator.com/quick/YfuRoOx9WW " +
                        $"It is to generate a token specific for your Twitch channel. To later remove access through this token you remove it on Twitch under " +
                        $"settings>Connections. It will be called \"Twitch Token Generator by swiftyspiffy\". Then run {Program.CommandCharacter}pubsub set <TOKEN>");
                    break;
                case "set":
                    if (arguments.Count < 2) { return; }
                    arguments[1] = Crypto.Cipher.Encrypt(arguments[1]);
                    await Context.Message.DeleteAsync();
                    await _service.DiscordSetPubSubOauth(Context, arguments[1]);
                    break;
                case "start":
                    Program.Channels.StartPubSub(bChan);
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


        


        [Command("link", RunMode = RunMode.Async)]
        [Summary("Links a Discord channel to a Twitch channel.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task LinkCMD(string arg)
        {
            await _service.DiscordLinkChannelCommand(Context, arg);
        }
        [Command("twitchjoin", RunMode = RunMode.Async)]
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


        [Command("game", RunMode = RunMode.Async)]
        [Summary("Set the game for the stream on the linked twitchchannel.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task GameCMD()
        {
            if (Context.User.IsBot) { return; }
            BotChannel bChan = await Program.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            if (bChan.pubsubOauth == string.Empty)
            {
                await Context.Channel.SendMessageAsync($"To use this command you need to setup a pubsub token. See command {Program.CommandCharacter}pubsub.");
                return;
            }
            if (bChan.isLinked == false) { return; }
            StreamByUser stream = await Program.TwitchAPI.V5.Streams.GetStreamByUserAsync(bChan.TwitchChannelID);
            string msg = $"{stream.Stream.Channel.DisplayName} is streaming {stream.Stream.Channel.Status} ({stream.Stream.Game}) for {stream.Stream.Viewers} viewers.";
            await _service.SayOnDiscordAdmin(bChan, msg);
        }
        [Command("game", RunMode = RunMode.Async)]
        [Summary("Set the game for the stream on the linked twitchchannel.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task GameCMD([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            BotChannel bChan = await Program.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            if (bChan.pubsubOauth == string.Empty)
            {
                await Context.Channel.SendMessageAsync($"To use this command you need to setup a pubsub token. See command {Program.CommandCharacter}pubsub.");
                return;
            }
            if (bChan.isLinked == false) { return; }
            string game = text.Trim();

            StreamByUser stream = await Program.TwitchAPI.V5.Streams.GetStreamByUserAsync(bChan.TwitchChannelID);

            Channel channel = await Program.TwitchAPI.V5.Channels.UpdateChannelAsync(
                bChan.TwitchChannelID,
                stream.Stream.Channel.Status,
                game,
                stream.Stream.Delay.ToString(),
                true,
                Crypto.Cipher.Decrypt(bChan.pubsubOauth));
            stream = await Program.TwitchAPI.V5.Streams.GetStreamByUserAsync(bChan.TwitchChannelID);
            string msg = $"{stream.Stream.Channel.DisplayName} is streaming {stream.Stream.Channel.Status} ({stream.Stream.Game}) for {stream.Stream.Viewers} viewers.";
            await _service.SayOnDiscordAdmin(bChan, msg);
        }


        [Command("title", RunMode = RunMode.Async)]
        [Summary("Set the game for the stream on the linked twitchchannel.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task TitleCMD()
        {
            if (Context.User.IsBot) { return; }
            BotChannel bChan = await Program.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            if (bChan.pubsubOauth == string.Empty)
            {
                await Context.Channel.SendMessageAsync($"To use this command you need to setup a pubsub token. See command {Program.CommandCharacter}pubsub.");
                return;
            }
            if (bChan.isLinked == false)
            {
                await Context.Channel.SendMessageAsync($"To use this command you need to link this Discord to a twitch channel. See command {Program.CommandCharacter}link.");
                return;
            }
            StreamByUser stream = await Program.TwitchAPI.V5.Streams.GetStreamByUserAsync(bChan.TwitchChannelID);
            string msg = $"{stream.Stream.Channel.DisplayName} is streaming {stream.Stream.Channel.Status} ({stream.Stream.Game}) for {stream.Stream.Viewers} viewers.";
            await _service.SayOnDiscordAdmin(bChan, msg);
        }

        [Command("title", RunMode = RunMode.Async)]
        [Summary("Set the game for the stream on the linked twitchchannel.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task TitleCMD([Remainder]string text)
        {
            if (Context.User.IsBot) { return; }
            BotChannel bChan = await Program.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            if (bChan.pubsubOauth == string.Empty) {
                await Context.Channel.SendMessageAsync($"To use this command you need to setup a pubsub token. See command {Program.CommandCharacter}pubsub.");
                return; }
            if (bChan.isLinked == false) {
                await Context.Channel.SendMessageAsync($"To use this command you need to link this Discord to a twitch channel. See command {Program.CommandCharacter}link.");
                return; }
            string title = text.Trim();
            StreamByUser stream = await Program.TwitchAPI.V5.Streams.GetStreamByUserAsync(bChan.TwitchChannelID);
            Channel channel = await Program.TwitchAPI.V5.Channels.UpdateChannelAsync(
                bChan.TwitchChannelID,
                title,
                stream.Stream.Game,
                stream.Stream.Delay.ToString(),
                true,
                Crypto.Cipher.Decrypt(bChan.pubsubOauth));
            stream = await Program.TwitchAPI.V5.Streams.GetStreamByUserAsync(bChan.TwitchChannelID);
            string msg = $"{stream.Stream.Channel.DisplayName} is streaming {stream.Stream.Channel.Status} ({stream.Stream.Game}) for {stream.Stream.Viewers} viewers.";
            await _service.SayOnDiscordAdmin(bChan, msg);
        }




    }
}