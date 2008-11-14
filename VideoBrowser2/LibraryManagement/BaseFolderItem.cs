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

                            int desired_height = Config.Instance.MaximumPosterHeight;
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

        [MarkupVisible]
        public Image MCMLThumb
        {
            get
            {
                bool enableExperimentalPosterFix = false;

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
