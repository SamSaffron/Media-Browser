using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.IO;
using System.Reflection;
using System.Net;
using MediaBrowser.LibraryManagement;
using System.Diagnostics;
using MediaBrowser.Util.VideoProcessing;


namespace MediaBrowser.Library
{
    public class LibraryImage : ModelItem
    {
        private static BackgroundProcessor<LibraryImage> backgroundProcessor = new BackgroundProcessor<LibraryImage>(ThreadPoolSizes.IMAGE_CACHING_THREADS, LibraryImage.ProcessorCallback, "ImageCaching");
        private static BackgroundProcessor<LibraryImage> imageScalingProcessor = new BackgroundProcessor<LibraryImage>(ThreadPoolSizes.IMAGE_RESIZE_THREADS, LibraryImage.ImageLoadCallback, "SmallImageGeneration");
        private static BackgroundProcessor<LibraryImage> imageUpdateChecker = new BackgroundProcessor<LibraryImage>(1, LibraryImage.CheckImageUpdates, "CheckImageUpdates");
        private static Dictionary<string, Image> resourceCache = new Dictionary<string, Image>();

        Image image = null;
        Image smallImage = null;
        bool forceSmallThumbLoad = false;
        

        public LibraryImage(ImageSource source)
        {
            this.source = source;
            if (source != null)
            {
                if (source.OriginalSource == null)
                    Debugger.Break();
                
                if (source.LocalSource != null)
                {
                    if (!IsValidLocalSource)
                        source.LocalSource = null;
                    imageUpdateChecker.Enqueue(this);
                }
            }
        }
        
        private static void CheckImageUpdates(LibraryImage image)
        {
            image.CheckUpdate();
        }

        private void CheckUpdate()
        {
            if (IsResource)
                return;
            Trace.WriteLine("Checking image:" + this.Source.OriginalSource);
            if (this.Source.SourceTimestamp == DateTime.MinValue)
                return;
            // files cached from sources other than the file system will not have timestamps set
            if (File.Exists(this.source.OriginalSource))
            {
                DateTime ts = File.GetLastWriteTimeUtc(this.source.OriginalSource);
                if (this.source.SourceTimestamp != ts)
                {
                    Trace.WriteLine("Recache image:" + this.Source.OriginalSource);
                    this.Source.LocalSource = null;
                    CacheImageAsync();
                }
            }
        }

        public UniqueName UniqueName { get; set; }

        private bool IsResource
        {
            get
            {
                return (this.source.OriginalSource.StartsWith("res://") || this.source.OriginalSource.StartsWith("resx://"));
            }
        }

        private bool IsResourceLocal
        {
            get
            {
                return (this.source.LocalSource.StartsWith("res://") || this.source.LocalSource.StartsWith("resx://"));
            }
        }

        private bool IsValidLocalSource
        {
            get
            {
                if (source.LocalSource != null)
                {
                    if (IsResource || File.Exists(source.LocalSource))
                        return true;
                }
                return false;
            }
        }

        private ImageSource source;
        public virtual ImageSource Source
        {
            get { return this.source; }
            set 
            { 
                if (this.source != value) 
                {
                    this.source = value; 
                    this.image = null;
                    forceSmallThumbLoad = true;
                    //this.source.LocalSource = this.source.OriginalSource;
                    FirePropertyChanged("Source");
                    FirePropertyChanged("Image"); 
                    FirePropertyChanged("SmallImage");
                    FirePropertyChanged("AspectRatio");
                }
            }
        }
        private bool cachePending = false;
        private void CacheImageAsync()
        {
            if (this.source == null)
                return;
            lock (this)
            {
                if (cachePending)
                    return;
                cachePending = true;
            }
            backgroundProcessor.Inject(this);
            Microsoft.MediaCenter.UI.Application.DeferredInvokeOnWorkerThread(CacheImage, CacheImageDone, null);
        }

