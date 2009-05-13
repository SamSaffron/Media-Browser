using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.ImageManagement;
using System.IO;

namespace MtnFrameGrabProvider {
    class GrabImage : LibraryImage {

        protected override string LocalFilename {
            get {
                return System.IO.Path.Combine(cachePath, Id.ToString() + ".jpg");
            }
        }

        public override string GetLocalImagePath() {
            lock (Lock) {
                if (File.Exists(LocalFilename)) {
                    return LocalFilename;
                }

                // path without mtngrab://
                string video = this.Path.Substring(10);

                Plugin.Logger.ReportInfo("Trying to extract mtn thumbnail for " + video);

                if (ThumbCreator.CreateThumb(video, LocalFilename, 600)) {
                    return LocalFilename;
                } else {
                    Plugin.Logger.ReportWarning("Failed to grab mtn thumbnail for " + video);
                    return null;
                }

            }

        }
    }
}
