using System;
using Microsoft.MediaCenter.UI;

namespace MediaBrowser
{
    public class Clock : ModelItem
    {
        private string _time = String.Empty;
        private Timer _timer;

        public Clock()
        {
            _timer = new Timer(this);
            _timer.Interval = 10000;
            _timer.Tick += delegate { RefreshTime(); };
            _timer.Enabled = true;

            RefreshTime();
        }

        // Current time. 
        public string Time
        {
            get { return _time; }
            set
            {
                if (_time != value)
                {
                    _time = value;
                    FirePropertyChanged("Time");
                }
            }
        }

        // Try to update the time.
        private void RefreshTime()
        {
            Time = DateTime.Now.ToShortTimeString();
        }
    }

    public class Scroller : ModelItem
    {
        public Scroller()
        {
        }

        public Size ComputeSize(Size size, Single currentPage, Single totalPages)
        {
            int height = size.Height;
            Single temp = currentPage / (totalPages );
            if (temp > 1)
                temp = 1;
            if (temp < 0)
                temp = 0;
            size.Height = (int)((height * temp) - 1);
            return size;
        }

    }
}

