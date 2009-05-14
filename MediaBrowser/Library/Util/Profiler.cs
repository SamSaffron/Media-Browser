using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Util
{
    class Profiler : IDisposable
    {
        string caller;
        string name;
        Stopwatch stopwatch;  

        public Profiler(string name)
        {
            this.name = name;
            StackTrace st = new StackTrace();
            caller = st.GetFrame(1).GetMethod().Name;
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }
        #region IDisposable Members

        public void Dispose()
        {
            stopwatch.Stop();
            string message = string.Format("{1} took {2} milliseconds.",
                caller, name, stopwatch.ElapsedMilliseconds.ToString());
            Logger.ReportInfo( message);
            Application.CurrentInstance.Information.AddInformation(new InfomationItem(message, false)); 
        }

        #endregion
    }
}
