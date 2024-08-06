using System;
using System.Collections.Generic;
using System.Timers;

namespace WoLightning
{
    public class TimerPlus : System.Timers.Timer
    {
        private DateTime m_dueTime;

        public TimerPlus() : base() => Elapsed += ElapsedAction;
        private readonly List<Action> subscribers = new();

        protected new void Dispose()
        {
            Elapsed -= ElapsedAction;
            base.Dispose();
        }

        public double TimeLeft => (m_dueTime - DateTime.Now).TotalMilliseconds;
        public new void Start()
        {
            m_dueTime = DateTime.Now.AddMilliseconds(Interval);
            base.Start();
        }



        // todo - finish
        public void addSubscriber(Action sub, ElapsedEventHandler del)
        {
            if (subscribers.Contains(sub)) return;
            subscribers.Add(sub);
            Elapsed += del;
        }

        public void removeSubscriber(Action sub, ElapsedEventHandler del)
        {
            if (!subscribers.Contains(sub)) return;
            subscribers.Remove(sub);
            Elapsed -= del;
        }

        private void ElapsedAction(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (AutoReset)
                m_dueTime = DateTime.Now.AddMilliseconds(Interval);
        }

        public void Refresh()
        {
            m_dueTime = DateTime.Now.AddMilliseconds(Interval);
        }

    }
}
