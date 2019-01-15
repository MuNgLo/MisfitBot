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
    public class TwitchCommandsService
    {
        private readonly string PLUGINNAME = "TwitchCommandsService";
        private readonly Random RNG = new Random();
        // Use this for initialization
        public TwitchCommandsService()
        {
            Core.Twitch._client.OnChatCommandReceived += ClientOnChatCommandReceived;
        }

        public async void ClientOnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            Random rng = new Random();
            int toss = 0;
            switch (e.Command.CommandText.ToLower())
            {
                case "juanage":
                    JuanAge(e);
                    break;
                case "insult":
                    Insult(e);
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

        private void Insult(OnChatCommandReceivedArgs e)
        {
            List<string> insults = new List<string>(
                new string[] {
                "Don’t feel bad, there are many people who have no talent!",
                "As an outsider, what do you think of the human race?",
                "I’d like to kick you in the teeth, but why should I improve your looks?",
                "At least there’s one thing good about your body – it’s not as ugly as your face.",
                "Brains aren’t everything. In fact, in your case they’re nothing.",
                "I like you.People say I’ve no taste, but I like you.",
                "Did your parents ever ask you to run away from home?",
                "If I had a face like yours I’d sue my parents.",
                "Any similarity between you and a human is purely coincidental.",
                "Keep talking – someday you’ll say something intelligent.",
                "Don’t you love nature, despite what it did to you?",
                "Don’t think, it might sprain your brain.",
                $"{e.Command.ChatMessage.DisplayName} has a mechanical mind.Too bad they forgot to wind it up this morning.",
                $"{e.Command.ChatMessage.DisplayName} is always lost in thought.It’s unfamiliar territory.",
                "Are you always so stupid or is today a special occasion?",
                $"{e.Command.ChatMessage.DisplayName} is listed in Who’s Who as What’s That?",
                $"{e.Command.ChatMessage.DisplayName} is living proof that man can live without a brain.",
                $"{e.Command.ChatMessage.DisplayName} is so short, when it rains they are always the last to know.",
                $"{e.Command.ChatMessage.DisplayName} is the kind of a person you’d use as a blueprint to build an idiot.",
                "How did you get here? Did someone leave your cage open?",
                "How would you like to feel the way you look?",
                "I can’t talk to you right now. Where will you be 10 years from now?",
                "I don’t want you to turn the other cheek, it’s just as ugly.",
                "I don’t know what it is that makes you so stupid but it really works.",
                "I can’t seem to remember your name, and please don’t help me.",
                "I’ve seen people like you but I had to pay admission.",
                "Do you practise being this ugly ?"
                }
                );
            Core.Twitch._client.SendMessage(e.Command.ChatMessage.Channel, insults[RNG.Next(0, insults.Count)]);
        }

    }
}
