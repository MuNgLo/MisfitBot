using System.Collections.Generic;
using System.Threading.Tasks;
using MisfitBot_MKII.MisfitBotEvents;
using TwitchLib.Api.Services;
using MisfitBot_MKII.Statics;

namespace MisfitBot_MKII.Twitch;

internal class TwitchChannelWatcher
{
    private LiveStreamMonitorService _channelMonitor;
    internal TwitchChannelWatcher(EventCatcherTwitchServices arg)
    {
        _channelMonitor = new LiveStreamMonitorService(Program.TwitchAPI, 30);
        Init(arg);
    }
    private async void Init(EventCatcherTwitchServices arg)
    {
        //List<string> ids = await FetchTwitchChannelIDs();
        List<string> ids = new(){ Secrets.UserID};
        if (ids.Count < 1)
        {
            await Core.LOG(new LogEntry(LOGSEVERITY.INFO, "TwitchChannelWatcher", $"No channels being watched."));
            return;
        }
        _channelMonitor.SetChannelsById(ids);
        _channelMonitor.OnStreamOnline += arg.OnStreamOnline;
        _channelMonitor.OnStreamOffline += arg.OnStreamOffline;
        _channelMonitor.OnStreamUpdate += arg.OnStreamUpdate;
        _channelMonitor.OnServiceStarted += arg.OnServiceStarted;
        _channelMonitor.OnChannelsSet += arg.OnChannelsSet;
        _channelMonitor.Start(); //Keep at the end!
        await Task.Delay(-1);
    }
    private async Task<List<string>> FetchTwitchChannelIDs()
    {
        List<string> chansToWatch = new List<string>();
        List<string> ids = new List<string>();
        foreach (BotChannel chan in await Program.Channels.GetChannels())
        {
            if (chan.TwitchChannelName != string.Empty)
            {
                chansToWatch.Add(chan.TwitchChannelName);
            }
        }
        if (chansToWatch.Count > 0)
        {
            TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse channelEntries = await Program.Users.GetUsersByTwitchUsernamesFromAPI(chansToWatch);
            if (channelEntries.Users.Length > 0)
            {
                foreach (TwitchLib.Api.Helix.Models.Users.GetUsers.User usr in channelEntries.Users)
                {
                    ids.Add(usr.Id);
                }
            }
        }
        return ids;
    }
}// EOF CLASS