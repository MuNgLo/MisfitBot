using MisfitBot_MKII.Statics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using Embed = Discord.Embed;

namespace MisfitBot_MKII.Extensions.CommandInterpreter
{
    public class CommandInterpreter
    {
        private Dictionary<string, Dictionary<string, RegisteredCommand>> registeredSubCommands;
        private Dictionary<string, RegisteredSingleCommand> registeredSingleCommands;
        private Pages pages;

        public int CommandsCount { get => NumberOfCommands(); }

        
        public CommandInterpreter()
        {
            registeredSubCommands = new Dictionary<string, Dictionary<string, RegisteredCommand>>();
            registeredSingleCommands = new Dictionary<string, RegisteredSingleCommand>();
            pages = new Pages();
            Core.LOG(new LogEntry(LOGSEVERITY.INFO, "CommandInterpreter", "Started"));
            Program.BotEvents.OnCommandReceived += OnCommandRecieved;
            
        }

        public void ProcessPlugin(PluginBase pluginbase)
        {
            ReadPluginData(pluginbase);
        }

        private async void OnCommandRecieved(BotWideCommandArguments args)
        {
            BotChannel bChan = await GetBotChannel(args);
            if (bChan == null) { return; }

            // Hijack "commands" to output commandlist if on discord
            if (args.command == "commands")
            {
                BotWideResponseArguments response = new BotWideResponseArguments(args);
                if (args.source == MESSAGESOURCE.TWITCH)
                {
                    response.message = $"To get full command list, use the \"{Secrets.CommandCharacter}commands\" command on Discord.";
                    Respond(bChan, response);
                    return;
                }
                //await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "CommandInterpreter", $"Commands listed by {args.user}"));

                pages.OpenMenu(bChan, args);

                
                return;
            }

            // reroute all commands matching registered commands to relevent reciever
            // singlecommands
            if (registeredSingleCommands.ContainsKey(args.command))
            {
                if (registeredSingleCommands[args.command].source == MESSAGESOURCE.BOTH || registeredSingleCommands[args.command].source == args.source)
                {
                    registeredSingleCommands[args.command].method(bChan, args);
                }
                return;
            }

            // subcommands
            if (registeredSubCommands.ContainsKey(args.command))
            {
                if (args.arguments.Count < 1)
                {
                    // TODO add 0 argument method overrides for blank plugin command responses ADd specific attribute for that
                    return;
                }
                if (registeredSubCommands[args.command].ContainsKey(args.arguments[0]))
                {
                    if (registeredSubCommands[args.command][args.arguments[0]].source == MESSAGESOURCE.BOTH || registeredSubCommands[args.command][args.arguments[0]].source == args.source)
                    {
                        registeredSubCommands[args.command][args.arguments[0]].method(bChan, args);
                        return;
                    }
                }
            }
        }

        

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
                MESSAGESOURCE source = MESSAGESOURCE.BOTH;
                if(method.GetCustomAttribute<CommandSourceAccessAttribute>() != null)
                {
                    source = method.GetCustomAttribute<CommandSourceAccessAttribute>().source;
                }
                string text = string.Empty;
                if (method.GetCustomAttribute<CommandHelpAttribute>() != null)
                {
                    text = method.GetCustomAttribute<CommandHelpAttribute>().text;
                }

