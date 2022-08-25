using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MisfitBot_MKII;
using MisfitBot_MKII.Components;
using MisfitBot_MKII.Statics;

namespace ShoutOut
{
    public class ShoutOut : PluginBase
    {
        #region Fields
        private DatabaseStrings dbStrings;
        private Dictionary<string, string> filter = new Dictionary<string, string>() {
            {"[USER]", "UNSET" },
            {"[EVENT]", "UNSET" },
            {"[COUNT]", "UNSET" },
            {"[GAME]", "UNSET" }
        };
        #endregion
        public ShoutOut() : base("so", "ShoutOutPlugin", 3, "Automatic shoutouts picked randomly from message table.")
        {
            dbStrings = new DatabaseStrings("ShoutOut", "so");
            Program.BotEvents.OnRaidEvent += OnRaidEvent;
            Program.BotEvents.OnTwitchHostEvent += OnHostEvent;
        }
        #region Command Methods
        [SubCommand("add", 0), CommandHelp("Add a line to the collection of lines that randomly gets used."), CommandSourceAccess(MESSAGESOURCE.DISCORD), CommandVerified(3)]
        public async void DBAdd(BotChannel bChan, BotWideCommandArguments args)
        {
            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages)
            {
                // No access below
                return;
            }
            ShoutOutSettings settings = await Settings<ShoutOutSettings>(bChan, PluginName);
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            if (!settings._active) { return; }
            if (args.source == MESSAGESOURCE.TWITCH) { return; }
            if (args.arguments.Count <= 1)
            {
                response.message = "You need to have line after the add. Patters [USER] [GAME] [COUNT] [EVENT].";
                Respond(bChan, response);
                return;
            }
            args.arguments.RemoveAt(0);
            string line = string.Empty;
            foreach (string part in args.arguments) { line += " " + part; }
            line = line.Trim();
            dbStrings.SaveNewLine(bChan, "ALL", line);
            response.message = $"Added one more line for the Shoutout plugin.";
            Respond(bChan, response);
        }
        [SubCommand("use", 0), CommandHelp("Change the use flag on a line in the collection."), CommandSourceAccess(MESSAGESOURCE.DISCORD), CommandVerified(3)]
        public async void DBSetUseFlag(BotChannel bChan, BotWideCommandArguments args)
        {
            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages)
            {
                // No access below
                return;
            }
            ShoutOutSettings settings = await Settings<ShoutOutSettings>(bChan, PluginName);
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            if (!settings._active) { return; }
            if (args.source == MESSAGESOURCE.TWITCH) { return; }
            if (args.arguments.Count <= 1)
            {
                response.message = "You need to give a valid ID. Check the List command to see ID for the lines in the database.";
                Respond(bChan, response);
                return;
            }
            int id = -1;
            int.TryParse(args.arguments[1], out id);
            if (id < 1)
            {
                response.message = "You need to give a valid ID. That ID couldn't be used.";
                Respond(bChan, response);
                return;
            }
            DBString entry = await dbStrings.GetStringByID(bChan, id);
            if (entry == null)
            {
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
        }
        [SubCommand("remove", 0), CommandHelp("Remove an unused line from the collection."), CommandSourceAccess(MESSAGESOURCE.DISCORD), CommandVerified(3)]
        public async void DBRemove(BotChannel bChan, BotWideCommandArguments args)
        {
            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages)
            {
                // No access below
                return;
            }
            ShoutOutSettings settings = await Settings<ShoutOutSettings>(bChan, PluginName);
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            if (!settings._active) { return; }
            if (args.arguments.Count <= 1)
            {
                response.message = "You need to give a valid ID. Check the List command to see ID for the lines in the database.";
                Respond(bChan, response);
                return;
            }
            int id2 = -1;
            int.TryParse(args.arguments[1], out id2);
            if (id2 < 1)
            {
                response.message = "You need to give a valid ID. That ID couldn't be used.";
                Respond(bChan, response);
                return;
            }
            DBString entry2 = await dbStrings.GetStringByID(bChan, id2);
            if (entry2 == null)
            {
                response.message = "That ID didn't match anything I could find. Doublecheck it.";
                Respond(bChan, response);
                return;
            }
            if (entry2._inuse)
            {
                response.message = $"Only entries that is not in use can be deleted. Use \"{CMC}insults use <ID>\" to toggle the inuse flag on entries.";
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
        }
        [SubCommand("list", 0), CommandHelp("Show the collection of lines. All with use flag true will be randomly used as shoutouts."), CommandSourceAccess(MESSAGESOURCE.DISCORD), CommandVerified(3)]
        public async void DBList(BotChannel bChan, BotWideCommandArguments args)
        {
            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages)
            {
                // No access below
                return;
            }
            ShoutOutSettings settings = await Settings<ShoutOutSettings>(bChan, PluginName);
            if (!settings._active) { return; }
            if (args.arguments.Count == 1)
            {
                await ListLinesFromDB(bChan, args.channelID, 0);
                return;
            }
            int page = 0;
            int.TryParse(args.arguments[1], out page);
            if (page <= 0) { page = 1; }
            await ListLinesFromDB(bChan, args.channelID, page - 1);
        }
        [SubCommand("debug", 0), CommandHelp("Use specified channel to generate a debug shoutout message."), CommandVerified(3)]
        public async void RaidDebug(BotChannel bChan, BotWideCommandArguments args)
        {
            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages)
            {
                // No access below
                return;
            }
            ShoutOutSettings settings = await Settings<ShoutOutSettings>(bChan, PluginName);
            if (!settings._active) { return; }
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            if (args.arguments.Count < 2) { return; }
            if (args.arguments[1].Length > 1)
            {
                ShoutOutArguments shout = new ShoutOutArguments(args.arguments[1]);
                shout = await ResolveShoutOut(shout);
                string pickedLine = dbStrings.GetRandomLine(bChan, "ALL");
                response.message = pickedLine;
                response.parseMessage = false;
                filter["[USER]"] = shout.ChannelName;
                filter["[EVENT]"] = "Debug";
                filter["[GAME]"] = shout.game;
                filter["[COUNT]"] = "All of them";
                response.message = StringFormatter.ConvertMessage(response.message, filter);
                Respond(bChan, response);
                return;
            }
        }
        [SubCommand("raidtest", 0), CommandHelp("Use specified channel to generate a debug raid event. Should result in a shoutout."), CommandVerified(3)]
        public async void RaidTest(BotChannel bChan, BotWideCommandArguments args)
        {
            if (!args.isModerator && !args.isBroadcaster && !args.canManageMessages)
            {
                // No access below
                return;
            }
            ShoutOutSettings settings = await Settings<ShoutOutSettings>(bChan, PluginName);
            if (!settings._active) { return; }
            if (args.arguments.Count < 2) { return; }
            RaidEventArguments raidArgs = new RaidEventArguments(args.arguments[1], bChan.TwitchChannelName, 69);
            OnRaidEvent(bChan, raidArgs);
        }
        #endregion


        private async Task ListLinesFromDB(BotChannel bChan, ulong channelID, int page)
        {
            // LINES IN USE
            string inuseText = $"Currently stored lines...```fix{Environment.NewLine}" +
                $"These are lines stored in the database that the Shoutout plugin will use if they are marked as inuse.{Environment.NewLine}{Environment.NewLine}" +
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

        private string HelpText()
        {
            string message = $"This plugin will pick a random shoutout response when a raid happens." +
            System.Environment.NewLine + System.Environment.NewLine +
            $"**{CMC}so on/off** to turn the plugin on or off.";

            return message;
        }

        private async void OnRaidEvent(BotChannel bChan, RaidEventArguments e)
        {
            await DBVerify(bChan);
            ShoutOutSettings settings = await Settings<ShoutOutSettings>(bChan, PluginName);

            ShoutOutArguments shout = new ShoutOutArguments(e.SourceChannel);
            shout = await ResolveShoutOut(shout);
            if (shout == null) { return; }

            string pickedLine = dbStrings.GetRandomLine(bChan, "ALL");

            filter["[USER]"] = shout.ChannelName;
            filter["[EVENT]"] = "raid";
            filter["[GAME]"] = shout.game;
            filter["[COUNT]"] = e.RaiderCount.ToString();

            pickedLine = StringFormatter.ConvertMessage(pickedLine, filter);
            SayOnTwitchChannel(bChan.TwitchChannelName, pickedLine);
        }
        /// <summary>
        /// Can return NULL
        /// </summary>
        /// <param name="bChan"></param>
        /// <returns></returns>
        private async Task<ShoutOutArguments> ResolveShoutOut(ShoutOutArguments shout)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, PluginName, $"GetChannelVideosByUsername starting {shout.ChannelName}."));

            TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse getUsersResponse = null;

            getUsersResponse = await Program.TwitchAPI.Helix.Users.GetUsersAsync(logins: new List<string> { shout.ChannelName });
            if (getUsersResponse.Users.Length < 1) { return null; }
            // We'll assume the request went well and that we made no typo's, meaning we should have 1 user at index 0, which is LuckyNoS7evin
            string luckyId = getUsersResponse.Users[0].Id;

            TwitchLib.Api.Helix.Models.Channels.GetChannelInformation.GetChannelInformationResponse channelInfo = null;

            channelInfo = await Program.TwitchAPI.Helix.Channels.GetChannelInformationAsync(luckyId);
            //if(channelInfo.Data.Length > 0)
            //{
            //    return ConstructShoutOut(channelInfo.Data[0]);
            //}
            return new ShoutOutArguments(shout.ChannelName) { game = channelInfo.Data[0].GameName };
        }
            
        //private string ConstructShoutOut(TwitchLib.Api.Helix.Models.Channels.GetChannelInformation.ChannelInformation cInfo)
        //{
        //    return $"{cInfo.BroadcasterName} was last playing {cInfo.GameName}";
        //}

        private async Task DBVerify(BotChannel bChan)
        {
            if (!await dbStrings.TableInit(bChan))
            {
                await Task.Run(() =>
                {
                    DefaultDBLines lines = new DefaultDBLines();
                    foreach (string line in lines._lines)
                    {
                        dbStrings.SaveNewLine(bChan, "ALL", line);
                    }
                });
            }
        }

        

        #region Unused
        private void OnHostEvent(BotChannel bChan, HostedEventArguments e)
        {
            throw new NotImplementedException();
        }
        
        public override void OnBotChannelEntryMergeEvent(BotChannel discordGuild, BotChannel twitchChannel)
        {
        }
        public override void OnMinuteTick(int minutes)
        {
        }
        public override void OnSecondTick(int seconds)
        {
        }
        public override void OnUserEntryMergeEvent(UserEntry discordUser, UserEntry twitchUser)
        {
        }
        #endregion
    }// EOF CLASS
}
