using System;
using MisfitBot_MKII.Statics;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;

namespace MisfitBot_MKII.MisfitBotEvents;

internal class EventCatcherTwitchServices
{
    internal async void OnStreamOnline(object sender, OnStreamOnlineArgs e) // verified Gets raised first time bot sees channel is Live
    {
        //await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "TwitchChannelWatcher", $"Stream Online {e.Channel} ({e.Stream.GameName}, \"{e.Stream.Title}\")"));
        BotChannel bChan = await Program.Channels.GetTwitchChannelByID(e.Channel);
        Program.BotEvents.RaiseOnTwitchChannelGoesLive(bChan, e.Stream);

    }
    internal void OnStreamUpdate(object sender, OnStreamUpdateArgs e) // verified Raised every check for live channels
    {
        //Core.LOG(new LogEntry(LOGSEVERITY.INFO, "TwitchChannelWatcher", $"Stream Update {e.Channel} ({e.Stream.GameName}, \"{e.Stream.Title}\" {e.Stream.ViewerCount})"));
    }
    internal async void OnStreamOffline(object sender, OnStreamOfflineArgs e)
    {
        await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "TwitchChannelWatcher", $"Stream offline {e.Channel} ({e.Stream.GameName}, \"{e.Stream.Title}\")"));
        BotChannel bChan = await Program.Channels.GetTwitchChannelByID(e.Channel);
        Program.BotEvents.RaiseOnTwitchChannelGoesOffline(bChan, e.Stream);
    }
    internal void OnChannelsSet(object sender, OnChannelsSetArgs e)
    {
        string channels = String.Join(',', e.Channels);
        Core.LOG(new LogEntry(LOGSEVERITY.INFO, "TwitchChannelWatcher", $"Channel set {channels})"));
    }
    internal void OnServiceStarted(object sender, OnServiceStartedArgs e) // verified
    {
        Core.LOG(new LogEntry(LOGSEVERITY.INFO, "TwitchChannelWatcher", $"ChannelWatcher Service started"));
    }
}// EOF CLASS