                if (method.GetCustomAttribute<SubCommandAttribute>() != null)
                {
                    CheckVerifiedFlag(method, plugin.PluginName, method.GetCustomAttribute<SubCommandAttribute>().cmd);
                    StoreSubCommand(
                        plugin.PluginName, 
                        plugin.CMD,
                        method.GetCustomAttribute<SubCommandAttribute>().cmd,
                        (SubCommandMethod)method.CreateDelegate(typeof(SubCommandMethod), plugin),
                        text,
                        source
                        );
                }
                if (method.GetCustomAttribute<SingleCommandAttribute>() != null)
                {
                    CheckVerifiedFlag(method, plugin.PluginName, method.GetCustomAttribute<SingleCommandAttribute>().cmd);
                    StoreSingleCommand(
                        plugin.PluginName,
                        method.GetCustomAttribute<SingleCommandAttribute>().cmd,
                        (CommandMethod)method.CreateDelegate(typeof(CommandMethod), plugin),
                        text,
                        source
                        );
                }
            }
        }

        private void CheckVerifiedFlag(MethodInfo method, string plugin, string command)
        {
            if (method.GetCustomAttribute<CommandVerifiedAttribute>() != null)
            {
                if (method.GetCustomAttribute<CommandVerifiedAttribute>().version < Program.Version)
                {
                    Core.LOG(new LogEntry(LOGSEVERITY.WARNING, "CommandInterpreter", $"The command {command} in plugin {plugin} verification flag({method.GetCustomAttribute<CommandVerifiedAttribute>().version}) is older then the Bot version {Program.Version}!"));
                }
                else if (method.GetCustomAttribute<CommandVerifiedAttribute>().version > Program.Version)
                {
                    Core.LOG(new LogEntry(LOGSEVERITY.WARNING, "CommandInterpreter", $"The command {command} in plugin {plugin} verification flag({method.GetCustomAttribute<CommandVerifiedAttribute>().version}) is newer then the Bot version {Program.Version}!"));
                }
                return;
            }
            Core.LOG(new LogEntry(LOGSEVERITY.WARNING, "CommandInterpreter", $"The command {command} in plugin {plugin} lacks verification flag!"));
        }
        /// <summary>
        /// Registers singlecommand and stores the relevant data. Throw log Error if alread registered.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="subcommand"></param>
        /// <param name="method"></param>
        /// <param name="helptext"></param>
        private void StoreSingleCommand(string pluginName, string command, CommandMethod method, string helptext, MESSAGESOURCE source)
        {
            if (registeredSingleCommands.ContainsKey(command))
            {
                // Singlecommand already regiustered
                Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "CommandInterpreter", $"Singlecommand {command} already registered!"));
                return;
            }
            //Core.LOG(new LogEntry(LOGSEVERITY.INFO, "CommandInterpreter", $"Registering command {command} {subcommand}"));
            registeredSingleCommands[command] = new RegisteredSingleCommand(pluginName, command, method, helptext, source);
        }
        /// <summary>
        /// Registers subcommand and stores the relevant data. Throw log Error if alread registered.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="subcommand"></param>
        /// <param name="method"></param>
        /// <param name="helptext"></param>
        private void StoreSubCommand(string pluginName, string command, string subcommand, SubCommandMethod method, string helptext, MESSAGESOURCE source)
        {
            if (!registeredSubCommands.ContainsKey(command))
            {
                registeredSubCommands[command] = new Dictionary<string, RegisteredCommand>();
            }

            if (registeredSubCommands[command].ContainsKey(subcommand))
            {
                // Subcommand already regiustered
                Core.LOG(new LogEntry(LOGSEVERITY.ERROR, "CommandInterpreter", $"Subcommand {subcommand} already registered! [Command={command}]"));
                return;
            }
            //Core.LOG(new LogEntry(LOGSEVERITY.INFO, "CommandInterpreter", $"Registering command {command} {subcommand}"));
            registeredSubCommands[command][subcommand] = new RegisteredCommand(pluginName, command, subcommand, method, helptext, source);
        }

        

        /// <summary>
        /// Simply counts all registered commands
        /// </summary>
        /// <returns></returns>
        private int NumberOfCommands()
        {
            int count = 0;
            foreach (string item in registeredSingleCommands.Keys)
            {
                count++;
            }
            foreach (string key in registeredSubCommands.Keys)
            {
                foreach (string item in registeredSubCommands[key].Keys)
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
            return await Program.Channels.GetDiscordGuildByID(args.guildID);
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

        internal Dictionary<string, RegisteredCommand> GetSubCommands(string pluginName)
        {
            if (registeredSubCommands.ContainsKey(pluginName))
            {
                return registeredSubCommands[pluginName];
            }
            return null;
        }
        #endregion
    }// EOF CLASS
}
