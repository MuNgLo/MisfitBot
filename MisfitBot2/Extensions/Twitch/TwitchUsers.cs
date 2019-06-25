using System.Collections;
using System.Collections.Generic;
//using TwitchLib.Events.Client;

namespace MisfitBot2.Twitch
{
    /// <summary>
    /// Keeps a list of users per channel that we know about. Each user has a timestamp of lastseen.
    /// </summary>
    public class TwitchUsers
    {
        private Dictionary<string, List<TwitchChannelUser>> _users = new Dictionary<string, List<TwitchChannelUser>>();
        /// <summary>
        /// Add new user or updates the timestamp of existing user
        /// </summary>
        /// <param name="userName"></param>
        public void TouchUser(string channelName, string userName)
        {
            if (!_users.ContainsKey(channelName))
            {
                _users[channelName] = new List<TwitchChannelUser>();
            }
            TwitchChannelUser user = new TwitchChannelUser(userName);
            if (_users[channelName].Exists(p => p._twitchUsername == userName))
            {
                _users[channelName][_users[channelName].FindIndex(p => p._twitchUsername == userName)] = user;
            }
            else
            {
                _users[channelName].Add(user);
            }
        }
        /// <summary>
        /// Returns a list of twitch usernames for users currently in channel.
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public List<string> GetUsersInChannel(string channelName)
        {
            if (!_users.ContainsKey(channelName))
            {
                _users[channelName] = new List<TwitchChannelUser>();
            }
            List<string> result = new List<string>();
            foreach(TwitchChannelUser user in _users[channelName])
            {
                result.Add(user._twitchUsername);
            }
            return result;
        }
        public string GetRandomUserInChannel(string channelName)
        {
            if (!_users.ContainsKey(channelName))
            {
                _users[channelName] = new List<TwitchChannelUser>();
                return null;
            }
            List<string> result = new List<string>();
            foreach (TwitchChannelUser user in _users[channelName])
            {
                result.Add(user._twitchUsername);
            }
            System.Random rng = new System.Random();
            return result[rng.Next(0,result.Count)];
        }

    }
    struct TwitchChannelUser
    {
        public string _twitchUsername;
        public int _lastseen;
        public TwitchChannelUser(string username)
        {
            _twitchUsername = username;
            _lastseen = Core.CurrentTime;
        }
    }
}
