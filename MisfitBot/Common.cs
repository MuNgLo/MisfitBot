using Discord;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using MisfitBot_MKII.Statics;
using TwitchStream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;
using TwitchLib.Client.Enums;

namespace MisfitBot_MKII
{
    public enum MESSAGESOURCE { BOTH, DISCORD, TWITCH}
    public enum LOGSEVERITY {CRITICAL, ERROR, WARNING, INFO, VERBOSE, DEBUG, RESPONSE}
    public enum RESPONSEACTION {ERROR, ADDED, REMOVED, CLEARED}

    public delegate void CommandMethod(BotChannel bChan, BotWideCommandArguments args);
    public delegate void SubCommandMethod(BotChannel bChan, BotWideCommandArguments args);

    public struct TwitchStreamGoLiveEventArguments{
        public BotChannel bChan;
        public TwitchStream stream;
    }
    public struct TwitchStreamGoOfflineEventArguments{
        public BotChannel bChan;
        public TwitchStream stream;
    }
    public struct BotWideMessageArguments
    {
        public MESSAGESOURCE source;
        public string channel;
        public ulong guildID;
        public ulong channelID;
        public bool isBroadcaster;
        public bool isModerator;
        public bool canManageMessages;
        public UserEntry user;
        public string userDisplayName;
        public string message;
    }
    public struct BotWideCommandArguments
    {
        public MESSAGESOURCE source;
        public string channel;
        public ulong guildID;
        public ulong channelID;
        public ulong messageID;
        public bool isBroadcaster;
        public bool isModerator;
        public bool canManageMessages;
        public UserEntry user;
        public string userDisplayName;
        public string command;
        public string message;
        public List<string> arguments;
    }
    public class BotWideResponseArguments
    {
        public MESSAGESOURCE source;
        public string twitchChannel;
        public ulong discordChannel;
        public UserEntry user;
        public UserEntry victim;
        public string message;
        public bool parseMessage;
        public BotWideResponseArguments(BotWideMessageArguments args){
            source = args.source;
            user = args.user;
            parseMessage = false;
            twitchChannel = args.channel;
            discordChannel = args.channelID;
        }
        public BotWideResponseArguments(BotWideCommandArguments args){
            source = args.source;
            user = args.user;
            parseMessage = false;
            twitchChannel = args.channel;
            discordChannel = args.channelID;
        }
    }

public struct LogEntry
    {
        public LogEntry(LOGSEVERITY severity, string source, string message, Exception exception = null)
        {
            Severity = severity;
            Source = source;
            Message = message;
            Exception = exception;
        }
        public readonly LOGSEVERITY Severity { get; }
        public readonly string Source { get; }
        public readonly string Message { get; }
        public readonly Exception Exception { get; }
    }

    public struct GenericTimeStamp
    {
        public string stringID;
        public ulong ulongID;
        public int timestamp;
    }



