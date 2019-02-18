using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MisfitBot2.Services
{
    class HelpService : ServiceBase, IService
    {
        /// <summary>
        /// The basic response to the help command without any arguments given.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task DiscordHelp(ICommandContext context)
        {
            string cmdIdent = $"{Core._commandCharacter}";
            int i = 0;
            foreach (CommandInfo cmd in Program._commands.Commands)
            {
                i++;
            }

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle("Help page. General use of help");
            builder.WithColor(Color.Purple);
            builder.AddField("General use of help", $"There are three ways of using the help command." +
                $" With just <{cmdIdent}help> you get this. You can also show list of commands by page with <{cmdIdent}help #>." +
                $"Examples would be like: {cmdIdent}help, {cmdIdent}help 2");
            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer.Text = $"There are a total of {i} commands registered.";
            builder.Footer = footer;
            Embed obj = builder.Build();
            await context.Channel.SendMessageAsync("", false, obj);
        }
        /// <summary>
        /// Outputs the given page to the Discord channel.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task DiscordHelpPage(ICommandContext context, int page)
        {
            //string msg = string.Empty;
            int i = 0;
            int offset = (page - 1) * 10;
            // Make the embedded stuff
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle($"Help page {page}");
            builder.WithColor(Color.Purple);

            foreach (CommandInfo cmd in Program._commands.Commands)
            {
                if (i < offset)
                {
                    i++;
                    continue;
                }
                if (cmd.Summary != null)
                {
                    //EmbedFieldBuilder field = new EmbedFieldBuilder();
                    //field.Name = cmd.Summary;
                    builder.AddField(cmd.Name, cmd.Summary);
                    //msg += $"   [{cmd.Name}] :: {cmd.Summary}{System.Environment.NewLine}";
                }
                else
                {
                    continue;
                }
                i++;
                if (i > 10 + offset)
                {
                    break;
                }
            }
            //await context.Channel.SendMessageAsync(msg);
            Embed obj = builder.Build();
            await context.Channel.SendMessageAsync("", false, obj);
        }
        /// <summary>
        /// Outputs hits for the search into the Discord channel. 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        public async Task DiscordHelpSearch(ICommandContext context, string search)
        {
            // Make the embedded stuff
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle($"Help for the search");
            builder.WithColor(Color.Purple);
            foreach (CommandInfo cmd in Program._commands.Commands)
            {
                if(cmd.Name == null)
                {
                    continue;
                }
                if(cmd.Summary == null)
                {
                    continue;
                }
                if (cmd.Name.Contains(search) || cmd.Summary.Contains(search))
                {
                    builder.AddField(cmd.Name, cmd.Summary);
                }
            }
            await context.Channel.SendMessageAsync("wooop woop!");
            Embed obj = builder.Build();
            await context.Channel.SendMessageAsync("", false, obj);
        }

        #region Interface compatability
        public Task ClearDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            throw new NotImplementedException();
        }
        public void NewUserValuesEntry(ulong userID, ulong guildID)
        {
            throw new NotImplementedException();
        }
        public void NewUserValuesEntry(string twitchUserID, string twitchChannelID)
        {
            throw new NotImplementedException();
        }
        public void OnBotChannelEntryMerge(BotChannel discordGuild, BotChannel twitchChannel)
        {
            throw new NotImplementedException();
        }
        public void OnMinuteTick(int minutes)
        {
            throw new NotImplementedException();
        }
        public void OnSecondTick(int seconds)
        {
            throw new NotImplementedException();
        }
        public void OnUserEntryMerge(UserEntry discordUser, UserEntry twitchUser)
        {
            throw new NotImplementedException();
        }
        public Task SetDefaultDiscordChannel(BotChannel bChan, ulong discordChannelID)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
