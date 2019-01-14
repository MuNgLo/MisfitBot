using System;
using System.Collections.Generic;
using System.Text;

namespace MisfitBot2.Plugins.Betting
{
    public class IndividualBet
    {
        public UserEntry _user { get; private set; }
        public string _optionPick { get; private set; }
        public int _amount = 0;
        public int _timestamp = -1;
        public int _winnings = 0;
        public int _totalBet = 0;
        public int _brPlacementDistance = 101;

        public IndividualBet(UserEntry user, string result, int amount)
        {
            _user = user;
            _optionPick = result;
            _amount = amount;
            _timestamp = Core.CurrentTime;
        }
    }
}
