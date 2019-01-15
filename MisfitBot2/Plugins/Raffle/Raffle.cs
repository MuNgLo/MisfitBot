using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using MisfitBot2.Extensions.ChannelManager;

namespace MisfitBot2.Plugins.Raffle
{
    public class Raffle
    {
        public string _raffleIdentifier; // Twich channel name for unlinked twitch channels, Otherwise it is Discord Guild ID
        public RaffleRange _range = RaffleRange.NOTSET;
        public bool isSelling = true;
        public int _ticketprice;
        private List<RaffleTicket> _tickets = new List<RaffleTicket>();
        private List<RaffleTicket> _drawnTickets = new List<RaffleTicket>();
        private int _reminderInterval = 60;
        private int _reminderTimestamp = 0;
        public int _msgSinceLast = 0;
        public ulong _discordChannel = 0;
        public string _twitchChannel = string.Empty;

        public Raffle(BotChannel bChan, int numberOfTickets, int ticketPrice)
        {
            if (bChan.isLinked)
            {
                _raffleIdentifier = bChan.Key;
                _twitchChannel = bChan.TwitchChannelName;
                _range = RaffleRange.BOTH;
            }
            else
            {
                    _raffleIdentifier = bChan.Key;
                    _range = RaffleRange.TWITCHONLY;
            }


            _ticketprice = ticketPrice;
            for(int i=0; i < numberOfTickets; i++)
            {
                _tickets.Add(new RaffleTicket(i));
            }
        }

        public int AvailableTickets() {
            return _tickets.FindAll(p => p._owner == string.Empty).Count;
        }

