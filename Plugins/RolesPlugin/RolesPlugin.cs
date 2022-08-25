using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MisfitBot_MKII;
using MisfitBot_MKII.Components;
using System.Data.SQLite;
using System.Data;
using MisfitBot_MKII.Statics;

namespace RolesPlugin
{
    public class RolesPlugin : PluginBase
    {
        public readonly string PLUGINNAME = "RolesPlugin";

        public RolesPlugin():base("roles", "Roles", 2)
        {
            Program.BotEvents.OnDiscordReactionAdded += OnDiscordReactionAdded;
            Program.BotEvents.OnDiscordReactionRemoved += OnDiscordReactionRemoved;
            Program.BotEvents.OnCommandReceived += OnCommandReceived;
        }

        private async void OnCommandReceived(BotWideCommandArguments args)
        {
            if (args.source != MESSAGESOURCE.DISCORD) { return; }
            if (!args.canManageMessages) { return; }

            BotChannel bChan = await GetBotChannel(args);
            RolesSettings settings = await Settings<RolesSettings>(bChan, PLUGINNAME);
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            if (args.command.ToLower() != "roles") { return; }
            // Helptext
            if (args.arguments.Count < 1)
            {
                InfoDump(bChan, settings, args);
                return;
            }
            // break down the command
            switch (args.arguments[0].ToLower())
            {

                case "mark":
                    if (!settings._active) { return; }
                    if (args.arguments.Count < 3)
                    {
                        response.message = $"Not enough arguments. Use \"{CMC}roles mark <DiscordMessageID> <topic>\" as syntax. Get The ID by rightclicking the message when your Discordclient has developer mode turned on in advanced settings.";
                        Respond(bChan, response);
                        return;
                    }
                    if (!settings.Topics.Exists(p => p.TopicName == args.arguments[2]))
                    {
                        response.message = $"Can't find that topic. Doublecheck that it exist and you spelled it right.";
                        Respond(bChan, response);
                    }
                    ulong msgID = Core.StringToUlong(args.arguments[1]);
                    if (settings.MarkedMessages.Exists(p => p.MessageID == msgID))
                    {
                        response.message = $"That message has already been marked with a topic. To replace it you have to unmark it first.";
                        Respond(bChan, response);
                        return;
                    }
                    DiscordChannelMessage dMessage = await MisfitBot_MKII.DiscordWrap.DiscordClient.DiscordGetMessage(response.discordChannel, msgID);
                    if(dMessage == null){
                        response.message = $"Can't find that message. Make sure I got access to channel and rights to manage messages in it.";
                        Respond(bChan, response);
                        return;
                    }
                    response.message = $"Marking that message with the topic \"{args.arguments[2]}\".";
                    settings.MarkedMessages.Add(new MarkedMessage() { MessageID = msgID, Topic = args.arguments[2], TimeStamp = Core.CurrentTime });

                    await MarkMessage(bChan, settings, dMessage, args.arguments[2]);
                    SaveBaseSettings(bChan, PLUGINNAME, settings);

                    Respond(bChan, response);
                    break;
                case "unmark":
                    if (!settings._active) { return; }
                    if (args.arguments.Count < 2)
                    {
                        response.message = $"Not enough arguments. Use \"{CMC}roles unmark <DiscordMessageID>\" as syntax. Get The ID by rightclicking the message when your Discordclient has developer mode turned on in advanced settings.";
                        Respond(bChan, response);
                        return;
                    }
                    ulong msgID2 = Core.StringToUlong(args.arguments[1]);
                    if (!settings.MarkedMessages.Exists(p => p.MessageID == msgID2))
                    {
                        response.message = $"That message isn't listed as marked.";
                        Respond(bChan, response);
                        return;
                    }
                    int removedNB = settings.MarkedMessages.RemoveAll(p => p.MessageID == msgID2);
                    if (!settings.MarkedMessages.Exists(p => p.MessageID == msgID2))
                    {
                        response.message = $"{removedNB} message has been unmarked.";
                        SaveBaseSettings(bChan, PLUGINNAME, settings);
                        Respond(bChan, response);
                        return;
                    }
                    else
                    {
                        response.message = $"Something went wrong and message couldn't be unmarked. Try again and if it doesn't work complain to your mum.";
                        Respond(bChan, response);
                        return;
                    }
                case "topic":
                    if (!settings._active) { return; }
                    if (args.arguments.Count < 2){
                        response.message = $"Topic is used to manage the topics. add/remove to create or delete topics. To add/remove roles to a topic use addrole/removerole.";
                            Respond(bChan, response);
                            return;
                    }
                    if (args.arguments[1].ToLower() == "add")
                    {
                        if (args.arguments.Count < 3)
                        {
                            response.message = $"Not enough arguments. Use \"{CMC}roles topic add <TheTopicNameYouWant>\" as syntax";
                            Respond(bChan, response);
                            return;
                        }
                        if (TopicAdd(bChan, settings, args.arguments[2]))
                        {
                            response.message = $"Topic was added.";
                            Respond(bChan, response);
                        }
                        else
                        {
                            response.message = $"Topic could not be added. Make sure it doens't already exist.";
                            Respond(bChan, response);
                        }
                    }
                    if (args.arguments[1].ToLower() == "remove")
                    {
                        if (args.arguments.Count < 3)
                        {
                            response.message = $"Not enough arguments. Use \"{CMC}roles topic remove <NameofTopic>\" as syntax";
                            Respond(bChan, response);
                            return;
                        }
                        if (TopicRemove(bChan, settings, args.arguments[2]))
                        {
                            response.message = $"Topic was removed.";
                            Respond(bChan, response);
                        }
                        else
                        {
                            response.message = $"Could not match topic.";
                            Respond(bChan, response);
                        }
                    }
                    if (args.arguments[1].ToLower() == "addrole")
                    {
                        if (args.arguments.Count < 4)
                        {
                            response.message = $"Not enough arguments. Use \"{CMC}roles topic addrole <Topic> <RoleYouWantAdded>\" as syntax";
                            Respond(bChan, response);
                            return;
                        }
                        if (!MisfitBot_MKII.DiscordWrap.DiscordClient.DiscordRoleExist(bChan, args.arguments[3]))
                        {
                            response.message = $"That role does not exist on this Discord. This matching is case sensitive.";
                            Respond(bChan, response);
                            return;
                        }
                        if (!settings.RoleTable.ContainsKey(args.arguments[3]))
                        {
                            response.message = $"That role exists on the Discord but needs to be registered with an emote for this plugin. See the \"{CMC}roles role\" command.";
                            Respond(bChan, response);
                            return;
                        }

                        if (TopicAddRole(bChan, settings, args.arguments[2], args.arguments[3]))
                        {
                            response.message = $"Role({args.arguments[3]}) was added to topic({args.arguments[2]}).";
                            Respond(bChan, response);
                        }
                        else
                        {
                            response.message = $"Role({args.arguments[3]}) could not be added to Topic({args.arguments[2]}). Make sure you type it right.";
                            Respond(bChan, response);
                        }
                    }

                    if (args.arguments[1].ToLower() == "removerole")
                    {
                        if (args.arguments.Count < 4)
                        {
                            response.message = $"Not enough arguments. Use \"{CMC}roles topic removerole <Topic> <RoleYouWantRemoved>\" as syntax";
                            Respond(bChan, response);
                            return;
                        }

                        if (!settings.RoleTable.ContainsKey(args.arguments[3]))
                        {
                            response.message = $"That role cant be matched with the known roles for this plugin. See the \"{CMC}roles role\" command.";
                            Respond(bChan, response);
                            return;
                        }

                        if (TopicRemoveRole(bChan, settings, args.arguments[2], args.arguments[3]))
                        {
                            response.message = $"Role({args.arguments[3]}) was removed from topic({args.arguments[2]}).";
                            Respond(bChan, response);
                        }
                        else
                        {
                            response.message = $"Role({args.arguments[3]}) could not be removed Topic({args.arguments[2]}). Make sure you type it right.";
                            Respond(bChan, response);
                        }
                    }
                    break;
            }


        }// EOF OnCommandReceived