    /// <summary>
    /// Default userValues class. Only contains a simple timestamp so far
    /// </summary>
    public class UserValues
    {
        public int _timestamp = 0;
        public UserValues(int time)
        {
            _timestamp = time;
        }
    }
    public class DBString
    {
        public readonly int _id;
        public readonly bool _inuse;
        public readonly string _topic;
        public readonly string _text;
        public DBString(int id, bool inuse, string topic, string text)
        {
            _id = id; _inuse = inuse; _topic = topic; _text = text;
        }
    }
    /// <summary>
    /// Bot wide event argument class
    /// </summary>
    public class BitEventArguments
    {
        public BotChannel bChan;
        public UserEntry user;
        public int timestamp;
        public int bitsGiven;
        public int bitsTotal;
        public string context;
        public string chatMessage;
        public BitEventArguments(BotChannel chan, UserEntry usr, int time, int bits, int total, string con, string chatM)
        {
            bChan = chan;user = usr;timestamp = time;bitsGiven = bits;bitsTotal = total;context = con;chatMessage = chatM;
        }
    }
    /// <summary>
    /// Bot wide event argument class
    /// </summary>
    public class HostedEventArguments
    {
        public string HostedChannel, HostChannel;
        bool AutoHost;
        int Viewers;
        public HostedEventArguments(string hostedChannel, string hostChannel, bool isAutoHost, int viewers)
        {
            HostedChannel = hostedChannel;
            HostChannel = hostChannel;
            AutoHost = isAutoHost;
            Viewers = viewers;
        }
    }
    /// <summary>
     /// BotWide event argument class
     /// </summary>
    public class RaidEventArguments
    {
        public string SourceChannel, TargetChannel;
        public int RaiderCount;
        public RaidEventArguments(string sourceChannel, string targetChannel, int raiderCount)
        {
            SourceChannel = sourceChannel;
            TargetChannel = targetChannel;
            RaiderCount = raiderCount;
        }
    }
    /// <summary>
    /// BotWide event argument class
    /// </summary>
    public class TwitchNewSubArguments
    {
        public int userID;
        public string username;
        public string userDisplayName;
        public string Channelname;
        public SubscriptionPlan subscriptionPlan;
        public string subscriptionPlanName;
        public int monthsTotal; 
        public int monthsStreak; 
        public TwitchNewSubArguments(TwitchLib.Client.Events.OnNewSubscriberArgs e){
            
            int.TryParse(e.Subscriber.UserId, out userID);
            username = e.Subscriber.Login;
            userDisplayName = e.Subscriber.DisplayName;

            Channelname = e.Channel;

            subscriptionPlan = e.Subscriber.MsgParamSubPlan;
            subscriptionPlanName = e.Subscriber.MsgParamSubPlanName;

            monthsTotal = e.Subscriber.MsgParamCumulativeMonths;
            monthsStreak = e.Subscriber.MsgParamStreakMonths;
        }
    }// EOF CLASS
    /// <summary>
    /// BotWide event argument class
    /// </summary>
    public class TwitchReSubArguments
    {
        public int userID;
        public string username;
        public string userDisplayName;

        public string Channelname;

        public SubscriptionPlan subscriptionPlan;
        public string subscriptionPlanName;
        public int monthsTotal; 
        public int monthsStreak; 

        public TwitchReSubArguments(TwitchLib.Client.Events.OnReSubscriberArgs e){
            
            int.TryParse(e.ReSubscriber.UserId, out userID);
            username = e.ReSubscriber.Login;
            userDisplayName = e.ReSubscriber.DisplayName;

            Channelname = e.Channel;

            subscriptionPlan = e.ReSubscriber.MsgParamSubPlan;
            subscriptionPlanName = e.ReSubscriber.MsgParamSubPlanName;


            monthsTotal = e.ReSubscriber.MsgParamCumulativeMonths;
            monthsStreak = e.ReSubscriber.MsgParamStreakMonths;
        }
    }// EOF CLASS
    /// <summary>
    /// BotWide event argument class
    /// </summary>
    public class TwitchSubGiftEventArguments
    {
        public int twitchUserID;
        public string username;
        public string userDisplayName;
        public int recipientUserID;
        public string recipientUsername;
        public string recipientDisplayName;
        public int twitchChannelID;
        public string twitchChannelname;
        public SubscriptionPlan subscriptionPlan;
        public string subscriptionPlanName;
        public int months; // This seems highly unreliable

        public TwitchSubGiftEventArguments(TwitchLib.Client.Events.OnGiftedSubscriptionArgs e){
            
            int.TryParse(e.GiftedSubscription.Id, out twitchUserID);
            userDisplayName = e.GiftedSubscription.DisplayName;
            username = e.GiftedSubscription.Login;

            int.TryParse(e.GiftedSubscription.MsgParamRecipientId, out recipientUserID);
            recipientUsername = e.GiftedSubscription.MsgParamRecipientUserName;
            recipientDisplayName = e.GiftedSubscription.MsgParamRecipientDisplayName;

            twitchChannelname = e.Channel;

            subscriptionPlan = (SubscriptionPlan)e.GiftedSubscription.MsgParamSubPlan;
            int.TryParse(e.GiftedSubscription.MsgParamMonths, out months);

            subscriptionPlanName = e.GiftedSubscription.MsgParamSubPlanName;
        }

