using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Plugins.Queue
{
    class Queue
    {
        private string _twitchChannelName;
        private string _twitchChannelID;
        private int _tsAntiSpam;
        private int _tsLastAnnounce;
        private int _msgSinceLastAnnounce = 0;
        private int _announceTimer;
        private List<string> _queuedUsers; // uses Twitch Displaynames

        public Queue(BotChannel bChan, int announceTimer)
        {
            _announceTimer = announceTimer;
            _queuedUsers = new List<string>();
            _tsAntiSpam = Core.CurrentTime;
            _twitchChannelName = bChan.TwitchChannelName;
            _twitchChannelID = bChan.TwitchChannelID;
        }

        internal void CheckAnnounce()
        {
            if(Core.CurrentTime > _tsLastAnnounce + _announceTimer)
            {
                Announce();
            }
        }

        private void Announce()
        {
            string tail = string.Empty;
            if(_msgSinceLastAnnounce < 10){return;}
            _msgSinceLastAnnounce = 0;
            if(_queuedUsers.Count > 0)
            {
                tail = $" Next one up is {_queuedUsers[0]}.";
            }

            Core.Twitch._client.SendMessage(_twitchChannelName, $"Queue Up with >{Core._commandCharacter}q< There is now {_queuedUsers.Count} in queue.{tail}");
            _tsLastAnnounce = Core.CurrentTime;
        }

        internal void Reset()
        {
            _msgSinceLastAnnounce = 0;
            _queuedUsers = new List<string>();
            _tsLastAnnounce = Core.CurrentTime - _announceTimer + 10;
            Core.Twitch._client.SendMessage(_twitchChannelName, "Queue reset!");
        }

        internal void AddUser(string twitchDisplayName)
        {
            if(!_queuedUsers.Exists(p=>p == twitchDisplayName))
            {
                _queuedUsers.Add(twitchDisplayName);
                Core.Twitch._client.SendMessage(_twitchChannelName,  $"{twitchDisplayName} queued up on spot {_queuedUsers.Count}.");
            }
            else
            {
                if(Core.CurrentTime > _tsAntiSpam + 30)
                {
                    Core.Twitch._client.SendMessage(_twitchChannelName, $"{twitchDisplayName} is already queued up on spot {_queuedUsers.Count}.");
                    _tsAntiSpam = Core.CurrentTime;
                }
            }
        }

        internal void PullnextInQueue()
        {
            if(_queuedUsers.Count < 1)
            {
                Core.Twitch._client.SendMessage(_twitchChannelName, $"There is nobody in queue.");
                return;
            }
            string pickedUser = _queuedUsers[0];
            _queuedUsers.RemoveAt(0);
            Core.Twitch._client.SendMessage(_twitchChannelName, $"Calling {pickedUser} Time to step up.");

        }

        internal void AddMessage()
        {
            _msgSinceLastAnnounce++;
        }
    }
}
