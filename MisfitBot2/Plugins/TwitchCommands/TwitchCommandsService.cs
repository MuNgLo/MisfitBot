using System;
using System.Collections;
using System.Collections.Generic;
using TwitchLib;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using MisfitBot2;
using System.Threading;
using MisfitBot2.Extensions.ChannelManager;

namespace MisfitBot2.Services
{
    /// <summary>
    /// Simple Twitch only commands that don't need any other outside information.
    /// </summary>
    public class TwitchCommandsService
    {
        private readonly string PLUGINNAME = "TwitchCommandsService";
        private readonly Random RNG = new Random();
        #region insults
        List<string> insultsRMG = new List<string>(
                new string[] {
                $"Don’t feel bad [RandomUser], there are many people who have no talent!",
                $"Hey [RandomUser], as an outsider, what do you think of the human race?",
                $"I’d like to kick [RandomUser] in the teeth, but why should I improve their looks?"
                }
                );
        List<string> insults = new List<string>(
                 new string[] {
                $"Don’t feel bad [User], there are many people who have no talent!",
                $"Hey [User], as an outsider, what do you think of the human race?",
                $"I’d like to kick [User] in the teeth, but why should I improve their looks?",
                $"At least there’s one thing good about [User]'s body – it’s not as ugly as their face.",
                $"Brains aren’t everything. In fact, in [User]'s case they’re nothing.",
                $"I like [User]. People say I’ve no taste, but I like [User].",
                $"[User], did your parents ever ask you to run away from home?",
                $"[User], If I had a face like yours I’d sue my parents.",
                $"Any similarity between [User] and a human is purely coincidental.",
                $"[User], keep talking – someday you’ll say something intelligent.",
                $"[User], don’t you love nature, despite what it did to you?",
                $"[User], don’t think, it might sprain your brain.",
                $"[User] has a mechanical mind.Too bad they forgot to wind it up this morning.",
                $"[User] is always lost in thought. It’s unfamiliar territory.",
                $"Are [User] always so stupid or is today a special occasion?",
                $"[User] is listed in Who’s Who as What’s That?",
                $"[User] is living proof that man can live without a brain.",
                $"[User] is so short, when it rains they are always the last to know.",
                $"[User] is the kind of a person you’d use as a blueprint to build an idiot.",
                $"How did you get here? Chat, did someone leave [User]'s cage open?",
                $"[User], how would you like to feel the way you look?",
                $"I can’t talk to you right now. Where will you be 10 years from now?",
                $"I don’t want you to turn the other cheek, it’s just as ugly.",
                $"I don’t know what it is that makes you so stupid but it really works.",
                $"I can’t seem to remember your name, and please don’t help me.",
                $"I’ve seen people like you but I had to pay admission.",
                $"[User], do you practise being this ugly ?"
                 }
                 );
        #endregion

        // Use this for initialization
        public TwitchCommandsService()
        {
            Core.Twitch._client.OnChatCommandReceived += ClientOnChatCommandReceived;
            Core.Twitch._client.OnMessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Message.ToLower() == "shutup juan")
            {
                Insult(e.ChatMessage.DisplayName, e.ChatMessage.Channel);
            }
            if (e.ChatMessage.Message.ToLower() == "fuck off juan")
            {
                Insult(e.ChatMessage.DisplayName, e.ChatMessage.Channel);
            }
            if (e.ChatMessage.Message.ToLower() == "juan sucks")
            {
                Insult(e.ChatMessage.DisplayName, e.ChatMessage.Channel);
            }
        }

        public async void ClientOnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            Random rng = new Random();
            int toss = 0;
            switch (e.Command.CommandText.ToLower())
            {
                case "trello":
                    Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, "My Trello page is https://trello.com/b/JvyAKGJt/misfit-bot");
                    break;
                case "juanage":
                    JuanAge(e);
                    break;
                case "insult":
                    Insult(e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Channel);
                    break;
                case "revulsion":
                    Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, "Revulsion store link https://store.steampowered.com/app/719180/Revulsion/");
                    break;
                case "synthetik":
                    Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, "SYNTHETIK store link https://store.steampowered.com/app/528230/SYNTHETIK/");
                    break;
                case "starwardrogue":
                    Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, "Starward Rogue store link https://store.steampowered.com/app/410820/Starward_Rogue/");
                    break;
                case "link":
                    if(e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                    {
                        BotChannel link = await Core.Channels.GetTwitchChannelByName(e.Command.ChatMessage.Channel);
                        if(!link.isLinked)
                        {
                            Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, "No linked Discord channel.");
                        }
                        else
                        {
                            Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, "Linked Discord channel found.");
                        }
                    }
                    break;
                case "coin":
                    BotChannel bChan = await Core.Channels.GetTwitchChannelByName(e.Command.ChatMessage.Channel);
                    if (bChan == null) { return; }
                    await Core.LOG(new Discord.LogMessage(Discord.LogSeverity.Info,
                        PLUGINNAME,
                        $"{e.Command.ChatMessage.Username} used !Coin in {e.Command.ChatMessage.Channel}."));
                    toss = rng.Next(0, 20);
                    if (toss < 10)
                    {
                        Core.Twitch._client.SendMessage(
                                    e.Command.ChatMessage.Channel,
                                    $"Heads."
                                    );
                    }
                    else
                    {
                        Core.Twitch._client.SendMessage(
                                    e.Command.ChatMessage.Channel,
                                    $"Tails."
                                    );
                    }
                    break;
            }
        }

        private void JuanAge(OnChatCommandReceivedArgs e)
        {
                TimeSpan time = TimeSpan.FromSeconds(Core.UpTime);

            //here backslash is must to tell that colon is
            //not the part of format, it just a character that we want in output
            //string str = time.ToString(@"dd\d\a\y\s\ \ hh\:mm\:ss");
            string str = time.ToString(@"d\ \d\a\y\s\ \a\n\d\ \ hh\:mm\:ss");

            //seconds -= minutes * 60;
            //minutes -= hours * 60;

            string msg = $"I have been running for {str}";
                Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, msg);

        }

        private void Insult(string victim, string twitchChannel)
        {

            Core.Twitch._client.SendMessage(
                twitchChannel,
                StringFormatter.ConvertMessage(new StringFormatterArguments()
                {
                  message=insults[RNG.Next(0, insults.Count)],
                  user = victim,
                  targetUser = null,
                  twitchChannel = twitchChannel
                }
                ));
        }

    }
}
