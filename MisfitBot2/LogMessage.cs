using Discord;
using System;
using System.Text;

namespace MisfitBot2
{
    public struct JuanMessage
    {
        public JuanMessage(Discord.LogMessage msg, string timestamp)
        {
            Severity = msg.Severity; Source = msg.Source;Message = msg.Message; Exception = msg.Exception;Timestamp = timestamp;
        }
        public JuanMessage(LogSeverity severity, string source, string message, string timestamp, Exception exception = null)
        {
            Severity = severity; Source = source; Message = message; Exception = exception; Timestamp = timestamp;
        }
        public LogSeverity Severity { get; }
        public string Source { get; }
        public string Message { get; }
        public string Timestamp { get; }
        public Exception Exception { get; }

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
}
