using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MisfitBot_MKII.Statics;

namespace MisfitBot_MKII.DiscordWrap
{
    public static class DiscordClient{
        public static async Task ClearReactionsOnMessage(DiscordChannelMessage dMessage){
            if(Program.DiscordClient.ConnectionState != ConnectionState.Connected){return;}
            IMessage message = await (Program.DiscordClient.GetChannel(dMessage.ChannelID) as ISocketMessageChannel).GetMessageAsync(dMessage.MessageID);
            await message.RemoveAllReactionsAsync();
        }
        public static async Task ReactionAdd(DiscordChannelMessage dMessage,string emoteName){
            if(Program.DiscordClient.ConnectionState != ConnectionState.Connected){return;}
            IMessage message = await (Program.DiscordClient.GetChannel(dMessage.ChannelID) as ISocketMessageChannel).GetMessageAsync(dMessage.MessageID);
            GuildEmote emote;
            // Check if emote is unicode
            if (Char.IsSurrogate(emoteName, 0)){
                // Resolve Unicode to emote object
                Emoji heartEmoji = new Emoji(emoteName);
                await message.AddReactionAsync(heartEmoji);
            }else{
                // resolve custom emote
                string[] parts = emoteName.Split(':');
                emote = (message.Channel as SocketGuildChannel).Guild.Emotes.FirstOrDefault(x => x.Name == parts[1]);
                await message.AddReactionAsync(emote);
            }
        }
        public static async Task DiscordSayMessage(string channel, string message)
        {
            await DiscordSayMessage(Core.StringToUlong(channel), message);
        }
        public static async Task DiscordSayMessage(ulong channel, string message)
        {
            if(Program.DiscordClient.ConnectionState != ConnectionState.Connected){return;}
            await (Program.DiscordClient.GetChannel(channel) as ISocketMessageChannel).SendMessageAsync(message);
        }
        public static async Task DiscordResponse(BotWideResponseArguments args)
        {
            SocketChannel sChannel = Program.DiscordClient.GetChannel(args.discordChannel);
            SocketGuildUser sUser = (sChannel as SocketGuildChannel).Guild.GetUser(args.user._discordUID);
            // Create permissions list
            ChannelPermissions asd = sUser.GetPermissions(sChannel as IGuildChannel);
            if(!asd.SendMessages){
                // Can't send to channel so aborting
                return;
            }
            await (sChannel as ISocketMessageChannel).SendMessageAsync(args.message);
        }
        public static async Task<bool> RoleAddUser(BotChannel bChan, UserEntry user, string role){
            IGuild iGuild = Program.DiscordClient.GetGuild(bChan.GuildID) as IGuild;
            if(iGuild == null) {return false;}
            SocketRole sRole = iGuild.Roles.FirstOrDefault(x => x.Name == role) as SocketRole;
            if(sRole == null) {return false;}
            RequestOptions options = new RequestOptions();
            options.Timeout = 1000;
            options.RetryMode = RetryMode.RetryTimeouts;
            IGuildUser iUser = await iGuild.GetUserAsync(user._discordUID, CacheMode.AllowDownload, options);
            if(iUser == null) {return false;}

            await iUser.AddRoleAsync(sRole as IRole);

            return true;
        }
        public static async Task<bool> RoleRemoveUser(BotChannel bChan, UserEntry user, string role){
            SocketGuild sGuild = Program.DiscordClient.GetGuild(bChan.GuildID);
            if(sGuild == null) {return false;}
            SocketRole sRole = sGuild.Roles.FirstOrDefault(x => x.Name == role);
            if(sRole == null) {return false;}
            IGuildUser iUser = await (sGuild as IGuild).GetUserAsync(user._discordUID);
            if(iUser == null) {return false;}

            await iUser.RemoveRoleAsync(sRole as IRole);

            return true;
        }
        public static bool DiscordRoleExist(BotChannel bChan, string role)
        {
            SocketRole sRole = Program.DiscordClient.GetGuild(bChan.GuildID).Roles.FirstOrDefault(x => x.Name == role);
            if(sRole != null){
                return true;
            }
            return false;
        }
        public static async Task<DiscordChannelMessage> DiscordGetMessage(ulong chID, ulong msgID){
            IMessage message = await (Program.DiscordClient.GetChannel(chID) as ISocketMessageChannel).GetMessageAsync(msgID);
            if(message == null){
                return null;
            }
            return new DiscordChannelMessage(message);
        }
        public static bool DiscordEmoteExist(BotChannel bChan, string emote)
        {
            GuildEmote gEmote = Program.DiscordClient.GetGuild(bChan.GuildID).Emotes.FirstOrDefault(x => x.Name == emote);
            if(gEmote != null){
                return true;
            }
            return false;
        }
    }// EOF CLASS
}