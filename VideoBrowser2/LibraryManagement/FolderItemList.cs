using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Collections;
using System.Xml;
using System.IO.IsolatedStorage;
using System.Security.Cryptography;
using System.Drawing;

namespace SamSoft.VideoBrowser.LibraryManagement
{

    public delegate void FolderItemListModifiedDelegate();
    public delegate void SortOrdersModifiedDelegate();

    public class FolderItemList : List<IFolderItem> , IList
    {
        public event FolderItemListModifiedDelegate OnChanged;
        public event SortOrdersModifiedDelegate OnSortOrdersChanged;
        static FolderItemList newVideos = new FolderItemList();
        
        string _path;
        private VirtualFolder _virtualFolder;
        string _cacheKey = null;
        private bool _data_is_cached = false;
        private System.Threading.ManualResetEvent _populatedRealItems = new ManualResetEvent(false); 
        List<FolderItem> _realItems;

        FolderItemListPrefs _prefs; 

        static FolderItemList()
        {
            // TODO: Find new videos 
          //   ThreadPool.QueueUserWorkItem(new WaitCallback(delegate { FindNewVideos(); })); 
        } 

        public FolderItemList()
        {
            // add default sort orders
            sortOrders = new List<string>();
            sortOrders.Add("by name");
            sortOrders.Add("by date");
            Changed();
        }

        private void Changed()
        {
            if (OnChanged != null)
            {
                OnChanged();
            }
        }

        public override string ToString()
        {
            return _path;
        }

        public FolderItemListPrefs Prefs
        {
            get
            {
                if (_prefs == null)
                {
                    _prefs = new FolderItemListPrefs(CacheKey);
                }
                return _prefs;
            }
        }

        /*
        private static void FindNewVideos()
        {
            SortedCache<FolderItem> sortedCache = new SortedCache<FolderItem>(30, new FolderItemSorter(SortOrderEnum.Date));
            SearchForNewVideos(sortedCache, Helper.MyVideosPath);
            foreach (FolderItem item in sortedCache.Contents)
            {
                newVideos.Add(item);
            }
        }


        private static void SearchForNewVideos(SortedCache<FolderItem> currentList, string path)
        {
            List<FolderItem> list = GetFolderDetails(path);

            foreach (FolderItem item in list)
            {
                if (item.IsFolder && !item.IsVideo)
                {
                    SearchForNewVideos(currentList, item.Filename);
                }
                else
                {
                    currentList.Add(item); 
                } 
            } 
        }
         * 
         */

        
        
        // returns null if no cached items
        private List<CachedFolderItem> GetCache()
        {
            try
            {
                // look up to see if the filename is there 
                string xmlPath = System.IO.Path.Combine(Helper.AppDataPath, string.Format("{0}.xml", CacheKey));
                if (System.IO.File.Exists(xmlPath))
                {
                    // yay a cache hit. 
                    XmlDocument doc = new XmlDocument();
                    doc.Load(xmlPath);
                    var rval = new List<CachedFolderItem>(); 
                    foreach (XmlElement elem in doc.SelectNodes("Items/Item"))
                    {
                        rval.Add(new CachedFolderItem(elem, CacheKey, this));
                    }
                    return rval;
                }
            }
            catch
            {
                // cache failure
                Trace.WriteLine("Cache loading failed !!! (GetCache)");
            }
            // cache miss of failure
            return null;
        }

        
        private string CacheKey
        {
            get
            {
                if (_cacheKey == null)
                {
                    string key;
                    if (this._virtualFolder != null)
                    {
                        key = this._virtualFolder.Path;
                    }
                    else
                    {
                        key = this._path;
                    }

                    if (key != null)
                    {
                        // hash and stringify 
                        _cacheKey = Helper.HashString(key);
                    }
                }
                return _cacheKey;
            }
        }

         

        public void Navigate(VirtualFolder virtualFolder)
        {
            //List<FolderItem> items = GetCache(virtualFolder.Path.ToLower());

            this._virtualFolder = virtualFolder;

            var items = GetCache();
            if (items != null)
            {
                lock (this)
                {
                    Clear();
                    AddRange(items.ToArray());
                    _data_is_cached = true;
                }
            }
            else
            {

                lock (this)
                {
                    Clear();
                    foreach (var item in virtualFolder.Folders)
                    {
                        AddRange(GetFolderDetails(item).ToArray());
                    }
                }
            }

            
            Sort(Prefs.SortOrder);

            Changed();
        }

         

