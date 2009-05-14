using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Library.Threading {

    public static class Async {

        public static Timer Every(int milliseconds, Action action) {
            Timer timer = new Timer(_ => action(), null, 0, milliseconds);
            return timer;
        }

        public static void Queue(Action action) {
            Queue(action, null);
        }

        public static void Queue(Action action, Action done) {

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try {
                    action();
                } 
                catch (ThreadAbortException) { /* dont report on this */ } 
                catch (Exception ex) {
                    Debug.Assert(false, "Async thread crashed! This must be fixed. " + ex.ToString());
                    Logger.ReportException("Async thread crashed! This must be fixed. ", ex);
                }
                if (done != null) done();
            });

        }
    }
}
