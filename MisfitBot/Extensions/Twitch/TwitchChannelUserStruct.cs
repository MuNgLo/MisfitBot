using System;
using MisfitBot_MKII.Statics;

namespace MisfitBot_MKII.Twitch;

[Obsolete("Seems to be unused?")]
public struct TwitchChannelUser
{
    public string _twitchUsername;
    public string _twitchDisplayname;
    public int _lastseen;
    public TwitchChannelUser(string username, string displayname)
    {
        _twitchUsername = username;
        _twitchDisplayname = displayname;
        _lastseen = Core.CurrentTime;
    }
}// EOF STRUCT