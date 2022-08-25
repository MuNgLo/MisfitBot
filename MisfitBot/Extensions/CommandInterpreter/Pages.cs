using MisfitBot_MKII.Statics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Embed = Discord.Embed;
using Emote = Discord.Emote;
using EmbedBuilder = Discord.EmbedBuilder;
using EmbedFooterBuilder = Discord.EmbedFooterBuilder;
using ISocketMessageChannel = Discord.WebSocket.ISocketMessageChannel;
using IMessage = Discord.IMessage;

namespace MisfitBot_MKII.Extensions.CommandInterpreter
{
    //  #️⃣ keycap: # (pound) *️⃣ keycap: * (asterisk) 0️⃣ keycap: 0 1️⃣ keycap: 1 2️⃣ keycap: 2 3️⃣ keycap: 3
    //  4️⃣ keycap: 4 5️⃣ keycap: 5 6️⃣ keycap: 6 7️⃣ keycap: 7 8️⃣ keycap: 8 9️⃣ keycap: 9 🔟 keycap: 10
    // ⬅️ ➡️ 🏠

    public class Pages
    {
#region Private Fields
        private string[] kce = new string[14] { "0️⃣", "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟", "⬅️", "➡️", "🏠" };
        private Task[] rTasks = new Task[14];
        private string NL = System.Environment.NewLine;
        private Dictionary<ulong, Helpmenu> openMenus;
        //private delegate void ReactionDirection(ulong msgID, int page=0);
        /*private Task RZero;
        private Task ROne;
        private Task RTwo;
        private Task RThree;
        private Task RFour;
        private Task RFive;
        private Task RSix;
        private Task RSeven;
        private Task REight;
        private Task RNine;
        private Task RTen;
        private Task RLeftArrow;
        private Task RRightArrow;
        private Task RHome;
        */
        #endregion
        #region Public Methods
        public Pages()
        {
            openMenus = new Dictionary<ulong, Helpmenu>();
            TimerStuff.OnMinuteTick += OnMinuteTick;
            Program.BotEvents.OnDiscordReactionAdded += OnDiscordReactionAdded;
            //Program.BotEvents.OnDiscordReactionRemoved += OnDiscordReactionRemoved;
        }

        public async void OpenMenu(BotChannel bChan, BotWideCommandArguments args)
        {
            Embed page = Frontpage();
            Discord.Rest.RestUserMessage msg = await(Program.DiscordClient.GetChannel(args.channelID) as ISocketMessageChannel).SendMessageAsync("", false, page);
            openMenus[msg.Channel.Id] = new Helpmenu(msg.Channel.Id, msg.Id);
            FrontPageReactions(msg.Channel.Id, msg.Id, Program.PluginCount);
        }
        #endregion

        #region FrontPage
        public async void BackToStartPage(ulong channelID)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Pages", $"BackToStartPage"));
            if (!openMenus.ContainsKey(channelID))
            {
                return; // no menu open in channel
            }
            else if (openMenus[channelID].Timestamp + 300 < TimerStuff.Uptime)
            {
                openMenus.Remove(channelID);
                return; // menu to old
            }
            // Get message
            IMessage message = await (Program.DiscordClient.GetChannel(channelID) as ISocketMessageChannel).GetMessageAsync(openMenus[channelID].MessageID);
            if (message == null)
            {
                return;
            }
            // Clear old reactions
            await message.RemoveAllReactionsAsync();
            ClearReactionBinds();

            Embed page = Frontpage();
            await (message as Discord.IUserMessage).ModifyAsync(m => m.Embed = page);