        private void CacheImageDone(object nothing)
        {
            lock (this)
                cachePending = false;
            FirePropertyChanged("Image");
            forceSmallThumbLoad = true;
            FirePropertyChanged("SmallImage");
            FirePropertyChanged("AspectRatio");
            FirePropertyChanged("SourceCache");
        }
        private static object saveLock = new object();

        private static void ProcessorCallback(LibraryImage image)
        {
            image.CacheImage(null);
            Microsoft.MediaCenter.UI.Application.DeferredInvoke(image.CacheImageDone, null);
        }

        private void CacheImage(object nothing)
        {
            if (this.source == null)
                return;
            Debug.WriteLine("Caching image: " + this.source.OriginalSource);
            string sourcePath = this.source.OriginalSource;
            lock (this.source)
            {
                if (this.source.LocalSource == null)
                {
                    if (IsResource)
                    {
                        this.source.LocalSource = this.source.OriginalSource;    
                    }
                    else
                    {
                        string localPath = Path.Combine(Helper.AppCachePath, "images");
                        if (!Directory.Exists(localPath))
                            Directory.CreateDirectory(localPath);
                        UniqueName name = UniqueName.Fetch("IMG:" + sourcePath, true);

                        try
                        {
                            if (sourcePath.StartsWith("http://"))
                            {
                                localPath = Path.Combine(localPath, name.Value + Path.GetExtension(sourcePath));
                                CacheHttpImage(sourcePath, localPath);
                                source.SourceTimestamp = DateTime.MinValue;
                            }
                            else if (sourcePath.StartsWith("grab://"))
                            {
                                localPath = Path.Combine(localPath, name.Value + ".png");
                                CacheGrabImage(sourcePath, localPath);
                                source.SourceTimestamp = DateTime.MinValue;
                            }
                            else
                            {
                                localPath = Path.Combine(localPath, name.Value + Path.GetExtension(sourcePath));
                                CacheLocalFileImage(localPath);
                                this.Source.SourceTimestamp = File.GetLastWriteTimeUtc(this.Source.OriginalSource);
                            }
                            this.source.LocalSource = localPath;
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error caching image for source: " + sourcePath + "\n" + ex.ToString());
                        }
                    }
                }
            }
        }

        private void CacheGrabImage(string sourcePath,string localPath)
        {
            sourcePath = sourcePath.Substring("grab://".Length);
            if (File.Exists(sourcePath))
            {
                if (Helper.IsVideo(sourcePath))
                    ThumbCreator.CreateThumb(sourcePath, localPath, 0.2);
                
            }
        }

        private void CacheLocalFileImage(string localPath)
        {
            if (File.Exists(this.source.OriginalSource))
            {
                if (File.Exists(localPath))
                    File.Delete(localPath);
                File.Copy(this.Source.OriginalSource, localPath);
            }
        }

        private static void CacheHttpImage(string sourcePath, string localPath)
        {
            if (!File.Exists(localPath))
            {
                int attempt = 0;
                while (attempt < 2)
                {
                    try
                    {
                        attempt++;
                        Trace.TraceInformation("Fetching image: " + sourcePath);
                        HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(sourcePath);
                        req.Timeout = 60000;
                        HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                        using (MemoryStream ms = new MemoryStream())
                        {
                            Stream r = resp.GetResponseStream();
                            int read = 1;
                            byte[] buffer = new byte[10000];
                            while (read > 0)
                            {
                                read = r.Read(buffer, 0, buffer.Length);
                                ms.Write(buffer, 0, read);
                            }
                            ms.Flush();
                            ms.Seek(0, SeekOrigin.Begin);
                            lock (saveLock) // sometimes mutiple things point at the same image so we would crash if they try to write at the same time
                            {
                                if (!File.Exists(localPath))
                                {
                                    using (FileStream fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None))
                                    {
                                        fs.Write(ms.ToArray(), 0, (int)ms.Length);
                                        fs.Flush();
                                        fs.Close();
                                    }
                                }
                            }
                            ms.Close();
                        }

                        resp.Close();
                        break;
                    }
                    catch { }
                }
            }
        }

