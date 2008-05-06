using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace SamSoft.VideoBrowser.Util
{
    public class Profiler : IDisposable
    {
#if DEBUG

        long startTime;
        string description;

        public Profiler(string description)
        {
            startTime = DateTime.Now.Ticks;
            this.description = description;
        }
#endif 

        #region IDisposable Members

        public void Dispose()
        {
#if DEBUG
            Trace.WriteLine(string.Format("{0} : {1}", description, DateTime.Now.Ticks - startTime));
#endif 
        }

        #endregion
    }
}
