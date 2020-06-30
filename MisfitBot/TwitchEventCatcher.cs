using System;
using System.Threading.Tasks;
using Discord;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace MisfitBot_MKII
{
    internal class TwitchEventCatcher
    {
        internal TwitchEventCatcher(ITwitchClient client)
        {
            client.OnLog += OnLog;
            client.OnConnected += Program.BotEvents.RaiseOnTwitchConnected;
            client.OnDisconnected += OnDisconnected;
            client.OnMessageReceived += OnMessageReceived;
        }

        private async void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            UserEntry usr = await Program.Users.GetUserByTwitchID(e.ChatMessage.UserId);
            Program.BotEvents.RaiseOnMessageReceived(new BotWideMessageArguments(){
                source = MESSAGESOURCE.TWITCH,
                channel = e.ChatMessage.Channel,
                user = usr,
                message = e.ChatMessage.Message
            });
        }

        private async void OnLog(object sender, OnLogArgs e)
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, "Program", $"LOG:{e.Data}"));
        }

        private async void OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            await Core.LOG(new LogMessage(LogSeverity.Error, "TwitchService", $"Connection Error!! {e.Error.Message}."));
        }
        private async void OnConnected(object sender, OnConnectedArgs e) // TODO check if sender has something useful
        {
            await Core.LOG(new LogMessage(LogSeverity.Info, "TwitchService", "Twitch Connected"));
        }
        private async void OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            await Core.LOG(new LogMessage(LogSeverity.Error, "TwitchService", $"Disconnected from Twitch. Reconnecting... :: {e}"));
        }
    }
}