using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot_MKII.Extensions.CommandInterpreter
{
    internal class RegisteredCommand
    {
        readonly public string plugin;
        readonly public string command;
        readonly public string subcommand;
        readonly public SubCommandMethod method;
        readonly public string helptext;
        readonly public MESSAGESOURCE source;
        public RegisteredCommand(string pluginName, string cmd, string subcmd, SubCommandMethod mtd, string help, MESSAGESOURCE src)
        {
            plugin = pluginName; command = cmd; subcommand = subcmd; method = mtd; helptext = help; source = src;
        }
    }
    internal class RegisteredSingleCommand
    {
        readonly public string plugin;
        readonly public string command;
        readonly public CommandMethod method;
        readonly public string helptext;
        readonly public MESSAGESOURCE source;
        public RegisteredSingleCommand(string pluginName, string cmd, CommandMethod mtd, string help, MESSAGESOURCE src)
        {
            plugin = pluginName; command = cmd; method = mtd; helptext = help; source = src;
        }
    }
}
