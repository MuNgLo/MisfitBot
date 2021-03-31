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
    public delegate void DiscordReactionClearedEvent(BotChannel bchan, ulong channelID);
    public delegate void DiscordReactionRemovedEvent(BotChannel bchan, UserEntry currentUser, DiscordReactionArgument args);
    public delegate void DiscordReactionAddedEvent(BotChannel bchan, UserEntry currentUser, DiscordReactionArgument args);
    // All Twitch events here all all need to start with Twitch and end with Event
    public delegate void TwitchHostEvent(BotChannel bChan, HostedEventArguments e);
    public delegate void TwitchSubGiftEvent(BotChannel bChan, TwitchSubGiftEventArguments e);
    
    public delegate void TwitchConnectedEvent(string args);
    public delegate void TwitchConnectionErrorEvent(string msg);
    public delegate void TwitchChannelChatClearedEvent(string channel);
    public delegate void TwitchChannelJoinLeaveEvent(string channel, string botname);
    public delegate void TwitchCommunitySubscriptionEvent(BotChannel bChan, string message);
    public delegate void TwitchNewFollowerEvent(BotChannel botChannel, UserEntry user);
    public delegate void TwitchResubscriberEvent(BotChannel botChannel, TwitchReSubArguments e);
    public delegate void TwitchNewsubscriberEvent(BotChannel botChannel, TwitchNewSubArguments e);
    public delegate void TwitchDisconnectedEvent();
    public delegate void TwitchMessageClearedEvent(BotChannel botChannel, OnMessageClearedArgs e);
    public delegate void TwitchMessageSentEvent(string channel, string message);
    public delegate void TwitchWhisperMessageEvent(string username, string message);
    public delegate void TwitchUserJoinLeaveEvent(BotChannel botChannel, UserEntry user);
    // Botwide type events
    public delegate void MessageReceivedEvent(BotWideMessageArguments args);
    public delegate void CommandReceivedEvent(BotWideCommandArguments args);

    // Below needs to be verified
    public delegate void TwitchChannelGoesLiveEvent(BotChannel bChan, int delay);
    public delegate void TwitchChannelGoesOfflineEvent(BotChannel bChan);
    public delegate void BanEvent(BanEventArguments e);
    public delegate void BitEvent(BitEventArguments e);
    public delegate void DiscordUserStartsStreamEvent(BotChannel bChan, UserEntry user, StreamingGame streaminfo);
    public delegate void BotChannelMergeEvent(BotChannel discordBotChannel, BotChannel twitchBotChannel);
    public delegate void RaidEvent(BotChannel bChan, RaidEventArguments e);
    public delegate void UnBanEvent(UnBanEventArguments e);
    public delegate void UserEntryMergeEvents(UserEntry discordUser, UserEntry twitchUser);
    public delegate void ViewerCountEvent(BotChannel bChan, int oldCount, int newCount);
    #endregion
}