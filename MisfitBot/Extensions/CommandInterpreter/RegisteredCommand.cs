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
        public RegisteredCommand(string pluginName, string cmd, string subcmd, SubCommandMethod mtd, string help)
        {
            plugin = pluginName; command = cmd; subcommand = subcmd; method = mtd; helptext = help;
        }
    }
}