            // Update timestamp
            openMenus[channelID] = new Helpmenu(channelID, openMenus[channelID].MessageID);
            FrontPageReactions(channelID, openMenus[channelID].MessageID, Program.PluginCount);
        }
        private Embed Frontpage()
        {
            EmbedBuilder embedded = new Discord.EmbedBuilder
            {
                Title = Program.BotNameTwitch,
                Description = $"Here you will find general info about how to use the bot as well as generated info about running plugins.{NL}" +
                $"{NL}**Navigation**{NL}" +
                $"To navigate this help click the reactions bellow the message and the message will be updated. A message will only work for a while though, so if it doesn't respond try opening a new one again.{NL}" +
                $"Some reactions will always have the same role.{NL}" +
                $"> 🏠 Brings you to the startpage.{NL}" +
                $"> ⬅️ Go back/Previous page.{NL}" +
                $"> ➡️ Forward/Next page.{NL}{NL}",
                Color = Discord.Color.DarkOrange,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"> ({Program.PluginCount})Plugins running with {Program.Commands.CommandsCount} commands in total. Bot v{Program.Version}"
                }
            };
            embedded.AddField(name: $"{kce[1]}", $">  General Info", true);
            embedded.AddField(name: $"{kce[2]}", $">  Running Plugins", true);
            return embedded.Build();
        }
        private async void FrontPageReactions(ulong dChannel, ulong msgID, int pages, bool backArrow = false, bool nextArrow = false)
        {
            // get message and remove previous reactions
            DiscordChannelMessage dMessage = await MisfitBot_MKII.DiscordWrap.DiscordClient.DiscordGetMessage(dChannel, msgID);
            await MisfitBot_MKII.DiscordWrap.DiscordClient.ClearReactionsOnMessage(dMessage);
            ClearReactionBinds();
            await MisfitBot_MKII.DiscordWrap.DiscordClient.ReactionAdd(dMessage, kce[1]);
            rTasks[1] = new Task(async () => { await Generalpage(dChannel); });
            await MisfitBot_MKII.DiscordWrap.DiscordClient.ReactionAdd(dMessage, kce[2]);
            rTasks[2] = new Task<Task>(async () => { await PluginListPage(dChannel, 0); });
        }
        #endregion
        #region General Page
        private async Task Generalpage(ulong channelID)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Pages", $"Generalpage"));
            EmbedBuilder embedded = new Discord.EmbedBuilder
            {
                Title = "MisfitBot",
                Description = $"This bot is made by MuNgLo and the code can be found on Github. It is based on Discord.NET and TwitchLib and uses both of those to wrap the API's and then present an enviroment for plugins where you can easily write a plugin, compile and distribute just the binary dll file. On lauch the bot will check for and load plugins.{NL}" +
                $"The bot can also be used on Twitch or Discord or both. A plugin can be eaily written to work on both or just one of them. In fact you could run the bot with no connection but why would you.{NL}" +
                $"{NL}**About Plugins**{NL} Most plugins should follow this pattern. They have a master command for all other commands. So for say **Admin** plugin you would...{NL} {Program.CommandCharacter}admin *subcommand* *arguments*{NL}" +
                $"Subcommands would then follow the master command. After that all relevant arguments for the command.{NL}" +
                $"> Example -> {Program.CommandCharacter}admin setadminchannel{NL}" +
                $"In this case the command would only work on Discord side and would designate the channel to be the adminchannel for the Discord server. Since it would use the channel the command is given in nor extra arguments are needed." +
                $"{NL}{NL}**General Commands**{NL}" +
                $"There are commands from the bot itself or even from plugins that do not use the mastercommand. Usually it will be a single word command with no arguments.{NL}" +
                $"The most obvious one would be the **{Program.CommandCharacter}commands** that got you here.",
                Url = "https://github.com/MuNgLo/MisfitBot", 
                Color = Discord.Color.DarkOrange,
                //Footer = new EmbedFooterBuilder
                //{
                //    Text = $"> ({Program.PluginCount})Plugins running with {Program.Commands.CommandsCount} commands in total. Bot v{Program.Version}"
                //}
            };
            //embedded.AddField(name: $"{kce[1]}", $">  General Info", true);
            //embedded.AddField(name: $"{kce[2]}", $">  Running plugins", true);
            //embedded.AddField(name: $"{kce[3]}", $">  About this Bot", true);
            Discord.Rest.RestUserMessage msg = await(Program.DiscordClient.GetChannel(channelID) as ISocketMessageChannel).SendMessageAsync("", false, embedded.Build());
            openMenus[msg.Channel.Id] = new Helpmenu(msg.Channel.Id, msg.Id);
            GeneralPageReactions(msg.Channel.Id, msg.Id, Program.PluginCount);
        }
        private async void GeneralPageReactions(ulong dChannel, ulong msgID, int pages, bool backArrow = false, bool nextArrow = false)
        {
            // get message and remove previous reactions
            DiscordChannelMessage dMessage = await MisfitBot_MKII.DiscordWrap.DiscordClient.DiscordGetMessage(dChannel, msgID);
            await MisfitBot_MKII.DiscordWrap.DiscordClient.ClearReactionsOnMessage(dMessage);
            ClearReactionBinds();
            await DiscordWrap.DiscordClient.ReactionAdd(dMessage, kce[13]);
            rTasks[13] = new Task(() => { BackToStartPage(dChannel); });
        }
