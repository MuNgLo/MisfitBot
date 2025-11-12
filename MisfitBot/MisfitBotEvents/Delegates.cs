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
    public delegate void DiscordMemberUpdatedEvent(BotChannel bChan, UserEntry currentUser, UserEntry oldUser);
    public delegate void DiscordMembersDownloadedEvent(SocketGuild arg);
    public delegate void DiscordReadyEvent();
    public delegate void DiscordReactionClearedEvent(BotChannel bChan, ulong channelID);
    public delegate void DiscordReactionRemovedEvent(BotChannel bChan, UserEntry currentUser, DiscordReactionArgument args);
    public delegate void DiscordReactionAddedEvent(BotChannel bChan, UserEntry currentUser, DiscordReactionArgument args);
    // All Twitch events here all all need to start with Twitch and end with Event
    public delegate void TwitchHostEvent(BotChannel bChan, HostedEventArguments e);
    public delegate void TwitchSubGiftEvent(BotChannel bChan, TwitchSubGiftEventArguments e);
    
    public delegate void TwitchConnectedEvent();
    public delegate void TwitchConnectionErrorEvent(string msg);
    public delegate void TwitchChannelChatClearedEvent(string channel);
    public delegate void TwitchChannelJoinLeaveEvent(string channel, string botName);
    public delegate void TwitchCommunitySubscriptionEvent(BotChannel bChan, string message);
    public delegate void TwitchNewFollowerEvent(BotChannel botChannel, UserEntry user);
    public delegate void TwitchReSubscriberEvent(BotChannel botChannel, TwitchReSubArguments e);
    public delegate void TwitchNewSubscriberEvent(BotChannel botChannel, TwitchNewSubArguments e);
    public delegate void TwitchDisconnectedEvent();
    public delegate void TwitchMessageClearedEvent(BotChannel botChannel, OnMessageClearedArgs e);
    public delegate void TwitchMessageSentEvent(string channel, string message);
    public delegate void TwitchWhisperMessageEvent(string username, string message);
    public delegate void TwitchUserJoinLeaveEvent(BotChannel botChannel, UserEntry user);
    // BotWide type events
    public delegate void MessageReceivedEvent(BotWideMessageArguments args);
    public delegate void CommandReceivedEvent(BotWideCommandArguments args);

    // Below needs to be verified
    public delegate void TwitchChannelGoesLiveEvent(TwitchStreamGoLiveEventArguments args);
    public delegate void TwitchChannelGoesOfflineEvent(TwitchStreamGoOfflineEventArguments args);
    public delegate void BanEvent(BanEventArguments e);
    public delegate void BitEvent(BitEventArguments e);
    public delegate void DiscordUserStartsStreamEvent(BotChannel bChan, UserEntry user, StreamingGame streamInfo);
    public delegate void BotChannelMergeEvent(BotChannel discordBotChannel, BotChannel twitchBotChannel);
    public delegate void RaidEvent(BotChannel bChan, RaidEventArguments e);
    public delegate void UnBanEvent(UnBanEventArguments e);
    public delegate void UserEntryMergeEvents(UserEntry discordUser, UserEntry twitchUser);
    public delegate void ViewerCountEvent(BotChannel bChan, int oldCount, int newCount);
    #endregion
}