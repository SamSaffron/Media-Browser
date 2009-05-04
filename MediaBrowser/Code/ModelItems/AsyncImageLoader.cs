using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using Microsoft.MediaCenter.UI;
using System.IO;
using MediaBrowser.Library.Factories;
using System.Reflection;
using MediaBrowser.Library;
using MediaBrowser.Library.ImageManagement;
using MediaBrowser.Library.Threading;

namespace MediaBrowser.Code.ModelItems {
    class AsyncImageLoader {

        static MethodInfo ImageFromStream = typeof(Image).GetMethod("FromStream", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(string), typeof(Stream) }, null);

        Item item;
        Func<LibraryImage> source;
        Action afterLoad;
        Image image = null;
        Image defaultImage = null;
        Microsoft.MediaCenter.UI.Size size;
        object sync = new object();

        public Microsoft.MediaCenter.UI.Size Size {
            get {
                return size;
            }
            set {
                lock (this) {
                    size = value;
                    image = null;
                }
            }
        }

        public bool IsLoaded {
            get;
            private set;
        }

        public AsyncImageLoader(Func<LibraryImage> source, Image defaultImage, Action afterLoad) {
            this.source = source;
            this.afterLoad = afterLoad;
            this.IsLoaded = false;
            this.defaultImage = defaultImage;
        }

        public Image Image {
            get {
                lock (this) {
                    if (image == null && source != null) {
                        Async.Queue(() => LoadImage());
                    }

                    if (image != null) {
                        return image;
                    }
                    if (!IsLoaded) {
                        return null;
                    } else {
                        // fall back
                        return defaultImage;
                    }
                }
            }
        }

        private void LoadImage() {
            try {
                lock (sync) {
                    LoadImageImpl();
                    IsLoaded = true;
                }
            } catch (Exception e) {
                // this may fail in if we are unable to write a file... its not a huge problem cause we will pick it up next time around
                Application.Logger.ReportException("Failed to load image: " + item.Name, e);
                if (Debugger.IsAttached) {
                    Debugger.Break();
                }
            }
        }

        private void LoadImageImpl() {

            byte[] bytes;

            bool sizeIsSet = Size != null && Size.Height > 0 && Size.Width > 0;

            var localImage = source();

            // if the image is invalid it may be null.
            if (localImage != null) {

                string localPath = localImage.GetLocalImagePath();
                if (sizeIsSet) {
                    localPath = localImage.GetLocalImagePath(Size.Width, Size.Height);
                }

                bytes = File.ReadAllBytes(localPath);

                MemoryStream imageStream = new MemoryStream(bytes);
                imageStream.Position = 0;

                Image newImage = (Image)ImageFromStream.Invoke(null, new object[] { null, imageStream });
                lock (this) {
                    image = newImage;
                    if (!sizeIsSet) {
                        size = new Size(localImage.Width, localImage.Height);
                    }
                }

                if (afterLoad != null) {
                    // this makes it a bit more convenient 
                    Microsoft.MediaCenter.UI.Application.DeferredInvoke( _ => afterLoad());
                }
            }
        }

    }
}
