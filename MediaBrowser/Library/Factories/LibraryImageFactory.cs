using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.ImageManagement;
using System.Diagnostics;

namespace MediaBrowser.Library.Factories {
    public class LibraryImageFactory {
        public static LibraryImageFactory Instance = new LibraryImageFactory();

        private LibraryImageFactory() {
        }

        Dictionary<string, LibraryImage> cache = new Dictionary<string, LibraryImage>();

        public LibraryImage GetImage(string path) {
            LibraryImage image = null;
            bool cached = false;

            lock(cache){
                cached = cache.TryGetValue(path, out image);
            }

            if (!cached && image == null) {
                try {

                    if (path.ToLower().StartsWith("http")) {
                        image = new RemoteImage();
                    } else {
                        image = new FilesystemImage();
                    }
                    image.Path = path;
                    image.Init();

                    // this will trigger a download a resize
                    image.EnsureImageSizeInitialized();
                } catch (Exception ex) {
                    Application.Logger.ReportException("Failed to load image: " + path + " ", ex);
                    image = null;
                }
            }

            lock (cache) {
                cache[path] = image;
            }

            return image;
        }
    }
}
