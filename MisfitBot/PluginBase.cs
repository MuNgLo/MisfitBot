using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using MisfitBot_MKII.Statics;

namespace MisfitBot_MKII
{
    /// <summary>
    /// Any service class should inherit from this class
    /// </summary>
    public abstract class PluginBase : PluginBaseDB
    {
        abstract public void OnSecondTick(int seconds);
        abstract public void OnMinuteTick(int minutes);
        abstract public void OnUserEntryMergeEvent(MisfitBot_MKII.UserEntry discordUser, MisfitBot_MKII.UserEntry twitchUser);
        abstract public void OnBotChannelEntryMergeEvent(MisfitBot_MKII.BotChannel discordGuild, MisfitBot_MKII.BotChannel twitchChannel);

        public char CMC { get { return Program.CommandCharacter; } set {}}
        public string version = "0.0";

        public async Task MakeConfig<T>(BotChannel bChan, string plugin, T obj){
            await Core.Configs.ConfigSetup<T>(bChan, plugin, obj);
        }
        /// <summary>
        /// Returns saved settings from DB or creates a new entry in DB and returns that.
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="plugin"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> Settings<T>(BotChannel bChan, string plugin)
        {
            T settings = await Core.Configs.GetConfig<T>(bChan, plugin);
            return settings;
        }

        public async void Respond(BotChannel bChan, BotWideResponseArguments args)
        {
            if(args.parseMessage){
                args.message = StringFormatter.ConvertMessage(ref args);
            }

            if(args.discordChannel != 0){
                await MisfitBot_MKII.DiscordWrap.DiscordClient.DiscordResponse(args);
            }
            if(args.twitchChannel != null){
                Program.TwitchResponse(args);
            }
        }

        public async Task<BotChannel> GetBotChannel(BotWideCommandArguments args){
            if(args.source == MESSAGESOURCE.TWITCH){
               return await Program.Channels.GetTwitchChannelByName(args.channel);
            }
            return await Program.Channels.GetDiscordGuildbyID(args.guildID);
        }

        public async Task<BotChannel> GetBotChannel(BotWideMessageArguments args){
            if(args.source == MESSAGESOURCE.TWITCH){
               return await Program.Channels.GetTwitchChannelByName(args.channel);
            }
            return await Program.Channels.GetDiscordGuildbyID(args.guildID);
        }

        public async Task SayOnDiscord(BotChannel bChan, string message)
        {
            if (bChan.discordDefaultBotChannel != 0)
            {
                await (Program.DiscordClient.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(message);
                return;
            }
        }
        public async Task SayOnDiscord(string message, string discordChannel)
        {
            await SayOnDiscord(message, Core.StringToUlong(discordChannel));
        }
        public async Task SayOnDiscord(string message, ulong discordChannel)
        {
            await (Program.DiscordClient.GetChannel(discordChannel) as ISocketMessageChannel).SendMessageAsync(message);
        }
        public async Task SayOnDiscordAdmin(BotChannel bChan, string message)
        {
            // check if we are connected to discord first
            if(Program.DiscordClient.Status != UserStatus.Online)
            {
                await Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "ServiceBase", $"SayOnDiscordAdmin({bChan.discordAdminChannel},  {message}) failed because online check failed."));
                return;
            }

            if (bChan.discordAdminChannel != 0)
            {
                await (Program.DiscordClient.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(message);
                return;
            }
        }
        public async Task SayEmbedOnDiscordAdmin(BotChannel bChan, Embed obj)
        {
            if (bChan.discordAdminChannel != 0)
            {
                await (Program.DiscordClient.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync("", false, obj);
                return;
            }
        }

        public async Task SayEmbedOnDiscord(ulong channel, Embed obj)
        {
            await (Program.DiscordClient.GetChannel(channel) as ISocketMessageChannel).SendMessageAsync("", false, obj);
        }
    }
}
