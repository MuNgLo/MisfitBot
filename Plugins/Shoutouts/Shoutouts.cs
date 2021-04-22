using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MisfitBot_MKII;
using MisfitBot_MKII.Components;
using MisfitBot_MKII.Statics;
using System.Data;

namespace Shoutouts
{
    public class Shoutouts : PluginBase
    {
        public readonly string PLUGINNAME = "Shoutouts";

        private DatabaseStrings dbStrings;

        public Shoutouts(){
            dbStrings = new DatabaseStrings(PLUGINNAME, PLUGINNAME.ToLower());
            version = "0.1";
            Core.LOG(new LogEntry(LOGSEVERITY.INFO,
            "PLUGIN",
            $"{PLUGINNAME} v{version} loaded."));
        }

        public override void OnBotChannelEntryMergeEvent(BotChannel discordGuild, BotChannel twitchChannel)
        {
            throw new NotImplementedException();
        }

        public override void OnMinuteTick(int minutes)
        {
            throw new NotImplementedException();
        }

        public override void OnSecondTick(int seconds)
        {
            throw new NotImplementedException();
        }

        public override void OnUserEntryMergeEvent(UserEntry discordUser, UserEntry twitchUser)
        {
            throw new NotImplementedException();
        }
    }// EOF CLASS
}
