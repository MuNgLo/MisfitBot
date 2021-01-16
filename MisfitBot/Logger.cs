using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.PubSub;
namespace MisfitBot_MKII
{
    /// <summary>
    /// The main logging class for the bot. The updateScreen part should get rewritten.
    /// </summary>
    public class JuansLog : ILogger, ILogger<TwitchLib.PubSub.TwitchPubSub>
    {
        private List<JuanMessage> _logLines = new List<JuanMessage>();
        //private int _maxMissingConnection = 60, _missedConnections = 0;
        /// <summary>
        /// Example of a logging handler. This can be re-used by addons that ask for a Func<LogMessage, Task>.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task LogThis(LogEntry entry)
        {
            await AddLogLine(entry);
        }
        public async void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            await AddLogLine(new LogEntry(LOGSEVERITY.INFO, "JUANSLOG", $"state:{state} e:{exception}"));
        }
        private async Task AddLogLine(LogEntry entry)
        {
            ConsoleColor defCol = Console.ForegroundColor;
            SetConsoleColour(entry.Severity);
            Console.WriteLine($"{DateTime.Now,-19} {entry.Message}");
            Console.ForegroundColor = defCol;
            if (Program.DiscordClient != null)
            {
                if (Program.DiscordClient.Status == Discord.UserStatus.Online)
                {
                    await Program.DiscordSayMessage(Program.LOGChannel, entry.Message);
                }
            }
        }

        /// <summary>
        /// Matches log severity to a consol colour.
        /// </summary>
        /// <param name="severity"></param>
        private void SetConsoleColour(LOGSEVERITY severity)
        {
            switch (severity)
            {
                case LOGSEVERITY.CRITICAL:
                case LOGSEVERITY.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LOGSEVERITY.WARNING:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LOGSEVERITY.INFO:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LOGSEVERITY.VERBOSE:
                case LOGSEVERITY.DEBUG:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
        }


        #region ILogger compliance
        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
