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


        private readonly string _pluginName;
        private readonly string cmd;

        private readonly string pluginInfo = "No info";
        public string PluginName { get => _pluginName; private set {} }
        public string PluginInfo { get => pluginInfo; private set {} }
        public string CMD { get => cmd; private set {} }

        private readonly int _version = 0;
        public int Version { get => _version; private set {} }


        public PluginBase(string command, string pluginName, int version, string info=null){
            _pluginName = pluginName;
            cmd = command;
            _version = version;
            if(info!= null)
            {
                pluginInfo = info;
            }
            Core.LOG(new LogEntry(LOGSEVERITY.INFO,
            "PLUGIN",
            $"{pluginName} v{version} loaded."));
            CompatibilityCheck();
        }


        public void CompatibilityCheck(){
            if(Program._version < _version){
                Core.LOG(new LogEntry(LOGSEVERITY.WARNING, _pluginName, $"WARNING! {_pluginName} was compiled against a newer version of the bot({Program._version})."));
            }
            if(Program._version > _version){
                Core.LOG(new LogEntry(LOGSEVERITY.WARNING, _pluginName, $"WARNING! {_pluginName} was compiled against an older version of the bot({Program._version})."));
            }
        }

        [SubCommand("on", 0), CommandHelp("Turn on the plugin"), CommandVerified(3)]
        public async void TurnPluginOn(BotChannel bChan, BotWideCommandArguments args)
        {
            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages)
            {
                // No access below
                return;
            }
            PluginSettingsBase settings = await Settings<PluginSettingsBase>(bChan, PluginName);
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            settings._active = true;
            SaveBaseSettings(bChan, PluginName, settings);
            response.message = $"{PluginName} is active.";
            Respond(bChan, response);
        }
        [SubCommand("off", 0), CommandHelp("Turn off the plugin"), CommandVerified(3)]
        public async void TurnPluginOff(BotChannel bChan, BotWideCommandArguments args)
        {
            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages)
            {
                // No access below
                return;
            }
            PluginSettingsBase settings = await Settings<PluginSettingsBase>(bChan, PluginName);
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            settings._active = false;
            SaveBaseSettings(bChan, PluginName, settings);
            response.message = $"{PluginName} is inactive.";
            Respond(bChan, response);
        }
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

            if(args.source == MESSAGESOURCE.DISCORD && args.discordChannel != 0){
                await MisfitBot_MKII.DiscordWrap.DiscordClient.DiscordResponse(args);
            }
            if(args.source == MESSAGESOURCE.TWITCH && args.twitchChannel != null){
                Program.TwitchResponse(args);
            }
        }

        public void SayOnTwitchChannel(string twitchChannel, string message)
        {
            if (Program.Channels.CheckIfInTwitchChannel(twitchChannel))
            {
                Program.TwitchSayMessage(twitchChannel, message);
            }
        }


        public async Task<BotChannel> GetBotChannel(BotWideCommandArguments args){
            if(args.source == MESSAGESOURCE.TWITCH){
               return await Program.Channels.GetTwitchChannelByName(args.channel);
            }
            return await Program.Channels.GetDiscordGuildByID(args.guildID);
        }

        public async Task<BotChannel> GetBotChannel(BotWideMessageArguments args){
            if(args.source == MESSAGESOURCE.TWITCH){
               return await Program.Channels.GetTwitchChannelByName(args.channel);
            }
            return await Program.Channels.GetDiscordGuildByID(args.guildID);
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

            if (bChan.discordAdminChannel != 0 && Program.DiscordClient.Status == UserStatus.Online) // Need to verify this online check
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
