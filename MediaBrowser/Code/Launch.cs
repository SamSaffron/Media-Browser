using System.Collections.Generic;
using Microsoft.MediaCenter.Hosting;
using Microsoft.MediaCenter;
using System.Diagnostics;
using System.IO;
using System;
using System.Threading;
using MediaBrowser.LibraryManagement;

namespace MediaBrowser
{
    public class MyAddIn : IAddInModule, IAddInEntryPoint
    {

        public void Initialize(Dictionary<string, object> appInfo, Dictionary<string, object> entryPointInfo)
        {
            
            
        }

        public void Uninitialize()
        {
        }

        public void Launch(AddInHost host)
        {
            //  uncomment to debug
#if DEBUG
            host.MediaCenterEnvironment.Dialog("Attach debugger and hit ok", "debug", DialogButtons.Ok, 100, true); 
#endif
            if (!Config.Initialize())
            {
                Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
                return; // there is a problem with the config and the user opt'd not to reset it to defaults
            }
            if (Config.Instance.EnableTraceLogging)
            {
                TextWriterTraceListener t = new TextWriterTraceListener(Path.Combine(Helper.AppConfigPath, "Log_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt"));
                t.TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ThreadId;
                Trace.AutoFlush = true;
                Trace.Listeners.Add(t);
            }

            Application app = new Application(new MyHistoryOrientedPageSession(), host);
            app.GoToMenu();
        }
    }
}