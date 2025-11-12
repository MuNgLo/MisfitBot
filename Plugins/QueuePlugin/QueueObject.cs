using System;
using System.Collections.Generic;
using System.Text;
using MisfitBot_MKII;
using MisfitBot_MKII.Components;
using MisfitBot_MKII.Statics;

namespace QueuePlugin
{
    class QueueObject
    {
        private string _twitchChannelName;
        private string _twitchChannelID;
        private int _tsAntiSpam;
        private int _tsLastAnnounce;
        private int _msgSinceLastAnnounce = 0;
        private int _announceTimer;
        private List<string> _queuedUsers; // uses Twitch Displaynames

        public int Count { get => _queuedUsers.Count; }

        public QueueObject(BotChannel bChan, int announceTimer)
        {
            _announceTimer = announceTimer;
            _queuedUsers = new List<string>();
            _tsAntiSpam = Core.CurrentTime;
            _twitchChannelName = bChan.TwitchChannelName;
            _twitchChannelID = bChan.TwitchChannelID;
        }

        internal bool CheckAnnounce()
        {
            return Core.CurrentTime > _tsLastAnnounce + _announceTimer;
        }

        internal string Announce(Char CMC)
        {
            string tail = string.Empty;
            if(_msgSinceLastAnnounce < 10){return null;}
            _msgSinceLastAnnounce = 0;
            if(_queuedUsers.Count > 0)
            {
                tail = $" Next one up is {_queuedUsers[0]}.";
            }
            _tsLastAnnounce = Core.CurrentTime;
            return $"Queue Up with >{CMC}aq< There is now {_queuedUsers.Count} in queue.{tail}";
        }

        internal void Reset()
        {
            _msgSinceLastAnnounce = 0;
            _queuedUsers = new List<string>();
            _tsLastAnnounce = Core.CurrentTime - _announceTimer + 10;
            Program.TwitchSayMessage(_twitchChannelName, "Queue reset!");
        }

        internal void AddUser(string twitchDisplayName)
        {
            if(!_queuedUsers.Exists(p=>p == twitchDisplayName))
            {
                _queuedUsers.Add(twitchDisplayName);
                Program.TwitchSayMessage(_twitchChannelName,  $"{twitchDisplayName} queued up on spot {_queuedUsers.Count}.");
            }
            else
            {
                if(Core.CurrentTime > _tsAntiSpam + 30)
                {
                    Program.TwitchSayMessage(_twitchChannelName, $"{twitchDisplayName} is already queued up on spot {_queuedUsers.Count}.");
                    _tsAntiSpam = Core.CurrentTime;
                }
            }
        }

        internal bool HasUser(string twitchDisplayName)
        {
            return _queuedUsers.Exists(p=>p == twitchDisplayName);
        }

        internal void RemoveUser(string twitchDisplayName)
        {
            if(!_queuedUsers.Exists(p=>p == twitchDisplayName))
            {
                return;
            }
            else
            {
                _queuedUsers.RemoveAll(p=>p == twitchDisplayName);
            }
        }

        internal string PullnextInQueue()
        {
            if(_queuedUsers.Count < 1)
            {
                return null;
            }
            string pickedUser = _queuedUsers[0];
            _queuedUsers.RemoveAt(0);
            return pickedUser;
        }

        internal void AddMessage()
        {
            _msgSinceLastAnnounce++;
        }

        public override string ToString()
        {
            return String.Join(", ", _queuedUsers);
        }
    }
}
