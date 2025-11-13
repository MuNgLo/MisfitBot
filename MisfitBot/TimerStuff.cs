using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MisfitBot_MKII
{
    public delegate void MinuteTick(int minute);
    /// <summary>
    /// This class creates and fires events on every second and minute.
    /// </summary>
    public class TimerStuff
    {
        static public event EventHandler<int> OnSecondTick;
        static private System.Threading.Timer seconds;
        private static int _secondsFromStart = 0;
        static public int Uptime { get { return _secondsFromStart; } private set { } }
        static public MinuteTick OnMinuteTick;

        public TimerStuff()
        {
            // Create the timer callback delegate.
            System.Threading.TimerCallback cb = new System.Threading.TimerCallback(ProcessTimerEvent);

            // Create the object for the timer.
            ClsTime time = new ClsTime();
            // Create the timer. It is autostart, so creating the timer will start it.
            seconds = new System.Threading.Timer(cb, time, 500, 1000);
        }

        private void ProcessTimerEvent(object obj)
        {
            _secondsFromStart++;
            if(OnSecondTick is not null)
            {
                OnSecondTick?.Invoke(this, _secondsFromStart);
            }
            if(_secondsFromStart % 60 == 0)
            {
                OnMinuteTick?.Invoke(_secondsFromStart / 60);
            }
        }
        /// <summary>
        /// Define a class to use as the object argument for the timer.
        /// </summary>
        class ClsTime
        {
            public string GetTimeString()
            {
                string str = DateTime.Now.ToString();
                int index = str.IndexOf(" ");
                return (str.Substring(index + 1));
            }
        }
    }
}