        [SubCommand("role", 0), CommandHelp("Open the couch in the twitch channel tied to the botchannel command was given to."), CommandSourceAccess(MESSAGESOURCE.DISCORD), CommandVerified(3)]
        public async void Role(BotChannel bChan, BotWideCommandArguments args)
        {
            RolesSettings settings = await Settings<RolesSettings>(bChan, PLUGINNAME);
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            if (!settings._active) { return; }
            if (args.arguments.Count == 1)
            {
                response.message = $"This manages the roles. Make sure they exist on the Discord side of things. Use add/remove like \"{CMC}roles role <add/remove>\"";
                Respond(bChan, response);
                return;
            }

            if (args.arguments[1].ToLower() == "add")
            {
                if (args.arguments.Count < 4)
                {
                    response.message = $"Not enough arguments. Use \"{CMC}roles role add <NameofDiscordrole> <NameofEmote>\" as syntax.";
                    Respond(bChan, response);
                    return;
                }
                if (!MisfitBot_MKII.DiscordWrap.DiscordClient.DiscordRoleExist(bChan, args.arguments[2]))
                {
                    response.message = $"That role({args.arguments[2]}) does not exist on this Discord. This matching is case sensitive.";
                    Respond(bChan, response);
                    return;
                }
                if (!Char.IsSurrogate(args.arguments[3], 0))
                {
                    // Verify existence of custom emote
                    if (!MisfitBot_MKII.DiscordWrap.DiscordClient.DiscordEmoteExist(bChan, args.arguments[3]))
                    {
                        response.message = $"That emote does not exist. This matching is case sensitive.";
                        Respond(bChan, response);
                        return;
                    }
                }
                if (RoleAdd(bChan, settings, args.arguments[2], args.arguments[3]))
                {
                    response.message = $"Role was added.";
                    Respond(bChan, response);
                }
                else
                {
                    response.message = $"Could not add role. Make sure role and emote isn't already used.";
                    Respond(bChan, response);
                }
            }
            if (args.arguments[1].ToLower() == "remove")
            {
                // TODO also remove from topics
                if (args.arguments.Count < 3)
                {
                    response.message = $"Not enough arguments. Use \"{CMC}roles role remove <NameofDiscordrole>\" as syntax.";
                    Respond(bChan, response);
                    return;
                }
                if (RoleRemove(bChan, settings, args.arguments[2]))
                {
                    response.message = $"Role was removed.";
                    Respond(bChan, response);
                }
                else
                {
                    response.message = $"Could not match role.";
                    Respond(bChan, response);
                }
            }
            if (args.arguments[1].ToLower() == "editemote")
            {
                if (args.arguments.Count < 4)
                {
                    response.message = $"Not enough arguments. Use \"{CMC}roles role editemote <RoleToEdit> <NewEmote>\" as syntax";
                    Respond(bChan, response);
                    return;
                }
                if (!settings.RoleTable.ContainsKey(args.arguments[2]))
                {
                    response.message = $"Can't find that role. Make sure you enter it correctly and remember it is case sensitive.";
                    Respond(bChan, response);
                    return;
                }
                if (settings.RoleTable[args.arguments[2]] == args.arguments[3])
                {
                    response.message = $"That is the already stored emote for that role. Baka!";
                    Respond(bChan, response);
                }
                if (RoleEdit(bChan, settings, args.arguments[2], args.arguments[3]))
                {
                    response.message = $"The role {args.arguments[2]}'s emote was updated to {args.arguments[3]}.";
                    Respond(bChan, response);
                }
                else
                {
                    response.message = $"Failed to change the emote.";
                    Respond(bChan, response);
                }
            }
        }




