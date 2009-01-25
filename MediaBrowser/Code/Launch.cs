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
            Environment.CurrentDirectory = Helper.AppDataPath;
            SetupStylesMcml();
            SetupFontsMcml();

            Application app = new Application(new MyHistoryOrientedPageSession(), host);
            app.GoToMenu();
        }

        private void SetupFontsMcml()
        {
            try
            {
                string file = Path.Combine(Helper.AppDataPath, "Fonts_DoNotEdit.mcml");
                string custom = Path.Combine(Helper.AppDataPath, "CustomFonts.mcml");
                if (File.Exists(file))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
                if (File.Exists(custom))
                {
                    Trace.TraceInformation("Using custom fonts mcml");
                    File.Copy(custom, file);
                }
                else
                {
                    switch (Config.Instance.FontTheme)
                    {
                        case "Small":
                            File.WriteAllBytes(file, Resources.FontsSmall);
                            break;
                        case "Default":
                        default:
                            File.WriteAllBytes(file, Resources.FontsDefault);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error creating Fonts_DoNotEdit.mcml\n" + ex.ToString());
            }
        }

        private void SetupStylesMcml()
        {
            try
            {
                string file = Path.Combine(Helper.AppDataPath, "Styles_DoNotEdit.mcml");
                string custom = Path.Combine(Helper.AppDataPath, "CustomStyles.mcml");
                if (File.Exists(file))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
                if (File.Exists(custom))
                {
                    Trace.TraceInformation("Using custom styles mcml");
                    File.Copy(custom, file);
                }
                else
                {
                    switch (Config.Instance.Theme)
                    {
                        case "Black":
                            File.WriteAllBytes(file, Resources.StylesBlack);
                            break;
                        case "Default":
                        default:
                            File.WriteAllBytes(file, Resources.StylesDefault);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error creating Styles_DoNotEdit.mcml\n" + ex.ToString());
            }

        }
    }
}