        public void Navigate(string path)
        {
            this._path = path;
            
            // check to see if we have this cached
            var items = GetCache();
            if (items != null)
            {
                lock (this)
                {
                    Clear();
                    AddRange(items.ToArray());
                    _data_is_cached = true;
                }
            }
            else
            {
                lock (this)
                {
                    Clear();

                    if (path == Helper.MyVideosPath)
                    {
                        AddSpecialFolders();
                    }

                    if (path == NewVideosPath)
                    {
                        AddRange(newVideos);
                    }
                    else
                    {
                        AddRange(GetFolderDetails(path).ToArray());
                    }
                }
            }
 
            Sort(Prefs.SortOrder);
            Changed();
        }

        // this is here so GetFolderDetails does not block the UI thread 
        class NonCommandFolderItem
        {
            string filename;
            bool isFolder;
            string description;
            public string ThumbPath;
            public VirtualFolder VirtualFolder;
            public string Path; 

            public NonCommandFolderItem(string filename, bool isFolder)
            : this (filename, isFolder, 
                isFolder ? 
                    System.IO.Path.GetFileName(filename) : 
                    System.IO.Path.GetFileNameWithoutExtension(filename))
            {
            }


            public NonCommandFolderItem(string filename, bool isFolder, string description)
            {
                this.filename = filename;
                this.isFolder = isFolder;
                this.description = description;
            }

            public FolderItem Upgrade()
            {
                FolderItem fi = new FolderItem(filename, isFolder, description);
                if (ThumbPath != null)
                {
                    fi.ThumbPath = ThumbPath; 
                }
                if (VirtualFolder != null)
                {
                    fi.VirtualFolder = VirtualFolder; 
                }
                if (Path != null)
                {
                    fi.Path = Path;
                }

                return fi;
            }
        }

        private static List<FolderItem> GetFolderDetails(string path)
        {
            List<FolderItem> rval = new List<FolderItem>();
            var items = GetNonCommandFolderDetails(path);
            foreach (var item in items)
            {
                rval.Add(item.Upgrade()); 
            }
            return rval;
        }

        private static List<NonCommandFolderItem> GetNonCommandFolderDetails(string path)
        {

            List<NonCommandFolderItem> rval = new List<NonCommandFolderItem>();

            try
            {
                foreach (string filename in Directory.GetDirectories(path))
                {
                    if ((File.GetAttributes(filename) & FileAttributes.Hidden) == FileAttributes.Hidden)
                    {
                        continue;
                    }

                    if (System.IO.Path.GetFileName(filename).ToLower() == "metadata")
                    {
                        continue;
                    }

                    rval.Add(new NonCommandFolderItem(filename, true));
                }

                Dictionary<string, NonCommandFolderItem> thumbMap = new Dictionary<string, NonCommandFolderItem>(); 

                string[] files = Directory.GetFiles(path); 
                foreach (string filename in files)
                {
                    if (Helper.IsVirtualFolder(filename))
                    {
                        // extract the thumb for this virtual folder
                        VirtualFolder vf = new VirtualFolder(filename);
                        NonCommandFolderItem fi = new NonCommandFolderItem(filename, true, System.IO.Path.GetFileNameWithoutExtension(filename));
                        fi.VirtualFolder = vf;
                        rval.Add(fi); 
                    }
                    if (Helper.IsShortcut(filename))
                    {
                        var shortcut_path = Helper.ResolveShortcut(filename);
                        if (Directory.Exists(shortcut_path))
                        {
                            NonCommandFolderItem fi = new NonCommandFolderItem(shortcut_path, true, System.IO.Path.GetFileNameWithoutExtension(filename));
                            fi.Path = path;
                            rval.Add(fi);
                        }
                        else
                        {
                            // it may be a shortcut to a file 
                            if (Helper.IsVideo(shortcut_path))
                            {
                                // refactor
                                NonCommandFolderItem item = new NonCommandFolderItem(shortcut_path, false);
                                rval.Add(item);
                                thumbMap[System.IO.Path.GetFileNameWithoutExtension(shortcut_path)] = item;
                            }
                        } 
                    }

                    if (Helper.IsVideo(filename))
                    {
                        NonCommandFolderItem item = new NonCommandFolderItem(filename, false);
                        rval.Add(item);
                        thumbMap[System.IO.Path.GetFileNameWithoutExtension(filename)] = item;
                    }
                }

                foreach (string filename in files)
                {
                    // special handling for filename.jpg etc..
                    if (Helper.IsImage(filename))
                    {
                        NonCommandFolderItem item;
                        if (thumbMap.TryGetValue(System.IO.Path.GetFileNameWithoutExtension(filename), out item))
                        {
                            item.ThumbPath = filename;
                        }

                    } 
                }
            }
            catch (DirectoryNotFoundException)
            {
                Trace.WriteLine("Missing Dir: (Bad shortcut)" + path);
            }

            return rval;
        }


