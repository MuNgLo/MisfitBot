
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
    /// <summary>
    /// The help text for this command. Shown under generated info about the plugin.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class SingleCommandAttribute : System.Attribute
    {
        public string cmd;
        public SingleCommandAttribute(string command)
        {
            this.cmd = command;
        }
    }
    /// <summary>
    /// Use this attribute to limit command response depending on source. If not given it defaults to both.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class CommandSourceAccessAttribute : System.Attribute
    {
        public readonly MESSAGESOURCE source;
        public CommandSourceAccessAttribute(MESSAGESOURCE src)
        {
            this.source = src;
        }
    }
    /// <summary>
    /// This attribute is to keep track of what version the command was last verified as working
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class CommandVerifiedAttribute : System.Attribute
    {
        public readonly int version;
        public CommandVerifiedAttribute(int ver)
        {
            this.version = ver;
        }
    }

}
