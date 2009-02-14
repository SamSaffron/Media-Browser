using System.Collections.Generic;
using Microsoft.MediaCenter.Hosting;
using Microsoft.MediaCenter;
using System.Diagnostics;
using System.IO;
using System;
using System.Threading;
using MediaBrowser.LibraryManagement;
using System.Xml;

namespace MediaBrowser
{
    public class MyAddIn : IAddInModule, IAddInEntryPoint
    {
        private const string CUSTOM_STYLE_FILE = "CustomStyles.mcml";

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
            SetupStylesMcml(host);
            SetupFontsMcml(host);

            Application app = new Application(new MyHistoryOrientedPageSession(), host);
            app.GoToMenu();
        }

        private void SetupFontsMcml(AddInHost host)
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
                    if (VerifyStylesXml(custom, Resources.FontsDefault))
                    {
                        host.MediaCenterEnvironment.Dialog("CustomFonts.mcml as been pathed with missing values", "CustomFonts.mcml", DialogButtons.Ok, 100, true);
                    }
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

        private void SetupStylesMcml(AddInHost host)
        {
            try
            {
                string file = Path.Combine(Helper.AppDataPath, "Styles_DoNotEdit.mcml");
                string custom = Path.Combine(Helper.AppDataPath, CUSTOM_STYLE_FILE);
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
                    if (!VerifyStylesXml(custom, Resources.StylesDefault))
                    {
                        host.MediaCenterEnvironment.Dialog(CUSTOM_STYLE_FILE + " has been patched with missing values", CUSTOM_STYLE_FILE, DialogButtons.Ok, 100, true);
                    }
                    File.Copy(custom, file);
                }
                else
                {
                    // new options must be added to the ThemeModel choice in configpage.mcml
                    switch (Config.Instance.Theme)
                    {
                        case "Black":
                            File.WriteAllBytes(file, Resources.StylesBlack);
                            break;
                        case "Extender Default":
                            File.WriteAllBytes(file, Resources.StylesDefaultExtender);
                            break;
                        case "Extender Black":
                            File.WriteAllBytes(file, Resources.StylesBlackExtender);
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


        private bool VerifyStylesXml(string filename, byte[] resource)
        {
            XmlDocument custom = new XmlDocument();
            custom.Load(filename);
            XmlDocument def = new XmlDocument();
            using (MemoryStream ms = new MemoryStream(resource))
            {
                def.Load(ms);
            }
            List<XmlNode> missingNodes = new List<XmlNode>();
            foreach(XmlNode node in def.SelectNodes("//*[@Name]"))
            {
                if (custom.SelectSingleNode(string.Format("//*[@Name='{0}']", node.Attributes["Name"].Value)) == null)
                    missingNodes.Add(node);
                    //return string.Format("Missing entry in {0} for\n{1}", filename,node.OuterXml);
            }
            if (missingNodes.Count > 0)
            {
                foreach (XmlNode n in missingNodes)
                {
                    custom.DocumentElement.AppendChild(custom.ImportNode(n, true));
                }
                custom.Save(filename);
                return false;
            }
            return true;
        }
    }
}