        private void FolderWasChanged(IList<IFolderItem> items)
        {

            lock (this)
            {
                Clear();
                AddRange(items);
            }

            Changed();
        }

        public const string NewVideosPath = "New Videos";

        private void AddSpecialFolders()
        {
        //    Add(new SpecialFolderItem(NewVideosPath, "New Videos", true));
        }


        public string Path
        {
            get { return _path; }
        }

        public List<BaseFolderItem> nonGenreList;

        public SortOrderEnum SortOrder { get; set; } 

        public void Sort(SortOrderEnum sortOrderEnum)
        {
            Trace.WriteLine("Sort was called with " + sortOrderEnum.ToString() + " for " + this.Path); 

            lock (this)
            {
                SortOrder = sortOrderEnum;

                // do not attempt to cache if we have navigated directly to a list of items (in genere case)
                if (CacheKey != null)
                {
                    Prefs.SortOrder = sortOrderEnum;
                    Prefs.Save();
                }
      

                if (sortOrderEnum == SortOrderEnum.Genre || sortOrderEnum == SortOrderEnum.RunTime)
                {
                    AddGenreAndRuntimeSort();
                }

                if (sortOrderEnum == SortOrderEnum.Genre)
                {
                    if (nonGenreList == null)
                    {
                        nonGenreList = new List<BaseFolderItem> ();
                        foreach (BaseFolderItem item in this)
                        {
                            nonGenreList.Add(item);
                        }
                    }

                    var generes = new Dictionary<string, List<IFolderItem>>();


                    this.Clear();
                    foreach (var item in nonGenreList)
                    {
                        if (this.Count == 0) 
                        {
                            if (!item.IsMovie || item.Genres.Count == 0)
                            {
                                if (!generes.ContainsKey("Other"))
                                {
                                    generes["Other"] = new List<IFolderItem>();  
                                }
                                generes["Other"].Add(item);
                                
                            }
                        }
                        if (item.IsMovie)
                        {
                            foreach (var genre in item.Genres)
                            {
                                if (!generes.ContainsKey(genre))
                                {
                                    generes[genre] = new List<IFolderItem>();
                                }
                                generes[genre].Add(item);
                            }
                        }
                    }

                    foreach (var item in generes)
                    {
                        FolderItem fi = new FolderItem("test", true, item.Key);
                        fi.Contents = item.Value;
                        var movieText = " movie";
                        if (item.Value.Count > 1)
                        {
                            movieText = " movies";
                        }

                        fi.SetTitle2(item.Value.Count.ToString() + movieText);
                        fi.SetOverview(string.Format("Including: {0}", Helper.GetRandomNames(item.Value, 200)));
                        string thumbPath = System.IO.Path.Combine(System.IO.Path.Combine(Helper.AppDataPath, "genres"), item.Key + ".jpg");
                        if (File.Exists(thumbPath))
                        {
                            fi.ThumbPath = thumbPath;
                        }
                        this.Add(fi);
                    }

                    this.Sort(new FolderItemSorter(sortOrderEnum));
                }
                else
                {
                    if (nonGenreList != null)
                    {
                        this.Clear();
                        foreach (BaseFolderItem item in nonGenreList)
                        {
                            this.Add(item);
                        }
                        nonGenreList = null; 
                    }

                    this.Sort(new FolderItemSorter(sortOrderEnum));
                }


            }

            Changed();
        }