        public virtual Image Image
        {
            get
            {
                if (image == null)
                {
                    if (this.source == null)
                        return null;
                    if ((this.source.LocalSource != null) && (IsResourceLocal))
                    {
                        if (resourceCache.ContainsKey(this.source.LocalSource))
                            image = resourceCache[this.source.LocalSource];
                        else
                        {
                            lock (resourceCache)
                            {
                                if (resourceCache.ContainsKey(this.source.LocalSource))
                                    image = resourceCache[this.source.LocalSource];
                                else
                                {
                                    image = new Image(source.LocalSource);
                                    resourceCache[this.source.LocalSource] = image;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!IsValidLocalSource)
                            this.Source.LocalSource = null;
                        if (this.source.LocalSource == null)
                        {
                            CacheImageAsync();
                            return BlankLibraryImage.Instance.Image;
                        }
                        image = new Image("file://" + source.LocalSource);
                    }
                }
                return image;
            }
        }

        public float AspectRatio
        {
            get 
            {
                if (this.source.LocalSource == null) 
                    CacheImageAsync();
                if (File.Exists(this.Source.LocalSource))
                {
                    try
                    {
                        using (System.Drawing.Image image = new System.Drawing.Bitmap(this.Source.LocalSource))
                        {
                            return ((float)image.Height) / ((float)image.Width);
                        }
                    }
                    catch (Exception)
                    {
                        // handle concurrency bugs
                        Trace.TraceError("Failed to calculate aspect ratio for image: " + this.Source.LocalSource );
                    }
                }
                return 0;
            }
        }
                   

        public virtual Image SmallImage
        {
            get
            {
                if ((smallImage != null) && !forceSmallThumbLoad)
                    return smallImage;
                if (this.source == null)
                    return null;
                lock(this)
                    if ((!IsResource) && (!forceSmallThumbLoad) && (File.Exists(this.SmallImageCacheFile)) && !cachePending)
                    {
                        smallImage = new Image("file://" + this.SmallImageCacheFile);
                        return smallImage;
                    }
                if (!IsValidLocalSource)
                {
                    this.source.LocalSource = null;
                    if (smallImage==null)
                        smallImage = BlankLibraryImage.Instance.SmallImage;
                    CacheImageAsync();
                    return smallImage;
                }
                if (IsResourceLocal) 
                {
                    if (resourceCache.ContainsKey(this.source.LocalSource))
                        smallImage = resourceCache[this.source.LocalSource];
                    else
                    {
                        lock (resourceCache)
                        {
                            if (resourceCache.ContainsKey(this.source.LocalSource))
                                smallImage = resourceCache[this.source.LocalSource];
                            else
                            {
                                smallImage = new Image(source.LocalSource);
                                resourceCache[this.source.LocalSource] = smallImage;
                            }
                        }
                    }
                }
                else //if ((smallSize.Height != 1) || forceSmallThumbLoad)
                {
                    
                    Debug.WriteLine("Requesting small image load");
                    imageScalingProcessor.Inject(this);
                    
                    //Microsoft.MediaCenter.UI.Application.DeferredInvokeOnWorkerThread(LoadSmallImage, SmallImageLoaded, null);
                }
                return smallImage ?? BlankLibraryImage.Instance.SmallImage;
            }
        }

        private Size smallSize = new Size(1,1);
        public Size SmallSize
        {
            get { return this.smallSize; }
            set 
            {
                if (this.smallSize != value) 
                {
                    this.smallSize = value;
                    
                    if (smallImage != null)
                    {
                        forceSmallThumbLoad = true;
                        FirePropertyChanged("SmallImage"); // defer the regeneration to next time it is needed on screen
                    }
                    FirePropertyChanged("SmallSize"); 
                } 
            }
        }

        private static void ImageLoadCallback(LibraryImage image)
        {
            image.LoadSmallImage(null);
            Microsoft.MediaCenter.UI.Application.DeferredInvoke(image.SmallImageLoaded);
        }

        private void SmallImageLoaded(object nothing)
        {
            //Debug.WriteLine("Small image loaded");
            FirePropertyChanged("SmallImage");
        }

        private string SmallImageCacheFile
        {
            get
            {
                if (this.source.LocalSource == null)
                    return null;
                else
                {
                    string path = this.source.LocalSource;
                    return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "small.png");
                }
            }
        }
        private object loadLock = new object();

        private void LoadSmallImage(object nothing)
        {
            Debug.WriteLine("Loading small image: " + this.source.LocalSource);
            if (this.Source.LocalSource == null)
                return;


            lock (loadLock)
            {
                if ((smallImage == null) || (forceSmallThumbLoad))
                {
                    
                    forceSmallThumbLoad = false;
                    if (IsResourceLocal)
                    {
                        return;
                    }
                    else
                    {
                        string path = (string)this.Source.LocalSource;
                        Size maxSz = this.SmallSize;
                        if (maxSz.Width == 1)
                            maxSz = new Size(200, 200);
                            //return; // the size has not been set yet
                        //Debug.WriteLine("Generating small image for " + this.ThumbPath + ": " + maxSz.ToString());
                        System.Drawing.Size newSize = new System.Drawing.Size(maxSz.Width, maxSz.Height);
                        using (System.Drawing.Bitmap bmp = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(path))
                        {
                            System.Drawing.Size s = bmp.Size;
                            double constraintAspect = (double)maxSz.Width / (double)maxSz.Height;
                            double aspect = (double)s.Width / (double)s.Height;
                            if (Math.Abs(aspect - constraintAspect) < Config.Instance.MaximumAspectRatioDistortion)
                            {
                                newSize.Width = maxSz.Width; // if the aspect is close to what we want the stretch it to match
                                newSize.Height = maxSz.Height;
                            }
                            // movie poster
                             
                            else if (aspect > 0.65 && aspect < 0.75)
                            {
                                newSize.Height = maxSz.Height;
                                newSize.Width = (int)((double)newSize.Height * 0.7);
                                if (newSize.Width > maxSz.Width)
                                {
                                    newSize.Width = maxSz.Width;
                                    newSize.Height = (int)((double)newSize.Width / 0.7);
                                }
                            }
                            else
                            {
                                double xratio = (double)newSize.Width / (double)s.Width;
                                double yratio = (double)newSize.Height / (double)s.Height;
                                double ratio = Math.Min(xratio, yratio);
                                newSize.Width = (int)((double)s.Width * ratio);
                                newSize.Height = (int)((double)s.Height * ratio);
                            }
                            using (System.Drawing.Bitmap newBmp = new System.Drawing.Bitmap(newSize.Width, newSize.Height))
                            {
                                using (System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage(newBmp))
                                {
                                    
                                    graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                    graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                    graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                                    graphic.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                    /*
                                    graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
                                    graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
                                    graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Default;
                                    graphic.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                                     * */
                                    graphic.DrawImage(bmp, 0, 0, newSize.Width, newSize.Height);
                                }
                                
                                MemoryStream ms = new MemoryStream();
                                newBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                ms.Seek(0, SeekOrigin.Begin);
                                string f = this.SmallImageCacheFile;
                                if (f != null)
                                {
                                    lock (this)
                                    {
                                        File.WriteAllBytes(this.SmallImageCacheFile, ms.ToArray());
                                        // doing it this way seems to give odd results when we grow and shrink the thumbnails - MS is doing some caching inside the Image objects and because the filename doesn't change it gets confused!
                                        //ms.Dispose();
                                        //if (smallImage != null)
                                            //smallImage.Dispose();
                                        //smallImage = new Image("file://" + f);
                                    }
                                }
                                //else
                                {
                                    ms.Seek(0, SeekOrigin.Begin);
                                    MethodInfo mi = typeof(Image).GetMethod("FromStream", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(string), typeof(Stream) }, null);
                                    if (smallImage != null)
                                        smallImage.Dispose();
                                    smallImage = (Image)mi.Invoke(null, new object[] { null, ms });
                                }
                            }
                        }
                    }
                }
            }
        }

       
    }

}
