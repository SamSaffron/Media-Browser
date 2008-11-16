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
        static Image defImage = LoadDefault();
        Image image = null;
        Size smallThumbSize = new Size(1, 1);// Config.Instance.MaximumPosterSize;
        bool forceSmallThumbLoad = false;
        Image smallImage = null;
        private PlaybackController playbackController;
        private PlayState playState;

        // we need a base for mcml 
        public BaseFolderItem()
        {
            
        }

        private static Image LoadDefault()
        {
            return new Image("res://ehres!MOVIE.ICON.DEFAULT.PNG");
        }      
        
        [MarkupVisible]
        public Image MCMLThumb
        {
            get
            {
                if (image != null)
                    return image;
                else if (IsVideo)    
                    image = defImage;
                if (!string.IsNullOrEmpty(ThumbPath))
                {
                    //Microsoft.MediaCenter.UI.Application.DeferredInvokeOnWorkerThread(LoadImage, ImageLoaded, ThumbPath);
                    LoadImage(this.ThumbPath);  // now we only use this for the selected items don't lazy load it any more - looks odd that the focused item takes too long to load otherwise
                    ImageLoaded(null);
                }
                return image;
            }
        }

        private void LoadImage(object pathString)
        {
            if ((image==null) || (image==defImage))
                this.image = new Image("file://" + (string)pathString);   
        }

        private void ImageLoaded(object nothing)
        {
            FirePropertyChanged("MCMLThumb");
        }

        
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
                    FirePropertyChanged("PosterZoom");
                }
            }
        }
             

        public Vector3 PosterZoom
        {
            get
            {
                float x = Math.Max(this.SmallThumbSize.Height, this.SmallThumbSize.Width);
                float z = (float)((-0.007 * x) + 2.5);
                if (z < 1.15)
                    z = 1.15F;
                if (z > 1.9F)
                    z = 1.9F; // above this the navigation arrows start going in strange directions!
                return new Vector3(z,z,1);
            }
        }

        

        [MarkupVisible]
        public Image MCMLSmallThumb
        {
            get
            {
                if ((smallImage != null) && !forceSmallThumbLoad)
                    return smallImage;
                string path = this.ThumbPath;
                if ((path == null) || (path.Length == 0))
                {
                    if (this.IsVideo)
                    {
                        smallImage = defImage;
                        return smallImage;
                    }
                    else
                        return null;
                }
                Microsoft.MediaCenter.UI.Application.DeferredInvokeOnWorkerThread(LoadSmallImage, SmallImageLoaded,path);
                return smallImage;
            }
        }

        private void SmallImageLoaded(object nothing)
        {
            FirePropertyChanged("MCMLSmallThumb");
        }

        private void LoadSmallImage(object pathString)
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
                    //Trace.TraceInformation("Generating small image for " + this.ThumbPath + ": " + maxSz.ToString());
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

      

        #region Playback control, a bunch of shadow methods 

        
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

        public void ClearWatched()
        {
            this.playState.PlayCount = 0;
            this.playState.Position = new TimeSpan(0);
            FirePropertyChanged("HaveWatched");
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
