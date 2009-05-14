using System.Collections.Generic;
using Microsoft.MediaCenter.Hosting;
using Microsoft.MediaCenter;
using System.Diagnostics;
using System.IO;
using System;
using System.Threading;
using MediaBrowser.LibraryManagement;
using System.Xml;
using System.Reflection;
using Microsoft.MediaCenter.UI;
using System.Text;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library;

namespace MediaBrowser
{
    public class MyAddIn : IAddInModule, IAddInEntryPoint
    {
        private const string CUSTOM_STYLE_FILE = "CustomStyles.mcml";
        private const string FONTS_FILE = "Fonts_DoNotEdit.mcml";
        private const string CUSTOM_FONTS_FILE = "CustomFonts.mcml";

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

            var config = GetConfig();
            if (config == null) {
                Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
                return;
            }

            Kernel.Init(config); 

            Environment.CurrentDirectory = ApplicationPaths.AppConfigPath;
            try
            {
                SetupStylesMcml(host);
                SetupFontsMcml(host);
            }
            catch (Exception ex)
            {
                host.MediaCenterEnvironment.Dialog(ex.Message, "Customisation Error", DialogButtons.Ok, 100, true);
                Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
                return;
            }

            Application app = new Application(new MyHistoryOrientedPageSession(), host);

            app.GoToMenu();
        }

        private static ConfigData GetConfig() {
            ConfigData config = null;
            try {
                config = ConfigData.FromFile(ApplicationPaths.ConfigFile);
            } catch (Exception ex) {
                MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
                DialogResult r = ev.Dialog(ex.Message + "\nReset to default?", "Error in configuration file", DialogButtons.Yes | DialogButtons.No, 600, true);
                if (r == DialogResult.Yes) {
                    config = new ConfigData(ApplicationPaths.ConfigFile);
                    config.Save();
                } else {
                    Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();

                }
            }

            return config;
        }

        private void SetupFontsMcml(AddInHost host)
        {
            try
            {
                string file = Path.Combine(ApplicationPaths.AppConfigPath, FONTS_FILE);
                string custom = Path.Combine(ApplicationPaths.AppConfigPath, CUSTOM_FONTS_FILE);
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
                    Logger.ReportInfo("Using custom fonts mcml");
                    if (!VerifyStylesXml(custom, Resources.FontsDefault))
                    {
                        host.MediaCenterEnvironment.Dialog("CustomFonts.mcml as been pathed with missing values", CUSTOM_FONTS_FILE, DialogButtons.Ok, 100, true);
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
                Logger.ReportException("Error creating Fonts_DoNotEdit.mcml" , ex);
                throw;
            }
        }

        private void SetupStylesMcml(AddInHost host)
        {
            try
            {
                string file = Path.Combine(ApplicationPaths.AppConfigPath, "Styles_DoNotEdit.mcml");
                string custom = Path.Combine(ApplicationPaths.AppConfigPath, CUSTOM_STYLE_FILE);
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
                    Logger.ReportInfo("Using custom styles mcml");
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
                Logger.ReportException("Error creating Styles_DoNotEdit.mcml", ex);
                throw;
            }

        }


        private bool VerifyStylesXml(string filename, byte[] resource)
        {
            XmlDocument custom = new XmlDocument();
            try
            {
                custom.Load(filename);
            }
            catch
            {
                throw new ApplicationException(filename + " is not well formed xml");
            }
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
            try
            {
                Type m = Type.GetType("Microsoft.MediaCenter.UI.Template.MarkupSystem,Microsoft.MediaCenter.UI");
                MethodInfo mi = m.GetMethod("Load", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                object sys = Activator.CreateInstance(m);
                object r = mi.Invoke(sys, new object[] { "file://" + filename });
                LoadResult lr = (LoadResult)r;
                if (lr.Status != LoadResultStatus.Success)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string s in lr.Errors)
                        sb.AppendLine(s);
                    throw new ApplicationException("Error loading " + filename + "\n" + sb.ToString());
                }
            }
            catch (ApplicationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error attempting to verify custom mcml files. Microsoft may have changed the internals of Media Center.\n" + ex.ToString());
            }
            return true;
        }
    }
}