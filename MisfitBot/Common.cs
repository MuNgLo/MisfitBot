using Discord;
using Microsoft.Extensions.Logging;
using System;
using TwitchLib.PubSub.Enums;

namespace MisfitBot_MKII
{
    public enum TWSUBCONTEXT {  UNKNOWN, SUB, RESUB, GIFTSUB, ANONSUBGIFT }
    public enum MESSAGESOURCE { DISCORD, TWITCH}

    public struct BotWideMessageArguments
    {
        public MESSAGESOURCE source;
        public string channel;
        public UserEntry user;
        public string message;
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
    public class HostEventArguments
    {
        public string Hostchannel, Moderator;
        public HostEventArguments(string hostchannel, string moderator)
        {
            Hostchannel = hostchannel;
            Moderator = moderator;
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
    public class TwitchSubEventArguments
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
        public int months; // This seems highly unreliable

        public TWSUBCONTEXT subContext;
        public string subMessage;

        public TwitchSubEventArguments(TwitchLib.PubSub.Events.OnChannelSubscriptionArgs args)
        {
            switch (args.Subscription.Context)
            {
                case "sub":
                    MakeSub(args, TWSUBCONTEXT.SUB);
                    break;
                case "resub":
                    MakeSub(args, TWSUBCONTEXT.RESUB);
                    break;
                case "subgift":
                    if(args.Subscription.Username == "ananonymousgifter") { return; } // Twitsub sends both this and anonsubgift so ignore one
                    MakeSub(args, TWSUBCONTEXT.GIFTSUB);
                    break;
                case "anonsubgift":
                    MakeSub(args, TWSUBCONTEXT.ANONSUBGIFT);
                    break;
                case "unknown":
                    Core.LOG(new LogMessage(LogSeverity.Error, $"TwitchEventArgument Constructor", $"Sub context is \"unknown\"! ({args.Subscription.Context.ToString()})"));
                    JsonDumper.DumpObjectToJson(args, "TwitchEventArgument");
                    break;
                default:
                    Core.LOG(new LogMessage(LogSeverity.Error, $"TwitchEventArgument", $"Sub context not recognized! ({args.Subscription.Context.ToString()})"));
                    subContext = TWSUBCONTEXT.UNKNOWN;
                    break;
            }
        }

        private void MakeSub(TwitchLib.PubSub.Events.OnChannelSubscriptionArgs e, TWSUBCONTEXT context)
        {
            int.TryParse(e.Subscription.UserId, out twitchUserID);
            username = e.Subscription.Username;
            userDisplayname = e.Subscription.DisplayName;

            int.TryParse(e.Subscription.RecipientId, out recipientUserID);
            recipientUsername = e.Subscription.RecipientName;
            recipientDisplayname = e.Subscription.RecipientDisplayName;

            int.TryParse(e.Subscription.ChannelId, out twitchChannelID);
            twitchChannelname = e.Subscription.ChannelName;

            subscriptionplan = e.Subscription.SubscriptionPlan;
            months = e.Subscription.Months != null ? (int)e.Subscription.Months : 0;

            subContext = context;

            subMessage = e.Subscription.SubMessage.Message;
        }
    }
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
}// EO Namespace


namespace MisfitBot2.Services
{
    interface IService
    {
        void OnSecondTick(int seconds);
        void OnMinuteTick(int minutes);
        void OnUserEntryMergeEvent(MisfitBot_MKII.UserEntry discordUser, MisfitBot_MKII.UserEntry twitchUser);
        void OnBotChannelEntryMergeEvent(MisfitBot_MKII.BotChannel discordGuild, MisfitBot_MKII.BotChannel twitchChannel);
    }
}


namespace MisfitBot_MKII{
    internal class MainConfig {
        public bool UseDiscord;
        public bool UseTwitch;
        public char CMDCharacter;
        public string DiscordToken;
        public string TwitchClientID;
        public string TwitchToken;
        public string TwitchUser;
    }
}