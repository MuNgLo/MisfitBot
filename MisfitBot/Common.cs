using Discord;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using TwitchLib.PubSub.Enums;

namespace MisfitBot_MKII
{
    public enum MESSAGESOURCE { DISCORD, TWITCH}
    public enum LOGSEVERITY {CRITICAL, ERROR, WARNING, INFO, VERBOSE, DEBUG}

    public struct BotWideMessageArguments
    {
        public MESSAGESOURCE source;
        public string channel;
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
            if(source == MESSAGESOURCE.TWITCH){twitchChannel = args.channel;}
            if(source == MESSAGESOURCE.DISCORD){discordChannel = Core.StringToUlong(args.channel);}
        }
        public BotWideResponseArguments(BotWideCommandArguments args){
            source = args.source;
            user = args.user;
            parseMessage = false;
            if(source == MESSAGESOURCE.TWITCH){twitchChannel = args.channel;}
            if(source == MESSAGESOURCE.DISCORD){discordChannel = Core.StringToUlong(args.channel);}
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
    /// Default uservalues class. Only contains a simple timestamp so far
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
    /// Botwide event argument class
    /// </summary>
    public class BitEventArguments
    {
        public BotChannel bChan;
        public UserEntry user;
        public int timestamp;
        public int bitsGiven;
        public int bitsTotal;
        public string context;
        public string chatmessage;
        public BitEventArguments(BotChannel chan, UserEntry usr, int time, int bits, int total, string con, string chatmsg)
        {
            bChan = chan;user = usr;timestamp = time;bitsGiven = bits;bitsTotal = total;context = con;chatmessage = chatmsg;
        }
    }
    /// <summary>
    /// Botwide event argument class
    /// </summary>
    public class HostedEventArguments
    {
        public string HostedChannel, HostChannel;
        bool AutoHost;
        int Viewers;
        public HostedEventArguments(string hostedChannel, string hostChannel, bool isautohost, int viewers)
        {
            HostedChannel = hostedChannel;
            HostChannel = hostChannel;
            AutoHost = isautohost;
            Viewers = viewers;
        }
    }
    /// <summary>
     /// Botwide event argument class
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
    /// Botwide event argument class
    /// </summary>
    public class TwitchNewSubArguments
    {
        public int userID;
        public string username;
        public string userDisplayname;

        public string Channelname;

        public SubscriptionPlan subscriptionplan;
        public string subscriptionplanName;
        public int monthsTotal; 
        public int monthsStreak; 

        public TwitchNewSubArguments(TwitchLib.Client.Events.OnNewSubscriberArgs e){
            
            int.TryParse(e.Subscriber.UserId, out userID);
            username = e.Subscriber.Login;
            userDisplayname = e.Subscriber.DisplayName;

            Channelname = e.Channel;

            subscriptionplan = (SubscriptionPlan)e.Subscriber.SubscriptionPlan;
            subscriptionplanName = e.Subscriber.SubscriptionPlanName;

            int.TryParse(e.Subscriber.MsgParamCumulativeMonths, out monthsTotal);
            int.TryParse(e.Subscriber.MsgParamStreakMonths, out monthsStreak);
        }
    }// EOF CLASS
    /// <summary>
    /// Botwide event argument class
    /// </summary>
    public class TwitchReSubArguments
    {
        public int userID;
        public string username;
        public string userDisplayname;

        public string Channelname;

        public SubscriptionPlan subscriptionplan;
        public string subscriptionplanName;
        public int monthsTotal; 
        public int monthsStreak; 

        public TwitchReSubArguments(TwitchLib.Client.Events.OnReSubscriberArgs e){
            
            int.TryParse(e.ReSubscriber.UserId, out userID);
            username = e.ReSubscriber.Login;
            userDisplayname = e.ReSubscriber.DisplayName;

            Channelname = e.Channel;

            subscriptionplan = (SubscriptionPlan)e.ReSubscriber.SubscriptionPlan;
            subscriptionplanName = e.ReSubscriber.SubscriptionPlanName;

            int.TryParse(e.ReSubscriber.MsgParamCumulativeMonths, out monthsTotal);
            int.TryParse(e.ReSubscriber.MsgParamStreakMonths, out monthsStreak);
        }
    }// EOF CLASS
    /// <summary>
    /// Botwide event argument class
    /// </summary>
    public class TwitchSubGiftEventArguments
    {
        public int twitchUserID;
        public string username;
        public string userDisplayname;

        public int recipientUserID;
        public string recipientUsername;
        public string recipientDisplayname;

        public int twitchChannelID;
        public string twitchChannelname;

        public SubscriptionPlan subscriptionplan;
        public string subscriptionplanName;
        public int months; // This seems highly unreliable

        public TwitchSubGiftEventArguments(TwitchLib.Client.Events.OnGiftedSubscriptionArgs e){
            
            int.TryParse(e.GiftedSubscription.Id, out twitchUserID);
            userDisplayname = e.GiftedSubscription.DisplayName;
            username = e.GiftedSubscription.Login;

            int.TryParse(e.GiftedSubscription.MsgParamRecipientId, out recipientUserID);
            recipientUsername = e.GiftedSubscription.MsgParamRecipientUserName;
            recipientDisplayname = e.GiftedSubscription.MsgParamRecipientDisplayName;

            twitchChannelname = e.Channel;

            subscriptionplan = (SubscriptionPlan)e.GiftedSubscription.MsgParamSubPlan;
            int.TryParse(e.GiftedSubscription.MsgParamMonths, out months);

            subscriptionplanName = e.GiftedSubscription.MsgParamSubPlanName;
        }

        internal string LogString { get {return $"SubGift in {twitchChannelname} from {userDisplayname} to {recipientDisplayname}";} private set{} }
    }// EOF CLASS
    /// <summary>
    /// Botwide event argument class
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
    /// Botwide event argument class
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
    /// Inherit from this for any pluginsetting. This contains all the common variable
    /// </summary>
    public class PluginSettingsBase
    {
        public bool _active = true;
        public int _defaultCooldown = 30;
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



    public interface IService
    {
        void OnSecondTick(int seconds);
        void OnMinuteTick(int minutes);
        void OnUserEntryMergeEvent(MisfitBot_MKII.UserEntry discordUser, MisfitBot_MKII.UserEntry twitchUser);
        void OnBotChannelEntryMergeEvent(MisfitBot_MKII.BotChannel discordGuild, MisfitBot_MKII.BotChannel twitchChannel);
    }




    internal class MainConfig {
        public bool UseDiscord;
        public bool UseTwitch;
        public char CMDCharacter;
        public string DiscordToken;
        public string TwitchClientID;
        public string TwitchToken;
        public string TwitchUser;
    }
}// EO Namespace
