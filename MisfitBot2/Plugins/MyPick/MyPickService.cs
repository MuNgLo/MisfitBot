using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MisfitBot2.Plugins.MyPick;
namespace MisfitBot2.Services
{
    class MyPickService : ServiceBase, IService
    {
        private readonly string PLUGINNAME = "MyPick";
        public MyPickService()
        {
            Core.Discord.GuildAvailable += OnGuildAvailable;
        }

        private async Task OnGuildAvailable(SocketGuild arg)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(arg.Id);
            if (bChan == null) { return; }
            MyPickSettings settings = await Settings(bChan);
        }

        #region Discord methods
        public async Task DiscordPicks(ICommandContext context, List<string> args)
        {
            await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info,
                PLUGINNAME,
                $"{context.User.Username} used command \"picks\" in {context.Channel.Name}."
                ));
            if(args.Count < 2) { return; }
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(context.Guild.Id);
            if (bChan == null) { return; }
            switch (args[0].ToLower())
            {
                case "nominate":
                    if (await AddNominee(bChan, args))
                    {
                        await context.Channel.SendMessageAsync($"{args[1]} is now nominated.");
                    }
                    else
                    {
                        if (await VerifyNominee(bChan, args[1]))
                        {
                            await context.Channel.SendMessageAsync($"Failed to nominate{args[1]}. That one is already nominated.");
                        }
                        else
                        {
                            await context.Channel.SendMessageAsync($"Failed to nominate{args[1]}.");
                        }
                    }
                    break;
                case "category":
                    if (await ChangeCategory(bChan, args[1]))
                    {
                        await context.Channel.SendMessageAsync($"Category updated.");
                    }
                    else
                    {
                        await context.Channel.SendMessageAsync($"Failed to update category.");
                    }
                    break;
                case "header":
                    if (await ChangeHeader(bChan, args))
                    {
                        await context.Channel.SendMessageAsync($"header updated.");
                    }
                    else
                    {
                        await context.Channel.SendMessageAsync($"Failed to update header.");
                    }
                    break;
                case "denominate":
                    if (await DeleteNominee(bChan, args[1]))
                    {
                        await context.Channel.SendMessageAsync($"{args[1]} is now removed from the list of nominees.");
                    }
                    else
                    {
                        if (!await VerifyNominee(bChan, args[1]))
                        {
                            await context.Channel.SendMessageAsync($"Failed to remove {args[1]}. That one is not nominated.");
                        }
                        else
                        {
                            await context.Channel.SendMessageAsync($"Failed to remove {args[1]}."); // SEE this live and something is very wrong
                        }
                    }
                    break;
            }
        }

       

        public async Task DiscordTopList(ICommandContext Context)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            MyPickSettings settings = await Settings(bChan);
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle(settings.header);
            // TOP LIST
            string msg = await TopList(bChan);
            string nominations = string.Empty;
            foreach(ListEntry entry in settings.nominees)
            {
                if (entry.Link != string.Empty)
                {
                    nominations += $"[{entry.Title}]({entry.Link}),";
                }
                else
                {
                    nominations += $"{entry.Title},";
                }
            }
            string help = $"To nominate a {settings.category} have a word with a moderator.{Environment.NewLine}" +
                $"To cast your vote type **{Core._commandCharacter}listvote <entryname>**{Environment.NewLine}";
            builder.WithDescription(msg);
            builder.WithColor(Color.Purple);

            EmbedFieldBuilder field = new EmbedFieldBuilder();
            if(nominations == string.Empty)
            {
                field.Name = $"There is no nominated {settings.category} yet.";
            }
            else
            {
                field.Name = nominations;
            }
            field.Value = 1000;
            builder.AddField($"Nominated {settings.category} are....", field.Build());

            EmbedFieldBuilder field2 = new EmbedFieldBuilder();
            field2.Name = help;
            field2.Value = 100;
            builder.AddField("How to participate", field2.Build());

            Embed obj = builder.Build();
            await Context.Channel.SendMessageAsync("", false, obj);
        }

        public async Task DiscordMyPick(ICommandContext Context, string game)
        {
            BotChannel bChan = await Core.Channels.GetDiscordGuildbyID(Context.Guild.Id);
            if (bChan == null) { return; }
            if (!await VerifyNominee(bChan, game))
            {
                await Context.Channel.SendMessageAsync($"Could not match {game} with any nomination.");
                return;
            }
            if(await AddVote(bChan, Context.User.Id, game))
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Username} now casts their vote on {game}.");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Username}, so sorry but something went wrong and your vote for \"{game}\" could not be registered.");
            }
        }

        #endregion
        #region internal methods
        private async Task<bool> ChangeCategory(BotChannel bChan, string newCategory)
        {
            MyPickSettings settings = await Settings(bChan);
            if (settings.category != newCategory)
            {
                settings.category = newCategory;
                SaveBaseSettings(PLUGINNAME, bChan, settings);
                return true;
            }
            return false;
        }
        private async Task<bool> ChangeHeader(BotChannel bChan, List<string> args)
        {
            MyPickSettings settings = await Settings(bChan);
            args.RemoveAt(0);
            string header = string.Empty;
            foreach(string str in args)
            {
                header += str + " ";
            }
            header = header.Trim();

            if (settings.header != header)
            {
                settings.header = header;
                SaveBaseSettings(PLUGINNAME, bChan, settings);
                return true;
            }
            return false;
        }
        private async Task<bool> AddVote(BotChannel bChan, ulong userID, string vote)
        {
            MyPickSettings settings = await Settings(bChan);
            settings.votes[userID] = new Vote(vote);
            SaveBaseSettings(PLUGINNAME, bChan, settings);
            return await VerifyVote(bChan, userID, vote);
        }
        private async Task<bool> VerifyVote(BotChannel bChan, ulong userID, string vote)
        {
            MyPickSettings settings = await Settings(bChan);
            if (!settings.votes.ContainsKey(userID))
            {
                return false;
            }
            return settings.votes[userID].title == vote;
        }
        private async Task<bool> VerifyNominee(BotChannel bChan, string nominee)
        {
            MyPickSettings settings = await Settings(bChan);
            foreach(ListEntry entry in settings.nominees)
            {
                if(entry.Title.ToLower() == nominee.ToLower())
                {
                    return true;
                }
            }
            return false;
        }
        private async Task<ListEntry> GetNominee(BotChannel bChan, string nominee)
        {
            MyPickSettings settings = await Settings(bChan);
            foreach (ListEntry entry in settings.nominees)
            {
                if (entry.Title.ToLower() == nominee.ToLower())
                {
                    return entry;
                }
            }
            return new ListEntry();
        }
        /// <summary>
        /// Trieds to add the nominee and returns true if it did.
        /// </summary>
        /// <param name="bChan"></param>
        /// <param name="nominees"></param>
        /// <returns></returns>
        private async Task<bool> AddNominee(BotChannel bChan, List<string> args)
        {
            MyPickSettings settings = await Settings(bChan);
            string nominee = args[1];
            string link = string.Empty;
            if (args.Count > 2)
            {
                link = args[2];
            }
                if (!await VerifyNominee(bChan, nominee) && nominee != string.Empty)
                {
                    settings.nominees.Add(new ListEntry()
                    {
                        Title = nominee,
                        Link = link
                    });
                    SaveBaseSettings(PLUGINNAME, bChan, settings);
                }
                return await VerifyNominee(bChan, nominee);
        }
        private async Task<bool> DeleteNominee(BotChannel bChan, string nominee)
        {
            MyPickSettings settings = await Settings(bChan);
            if (await VerifyNominee(bChan, nominee))
            {
                settings.nominees.RemoveAll(p => p.Title.ToLower() == nominee.ToLower());
            }
            SaveBaseSettings(PLUGINNAME, bChan, settings);
            return !(await VerifyNominee(bChan, nominee));
        }
        private async Task<string> TopList(BotChannel bChan)
        {
            MyPickSettings settings = await Settings(bChan);

            List<VoteCount> countedVotes = new List<VoteCount>();

            foreach(ulong userID in settings.votes.Keys)
            {
                ListEntry entry = await GetNominee(bChan, settings.votes[userID].title);
                if(entry.Title != string.Empty)
                {
                    if(!countedVotes.Exists(P=>P.Title == settings.votes[userID].title)) {
                        countedVotes.Add(new VoteCount(settings.votes[userID].title, entry.Link));
                    }
                    countedVotes.Find(P => P.Title == settings.votes[userID].title).Votes += 1;
                }
            }

            List<VoteCount> sortedVote = countedVotes.OrderByDescending(p => p.Votes).ToList();

            string toplist = string.Empty;
            int index = 1;

            foreach(VoteCount count in sortedVote)
            {
                toplist += $"#{index}  [{count.Title}]({count.Link}) ({count.Votes}){Environment.NewLine}";
                index++;
                if (index > 10) continue;
            }

            return $"{toplist}{Environment.NewLine}{Environment.NewLine}";
        }
        private async Task<MyPickSettings> Settings(BotChannel bChan)
        {
            MyPickSettings settings = new MyPickSettings();
            return await Core.Configs.GetConfig(bChan, PLUGINNAME, settings) as MyPickSettings;
        }
        #endregion
        #region IService methods
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
