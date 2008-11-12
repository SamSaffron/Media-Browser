using System.Collections.Generic;
using Microsoft.MediaCenter.Hosting;
using Microsoft.MediaCenter;
using System.Diagnostics;
using SamSoft.VideoBrowser.LibraryManagement;
using System.IO;
using System;
using System.Threading;

namespace SamSoft.VideoBrowser
{
    public class MyAddIn : IAddInModule, IAddInEntryPoint
    {

        public void Initialize(Dictionary<string, object> appInfo, Dictionary<string, object> entryPointInfo)
        {
            if (Config.Instance.EnableTraceLogging)
            {
                TextWriterTraceListener t = new TextWriterTraceListener(Path.Combine(Helper.AppConfigPath, "Log_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt"));
                Trace.AutoFlush = true;
                Trace.Listeners.Add(t);
            }
        }

        public void Uninitialize()
        {
        }

        public void Launch(AddInHost host)
        {
        //  uncomment to debug
           //host.MediaCenterEnvironment.Dialog("debug", "debug", DialogButtons.Ok, 100, true); 
            Application app = new Application(new MyHistoryOrientedPageSession(), host);
            app.GoToMenu();
        }
    }
}