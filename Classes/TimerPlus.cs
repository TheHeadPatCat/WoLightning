using System;

namespace WoLightning
{
    public class TimerPlus : System.Timers.Timer
    {
        private DateTime m_dueTime;

        public TimerPlus() : base() => Elapsed += ElapsedAction;

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
