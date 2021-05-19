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
        private DatabaseStrings dbStrings;

        public Shoutouts():base("Shoutouts", 0)
        {
            dbStrings = new DatabaseStrings("Shoutouts", "so");
            Program.BotEvents.OnCommandReceived += OnCommandRecieved;
        }

        private async void OnCommandRecieved(BotWideCommandArguments args)
        {
            BotChannel bChan = await GetBotChannel(args);
            if (bChan == null) { return; }
            await DBVerify(bChan);
            ShoutoutsSettings settings = await Settings<ShoutoutsSettings>(bChan, PluginName);
            BotWideResponseArguments response = new BotWideResponseArguments(args);

            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages){
                // No access below
                return;
            }
            if(args.command.ToLower() == "so"){
                // Blank Shoutouts response here
                if(args.arguments.Count == 0){
                    if(args.source == MESSAGESOURCE.DISCORD){
                        Discord.EmbedFooterBuilder footer = new Discord.EmbedFooterBuilder{
                            Text = $"The plugin is currently {(settings._active ? "active" : "inactive")} here."
                        };

                        Discord.EmbedBuilder embedded = new Discord.EmbedBuilder{
                            Title = "Plugin: Shoutouts ", 
                            Description = HelpText(settings),
                            Color = Discord.Color.DarkOrange,
                            Footer = footer
                        };
                        
                        await SayEmbedOnDiscord(args.channelID, embedded.Build());
                        return;
                    }
                    return;
                }
                // resolve subcommands
                switch(args.arguments[0]){
                    case "off":
                        settings._active = false;
                        SaveBaseSettings(bChan, PluginName, settings);
                        response.message = $"Shoutouts is inactive.";
                        Respond(bChan, response);
                        break;
                    case "on":
                        settings._active = true;
                        SaveBaseSettings(bChan, PluginName, settings);
                        response.message = $"Shoutouts is active.";
                        Respond(bChan, response);
                        break;
                    case "add":
                        if(args.arguments.Count <= 1){
                            response.message = "You need to have line after the add. Use [EVENT] and [EVENTUSER] as replacements in the message.";
                            Respond(bChan, response);
                            return;
                        }
                        args.arguments.RemoveAt(0);
                        string line = string.Empty;
                        foreach (string part in args.arguments) { line += " " + part; }
                        line = line.Trim();
                        dbStrings.SaveNewLine(bChan, "SHOUTOUT", line);
                        response.message = $"Added one more line for the Shoutouts plugin.";
                        Respond(bChan, response);
                    break;
                    case "use":
                        if(args.arguments.Count <= 1){
                            response.message = "You need to give a valid ID. Check the List command to see ID for the lines in the database.";
                            Respond(bChan, response);
                            return;
                        }
                        int id = -1;
                        int.TryParse(args.arguments[1], out id);
                        if(id < 1){
                            response.message = "You need to give a valid ID. That ID couldn't be used.";
                            Respond(bChan, response);
                            return;
                        }
                        DBString entry = await dbStrings.GetStringByID(bChan, id);
                         if(entry == null){
                            response.message = "That ID didn't match anything I could find. Doublecheck it.";
                            Respond(bChan, response);
                            return;
                        }
                        DBString edited = new DBString(entry._id, !entry._inuse, entry._topic, entry._text);
                        if (dbStrings.SaveEditedLineByID(bChan, edited))
                        {
                            response.message = "Entry updated.";
                        }
                        else
                        {
                            response.message = "Failed to update entry.";
                        }
                        Respond(bChan, response);
                    break;
                    case "remove":
                        if(args.arguments.Count <= 1){
                            response.message = "You need to give a valid ID. Check the List command to see ID for the lines in the database.";
                            Respond(bChan, response);
                            return;
                        }
                        int id2 = -1;
                        int.TryParse(args.arguments[1], out id2);
                        if(id2 < 1){
                            response.message = "You need to give a valid ID. That ID couldn't be used.";
                            Respond(bChan, response);
                            return;
                        }
                        DBString entry2 = await dbStrings.GetStringByID(bChan, id2);
                         if(entry2 == null){
                            response.message = "That ID didn't match anything I could find. Doublecheck it.";
                            Respond(bChan, response);
                            return;
                        }
                        if (entry2._inuse)
                        {
                            response.message = $"Only entries that is not in use can be deleted. Use \"{CMC}so use <ID>\" to toggle the inuse flag on entries.";
                            Respond(bChan, response);
                            return;
                        }
                        // Remove the actual entry
                        if (dbStrings.DeleteEntry(bChan, id2))
                        {
                            response.message = $"Entry {id2} deleted.";
                            Respond(bChan, response);
                            return;
                        }
                        response.message = $"Failed to delete line {id2} for some reason.";
                        Respond(bChan, response);
                    break;
                    case "list":
                        if (args.source != MESSAGESOURCE.DISCORD) { return; }
                        if (args.arguments.Count == 1)
                        {
                            await ListLinesFromDB(bChan, args.channelID, 0);
                            return;
                        }
                        int page = 0;
                        int.TryParse(args.arguments[1], out page);
                        if (page <= 0) { page = 1; }

                        await ListLinesFromDB(bChan, args.channelID, page - 1);
                    break;
                    default:
                        if(args.arguments.Count != 1){
                            return;
                        }
                        if(args.source == MESSAGESOURCE.DISCORD) { return; }
                        UserEntry user = await Program.Users.GetUserByTwitchDisplayName(args.arguments[0]);
                        if(user != null){
                            // TODO
                            //var streamInfo = await Program.TwitchStreamInfo(user._twitchUID);

                            string pickedLine = dbStrings.GetRandomLine(bChan, "SHOUTOUT");
                            response.message = pickedLine;
                            response.parseMessage = true;
                            response.victim = user;
                            Respond(bChan ,response);
                            return;
                        }
                    break;
                }

            }
            
        }

        private async Task ListLinesFromDB(BotChannel bChan, ulong channelID, int page)
        {
            // LINES IN USE
            string inuseText = $"Currently stored lines...```fix{Environment.NewLine}" +
                $"These are lines stored in the database that the Shoutouts plugin will use if they are marked as inuse.{Environment.NewLine}{Environment.NewLine}" +
                $"<ID> <INUSE> <TEXT>        Page {page + 1}{Environment.NewLine}";
            List<DBString> lines = dbStrings.GetRowsByTen(bChan, page);
            if (lines.Count == 0)
            {
                inuseText += "No hits. Try a lower page number.";
            }
            else
            {
                foreach (DBString entry in lines)
                {
                    inuseText += String.Format("{0,4}", entry._id);
                    //inuseText += String.Format("{0,8}", entry._topic);
                    inuseText += String.Format("{0,7}", entry._inuse);
                    inuseText += "   " + entry._text + Environment.NewLine;
                }
            }

            inuseText += $"```Use command {CMC}so list <page> to list a page. Those marked with **true** for INUSE are in rotation.";
            await SayOnDiscord(inuseText, channelID);
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
                        dbStrings.SaveNewLine(bChan, "SHOUTOUT", line);
                    }
                });
            }
        }

        private string HelpText(ShoutoutsSettings settings){
            string message = $"This plugin will respond with a random line from the database when the command {CMC}so is used." +
            System.Environment.NewLine + System.Environment.NewLine +
            $"**{CMC}so on/off** to turn the plugin on or off.";

            return message;
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