        internal string LogString { get {return $"SubGift in {twitchChannelname} from {userDisplayName} to {recipientDisplayName}";} private set{} }
    }// EOF CLASS
    /// <summary>
    /// BotWide event argument class
    /// </summary>
    public class BanEventArguments
    {
        public BotChannel bChan;
        public UserEntry enforcingUser;
        public UserEntry bannedUser;
        public bool isDiscord;
        public int timestamp;
        public int duration;
        public string reason;
        public BanEventArguments(BotChannel chan, UserEntry mod, UserEntry vic, int time, int dur, string cause, bool isTwitch)
        {
            bChan = chan; enforcingUser = mod; bannedUser = vic; timestamp = time; duration = dur; reason = cause; isDiscord = !isTwitch;
        }
    }
    /// <summary>
    /// BotWide event argument class
    /// </summary>
    public class UnBanEventArguments
    {
        public BotChannel bChan;
        public UserEntry enforcingUser;
        public UserEntry bannedUser;
        public bool isDiscord;
        public int timestamp;
        public UnBanEventArguments(BotChannel chan, UserEntry mod, UserEntry vic, int time, bool isTwitch)
        {
            bChan = chan; enforcingUser = mod; bannedUser = vic; timestamp = time; isDiscord = !isTwitch;
        }
    }
    /// <summary>
    /// Inherit from this for any plugin setting. This contains all the common variable
    /// </summary>
    public class PluginSettingsBase
    {
        public bool _active = true;
        public int _defaultCoolDown = 30;
        public ulong _defaultDiscordChannel = 0;
        public string _defaultTwitchRoom = string.Empty;
    }
    /// <summary>
    /// The format we remember the log entries as.
    /// </summary>
    public struct JuanMessage
    {
        public LogSeverity Severity { get; }
        public string Source { get; }
        public string Message { get; }
        public string Timestamp { get; }
        public Exception Exception { get; }
        public JuanMessage(Discord.LogMessage msg, string timestamp)
        {
            Severity = msg.Severity; Source = msg.Source; Message = msg.Message; Exception = msg.Exception; Timestamp = timestamp;
        }
        public JuanMessage(LogSeverity severity, string source, string message, string timestamp, Exception exception = null)
        {
            Severity = severity; Source = source; Message = message; Exception = exception; Timestamp = timestamp;
        }
        public override string ToString()
        {
            if (Exception == null)
            {
                return $"{Timestamp} | {Source}: {Message}";
            }
            else
            {
                return $"{Timestamp} | {Source}: {Message} {Exception}";
            }
        }
    }

    public struct StringFormatterArguments
    {
        public string message;
        public string user;
        public string targetUser;
        public string twitchChannel;
    }

    public class KSLogger : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
            //throw new NotImplementedException();
        }

        public ILogger CreateLogger(string categoryName)
        {
            //return new JuansLog();
            return Core.LOGGER;
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }

    public class DiscordReactionArgument
    {
        public readonly ulong ChannelID;
        public readonly ulong MessageID;
        public readonly RESPONSEACTION ActionTaken;
        public readonly string Emote;
        public DiscordReactionArgument(ulong channelID, ulong messageID, RESPONSEACTION actionTaken){
            ChannelID = channelID; MessageID=messageID; ActionTaken= actionTaken;
        }
        public DiscordReactionArgument(ulong channelID, ulong messageID, RESPONSEACTION actionTaken, string emote){
            ChannelID = channelID; MessageID=messageID; ActionTaken= actionTaken; Emote=emote;
        }
    }

    public class DiscordChannelMessage{
        public readonly ulong MessageID;
        public readonly ulong ChannelID;
        public readonly DiscordReactions Reactions;

        public DiscordChannelMessage(IMessage message){
            MessageID = message.Id;
            ChannelID = message.Channel.Id;
            Reactions = new DiscordReactions(message.Reactions);
        }
    }

    public class DiscordReactions{
        public Dictionary<string, DiscordReaction> ReactionMetaData;
        public DiscordReactions(IReadOnlyDictionary<IEmote, ReactionMetadata> reactions){
            ReactionMetaData = new Dictionary<string, DiscordReaction>();
            foreach(IEmote emote in reactions.Keys){
                ReactionMetaData[emote.Name] = new DiscordReaction(){
                    HasReacted = reactions[emote].IsMe, Count = reactions[emote].ReactionCount
                };
            }
        }
    }

    public struct DiscordReaction{
        public bool HasReacted;
        public int Count;
    }


}// EO Namespace
