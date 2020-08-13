using Discord;
using Discord.WebSocket;
using TwitchLib.Client.Events;

namespace MisfitBot_MKII.MisfitBotEvents
{
    #region Event definitions
    // All Discord events here and they all need to start with Discord and end with Event
    public delegate void DiscordConnectedEvent();
    public delegate void DiscordDisConnectedEvent();
    public delegate void DiscordGuildAvailableEvent(SocketGuild arg);
    public delegate void DiscordNewMemberEvent(BotChannel bChan, UserEntry user);
    public delegate void DiscordMemberLeftEvent(BotChannel bChan, UserEntry user);
    public delegate void DiscordMemberUpdatedEvent(BotChannel bchan, UserEntry currentUser, UserEntry oldUser);
    public delegate void DiscordMembersDownloadedEvent(SocketGuild arg);
    public delegate void DiscordReadyEvent();
    // All Twitch events here all all need to start with Twitch and end with Event
    public delegate void TwitchConnectedEvent(string args);
    public delegate void TwitchConnectionErrorEvent(string msg);
    public delegate void TwitchChannelChatCleared(string channel);
    public delegate void TwitchChannelJoinLeave(string channel, string botname);
    //public delegate void TwitchCommunitySubscription();
    public delegate void TwitchDisconnectedEvent();
    public delegate void TwitchMessageSentEvent(string channel, string message);
    public delegate void TwitchWhisperMessageEvent(string username, string message);

    // Botwide type events
    public delegate void MessageReceivedEvent(BotWideMessageArguments args);

    // Below needs to be verified
    public delegate void BanEvent(BanEventArguments e);
    public delegate void BitEvent(BitEventArguments e);
    public delegate void DiscordUserStartsStreamEvent(BotChannel bChan, UserEntry user, StreamingGame streaminfo);
    public delegate void TwitchChannelGoesLiveEvent(BotChannel bChan, int delay);
    public delegate void TwitchChannelGoesOfflineEvent(BotChannel bChan);
    public delegate void BotChannelMergeEvent(BotChannel discordBotChannel, BotChannel twitchBotChannel);
    public delegate void HostEvent(BotChannel bChan, HostEventArguments e);
    public delegate void RaidEvent(BotChannel bChan, RaidEventArguments e);
    public delegate void TwitchSubscriptionEvent(BotChannel bChan, TwitchSubEventArguments e);
    public delegate void UnBanEvent(UnBanEventArguments e);
    public delegate void UserEntryMergeEvents(UserEntry discordUser, UserEntry twitchUser);
    public delegate void ViewerCountEvent(BotChannel bChan, int oldCount, int newCount);
    #endregion
}