        private async void InfoDump(BotChannel bChan, RolesSettings settings, BotWideCommandArguments args)
        {
            string message = $"```fix{System.Environment.NewLine}Admin/Broadcaster commands {System.Environment.NewLine}" +
                                $"{Program.CommandCharacter}roles < Arguments >{System.Environment.NewLine}{System.Environment.NewLine}" +
                                $"Arguments....{System.Environment.NewLine}" +
                                $"< none > -> this response{System.Environment.NewLine}" +
                                $"role add/edit/remove -> manage the roles that should be used.{System.Environment.NewLine}{System.Environment.NewLine}" +
                                $"Roles : {settings.Roles()}{System.Environment.NewLine}{System.Environment.NewLine}" +
                                $"Topics : {settings.TopicsList()}{System.Environment.NewLine}{System.Environment.NewLine}" +
                                $"Currently {settings.MarkedMessages.Count} messages is marked{System.Environment.NewLine}{System.Environment.NewLine}" +
                                $"Roles plugin is currently {(settings._active ? "active" : "inactive")}```";
            await SayOnDiscord(message, args.channelID);
        }

        private async Task MarkMessage(BotChannel bChan, RolesSettings settings, DiscordChannelMessage dMessage, string topicToAdd)
        {
            await MisfitBot_MKII.DiscordWrap.DiscordClient.ClearReactionsOnMessage(dMessage);
            TopicDefinition topic = settings.Topics.Find(p => p.TopicName == topicToAdd);
            foreach (string role in topic.Roles)
            {
                if (settings.RoleTable.ContainsKey(role))
                {
                    await MisfitBot_MKII.DiscordWrap.DiscordClient.ReactionAdd(dMessage, settings.RoleTable[role]);
                }
            }
            //bool isSurrogate = Char.IsSurrogate(arg3.Emote.Name[0]);

        }


