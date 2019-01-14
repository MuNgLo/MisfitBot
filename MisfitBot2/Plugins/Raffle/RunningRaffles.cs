using System;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;
using MisfitBot2.Extensions.ChannelManager;

namespace MisfitBot2.Plugins.Raffle
{
    public class RunningRaffles
    {
        private List<Raffle> _raffles;

        public RunningRaffles()
        {
            _raffles = new List<Raffle>();
        }


        public bool HasRaffle(BotChannel bChan)
        {
                return _raffles.Exists(p => p._raffleIdentifier == bChan.Key);
        }

        public void StartRaffle(BotChannel bChan, int numberOfTickets, int ticketPrice)
        {
                Raffle raffle = new Raffle(bChan, numberOfTickets, ticketPrice);
            if (bChan.GuildID != 0){
                raffle._discordChannel = bChan.discordDefaultBotChannel;
                raffle._range = RaffleRange.DISCORDONLY;
            }
            if (bChan.isLinked)
            {
                raffle._discordChannel = bChan.discordDefaultBotChannel;
                raffle._range = RaffleRange.BOTH;
            }
            _raffles.Add(raffle);
                raffle.Reminder(true);
        }

        public void SellingTickets(BotChannel bChan, bool flag)
        {
                _raffles.Find(p => p._raffleIdentifier == bChan.Key).SellingTickets(flag);
        }

        public void FireReminders()
        {
            foreach(Raffle raffle in _raffles)
            {
                raffle.Reminder();
            }
        }

        public async void BuyTicket(UserEntry user, BotChannel bChan)
        {
            Raffle raffle = GetRaffle(bChan);
            if(raffle.AvailableTickets() < 1)
            {
                return;
            }
            if(raffle._range == RaffleRange.BOTH)
            {
                if(user._discordUID != 0)
                {
                    if(await Core.Treasury.MakePayment(user, bChan, raffle._ticketprice)){
                        raffle.RandomTicketNoOwner().SetOwner(user._discordUID);
                    }
                }
                else
                {
                    if (await Core.Treasury.MakePayment(user, bChan, raffle._ticketprice))
                    {
                        raffle.RandomTicketNoOwner().SetOwner(user._twitchUID);
                    }
                }

            }else if(raffle._range == RaffleRange.TWITCHONLY)
            {
                if (await Core.Treasury.MakePayment(user, bChan, raffle._ticketprice))
                {
                    raffle.RandomTicketNoOwner().SetOwner(user._twitchUID);
                }
            }
            else if(raffle._range== RaffleRange.DISCORDONLY)
            {
                if (await Core.Treasury.MakePayment(user, bChan, raffle._ticketprice))
                {
                    raffle.RandomTicketNoOwner().SetOwner(user._discordUID);
                }
            }
        }

        public Raffle GetRaffle(BotChannel bChan)
        {
                return _raffles.Find(p => p._raffleIdentifier == bChan.Key);
        }

        internal void DrawTicket(BotChannel botChannel)
        {
            Raffle raffle = GetRaffle(botChannel);
            if (raffle != null)
            {
                raffle.DrawATicket();
            }
        }
        /// <summary>
        /// Removes all active raffle entries from the botchannel
        /// Returns TRUE if succesfull
        /// </summary>
        /// <param name="bChan"></param>
        public void ClearRaffle(BotChannel bChan)
        {
            _raffles.RemoveAll(p => p._raffleIdentifier == bChan.Key);
            if (!HasRaffle(bChan)) {
                Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"Raffle cleared.");
                if (bChan.discordAdminChannel != 0)
                {
                    (Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                        $"Raffle cleared."
                        );
                }
            }
            else
            {
                Core.Twitch._client.SendMessage(bChan.TwitchChannelName, $"Raffle was not cleared.");
                if (bChan.discordAdminChannel != 0)
                {
                    (Core.Discord.GetChannel(bChan.discordAdminChannel) as ISocketMessageChannel).SendMessageAsync(
                        $"Raffle was not cleared."
                        );
                }
            }
        }
        public bool CancelRaffle(BotChannel bChan)
        {
            string msg = $"Raffle cancelled. Undrawn tickets refunded.";
            if (bChan.isLinked)
            {
                Raffle raffle = _raffles.Find(p => p._raffleIdentifier == bChan.Key);
                raffle.RefundTickets();
                Core.Twitch._client.SendMessage(bChan.TwitchChannelName, msg);
                if (bChan.discordDefaultBotChannel != 0)
                {
                    
                    (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(
                        $"Raffle cancelled. Undrawn tickets refunded."
                        );
                }
                _raffles.RemoveAt(_raffles.FindIndex(p => p._raffleIdentifier == bChan.Key));
            }
            else
            {
                if (bChan.isTwitch)
                {
                    Raffle raffle = _raffles.Find(p => p._raffleIdentifier == bChan.Key);
                    raffle.RefundTickets();
                    Core.Twitch._client.SendMessage(bChan.TwitchChannelName, msg);
                    _raffles.RemoveAll(p => p._raffleIdentifier == bChan.Key);
                }
                else
                {
                    Raffle raffle = _raffles.Find(p => p._raffleIdentifier == bChan.Key);
                    raffle.RefundTickets();
                    if (bChan.discordDefaultBotChannel != 0)
                    {
                        (Core.Discord.GetChannel(bChan.discordDefaultBotChannel) as ISocketMessageChannel).SendMessageAsync(
                            $"Raffle cancelled. Undrawn tickets refunded."
                            );
                    }
                    _raffles.RemoveAll(p => p._raffleIdentifier == bChan.Key);
                }
            }
            return !HasRaffle(bChan);
        }
    }
}
