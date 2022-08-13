using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Embed = Discord.Embed;
using EmbedBuilder = Discord.EmbedBuilder;
using EmbedFooterBuilder = Discord.EmbedFooterBuilder;

namespace MisfitBot_MKII.Extensions.CommandInterpreter
{
    //  #️⃣ keycap: # (pound) *️⃣ keycap: * (asterisk) 0️⃣ keycap: 0 1️⃣ keycap: 1 2️⃣ keycap: 2 3️⃣ keycap: 3
    //  4️⃣ keycap: 4 5️⃣ keycap: 5 6️⃣ keycap: 6 7️⃣ keycap: 7 8️⃣ keycap: 8 9️⃣ keycap: 9 🔟 keycap: 10
    // ⬅️ ➡️

    public class Pages
    {
        private string[] kce = new string[11] { "0️⃣", "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟" };
        private string NL = System.Environment.NewLine;
        internal Embed Frontpage(Dictionary<string, Dictionary<string, RegisteredCommand>> registeredCommands, int pages, bool leftArrow=false, bool backArrow=false)
        {
            EmbedBuilder embedded = new Discord.EmbedBuilder
            {
                Title = Program.BotName,
                Description = "These are the currently registered commands",
                Color = Discord.Color.DarkOrange,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"({Program.PluginCount})Plugins running with {Program.Commands.CommandsCount} commands in total. Bot v{Program.Version}"
                }
            };

            int count = 1;
            foreach (string key in registeredCommands.Keys)
            {
                //pluginName = registeredCommands.Keys.First().;// [key].firs.method.GetMethodInfo().DeclaringType.Name;

                IEnumerator enumerator = registeredCommands[key].Keys.GetEnumerator();
                enumerator.MoveNext();
                string first = (string)enumerator.Current;


                string commandList = string.Empty;
                embedded.AddField(
                    name: $"{kce[count]}   {registeredCommands[key][first].plugin}",
                    $" > {Program.Plugins.Find(p => p.PluginName == registeredCommands[key][first].plugin).PluginInfo}"
                    );
                count++;
            }


            //BuildCommandFields(registeredCommands, ref embedded);
            return embedded.Build();
        }
        public async void FrontPageReactions(ulong dChannel, ulong msgID, int pages, bool backArrow = false, bool nextArrow = false)
        {
            // get message and remove previous reactions
            DiscordChannelMessage dMessage = await MisfitBot_MKII.DiscordWrap.DiscordClient.DiscordGetMessage(dChannel, msgID);
            await MisfitBot_MKII.DiscordWrap.DiscordClient.ClearReactionsOnMessage(dMessage);
            if (backArrow) { await MisfitBot_MKII.DiscordWrap.DiscordClient.ReactionAdd(dMessage, "⬅️"); }

            for (int i = 0; i < pages; i++)
            {
                await MisfitBot_MKII.DiscordWrap.DiscordClient.ReactionAdd(dMessage, kce[i+1]);
            }

            if (nextArrow) { await MisfitBot_MKII.DiscordWrap.DiscordClient.ReactionAdd(dMessage, "➡️"); }
        }

        private void BuildCommandFields(Dictionary<string, Dictionary<string, RegisteredCommand>> registeredCommands, ref EmbedBuilder embedded)
        {
            


            foreach (string key in registeredCommands.Keys)
            {
                string commandList = string.Empty;
                string pluginName = key;
                foreach (string subKey in registeredCommands[key].Keys)
                {
                    pluginName = registeredCommands[key][subKey].method.GetMethodInfo().DeclaringType.Name;
                    //c1 = registeredCommands[key][subKey].method.GetMethodInfo().DeclaringType.ToString().PadLeft(12);
                    //c2 = Program.CommandCharacter + registeredCommands[key][subKey].command;
                    //c3 = registeredCommands[key][subKey].subcommand;
                    //c4 = registeredCommands[key][subKey].helptext;

                    //table += c1.PadLeft(12) + c2.PadLeft(8) + c3.PadLeft(8) + c4.PadLeft(40);

                    //commandList += $"> **{Program.CommandCharacter}{key} {subKey}** {NL}{registeredCommands[key][subKey].helptext}{NL}";
                    commandList += $"> **{Program.CommandCharacter}{key} {subKey}** ```{registeredCommands[key][subKey].helptext}```{NL}";
                }
                embedded.AddField(
                    name: $"Plugin: {pluginName}",
                    commandList
                    );
            }
        }
    }// EOF CLASS
}
