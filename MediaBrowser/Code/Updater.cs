using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;
using System.Diagnostics;
using Microsoft.MediaCenter;
using Microsoft.MediaCenter.Hosting;
using Microsoft.MediaCenter.UI;
using MediaBrowser;
using System.Reflection;
using MediaBrowser.Library.Logging;



// XML File structure
/*
 
 <Config> 
    <Beta url="" version=""/> 
    <Release url="" version="">
 </Config>
 
 
 */

namespace MediaBrowser.Util
{
    // Updater class deals with checking for updates and downloading/installing them.
    public class Updater
    {
        // Reference back to the application for displaying dialog (thread safe).
        private Application appRef;

        // Constructor.
        public Updater(Application appRef)
        {
            this.appRef = appRef;
        }

        public static System.Version CurrentVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        // private members.
        private string remoteFile;
        private string localFile;
        private System.Version newVersion;
        
        // This should be replaced with the real location of the version info XML.
        private const string infoURL = "http://www.mediabrowser.tv/mbinfo.xml";

        // Blocking call to check the XML file up in the cloud to see if we need an update.
        // This is really meant to be called as its own thread.
        public void checkUpdate()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(new XmlTextReader(infoURL));

                XmlNode node;

                if (appRef.Config.EnableBetas)
                {
                    node = doc.SelectSingleNode("/Config/Beta"); 
                }
                else
                {
                    node = doc.SelectSingleNode("/Config/Release"); 
                }

                newVersion = new System.Version(node.Attributes["version"].Value);
                remoteFile = node.Attributes["url"].Value;

                // Old -> start update
                if (CurrentVersion < newVersion)
                {
                    if (Application.MediaCenterEnvironment.Capabilities.ContainsKey("Console"))
                    {
                        // Prompt them if they want to update.
                        DialogResult reply = Application.DisplayDialog("Do you wish to update Media Browser now?  (Requires you to grant permissions and a restart of Media Browser)", "Update Available", (DialogButtons)12 /* Yes, No */, 10);
                        if (reply == DialogResult.Yes)
                        {
                            // If they want it, download in the background and prompt when done.
                            DownloadUpdate();
                        }
                    }
                    else
                    {
                        // Let the user know about the update, but do nothing as we can't install from 
                        // an extender.
                        DialogResult reply = Application.DisplayDialog("There is an update available for Media Browser.  Please update Media Browser next time you are at your MediaCenter PC.", "Update Available", (DialogButtons)1 /* OK */, 10);
                    }
                }
            }
            catch (Exception e)
            {
                // No biggie, just return out.
                Logger.ReportException("Failed to update plugin", e);
            }

        }

        // Downloads the update and stores the location.
        private void DownloadUpdate()
        {

            int bytesdone = 0;

            // Get a temp file name for the installer.  (This had better be an MSI file.)
            // Later we might make this smart about the extension of the web URL.
            localFile = System.IO.Path.GetTempFileName();
            localFile += ".msi";

            // Streams to read/write.
            Stream RStream = null;
            Stream LStream = null;

            // The respose of the web request.
            WebResponse response = null;
            try
            {
                // request the URL and get the response.
                WebRequest request = WebRequest.Create(remoteFile);
                if (request != null)
                {
                    response = request.GetResponse();
                    if (response != null)
                    {
                        // If we got a response lets KiB by KiB stream the 
                        // data into the temp file.
                        RStream = response.GetResponseStream();
                        LStream = File.Create(localFile);
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        do
                        {
                            bytesRead = RStream.Read(buffer, 0, buffer.Length);
                            LStream.Write(buffer, 0, bytesRead);
                            bytesdone += bytesRead;
                        }
                        while (bytesRead > 0);
                    }
                }
            }
            catch (Exception)
            {
                // We don't want error reporting here.
                bytesdone = 0;
            }
            finally
            {
                // Close out all of the streams.
                if (response != null)
                    response.Close();
                if (RStream != null)
                    RStream.Close();
                if (LStream != null)
                    LStream.Close();
            }

            if (bytesdone > 0)
            {
                // If we got it all, lets process the completed download.
                DownloadComplete();
            }
            else
            {
                // Otherwise let them know the download didn't work and they should just keep using VB.
                DialogResult reply = Application.DisplayDialog("Media Browser will operate normally and prompt you again the next time you load it.", 
                    "Update Download Failed", DialogButtons.Ok, 10);
            }
        }

        // Process the completed update download.
        public void DownloadComplete()
        {
            // Let them know we will be closing VB then restarting it.
            DialogResult reply = Application.DisplayDialog("Media Browser must now exit to apply the update.  It will restart automatically when it is done", 
                "Update Downloaded", DialogButtons.Ok, 10);
            
            // put together a batch file to execute the installer in silent mode and restart VB.
            string updateBat = "msiexec.exe /qb /i \"" + localFile + "\"\n";
            string windir = Environment.GetEnvironmentVariable("windir");
            updateBat += Path.Combine(windir, "ehome\\ehshell /entrypoint:{CE32C570-4BEC-4aeb-AD1D-CF47B91DE0B2}\\{FC9ABCCC-36CB-47ac-8BAB-03E8EF5F6F22}");
            string filename = System.IO.Path.GetTempFileName();
            filename += ".bat";
            System.IO.File.WriteAllText(filename, updateBat);

            // Start the batch file minimized so they don't notice.
            Process toDo = new Process();
            toDo.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            toDo.StartInfo.FileName = filename;

            toDo.Start();

            // Once we start the process we can kill the VB application.
            AddInHost context = AddInHost.Current;
            context.ApplicationContext.CloseApplication();

        }
    }
}
