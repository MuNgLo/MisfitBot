using Discord;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MisfitBot_MKII.Services;
using TwitchLib.PubSub;
namespace MisfitBot_MKII
{
    /// <summary>
    /// The main logging class for the bot. The updateScreen part should get rewritten.
    /// </summary>
    public class JuansLog : ILogger, ILogger<TwitchLib.PubSub.TwitchPubSub>
    {
        private List<JuanMessage> _logLines = new List<JuanMessage>();
        private int _maxMissingConnection = 60, _missedConnections = 0;
        /// <summary>
        /// Example of a logging handler. This can be re-used by addons that ask for a Func<LogMessage, Task>.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task LogThis(LogMessage message)
        {
            await AddLogLine(message);
        }
        public async void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            await AddLogLine(new LogMessage(LogSeverity.Info, "JUANSLOG", $"state:{state} e:{exception}"));
        }
        private async Task AddLogLine(LogMessage message)
        {
            /*await Task.Run(() =>
            {
                lock (_logLines)
                {
                    _logLines.Add(new JuanMessage(message, $"{DateTime.Now,-19}"));
                }
            });*/
            await Task.Run(() =>
            {
                Console.WriteLine($"{DateTime.Now,-19} {message}");
            });
        }

        /// <summary>
        /// Matches log severity to a consol colour.
        /// </summary>
        /// <param name="severity"></param>
        private void SetConsoleColour(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
        }
        /// <summary>
        /// Checks and updates indicators for the Discord connection.
        /// </summary>
        private void DiscordConnectionStatus()
        {
            int top = Console.CursorTop;
            int left = Console.CursorLeft;

            Console.SetCursorPosition(10, 1);
            switch (Program.DiscordClient.ConnectionState)
            {
                case ConnectionState.Connected:
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    _missedConnections--;
                    break;
                case ConnectionState.Connecting:
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    _missedConnections++;
                    break;
                case ConnectionState.Disconnected:
                    Console.BackgroundColor = ConsoleColor.Red;
                    _missedConnections++;
                    break;
                case ConnectionState.Disconnecting:
                    Console.BackgroundColor = ConsoleColor.DarkYellow;
                    _missedConnections++;
                    break;
                default:
                    Core.LOG(new LogMessage(LogSeverity.Warning, "Logger", $"Discord unrecognized status ({Program.DiscordClient.ConnectionState})"));
                    _missedConnections++;
                    break;
            }

            if (_missedConnections >= _maxMissingConnection)
            {
                _missedConnections = 0;
                Core.LOG(new LogMessage(LogSeverity.Warning, "Logger", "Discord connection check failed. Reconnecting."));
                //Program.DiscordReconnect(); // TODO FIX THIS
            }

            Console.Write("*");
            Console.SetCursorPosition(top, left);
            Console.ResetColor(); Console.BackgroundColor = ConsoleColor.Black;
        }
        /// <summary>
        /// Checks and updates indicators for the Twitch connection.
        /// </summary>
        public void TwitchConnectionStatus()
        {
            int top = Console.CursorTop;
            int left = Console.CursorLeft;

            Console.SetCursorPosition(20, 1);

            if (Program.TwitchClient.IsConnected)
            {
                Console.BackgroundColor = ConsoleColor.Green;
            }
            else
            {
                Console.BackgroundColor = ConsoleColor.Red;
                _missedConnections++;
                if (_missedConnections >= _maxMissingConnection)
                {
                    _missedConnections = 0;
                    Program.TwitchClient.Reconnect();
                    Core.LOG(new LogMessage(LogSeverity.Warning, "Logger", "Connectionstatus failed. Reconnecting."));
                }
            }
            Console.Write("*");
            Console.SetCursorPosition(top, left);
            Console.ResetColor(); Console.BackgroundColor = ConsoleColor.Black;
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
