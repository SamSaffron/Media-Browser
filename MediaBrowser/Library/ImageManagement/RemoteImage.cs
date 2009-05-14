using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaBrowser.Library.Filesystem;
using System.Net;
using System.Diagnostics;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Library.ImageManagement {
    public class RemoteImage : LibraryImage {


        private void DownloadImage() {
            Logger.ReportInfo("Fetching image: " + Path);
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(Path);
            req.Timeout = 60000;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (MemoryStream ms = new MemoryStream()) {
                Stream r = resp.GetResponseStream();
                int read = 1;
                byte[] buffer = new byte[10000];
                while (read > 0) {
                    read = r.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, read);
                }
                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);


                using (var stream = ProtectedFileStream.OpenExclusiveWriter(LocalFilename)) {
                    stream.Write(ms.ToArray(), 0, (int)ms.Length);
                }
            }
        }

        public override string GetLocalImagePath() {
            lock (Lock) {
                if (File.Exists(LocalFilename)) {
                    return LocalFilename;
                }
                int attempt = 0;
                bool success = false;
                while (attempt < 2) {
                    try {
                        attempt++;
                        DownloadImage();
                        success = true;
                        break;
                    } catch (Exception e) {
                        Logger.ReportException("Failed to download image: " + Path, e);
                    }
                }
                return success?LocalFilename:null;
            }
        }
    }
}