#endregion



        #region PluginListpage
        private async Task PluginListPage(ulong channelID, int page)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Pages", $"PluginListPage {page}"));
            if (!openMenus.ContainsKey(channelID))
            {
                return; // no menu open in channel
            }
            else if(openMenus[channelID].Timestamp + 300 < TimerStuff.Uptime){
                openMenus.Remove(channelID);
                return; // menu to old
            }
            // Get message
            IMessage message = await (Program.DiscordClient.GetChannel(channelID) as ISocketMessageChannel).GetMessageAsync(openMenus[channelID].MessageID);
            if (message == null)
            {
                return;
            }
            // Clear old reactions
            await message.RemoveAllReactionsAsync();
            ClearReactionBinds();
            // Build page
            EmbedBuilder embedded = new Discord.EmbedBuilder
            {
                //Title = Program.BotNameTwitch,
                Description = $"Installed plugins. page({page+1})",
                Color = Discord.Color.DarkOrange,
                Footer = new EmbedFooterBuilder
                {
                    //Text = $"({Program.PluginCount})Plugins running with {Program.Commands.CommandsCount} commands in total. Bot v{Program.Version}"
                    Text = $"Pluginpage"
                }
            };
            // check how many pages we need to shopw all running plugins
            int maxPage = (int)Math.Ceiling(Program.PluginCount / 5.0f);
            int pageOffset = 5 * page;



            for (int i = 0 + pageOffset; i < 5 + pageOffset; i++)
            {
                if(i < Program.Plugins.Count)
                {
                    embedded.AddField(name: $"{kce[i - pageOffset]}   {Program.Plugins[i].PluginName}", $" > {Program.Plugins[i].PluginInfo}");
                }
            }

            Embed embed = embedded.Build();

            await (message as Discord.IUserMessage).ModifyAsync(m => m.Embed = embed);

            // Set up reations
            DiscordChannelMessage dMessage = await DiscordWrap.DiscordClient.DiscordGetMessage(channelID, openMenus[channelID].MessageID);

            // Link to startpage
            await DiscordWrap.DiscordClient.ReactionAdd(dMessage, kce[13]);
            rTasks[13] = new Task(() => { BackToStartPage(channelID); });

            // Add link to previous page
            if (page > 0)
            {
                await DiscordWrap.DiscordClient.ReactionAdd(dMessage, kce[11]);
                rTasks[11] = new Task(async () => { await PluginListPage(channelID, page - 1); });
            }

            // Add reactions for each plugin listed
            for (int i = 0; i < 5; i++)
            {
                if (i < Program.Plugins.Count - pageOffset)
                {
                    int x = i;
                    await DiscordWrap.DiscordClient.ReactionAdd(dMessage, kce[i]);
                    rTasks[i] = new Task(async () => { await PluginInfoPage(channelID, x + pageOffset, 0, page); });
                }
            }

            // Add link to next page
            if(5 * pageOffset < Program.PluginCount)
            {
                await DiscordWrap.DiscordClient.ReactionAdd(dMessage, kce[12]);
                rTasks[12] = new Task(async () => { await PluginListPage(channelID, page + 1); });
            }



            // Update timestamp
            openMenus[channelID] = new Helpmenu(channelID, openMenus[channelID].MessageID);
        }


        #endregion


        #region Plugin Information Page
        private async Task PluginInfoPage(ulong channelID, int pluginIndex, int page, int privpage)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "Pages", $"PluginInfoPage ({Program.Plugins[pluginIndex].PluginName}  index:{pluginIndex} p.{page})"));
            if (!openMenus.ContainsKey(channelID))
            {
                return; // no menu open in channel
            }
            else if (openMenus[channelID].Timestamp + 300 < TimerStuff.Uptime)
            {
                openMenus.Remove(channelID);
                return; // menu to old
            }
            // Get message
            IMessage message = await (Program.DiscordClient.GetChannel(channelID) as ISocketMessageChannel).GetMessageAsync(openMenus[channelID].MessageID);
            if (message == null)
            {
                return;
            }
            // Clear old reactions
            await message.RemoveAllReactionsAsync();
            ClearReactionBinds();
            // Build page
            EmbedBuilder embedded = new Discord.EmbedBuilder
            {
                Title = Program.Plugins[pluginIndex].PluginName,
                Description = $"Info info page({page + 1})",
                Color = Discord.Color.DarkOrange,
                Footer = new EmbedFooterBuilder
                {
                    //Text = $"({Program.PluginCount})Plugins running with {Program.Commands.CommandsCount} commands in total. Bot v{Program.Version}"
                    Text = $"Pluginpage"
                }
            };
            // check how many pages we need to shopw all running plugins
            int maxPage = (int)Math.Ceiling(Program.PluginCount / 5.0f);
            int pageOffset = 5 * page;



            //for (int i = 0 + pageOffset; i < 5 + pageOffset; i++)
            //{

            Dictionary<string, RegisteredCommand> commands = Program.Commands.GetSubCommands(Program.Plugins[pluginIndex].CMD);
            if (commands != null)
            {
                foreach (string key in commands.Keys)
                {
                    string commandList = string.Empty;


                    embedded.AddField(
                            name: $"{key}",
                            commands[key].helptext
                            );
                }
            }

            //}

            await (message as Discord.IUserMessage).ModifyAsync(m => m.Embed = embedded.Build());

            // Set up reactions
            DiscordChannelMessage dMessage = await DiscordWrap.DiscordClient.DiscordGetMessage(channelID, openMenus[channelID].MessageID);

            // Link to startpage
            await DiscordWrap.DiscordClient.ReactionAdd(dMessage, kce[13]);
            rTasks[13] = new Task(() => { BackToStartPage(channelID); });

            // Add link to previous page
            
            await DiscordWrap.DiscordClient.ReactionAdd(dMessage, kce[11]);
            rTasks[11] = new Task(async () => { await PluginListPage(channelID, privpage); });

            // Add reactions for each plugin listed
            //for (int i = 0; i < 5; i++)
            //{
            //    if (i < Program.Plugins.Count - pageOffset)
            //    {
            //        await DiscordWrap.DiscordClient.ReactionAdd(dMessage, kce[i]);
            //        rTasks[i] = new Task(() => { BackToStartPage(channelID); });
            //    }
            //}
            //
            //// Add link to next page
            //if (5 * pageOffset < Program.PluginCount)
            //{
            //    await DiscordWrap.DiscordClient.ReactionAdd(dMessage, kce[12]);
            //    rTasks[12] = new Task(async () => { await PluginListPage(channelID, page + 1); });
            //}



            // Update timestamp
            openMenus[channelID] = new Helpmenu(channelID, openMenus[channelID].MessageID);
        }


        #endregion



        


        #region Discord Reaction Event Listeners
        private void OnDiscordReactionAdded(BotChannel bChan, UserEntry user, DiscordReactionArgument args)
        {
            int index = -1;
            if (!openMenus.ContainsKey(args.ChannelID)) { return; }
            if (kce.Contains(args.Emote))
            {
                index = Array.FindIndex(kce, p=>p == args.Emote);
            }
            if(index < 0) { return; }

            if (rTasks[index] != null) { if (!rTasks[index].IsCompleted) { rTasks[index].Start(); } }
        }
        #endregion


        private void BuildCommandFields(Dictionary<string, Dictionary<string, RegisteredCommand>> registeredCommands, ref EmbedBuilder embedded)
        {
            foreach (string key in registeredCommands.Keys)
            {
                string commandList = string.Empty;
                string pluginName = key;
                foreach (string subKey in registeredCommands[key].Keys)
                {
                    pluginName = registeredCommands[key][subKey].method.GetMethodInfo().DeclaringType.Name;
                    commandList += $"> **{Program.CommandCharacter}{key} {subKey}** ```{registeredCommands[key][subKey].helptext}```{NL}";
                }
                embedded.AddField(
                    name: $"Plugin: {pluginName}",
                    commandList
                    );
            }
        }

        /// <summary>
        /// All this does is clean up the references we have to help menus. ANy older then 300s will become inresponsive
        /// </summary>
        /// <param name="minute"></param>
        private void OnMinuteTick(int minute)
        {
            // flush old messages we cont have to repspond to anymore
            foreach (KeyValuePair<ulong, Helpmenu> entry in openMenus.Where(p => p.Value.Timestamp < TimerStuff.Uptime - 300).ToList())
            {
                openMenus.Remove(entry.Key);
            }
        }
        /// <summary>
        /// Remember to clear old binds when updating the page
        /// </summary>
        private void ClearReactionBinds()
        {
            rTasks = new Task[14];
        }
    }// EOF CLASS
}
