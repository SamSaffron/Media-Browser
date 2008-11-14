using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    public abstract class BaseFolderItem : Command, IFolderItem 
    {

        static MethodInfo fromStreamMethodInfo = null;
        static object syncObj = new object();
        static Thread imageLoaderThread = null;
        static Queue<BaseFolderItem> loadItemQueue = new Queue<BaseFolderItem>(); 



        // we need a base for mcml 
        public BaseFolderItem()
        {
            
        }

        #region Background image loading support

        private static Queue<BaseFolderItem> GetPendingItems()
        {
            Queue<BaseFolderItem> items = new Queue<BaseFolderItem>(); 

            lock (syncObj)
            {
                while (loadItemQueue.Count > 0)
                {
                    items.Enqueue(loadItemQueue.Dequeue()); 
                }
            }
            return items;
        }

        private static void ProcessQueue()
        {
            var pending = GetPendingItems();
            while (pending.Count > 0)
            {
                var item = pending.Dequeue();

                try
                {
                    // load it ... and notify 
                    var image = ImageFromStream(new MemoryStream(File.ReadAllBytes(item.ThumbPath)));

                    lock (item)
                    {
                        item.image = image;
                    }
                    Microsoft.MediaCenter.UI.Application.DeferredInvoke(item.NewThumbnailGenerated);
                }
                catch
                {
                    Trace.TraceInformation("Failed to load item");
                    // fall through 
                }

                if (pending.Count == 0)
                {
                    lock (syncObj)
                    {
                        pending = GetPendingItems();
                        if (pending.Count == 0)
                        {
                            // finish with this thread 
                            imageLoaderThread = null;
                        }
                    }
                }
            }
        }

        private static void StartProcessingQueue()
        {
            lock (syncObj)
            {
                if (imageLoaderThread == null)
                {
                    imageLoaderThread = new Thread(new ThreadStart(ProcessQueue));
                    imageLoaderThread.Start();
                }
            }
        }

        private void NewThumbnailGenerated(object state)
        {
            this.FirePropertyChanged("MCMLThumb"); 
        }

        Image image = null;
        /*
        Image poster_image = null; 

        [MarkupVisible]
        public Image PosterViewThumb
        {
            get
            {
                // only speed up cached folders
                if (this is FolderItem)
                {
                    return MCMLThumb;
                }

                if (poster_image == null)
                {
                    if (!File.Exists(PosterViewThumbPath))
                    {
                        if (File.Exists(ThumbPath))
                        {
                            System.Drawing.Image image = new System.Drawing.Bitmap(ThumbPath);
                            var aspect = ((float)image.Width) / ((float)image.Height) ;

                            int desired_height =200;
                            if (desired_height > image.Height)
                            {
                                desired_height = image.Height;
                            }

                            int desired_width = (int)(desired_height * aspect);

                            // movie poster
                            if (aspect > 0.65 && aspect < 0.75)
                            {
                                desired_width = (int)(desired_height * 0.7);
                            }

                            Helper.ResizeImage(ThumbPath, PosterViewThumbPath, desired_width, desired_height);
                        }
                    }

                    poster_image = Helper.GetMCMLThumb(PosterViewThumbPath, IsVideo);
                }

                return poster_image;
 
            } 
        }

        public string PosterViewThumbPath
        {
            get
            {
                return Path.Combine(Helper.AppPosterThumbPath, Key + ".png"); 
 
            } 
        }
         */

        [MarkupVisible]
        public Image MCMLThumb
        {
            get
            {
                if (image != null)
                    return image;
                bool enableExperimentalPosterFix = true;

                bool loadImage = false;

                lock (this)
                {
                    if (image == null)
                    {
                        if (enableExperimentalPosterFix)
                        {
                            loadImage = true;
                            image = Helper.GetMCMLThumb("", IsVideo);
                        }
                        else
                        {
                            image = Helper.GetMCMLThumb(ThumbPath, IsVideo);
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(ThumbPath) && loadImage)
                {
                    // start a background load 
                    lock (syncObj)
                    {
                        loadItemQueue.Enqueue(this);
                        StartProcessingQueue(); 
                    }
                }
                return image;
            }
        }

        Size smallThumbSize = new Size(1,1);// Config.Instance.MaximumPosterSize;
        bool forceSmallThumbLoad = false;
        public Size SmallThumbSize
        {
            get
            {
                return this.smallThumbSize;
            }
            set
            {
                if (this.smallThumbSize != value)
                {
                    this.smallThumbSize = value;
                    lock(this)
                        if (smallImage != null)
                        {
                            forceSmallThumbLoad = true;
                            FirePropertyChanged("MCMLSmallThumb"); // defer the regeneration to next time it is needed on screen
                        }
                    FirePropertyChanged("SmallThumbSize");
                    FirePropertyChanged("LabelConstraint");
                }
            }
        }

        public Size LabelConstraint
        {
            get
            {
                Size s = this.SmallThumbSize;
                s.Height = 33; // warning increasing this number causes some strange effects in the poster view when the first item is focused
                return s;
            }
        }

        Image smallImage = null;

        [MarkupVisible]
        public Image MCMLSmallThumb
        {
            get
            {
                if ((smallImage != null) && !forceSmallThumbLoad)
                    return smallImage;
                string path = this.ThumbPath;
                if ((path == null) || (path.Length == 0))
                    return null;
                Microsoft.MediaCenter.UI.Application.DeferredInvokeOnWorkerThread(LoadSmallImage, SmallImageLoaded,path);
                return smallImage;
            }
        }

        public void SmallImageLoaded(object nothing)
        {
            FirePropertyChanged("MCMLSmallThumb");
        }

        public void LoadSmallImage(object pathString)
        {
            string path = (string)pathString;
            lock (this)
            {
                if ((smallImage == null) || (forceSmallThumbLoad))
                {
                    forceSmallThumbLoad = false;
                    Size maxSz = this.SmallThumbSize;
                    if (maxSz.Width == 1)
                        return; // the size has not been set yet
                    Trace.TraceInformation("Generating small image for " + this.ThumbPath + ": " + maxSz.ToString());
                    System.Drawing.Size newSize = new System.Drawing.Size(maxSz.Width, maxSz.Height);
                    using (System.Drawing.Bitmap bmp = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(path))
                    {
                        System.Drawing.Size s = bmp.Size;
                        double constraintAspect = (double)maxSz.Width / (double)maxSz.Height;
                        double aspect = (double)s.Width / (double)s.Height;
                        if (Math.Abs(aspect - constraintAspect) < 0.05)
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
                                /*
                                graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                                graphic.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                 */
                                graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
                                graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
                                graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Default;
                                graphic.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                                graphic.DrawImage(bmp, 0, 0, newSize.Width, newSize.Height);
                            }
                            MemoryStream ms = new MemoryStream();
                            newBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                            ms.Seek(0, SeekOrigin.Begin);
                            MethodInfo mi = typeof(Image).GetMethod("FromStream", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(string), typeof(Stream) }, null);
                            smallImage = (Image)mi.Invoke(null, new object[] { null, ms });
                        }
                    }
                }
            }
        }

        public bool HasThumb
        {
            get { return ((this.ThumbPath != null) && (this.ThumbPath.Length > 0)); }
        }

        private static Image ImageFromStream(Stream stream)
        {
            if (fromStreamMethodInfo == null)
            {
                lock (syncObj)
                {
                    MethodInfo[] mis = typeof(Image).GetMethods(BindingFlags.Static | BindingFlags.NonPublic);

                    foreach (MethodInfo mi in mis)
                    {
                        ParameterInfo[] pis = mi.GetParameters();
                        if (mi.Name == "FromStream" && pis.Length == 2)
                        {
                            if (pis[0].ParameterType == typeof(String) &&
                                pis[1].ParameterType == typeof(Stream))
                            {
                                fromStreamMethodInfo = mi;
                            }
                        }
                    }
                }
            }

            return (Image)fromStreamMethodInfo.Invoke(null, new object[] { null, stream });
        }


        #endregion 

        #region Playback control, a bunch of shadow methods 

        private PlaybackController playbackController;
        public PlaybackController PlaybackController 
        {
            get
            {
                if (playbackController == null)
                {
                    // upgrade to folderItem if required
                    var folderItem = this as FolderItem;
                    if (folderItem == null)
                    {
                        folderItem = new FolderItem(this.Filename, this.IsFolder);
                    }

                    playbackController = new PlaybackController(folderItem);
                }

                return playbackController; 
            }
        }

        private PlayState playState;
        public PlayState PlayState
        {
            get
            {
                if (this.playState == null)
                    this.playState = PlayState.Get(this);
                return this.playState;
            }
        }

        public void FirePropertyChanged_Public(string prop)
        {
            this.FirePropertyChanged(prop);
        }

        public bool CanResume
        {
            get
            {
                return PlaybackController.CanResume;
            }
        }

        public void Play()
        {
            PlaybackController.Play();
        }

        public void Resume()
        {
           PlaybackController.Resume();
        }

        #endregion 

        #region IFolderItem Members

        public String LastWatched
        {
            get 
            {
                if ((!IsFolder || IsMovie) && (this.PlayState != null) && (this.PlayState.LastPlayed != DateTime.MinValue))
                {
                    return "Last Watched: " + this.PlayState.LastPlayed.ToShortDateString();
                }
                else
                    return "";
            }
        }

        [MarkupVisible]
        public bool HaveWatched
        {
            get
            {
                try
                {
                    if (!IsFolder || IsMovie)
                    {
                        if (this.PlayState == null)
                            return false;
                        if (this.PlayState.PlayCount != 0)
                            return true;
                        if (File.Exists(this.Filename))
                        {
                            FileInfo fi = new FileInfo(this.Filename);
                            if (fi.LastWriteTime <= Config.Instance.AssumeWatchedBefore)
                                return true;
                        }
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }

        }

        public abstract bool IsVideo
        {
            get;
        }

        public abstract bool IsMovie
        {
            get;
        }

        public abstract bool IsFolder
        {
            get;
        }

        public abstract DateTime CreatedDate
        {
            get;
        }

        public abstract DateTime ModifiedDate
        {
            get;
        }

        public abstract int RunningTime
        {
            get;
        }

        public abstract string RunningTimeString
        {
            get;
        }

        public abstract float IMDBRating
        {
            get;
        }

        public abstract string IMDBRatingString
        {
            get;
        }

        public abstract string Filename
        {
            get;
        }

        public abstract string Title1
        {
            get;
        }

        public abstract string Title2
        {
            get;
        }

        public abstract string Overview
        {
            get;
        }

		public abstract string SortableDescription
		{
			get;
		}
		
		public abstract string ThumbHash
        {
            get;
        }

        public abstract List<String> Genres
        {
            get;
        }

        public abstract string ThumbPath
        {
            get;
            set;
        }


        public abstract string GenresString
        {
            get;
        }

        public abstract int ProductionYear
        {
            get;
        }

        public abstract List<String> Actors
        {
            get;
        }

        public abstract List<String> Directors
        {
            get;
        }

        protected string uniqueKey;
        public string Key
        {
            get
            {
                if (uniqueKey == null)
                {
                    if (this.Filename == null)
                        return null;
                    uniqueKey = Helper.HashString(this.Filename);
                }
                return uniqueKey;
            }
        }

        #endregion




     


    }
}
