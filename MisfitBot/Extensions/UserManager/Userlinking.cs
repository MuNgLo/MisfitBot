using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using MisfitBot_MKII.Statics;

namespace MisfitBot_MKII.Extensions.UserManager
{
    /// <summary>
    /// This handles userlinking and the tokens used for it.
    /// </summary>
    class Userlinking
    {
        private List<LinkToken> Tokens = new List<LinkToken>();
        public Userlinking()
        {
            TimerStuff.OnSecondTick += OnSecondTick;
            if(Program.DiscordClient != null){
                Program.DiscordClient.MessageReceived += DiscordMessageReceived;
            }
            if(Program.TwitchClient != null){
                Program.TwitchClient.OnWhisperReceived += TwitchOnWhisperReceived;
            }
        }

        private async void OnSecondTick(int second)
        {
            if(second % 15 == 0)
            {
                List<LinkToken> cleaned = Tokens.FindAll(p => p.timestamp > Core.CurrentTime - 180);
                int removed = Tokens.Count - cleaned.Count;
                Tokens = cleaned;
                if(removed > 0)
                {
                    await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "UserLinking", $"Dumping old tokens. ({removed})"));
                }
            }
        }

        public async Task SetupAndInformLinkToken(UserEntry user)
        {
            if (user.linked) { return; }
            LinkToken token;
            bool informOnTwitch = true;
            if(user.discordUID != 0)
            {
                token = new LinkToken(user.discordUID);
                informOnTwitch = false;
            }
            else
            {
                token = new LinkToken(user.twitchUID);
            }
            Tokens.Add(token);
            if (informOnTwitch)
            {
                Program.TwitchClient.SendWhisper(user.twitchUsername,
                    $"Next step is to send me this code '{token.PIN}' in a direct message on Discord. Make sure to only send the code. Code expires in 180s."
                    );
            }
            else
            {

                SocketUser u = Program.DiscordClient.GetUser(user.discordUID);
                await Discord.UserExtensions.SendMessageAsync(u,
                    $"Next step is to send me this code '{token.PIN}' in a whisper on Twitch. Make sure to only send the code. Code expires in 180s."
                    );
            }

        }

        private async void MatchToken(LinkToken token)
        {
            Tokens.RemoveAll(p => p.discordID == token.discordID);
            UserEntry tUser = await Program.Users.GetUserByTwitchID(token.twitchID);
            UserEntry dUser = await Program.Users.GetUserByDiscordID(Core.StringToUlong(token.discordID));
            if(tUser != null && dUser != null)
            {
                await Program.Users.LinkAccounts(dUser, tUser);
            }
        }

        private async void TwitchOnWhisperReceived(object sender, TwitchLib.Client.Events.OnWhisperReceivedArgs e)
        {
            UserEntry user = await Program.Users.GetUserByTwitchID(e.WhisperMessage.UserId);
            if(user == null)
            {
                return;
            }
            string message = e.WhisperMessage.Message.Trim();
            string[] args = message.Split(' ');
            if(args.Length != 1)
            {
                return;
            }
            int pin = 000000;
            int.TryParse(args[0], out pin);
            if (pin == 000000) { return; }
            if(Tokens.Exists(p=>p.PIN == pin))
            {
                LinkToken token = Tokens.Find(p => p.PIN == pin);
                token.twitchID = e.WhisperMessage.UserId;
                MatchToken(token);
            }
        }

        private async Task DiscordMessageReceived(SocketMessage arg)
        {
            if(arg.Channel.Id != arg.Author.Id || arg.Author.IsBot == true)
            {
                return;
            }
            string message = arg.Content.Trim();
            string[] args = message.Split(' ');
            if (args.Length != 1)
            {
                return;
            }
            await arg.Channel.SendMessageAsync("Fail");
        }
    }

    class LinkToken
    {
        public string discordID = string.Empty;
        public string twitchID = string.Empty;
        public int timestamp = Core.CurrentTime;
        public int PIN = 000000;
        public LinkToken(ulong dID)
        {
            discordID = dID.ToString();
            Random rng = new Random();
            PIN = rng.Next(100000, 999999);
        }
        public LinkToken(string tID)
        {
            twitchID = tID;
            Random rng = new Random();
            PIN = rng.Next(100000, 999999);
        }
    }
}
