using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MisfitBot_MKII;
using MisfitBot_MKII.Components;
using MisfitBot_MKII.Statics;
using System.Data.SQLite;
using System.Data;

namespace Insults
{
    public class Insults : PluginBase
    {

        public readonly string PLUGINNAME = "Insults";
        public readonly string PLUGINSTATS = "_Couch_Stats";

        private DatabaseStrings dbStrings;

        public Insults()
        {
            dbStrings = new DatabaseStrings(PLUGINNAME, "insults");
            version = "0.1";
            Core.LOG(new LogEntry(LOGSEVERITY.INFO,
            "PLUGIN",
            $"{PLUGINNAME} v{version} loaded."));

            Program.BotEvents.OnCommandReceived += OnCommandRecieved;
        }

        private async void OnCommandRecieved(BotWideCommandArguments args)
        {
            BotChannel bChan = await GetBotChannel(args);
            if (bChan == null) { return; }
            await DBVerify(bChan);
            if(args.command.ToLower() == "insult"){
                BotWideResponseArguments response = new BotWideResponseArguments(args);
                string pickedLine = dbStrings.GetRandomLine(bChan, "INSULT");
                response.message = pickedLine;
                response.parseMessage = true;
                response.victim = args.user;
                Respond(bChan ,response);
            }
        }

        private async void OnMessageRecieved(BotWideMessageArguments args)
        {
            BotChannel bChan = await GetBotChannel(args);
            if (bChan == null) { return; }
            await DBVerify(bChan);


        }

        private async Task DBVerify(BotChannel bChan)
        {
            if (!await dbStrings.TableInit(bChan))
            {
                await Task.Run(() =>
                {
                    DefaultDBLines lines = new DefaultDBLines();
                    foreach (string line in lines._lines)
                    {
                        dbStrings.SaveNewLine(bChan, "INSULT", line);
                    }
                });
            }
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