        public async Task<RaffleTicket> RandomTicketNoOwner()
        {
            List<RaffleTicket> possible = _tickets.FindAll(p => p._owner == string.Empty);
            if(possible.Count == 1)
            {
                if (_range == RaffleRange.TWITCHONLY)
                {
                    Core.Twitch._client.SendMessage(_twitchChannel, $"Last ticket in the raffle is now sold.");
                }
                else if (_range == RaffleRange.DISCORDONLY)
                {
                    BotChannel bChan = await Core.Channels.GetBotchannelByKey(_raffleIdentifier);
                    if (bChan.discordDefaultBotChannel != 0)
                    {
                        await (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(
                            $"Last ticket in the raffle is now sold."
                            );
                    }
                }
                else if (_range == RaffleRange.BOTH)
                {
                    BotChannel bChan = await Core.Channels.GetBotchannelByKey(_raffleIdentifier);
                    Core.Twitch._client.SendMessage(_twitchChannel, $"Last ticket in the raffle is now sold.");
                    if (bChan.discordDefaultBotChannel != 0)
                    {
                        await (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(
                            $"Last ticket in the raffle is now sold."
                            );
                    }
                }
            }
            Random rng = new Random();
            int index = rng.Next(possible.Count);
            return _tickets.Find(p => p._ticketNumber == possible[index]._ticketNumber);
        }

        public async Task SellingTickets(bool flag)
        {
            if (isSelling == flag) { return; }
            BotChannel bChan = await Core.Channels.GetBotchannelByKey(_raffleIdentifier);
            isSelling = flag;
            int ticketsLeft = _tickets.FindAll(p => p._owner == string.Empty).Count;
            if (isSelling)
            {
                if (_range == RaffleRange.TWITCHONLY)
                {
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"Sale of raffle tickets are now open. There are {ticketsLeft} tickets left unsold.");
                }
                else if (_range == RaffleRange.DISCORDONLY)
                {
                    if (bChan.discordDefaultBotChannel != 0)
                    {
                        await (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(
                            $"Sale of raffle tickets are now open. There are {ticketsLeft} tickets left unsold."
                            );
                    }
                }
                else if (_range == RaffleRange.BOTH)
                {
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"Sale of raffle tickets are now open. There are {ticketsLeft} tickets left unsold.");
                    if (bChan.discordDefaultBotChannel != 0)
                    {
                        await (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(
                            $"Sale of raffle tickets are now open. There are {ticketsLeft} tickets left unsold."
                            );
                    }
                }
            }
            else
            {


                if (_range == RaffleRange.TWITCHONLY)
                {
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"Sale of raffle tickets are now closed. There are {ticketsLeft} tickets left unsold.");
                }
                else if (_range == RaffleRange.DISCORDONLY)
                {
                    if (bChan.discordDefaultBotChannel != 0)
                    {
                        (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(
                            $"Sale of raffle tickets are now closed. There are {ticketsLeft} tickets left unsold."
                            );
                    }
                }
                else if (_range == RaffleRange.BOTH)
                {
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"Sale of raffle tickets are now closed. There are {ticketsLeft} tickets left unsold.");
                    if (bChan.discordDefaultBotChannel != 0)
                    {
                        (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(
                            $"Sale of raffle tickets are now closed. There are {ticketsLeft} tickets left unsold."
                            );
                    }
                }
            }
        }

        public async Task Reminder(bool force=false)
        {
            if (!isSelling && !force) { return; }
            BotChannel bChan = await Core.Channels.GetBotchannelByKey(_raffleIdentifier);

            if (force || Core.CurrentTime > _reminderTimestamp + _reminderInterval && _msgSinceLast >= 8)
            {
                _reminderTimestamp = Core.CurrentTime;
                _msgSinceLast = 0;
                int ticketsLeft = _tickets.FindAll(p => p._owner == string.Empty).Count;



                if(_range == RaffleRange.TWITCHONLY)
                {
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"There is a raffle going. Tickets cost {_ticketprice}g each and there are {ticketsLeft} left.");
                }else if(_range == RaffleRange.DISCORDONLY)
                {
                    if (bChan.discordDefaultBotChannel != 0)
                    {
                        await (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(
                            $"There is a raffle going. Tickets cost {_ticketprice}g each and there are {ticketsLeft} left."
                            );
                    }
                }
                else if(_range == RaffleRange.BOTH)
                {
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"There is a raffle going. Tickets cost {_ticketprice}g each and there are {ticketsLeft} left.");
                    if (bChan.discordDefaultBotChannel != 0)
                    {
                        await (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(
                            $"There is a raffle going. Tickets cost {_ticketprice}g each and tere are {ticketsLeft} left."
                            );
                    }
                }
            }
        }

        public async void RefundTickets()
        {
            BotChannel bChan = await Core.Channels.GetBotchannelByKey(_raffleIdentifier);
            isSelling = false;
            foreach(RaffleTicket ticket in _tickets)
            {
                if (ticket._owner != string.Empty)
                {
                    UserEntry user = await GetUser(ticket);
                    await Core.Treasury.GiveGold(bChan, user, _ticketprice);
                }
            }
            _tickets = new List<RaffleTicket>();
        }

        private async Task<UserEntry> GetUser(RaffleTicket ticket)
        {
            if (ticket.IsDicordUser())
            {
                return await Core.UserMan.GetUserByDiscordID(Core.StringToUlong(ticket._owner));
            }
            else
            {
                return await Core.UserMan.GetUserByTwitchID(ticket._owner);
            }
        }

        internal async void DrawATicket()
        {
            Random rng = new Random();
            List<RaffleTicket> ownedTickets = _tickets.FindAll(p => p._owner != string.Empty);
            if(ownedTickets.Count < 1)
            {
                return;
            }
            int pick = rng.Next(0, ownedTickets.Count);

            RaffleTicket winningTicket = _tickets.Find(p=>p._ticketNumber == ownedTickets[pick]._ticketNumber);
            BotChannel bChan = await Core.Channels.GetBotchannelByKey(_raffleIdentifier);
            UserEntry user;
            if (winningTicket.IsDicordUser())
            {
                user = await Core.UserMan.GetUserByDiscordID(Core.StringToUlong(winningTicket._owner));
            }
            else
            {
                user = await Core.UserMan.GetUserByTwitchID(winningTicket._owner);
            }

            if (user == null) { return; }



            if (_range == RaffleRange.TWITCHONLY)
            {
                Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"{user._twitchUsername} won.");
            }
            else if (_range == RaffleRange.DISCORDONLY)
            {
                if (bChan.discordDefaultBotChannel != 0)
                {
                    await (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(
                        $"{user._username} won.");
                }
            }
            else if (_range == RaffleRange.BOTH)
            {
                if(user._twitchDisplayname == null)
                {
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"Discord user {user._username} won.");
                    if (bChan.discordDefaultBotChannel != 0)
                    {
                        await (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(
                            $"{user._username} won.");
                    }
                }
                else
                {
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"{user._twitchDisplayname} won.");
                    if (bChan.discordDefaultBotChannel != 0)
                    {
                        if (user._username == null)
                        {
                            await (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(
                            $"Twitchuser {user._twitchDisplayname} won.");
                        }
                        else
                        {
                            await (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(
                            $"{user._username} won.");
                        }
                    }
                }
            }



            _drawnTickets.Add(_tickets.Find(p=>p._ticketNumber == winningTicket._ticketNumber));
            _tickets.RemoveAll(p => p._ticketNumber == winningTicket._ticketNumber);
        }
    }
}
