using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace MediaBrowser.Library.Threading {
    public static class Async {
        public static void Queue(Action action) {
            ThreadPool.QueueUserWorkItem(_ => {
                try {
                    action();
                } catch (Exception ex) {
                    Debug.Assert(false, "Async thread crashed! This must be fixed. " + ex.ToString());
                    Application.Logger.ReportException("Async thread crashed! This must be fixed. ", ex);
                }
            });
  
        }
    }
}
