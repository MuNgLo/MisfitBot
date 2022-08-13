using MisfitBot_MKII.Statics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using Embed = Discord.Embed;
using ISocketMessageChannel = Discord.WebSocket.ISocketMessageChannel;

namespace MisfitBot_MKII.Extensions.CommandInterpreter
{
    public class CommandInterpreter
    {
        private Dictionary<string, Dictionary<string, RegisteredCommand>> registeredCommands;
        private Dictionary<ulong, int> openMenus;

        public int CommandsCount { get => NumberOfCommands(); }

        
        public CommandInterpreter()
        {
            registeredCommands = new Dictionary<string, Dictionary<string, RegisteredCommand>>();
            openMenus = new Dictionary<ulong, int>();
            Core.LOG(new LogEntry(LOGSEVERITY.INFO, "CommandInterpreter", "Started"));
            Program.BotEvents.OnCommandReceived += OnCommandRecieved;
            Program.BotEvents.OnDiscordReactionAdded += OnDiscordReactionAdded;
            Program.BotEvents.OnDiscordReactionRemoved += OnDiscordReactionRemoved;
            TimerStuff.OnMinuteTick += OnMinuteTick;
        }

        public void ProcessPlugin(PluginBase pluginbase)
        {
            ReadPluginData(pluginbase);
        }

        private async void OnCommandRecieved(BotWideCommandArguments args)
        {
            BotChannel bChan = await GetBotChannel(args);
            if(bChan == null) { return; }

            // Hijack "commands" to output commandlist if on discord
            if(args.command == "commands")
            {
                BotWideResponseArguments response = new BotWideResponseArguments(args);
                if(args.source == MESSAGESOURCE.TWITCH)
                {
                    response.message = $"To get full command list, use the \"{Program.CommandCharacter}commands\" command on Discord.";
                    Respond(bChan, response);
                    return;
                }
                //await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "CommandInterpreter", $"Commands listed by {args.user}"));
                Pages pages = new Pages();
                Embed help = pages.Frontpage(registeredCommands, Program.PluginCount);
                Discord.Rest.RestUserMessage msg = await (Program.DiscordClient.GetChannel(args.channelID) as ISocketMessageChannel).SendMessageAsync("", false, help);
                openMenus[msg.Id] = TimerStuff.Uptime;
                pages.FrontPageReactions(msg.Channel.Id, msg.Id, Program.PluginCount);
                return;
            }

            // reroute all commands matching registered commands to relevent reciever
            if (registeredCommands.ContainsKey(args.command))
            {
                if(args.arguments.Count < 1)
                {
                    // TODO add 0 argument method overrides for blank plugin command responses ADd specific attribute for that
                    return;
                }
                if (registeredCommands[args.command].ContainsKey(args.arguments[0]))
                {
                    registeredCommands[args.command][args.arguments[0]].method(bChan, args);
                }
            }
        }

        #region Discord Reaction Event Listeners
        private async void OnDiscordReactionAdded(BotChannel bChan, UserEntry user, DiscordReactionArgument args)
        {
            string botName = Program.BotName;
            string role = string.Empty;
            // Ignore self reaction
            if (user._discordUsername != botName)
            {
                //BotWideResponseArguments response = new BotWideResponseArguments(args);
                if (await MisfitBot_MKII.DiscordWrap.DiscordClient.RoleAddUser(bChan, user, role) == false)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "RolesPlugin", $"OnDiscordReactionAdded Failed to add user({user._discordUsername}) to role({role})"));
                }
            }
        }
        private async void OnDiscordReactionRemoved(BotChannel bChan, UserEntry user, DiscordReactionArgument args)
        {
            string botName = Program.BotName;
            string role = string.Empty;
            // Ignore self reaction
            if (user._discordUsername != botName)
            {
                if (await MisfitBot_MKII.DiscordWrap.DiscordClient.RoleRemoveUser(bChan, user, role) == false)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "RolesPlugin", $"OnDiscordReactionRemoved Failed to remove user({user._discordUsername}) from role({role})"));
                }
            }
        }
        #endregion

        #region simple supporting methods
        /// <summary>
        /// This reads the plugin data. Any commands found then gets fed to StoreCommand()
        /// </summary>
        /// <param name="plugin"></param>
        private void ReadPluginData(PluginBase plugin)
        {
            MethodInfo[] methodinfos = plugin.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (MethodInfo method in methodinfos)
            {
                if (method.GetCustomAttribute<SubCommandAttribute>() != null)
                {
                    //Core.LOG(new LogEntry(LOGSEVERITY.INFO, "ReadPluginData", $"{(plugin as PluginBase).PluginName} method({method.Name})"));
                    StoreCommand(
                        plugin.PluginName, 
                        plugin.CMD,
                        method.GetCustomAttribute<SubCommandAttribute>().cmd,
                        (SubCommandMethod)method.CreateDelegate(typeof(SubCommandMethod), plugin),
                        method.GetCustomAttribute<CommandHelpAttribute>().text
                        );
                }
            }
        }
        /// <summary>
        /// Registers command and stores the relevant data. Throw log Error if alread registered.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="subcommand"></param>
        /// <param name="method"></param>
        /// <param name="helptext"></param>
        private void StoreCommand(string pluginName, string command, string subcommand, SubCommandMethod method, string helptext)
        {
            if (!registeredCommands.ContainsKey(command))
            {
                registeredCommands[command] = new Dictionary<string, RegisteredCommand>();
            }

            if (registeredCommands[command].ContainsKey(subcommand))
            {
                // Subcommand already regiustered
                Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "CommandInterpreter", $"Subcommand {subcommand} already registered!"));
                return;
            }
            //Core.LOG(new LogEntry(LOGSEVERITY.INFO, "CommandInterpreter", $"Registering command {command} {subcommand}"));
            registeredCommands[command][subcommand] = new RegisteredCommand(pluginName, command, subcommand, method, helptext);
        }
        /// <summary>
        /// All this does is clean up the references we have to help menus. ANy older then 300s will become inresponsive
        /// </summary>
        /// <param name="minute"></param>
        private void OnMinuteTick(int minute)
        {
            // flush old messages we cont have to repspond to anymore
            foreach (KeyValuePair<ulong, int> entry in openMenus.Where(p => p.Value < TimerStuff.Uptime - 300).ToList())
            {
                openMenus.Remove(entry.Key);
            }
        }
        /// <summary>
        /// Simply counts all registered commands
        /// </summary>
        /// <returns></returns>
        private int NumberOfCommands()
        {
            int count = 0;
            foreach (string key in registeredCommands.Keys)
            {
                foreach (string item in registeredCommands[key].Keys)
                {
                    count++;
                }
            }
            return count;
        }
        private async Task<BotChannel> GetBotChannel(BotWideCommandArguments args)
        {
            if (args.source == MESSAGESOURCE.TWITCH)
            {
                return await Program.Channels.GetTwitchChannelByName(args.channel);
            }
            return await Program.Channels.GetDiscordGuildbyID(args.guildID);
        }
        
        private async void Respond(BotChannel bChan, BotWideResponseArguments args)
        {
            if (args.parseMessage)
            {
                args.message = StringFormatter.ConvertMessage(ref args);
            }

            if (args.source == MESSAGESOURCE.DISCORD && args.discordChannel != 0)
            {
                await MisfitBot_MKII.DiscordWrap.DiscordClient.DiscordResponse(args);
            }
            if (args.source == MESSAGESOURCE.TWITCH && args.twitchChannel != null)
            {
                Program.TwitchResponse(args);
            }
        }
        #endregion
    }// EOF CLASS
}