        #region Topic management
        private bool TopicRemove(BotChannel bChan, RolesSettings settings, string topic)
        {
            if (!settings.Topics.Exists(p => p.TopicName == topic)) { return false; }
            settings.Topics.RemoveAll(p => p.TopicName == topic);
            SaveBaseSettings(bChan, PLUGINNAME, settings);
            return true;
        }
        private bool TopicAdd(BotChannel bChan, RolesSettings settings, string topic)
        {
            if (settings.Topics.Exists(p => p.TopicName == topic)) { return false; }
            settings.Topics.Add(new TopicDefinition() { TopicName = topic, Roles = new List<string>() });
            SaveBaseSettings(bChan, PLUGINNAME, settings);
            return true;
        }
        private bool TopicAddRole(BotChannel bChan, RolesSettings settings, string topic, string role)
        {
            if (!settings.Topics.Exists(p => p.TopicName == topic)) { return false; }
            if (settings.Topics.Find(p => p.TopicName == topic).Roles.Exists(p => p == role)) { return false; }
            settings.Topics.Find(p => p.TopicName == topic).Roles.Add(role);
            SaveBaseSettings(bChan, PLUGINNAME, settings);
            return settings.Topics.Find(p => p.TopicName == topic).Roles.Exists(p => p == role);
        }
        private bool TopicRemoveRole(BotChannel bChan, RolesSettings settings, string topic, string role)
        {
            if (!settings.Topics.Exists(p => p.TopicName == topic)) { return false; }
            if (!settings.Topics.Find(p => p.TopicName == topic).Roles.Exists(p => p == role)) { return false; }
            settings.Topics.Find(p => p.TopicName == topic).Roles.RemoveAll(p=>p == role);
            SaveBaseSettings(bChan, PLUGINNAME, settings);
            return !settings.Topics.Find(p => p.TopicName == topic).Roles.Exists(p => p == role);
        }
        #endregion
        #region Role Management
        private bool RoleEdit(BotChannel bChan, RolesSettings settings, string role, string newEmote)
        {
            settings.RoleTable[role] = newEmote;
            SaveBaseSettings(bChan, PLUGINNAME, settings);
            return settings.RoleTable[role] == newEmote;
        }
        private bool RoleRemove(BotChannel bChan, RolesSettings settings, string role)
        {
            if (!settings.RoleTable.ContainsKey(role)) { return false; }
            settings.RoleTable.Remove(role);
            SaveBaseSettings(bChan, PLUGINNAME, settings);
            return true;
        }
        private bool RoleAdd(BotChannel bChan, RolesSettings settings, string role, string emote)
        {
            if (settings.RoleTable.ContainsKey(role)) { return false; }
            if (settings.RoleTable.ContainsValue(emote)) { return false; }
            settings.RoleTable[role] = emote;
            SaveBaseSettings(bChan, PLUGINNAME, settings);
            return true;
        }
        #endregion

        #region Discord Reaction Event Listeners
        private async void OnDiscordReactionAdded(BotChannel bChan, UserEntry user, DiscordReactionArgument args)
        {
            // Ignore self reaction
            string botName = Program.BotNameTwitch;
            if (user._discordUsername != botName)
            {
                RolesSettings settings = await Settings<RolesSettings>(bChan, PLUGINNAME);
                if(!settings._active){return;}
                if (!settings.RoleTable.ContainsValue(args.Emote)) { return; }
                string role = settings.RoleTable.FirstOrDefault(x => x.Value == args.Emote).Key;
                //BotWideResponseArguments response = new BotWideResponseArguments(args);

                if (settings.MarkedMessages.Exists(p => p.MessageID == args.MessageID))
                {
                    if(await MisfitBot_MKII.DiscordWrap.DiscordClient.RoleAddUser(bChan, user, role) == false){
                        await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "RolesPlugin", $"OnDiscordReactionAdded Failed to add user({user._discordUsername}) to role({role})"));
                    }
                }
            }
        }



        private async void OnDiscordReactionRemoved(BotChannel bChan, UserEntry user, DiscordReactionArgument args)
        {
            // Ignore self reaction
            string botName = Program.BotNameTwitch;
            if (user._discordUsername != botName)
            {
                RolesSettings settings = await Settings<RolesSettings>(bChan, PLUGINNAME);
                if(!settings._active){return;}

                if (!settings.RoleTable.ContainsValue(args.Emote)) { return; }
                string role = settings.RoleTable.FirstOrDefault(x => x.Value == args.Emote).Key;

                if (settings.MarkedMessages.Exists(p => p.MessageID == args.MessageID))
                {
                    if(await MisfitBot_MKII.DiscordWrap.DiscordClient.RoleRemoveUser(bChan, user, role) == false){
                        await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "RolesPlugin", $"OnDiscordReactionRemoved Failed to remove user({user._discordUsername}) from role({role})"));
                    }
                }
            }
        }
        #endregion
        #region Unused Interface compliance
        public override void OnSecondTick(int seconds)
        {

        }
        public override void OnMinuteTick(int minutes)
        {

        }
        public override void OnUserEntryMergeEvent(MisfitBot_MKII.UserEntry discordUser, MisfitBot_MKII.UserEntry twitchUser)
        {

        }
        public override void OnBotChannelEntryMergeEvent(MisfitBot_MKII.BotChannel discordGuild, MisfitBot_MKII.BotChannel twitchChannel)
        {

        }
        #endregion
    }// EOF CLASS
}