        /// <summary>
        /// Return the height / width of the first image in the folder
        /// </summary>
        public float ThumbAspectRatio
        {
            get
            {
                foreach (BaseFolderItem item in ActualItems)
                {
                    if (!string.IsNullOrEmpty(item.ThumbPath))
                    {
                        Image image = new Bitmap(item.ThumbPath);
                        return ((float)image.Height) / ((float)image.Width);
                    }
                }
                return 1; 
            }
        }


        private List<string> sortOrders; 
        public List<string> SortOrders
        {
            get
            {
                return sortOrders;
            }
        }

        // returns the actual list of items in the folder, regardless on if the genre browsing is selected
        private List<BaseFolderItem> ActualItems
        {
            get 
            {
                List<BaseFolderItem> items = new List<BaseFolderItem>(); 
                lock (this)
                {
                    if (nonGenreList != null)
                    {
                        foreach (var item in nonGenreList)
                        {
                            items.Add(item);
                        }
                    }
                    else
                    {
                        foreach (BaseFolderItem item in this)
                        {
                            items.Add(item);
                        }
                    }
                }
                return items;
            }
        }

        private void AddItem(BaseFolderItem item)
        {
            if (nonGenreList != null)
            {
                nonGenreList.Add(item);
                // TODO : refresh genre list
            }
            else
            {
                this.Add(item);
            }
        }

        private void RemoveItem(BaseFolderItem item)
        {
            if (nonGenreList != null)
            {
                nonGenreList.Remove(item);
                // TODO - refresh genre list 
            }
            else
            {
                this.Remove(item);
            }
        }

        internal void CacheMetadata()
        {
            try
            {
                bool cache_changed = false; 

                int minMoviesToShowGenreSearch = (int)(0.3 * this.Count);
                // unless we have no movies at all 
                if (minMoviesToShowGenreSearch == 0)
                {
                    minMoviesToShowGenreSearch = 1;
                }
                int moviesWithMetadata = 0;
                bool updatedSortOptions = false;

                Dictionary<string, BaseFolderItem> itemsToCache = new Dictionary<string, BaseFolderItem>();

                if (_data_is_cached)
                {
                    foreach (CachedFolderItem item in ActualItems)
                    {
                        if (item.IsMovie)
                        {
                            moviesWithMetadata++;
                        }

                        if (!updatedSortOptions && moviesWithMetadata >= minMoviesToShowGenreSearch)
                        {
                            AddGenreAndRuntimeSort();
                            updatedSortOptions = true;
                        }
                    }

                    var nonCommandItems = GetNonCommandFolderDetails();

                    Microsoft.MediaCenter.UI.Application.DeferredInvoke(new Microsoft.MediaCenter.UI.DeferredHandler(PopulateRealItemsForCache), nonCommandItems);
                    _populatedRealItems.WaitOne(); 

                    foreach (var item in _realItems)
                    {
                        ((FolderItem)item).LoadModifiedDate();                 
                        itemsToCache.Add(item.Filename, item);
                    }


                    List<CachedFolderItem> itemsToRemove = new List<CachedFolderItem>();


                    lock (this)
                    {
                        List<CachedFolderItem> ourItems = new List<CachedFolderItem>();

                        foreach (CachedFolderItem item in ActualItems)
                        {
                            if (!itemsToCache.ContainsKey(item.Filename))
                            {
                                itemsToRemove.Add(item);
                            }
                            else
                            {
                                if (itemsToCache[item.Filename].ModifiedDate == item.ModifiedDate)
                                {
                                    itemsToCache[item.Filename] = item;
                                }
                                else
                                {
                                    itemsToRemove.Add(item);
                                }
                            }
                        }
                    }

                    
                    bool itemsAdded = false; 
                    foreach (BaseFolderItem item in itemsToCache.Values)
                    {
                        lock (this)
                        {
                            if (item is FolderItem)
                            {
                                AddItem(item); 
                                itemsAdded = true;
                            }
                        }
                    }
                    
                    if (itemsToRemove.Count > 0)
                    {
                        lock (this)
                        {
                            foreach (var item in itemsToRemove)
                            {
                                RemoveItem(item);
                            }
                        }
                    }

                    if (itemsAdded || itemsToRemove.Count > 0)
                    {
                        cache_changed = true;
                        InvokeChanged(); 
                    }


                    foreach (BaseFolderItem item in itemsToCache.Values)
                    {
                        if (item is FolderItem)
                        {
                            ((FolderItem)item).EnsureMetadataLoaded();
                        } 
                    }
                }
                else
                {
                    cache_changed = true;
                    // start a process to cache all metadata xml for this folder
                    foreach (FolderItem item in ActualItems)
                    {
                        item.EnsureMetadataLoaded();
                        if (item.IsMovie)
                        {
                            moviesWithMetadata++;
                        }

                        if (!updatedSortOptions && moviesWithMetadata >= minMoviesToShowGenreSearch)
                        {
                            AddGenreAndRuntimeSort();
                            updatedSortOptions = true;
                        }

                        itemsToCache[item.Filename] = item;
                    }
                }

                if (!cache_changed)
                {
                    return;
                }

                CacheImages();
                CacheFolderXml(itemsToCache);

            }
            catch(Exception e)
            { 
                // forget about it its only the cache (could be collection modified cause we did a sort)
                Trace.WriteLine("Caching failed!!! " + e.ToString()); 
            }
        }

