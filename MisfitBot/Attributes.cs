
namespace MisfitBot_MKII
{

    /// <summary>
    /// Methods in a PluginBase derivatie class that is marked with this and public will get registered as a subcommand in the commandinterpreter
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class SubCommandAttribute : System.Attribute
    {
        public string cmd;
        public int argCount;

        public SubCommandAttribute(string subCommand, int args)
        {
            this.cmd = subCommand;
            this.argCount = args;
        }
    }
    /// <summary>
    /// The help text for this command. Shown under generated info about the plugin.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class CommandHelpAttribute : System.Attribute
    {
        public string text;

        public CommandHelpAttribute(string helptext)
        {
            this.text = helptext;
        }
    }
}
