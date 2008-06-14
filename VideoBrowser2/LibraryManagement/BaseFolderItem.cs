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
                    var image = ImageFromStream(File.OpenRead(item.ThumbPath));

                    lock (item)
                    {
                        item.image = image;
                        item.FirePropertyChanged("MCMLThumb"); 
                    }
                }
                catch
                {
                    Trace.WriteLine("Failed to load item");
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

        Image image = null;

        public Image MCMLThumb
        {
            get
            {
                lock (this)
                {
                    if (image == null)
                    {
                        image = Helper.GetMCMLThumb(ThumbPath, IsVideo);
                    }
                }
                /*
                if (!string.IsNullOrEmpty(ThumbPath))
                {
                    // start a background load 
                    lock (syncObj)
                    {
                        loadItemQueue.Enqueue(this);
                        StartProcessingQueue(); 
                    }
                }
                 */

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

        #region IFolderItem Members

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

        #endregion
    }
}