        private void InvokeChanged()
        {
            Microsoft.MediaCenter.UI.Application.DeferredInvoke(ChangedForInvoke); 
        }

        private void ChangedForInvoke(object state)
        {
            Changed();
        }

        // this is called if the cache is corrupt some how. 
        public void DestroyCache()
        {
            lock (this)
            {
                try 
                {
                    File.Delete(CacheXmlPath);
                }
                catch
                {
                    // well at least we tried 
                }
            } 
        }

        private void CacheImages()
        {
            // cache images
            string imagePath = System.IO.Path.Combine(Helper.AppDataPath, CacheKey);

            // ensure its there 
            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }

            Dictionary<string, bool> cachedImages = new Dictionary<string, bool>();

            foreach (var filename in Directory.GetFiles(imagePath))
            {
                cachedImages[System.IO.Path.GetFileName(filename)] = false;
            }


            foreach (object o in ActualItems)
            {
                FolderItem item = o as FolderItem;

                if (item != null)
                {
                    if (!String.IsNullOrEmpty(item.ThumbPath))
                    {
                        string key = item.ThumbHash;

                        if (!cachedImages.ContainsKey(key))
                        {
                            System.IO.File.Copy(item.ThumbPath, System.IO.Path.Combine(imagePath, key));
                        }
                        cachedImages[key] = true;
                    }
                }

                CachedFolderItem ci = o as CachedFolderItem;
                if (ci != null)
                {
                    if (!String.IsNullOrEmpty(ci.ThumbPath))
                    {
                        string key = ci.ThumbHash;
                        cachedImages[key] = true;
                    }
                }
            }

            foreach (var item in cachedImages)
            {
                if (item.Value == false)
                {
                    File.Delete(System.IO.Path.Combine(imagePath, item.Key));
                }
            }
        }

