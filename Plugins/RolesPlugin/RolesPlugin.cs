using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MisfitBot_MKII;
using MisfitBot_MKII.Components;
using System.Data.SQLite;
using System.Data;

namespace RolesPlugin
{
    public class RolesPlugin : PluginBase
    {
        public readonly string PLUGINNAME = "RolesPlugin";

        public RolesPlugin()
        {
            Program.BotEvents.OnDiscordReactionAdded += OnDiscordReactionAdded;
            Program.BotEvents.OnDiscordReactionCleared += OnDiscordReactionCleared;
            Program.BotEvents.OnDiscordReactionRemoved += OnDiscordReactionRemoved;
            Program.BotEvents.OnCommandReceived += OnCommandReceived;
            Core.LOG(new LogEntry(LOGSEVERITY.INFO,
            "PLUGIN",
            "RolesPlugin loaded."));
             
        }

        private async void OnCommandReceived(BotWideCommandArguments args)
        {
            if(args.source != MESSAGESOURCE.DISCORD){return;}
            BotChannel bChan = await GetBotChannel(args);
            RolesSettings settings = await Settings<RolesSettings>(bChan, PLUGINNAME);
            BotWideResponseArguments response = new BotWideResponseArguments(args);
            if(args.command.ToLower() != "roles"){return;}
            if(args.arguments.Count < 2){
                InfoDump(bChan, settings, args);
                return;}

 #region role stuff
                if(args.arguments[0].ToLower() == "topic" || args.arguments[1].ToLower() == "add") 
                {
                    if(args.arguments.Count < 3){
                            response.message = $"Not enough arguments. Use \"{CMC}roles topic add <TheTopicNameYouWant>\" as syntax";
                            Respond(bChan, response);
                            return;
                            }
                    if(TopicAdd(bChan, settings, args.arguments[2])){
                            response.message = $"Topic was added.";
                            Respond(bChan, response);
                    }else{
                            response.message = $"Topic could not be added. Make sure it doens't already exist.";
                            Respond(bChan, response);
                    }
                }
                if(args.arguments[0].ToLower() == "role" && args.arguments[1].ToLower() == "add") 
                {
                        if(args.arguments.Count < 4){
                            response.message = $"Not enough arguments. Use \"{CMC}roles role add <NameofDiscordrole> <NameofEmote>\" as syntax";
                            Respond(bChan, response);
                            return;
                            }
                        if(!Program.DiscordRoleExist(bChan, args.arguments[2])){
                            response.message = $"That role does not exist. This matching is case sensitive.";
                            Respond(bChan, response);
                            return;
                        }
                        /*if(!Program.DiscordEmoteExist(bChan, args.arguments[3])){
                            response.message = $"That emote does not exist. This matching is case sensitive.";
                            Respond(bChan, response);
                            return;
                        }*/
                        if(RoleAdd(bChan, settings, args.arguments[2], args.arguments[3])){
                            response.message = $"Role was added.";
                            Respond(bChan, response);
                        }else{
                            response.message = $"Could not add role. Make sure role and emote isn't already used.";
                            Respond(bChan, response);
                        }
                }
                if(args.arguments[0].ToLower() == "role" && args.arguments[1].ToLower() == "remove") 
                {
                    // TODO also remove from topics
                    if(args.arguments.Count < 3)
                    {
                        response.message = $"Not enough arguments. Use \"{CMC}roles role remove <NameofDiscordrole>\" as syntax";
                        Respond(bChan, response);
                        return;
                    }
                    if(RoleRemove(bChan, settings, args.arguments[2])) 
                    {
                        response.message = $"Role was removed.";
                        Respond(bChan, response);
                    }else{
                        response.message = $"Could not match role.";
                        Respond(bChan, response);
                    }
                }

                
                if(
                    (args.arguments[0].ToLower() == "topic" || args.arguments[1].ToLower() == "topic") 
                    && 
                    (args.arguments[0].ToLower() == "remove" || args.arguments[1].ToLower() == "remove"))
                {
                        TopicRemove(bChan, settings, args);
                } 
         
if(
                    (args.arguments[0].ToLower() == "topic" || args.arguments[1].ToLower() == "topic") 
                    && 
                    (args.arguments[0].ToLower() == "edit" || args.arguments[1].ToLower() == "edit"))
                {
                        TopicEdit(bChan, settings, args);
                }
                if(
                    (args.arguments[0].ToLower() == "role" || args.arguments[1].ToLower() == "role") 
                    && 
                    (args.arguments[0].ToLower() == "edit" || args.arguments[1].ToLower() == "edit"))
                {
                        RoleEdit(bChan, settings, args);
                }
#endregion
            SaveBaseSettings(bChan, PLUGINNAME, settings);
        }// EOF OnCommandReceived

        private async void InfoDump(BotChannel bChan, RolesSettings settings, BotWideCommandArguments args)
        {
            string message = $"```fix{System.Environment.NewLine}Admin/Broadcaster commands {System.Environment.NewLine}" +
                                $"{Program.CommandCharacter}roles < Arguments >{System.Environment.NewLine}{System.Environment.NewLine}" +
                                $"Arguments....{System.Environment.NewLine}" +
                                $"< none > -> this response{System.Environment.NewLine}" +
                                $"role add/edit/remove -> manage the roles that should be used.{System.Environment.NewLine}{System.Environment.NewLine}" +
                                $"Roles : {settings.Roles()}{System.Environment.NewLine}{System.Environment.NewLine}" +
                                $"Topics : {settings.TopicsList()}```";
                                await SayOnDiscord(message, args.channel);
        }







        private void TopicEdit(BotChannel bChan, RolesSettings settings, BotWideCommandArguments args)
        {
            throw new NotImplementedException();
        }


        private void TopicRemove(BotChannel bChan, RolesSettings settings, BotWideCommandArguments args)
        {
            throw new NotImplementedException();
        }

        private bool TopicAdd(BotChannel bChan, RolesSettings settings, string topic)
        {
            if(settings.Topics.Exists(p=>p.TopicName == topic)){return false;}
            settings.Topics.Add(new TopicDefinition(){ TopicName = topic, Roles = new List<string>() });
            SaveBaseSettings(bChan, PLUGINNAME, settings);
            return true;
        }

        #region Role Management
        private void RoleEdit(BotChannel bChan, RolesSettings settings, BotWideCommandArguments args)
        {
            throw new NotImplementedException();
        }
        private bool RoleRemove(BotChannel bChan, RolesSettings settings, string role)
        {
            if(!settings.RoleTable.ContainsKey(role)){return false;}
            settings.RoleTable.Remove(role);
            SaveBaseSettings(bChan, PLUGINNAME, settings);
            return true;
        }
        private bool RoleAdd(BotChannel bChan, RolesSettings settings, string role, string emote)
        {
            if(settings.RoleTable.ContainsKey(role)){return false;}
            if(settings.RoleTable.ContainsValue(emote)){return false;}
            settings.RoleTable[role] = emote;
            SaveBaseSettings(bChan, PLUGINNAME, settings);
            return true;
        }
        #endregion




















        private async void OnDiscordReactionAdded(BotChannel bChan, UserEntry user,DiscordReactionArgument args)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "RolesPlugin", "OnDiscordReactionAdded"));
        }

        private async void OnDiscordReactionCleared(BotChannel bChan, ulong channelID)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "RolesPlugin", "OnDiscordReactionCleared"));
        }

        private async void OnDiscordReactionRemoved(BotChannel bChan, UserEntry user,DiscordReactionArgument args)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "RolesPlugin", "OnDiscordReactionRemoved"));
        }

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
    }// EOF CLASS
}
