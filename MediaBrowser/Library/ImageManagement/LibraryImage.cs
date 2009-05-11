using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Extensions;
using System.IO;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Configuration;

namespace MediaBrowser.Library.ImageManagement {
    public abstract class LibraryImage {

        static Dictionary<Guid, object> FileLocks = new Dictionary<Guid, object>();

        protected static string cachePath = ApplicationPaths.AppImagePath;

        /// <summary>
        /// The raw path of this image including http:// or grab:// 
        /// </summary>
        public string Path { get; set; }

        protected int width = -1;
        protected int height = -1;

        protected Guid Id { get { return Path.GetMD5(); } }


        public virtual void Init() {
            
        }


        /// <summary>
        /// Write lock for a particular file in the cache
        /// </summary>
        protected object Lock {
            get {
                lock (FileLocks) {
                    object obj;
                    if (!FileLocks.TryGetValue(Id, out obj)) {
                        obj = new object();
                        FileLocks[Id] = obj;
                    }
                    return obj;
                }
            }
        }


        /// <summary>
        /// Will ensure a local copy is cached and return the path to the caller
        /// </summary>
        /// <returns></returns>
        public abstract string GetLocalImagePath();


        protected virtual string LocalFilename {
            get {
                return System.IO.Path.Combine(cachePath, Id.ToString() + System.IO.Path.GetExtension(Path));
            }
        }

        public int Width { get { EnsureImageSizeInitialized(); return width; } }
       
        public int Height { get { EnsureImageSizeInitialized(); return height; } }

        public float Aspect {
            get {
                EnsureImageSizeInitialized();
                return ((float)Height) / (float)Width;;
            }
        }

        // will clear all local copies
        public void ClearLocalImages() { 
            lock(Lock){
                foreach (var item in Directory.GetFiles(cachePath,Id.ToString() + "*"))
	            {
                    File.Delete(item);
	            }  
            }
        }

        public virtual void EnsureImageSizeInitialized() {
            if (width < 0 || height < 0) {
                using (var image = System.Drawing.Bitmap.FromFile(GetLocalImagePath())) {
                    width = image.Width;
                    height = image.Height;
                }
            }
        }

        /// <summary>
        /// Will ensure a local copy is cached and return the path to the caller
        /// </summary>
        /// <param name="width">required height</param>
        /// <param name="height">required width</param>
        /// <returns>local path</returns>
        /// <remarks>if width or height is -1 it will return a default size</remarks>
        public string GetLocalImagePath(int width, int height) {
            lock (Lock) {
                string localFile = GetLocalImagePath();
                string postfix = "." + width.ToString() + "x" + height.ToString();
                string resizedImagePath = System.IO.Path.Combine(cachePath,
                    Id.ToString() + postfix + ".png");

                if (File.Exists(resizedImagePath)) {
                    return resizedImagePath;
                }

                ResizeImage(localFile, resizedImagePath, width, height);
                return resizedImagePath;
            }
        }

        protected void ResizeImage(string source, string destination, int width, int height) {
            using (System.Drawing.Bitmap bmp = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(source))
            using (System.Drawing.Bitmap newBmp = new System.Drawing.Bitmap(width, height)) 
            using (System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage(newBmp))
            {
                        
                graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphic.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                graphic.DrawImage(bmp, 0, 0, width, height);

                MemoryStream ms = new MemoryStream();
                newBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                using (var fs = ProtectedFileStream.OpenExclusiveWriter(destination))
                {
                	BinaryWriter bw = new BinaryWriter(fs);
                    bw.Write(ms.ToArray());
                }
            }             
        }
    }
}