        private void CacheFolderXml(Dictionary<string, BaseFolderItem> itemsToCache)
        {
            // save this in a cache file ... 
            MemoryStream ms = new MemoryStream();
            XmlWriter writer = new XmlTextWriter(ms, Encoding.UTF8);
            writer.WriteStartDocument();
            writer.WriteStartElement("Items");
            foreach (BaseFolderItem item in itemsToCache.Values)
            {
                writer.WriteStartElement("Item");
                writer.WriteElementString("Filename", item.Filename);
                writer.WriteElementString("IsFolder", item.IsFolder.ToString());
                writer.WriteElementString("IsVideo", item.IsVideo.ToString());
                writer.WriteElementString("IsMovie", item.IsMovie.ToString());
                writer.WriteElementString("Description", item.Description);
                if (item is CachedFolderItem || !String.IsNullOrEmpty(((FolderItem)item).ThumbPath))
                {
                    writer.WriteElementString("ThumbHash", item.ThumbHash);
                }
                writer.WriteElementString("Title1", item.Title1);
                writer.WriteElementString("Title2", item.Title2);
                writer.WriteElementString("Overview", item.Overview);
                if (item.IsMovie)
                {
                    writer.WriteElementString("IMDBRating", item.IMDBRating.ToString());
                    writer.WriteElementString("RunningTime", item.RunningTime.ToString());
                }
                if (item.IsMovie && item.Genres.Count > 0)
                {
                    writer.WriteStartElement("Genres");
                    foreach (var genre in item.Genres)
                    {
                        writer.WriteElementString("Genre", genre);
                    }
                    writer.WriteEndElement();
                }
                writer.WriteStartElement("CreatedDate");
                writer.WriteValue(item.CreatedDate);
                writer.WriteEndElement();
                writer.WriteStartElement("ModifiedDate");
                writer.WriteValue(item.ModifiedDate);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
            ms.Flush();

            File.WriteAllBytes(CacheXmlPath, ms.ToArray());

        }

        private string CacheXmlPath
        {
            get
            {
                return System.IO.Path.Combine(Helper.AppDataPath, string.Format("{0}.xml", CacheKey));
            }
        }

        private List<NonCommandFolderItem> GetNonCommandFolderDetails()
        {
            List<NonCommandFolderItem> rval; 
            if (_virtualFolder != null)
            {
                rval = new List<NonCommandFolderItem>(); 
                foreach (var item in _virtualFolder.Folders)
                {
                    rval.AddRange(GetNonCommandFolderDetails(item).ToArray());
                }
            }
            else
            {
                rval = GetNonCommandFolderDetails(_path);
            }
            return rval;
        }

        private void PopulateRealItemsForCache(object o)
        {
            try
            {
                _realItems = new List<FolderItem>();
                List<NonCommandFolderItem> items = (List<NonCommandFolderItem>)o;
                foreach (var item in items)
                {
                    _realItems.Add(item.Upgrade());
                }
            }
            finally
            {
                _populatedRealItems.Set();
            }
        }

        private void AddGenreAndRuntimeSort()
        {
            if (this.sortOrders.Count == 2)
            {
                sortOrders.Add("by genre");
                sortOrders.Add("by runtime");
                if (OnSortOrdersChanged != null) OnSortOrdersChanged();
            }
        }

        // used for genre stuff 
        internal void Navigate(List<IFolderItem> items)
        {
            lock (this)
            {
                Clear();
                AddRange(items.ToArray());
            }

            Sort(SortOrderEnum.Name);

            Changed();
        }
    }
    
    #region Folder Sorter

    class FolderItemSorter : IComparer<IFolderItem>
    {
        public FolderItemSorter(SortOrderEnum sortOrderEnum)
        {
            this.sortOrderEnum = sortOrderEnum;
        }

        SortOrderEnum sortOrderEnum; 

        #region IComparer<IFolderItem> Members

        public int Compare(IFolderItem x, IFolderItem y)
        {
            if (x is SpecialFolderItem && ! (y is SpecialFolderItem))
            {
                return -1; 
            }

            if (!(x is SpecialFolderItem) && y is SpecialFolderItem)
            {
                return 1;
            } 

            if (sortOrderEnum == SortOrderEnum.Name)
            {
                if (x.IsFolder && !(y.IsFolder))
                {
                    return -1;
                }

                if (!(x.IsFolder) && y.IsFolder)
                {
                    return 1;
                }

                return x.Description.CompareTo(y.Description);
            }
            else if (sortOrderEnum == SortOrderEnum.Date)
            {
                // reverse order for dates
                return y.CreatedDate.CompareTo(x.CreatedDate);
            }
            else if (sortOrderEnum == SortOrderEnum.RunTime)
            {
                int xval = x.RunningTime;
                if (xval <= 0) xval = 999999;
                int yval = y.RunningTime;
                if (yval <= 0) yval = 999999;
                return xval.CompareTo(yval);
            }
            else
            {
                // genre sort
                return x.Description.CompareTo(y.Description);
            }
        }

        #endregion
    }

    #endregion
}
