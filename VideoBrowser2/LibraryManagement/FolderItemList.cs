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
using Microsoft.MediaCenter.UI;

namespace SamSoft.VideoBrowser.LibraryManagement
{

    public delegate void FolderItemListModifiedDelegate();
    public delegate void SortOrdersModifiedDelegate();

    public class FolderItemList : List<IFolderItem> , IList
    {
        public event FolderItemListModifiedDelegate OnChanged;
        public event SortOrdersModifiedDelegate OnSortOrdersChanged;
        static FolderItemList newVideos = new FolderItemList();
        private Choice sortOrders = new Choice();
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
            ArrayList al = new ArrayList();
            al.Add(SortOrderNames.GetName(SortOrderEnum.Name));
            al.Add(SortOrderNames.GetName(SortOrderEnum.Date));
            sortOrders.Options = al;
            sortOrders.ChosenChanged += new EventHandler(sortOrders_ChosenChanged);
            InvokeChanged();
        }

        public Choice SortOrders
        {
            get
            {
                return sortOrders;
            }
        }

        void sortOrders_ChosenChanged(object sender, EventArgs e)
        {
            this.Sort(SortOrderNames.GetEnum(sortOrders.Chosen.ToString()));
        }

        private void Changed()
        {
            if (OnChanged != null)
            {
                 OnChanged();
            }
        }

        private void AddMovieSortOptions()
        {
            if (this.sortOrders.Options.Count == 2)
            {
                sortOrders.Options.Add(SortOrderNames.GetName(SortOrderEnum.Genre));
                sortOrders.Options.Add(SortOrderNames.GetName(SortOrderEnum.RunTime));
                sortOrders.Options.Add(SortOrderNames.GetName(SortOrderEnum.ProductionYear));
                sortOrders.Options.Add(SortOrderNames.GetName(SortOrderEnum.Actor));
                sortOrders.Options.Add(SortOrderNames.GetName(SortOrderEnum.Director));
            }
        }

        internal void RefreshSortOrder()
        {
            // this is required as changed made to the list are not reflected in the UI and the changes need to happen on the UI thread
            sortOrders.ChosenChanged -= new EventHandler(sortOrders_ChosenChanged);
            ArrayList al = new ArrayList();
            al.AddRange(this.sortOrders.Options);
            object chosen = this.sortOrders.Chosen;
            this.sortOrders.Options = al;
            this.sortOrders.Chosen = chosen;
            sortOrders.ChosenChanged += new EventHandler(sortOrders_ChosenChanged);
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
                string xmlPath = System.IO.Path.Combine(Helper.AppCachePath, string.Format("{0}.xml", CacheKey));
                if (System.IO.File.Exists(xmlPath))
                {
                    // yay a cache hit. 
                    XmlDocument doc = new XmlDocument();
                    doc.Load(xmlPath);
                    var version = new System.Version(doc.SelectSingleNode("Items").Attributes["Version"].Value);
                    if (version == CachedFolderItem.Version)
                    {
                        var rval = new List<CachedFolderItem>();
                        foreach (XmlElement elem in doc.SelectNodes("Items/Item"))
                        {
                            rval.Add(new CachedFolderItem(elem, CacheKey, this));
                        }
                        return rval;
                    }  
                }
            }
            catch
            {
                // cache failure
                Trace.TraceInformation("Cache loading failed !!! (GetCache)");
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
                        if (this.Prefs != null)
                        {
                            AddRange(GetFolderDetails(item, this.Prefs.Banners).ToArray());
                        }
                        else
                        {
                            AddRange(GetFolderDetails(item, false).ToArray());
                        }
                    }
                }
            }

            
            Sort(Prefs.SortOrder);

            InvokeChanged();
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
                        if (this.Prefs != null)
                        {
                            AddRange(GetFolderDetails(path, this.Prefs.Banners).ToArray());
                        }
                        else
                        {
                            AddRange(GetFolderDetails(path, false).ToArray());
                        }
                    }
                }
            }
 
            Sort(Prefs.SortOrder);
            InvokeChanged();
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
            public bool useBanners;

            public NonCommandFolderItem(string filename, bool isFolder, bool useBanners)
            : this (filename, isFolder, 
                isFolder ? 
                    System.IO.Path.GetFileName(filename) : 
                    System.IO.Path.GetFileNameWithoutExtension(filename), useBanners)
            {
            }


            public NonCommandFolderItem(string filename, bool isFolder, string description, bool useBanners)
            {
                this.filename = filename;
                this.isFolder = isFolder;
                this.description = description;
                this.useBanners = useBanners;
            }

            public FolderItem Upgrade()
            {
                FolderItem fi = new FolderItem(filename, isFolder, description, useBanners);
                if (ThumbPath != null)
                {
                    fi.ThumbPath = ThumbPath;
                    fi.BannerPath = ThumbPath;
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

        private static List<FolderItem> GetFolderDetails(string path, bool useBanners)
        {
            List<FolderItem> rval = new List<FolderItem>();
            var items = GetNonCommandFolderDetails(path, useBanners);
            foreach (var item in items)
            {
                rval.Add(item.Upgrade()); 
            }
            return rval;
        }

        private static List<NonCommandFolderItem> GetNonCommandFolderDetails(string path, bool useBanners)
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

                    rval.Add(new NonCommandFolderItem(filename, true, useBanners));
                }

                Dictionary<string, NonCommandFolderItem> thumbMap = new Dictionary<string, NonCommandFolderItem>(); 

                string[] files = Directory.GetFiles(path); 
                foreach (string filename in files)
                {
                    if (Helper.IsVirtualFolder(filename))
                    {
                        // extract the thumb for this virtual folder
                        VirtualFolder vf = new VirtualFolder(filename);
                        NonCommandFolderItem fi = new NonCommandFolderItem(filename, true, System.IO.Path.GetFileNameWithoutExtension(filename), false);
                        fi.VirtualFolder = vf;
                        rval.Add(fi); 
                    }
                    if (Helper.IsShortcut(filename))
                    {
                        var shortcut_path = Helper.ResolveShortcut(filename);
                        if (Directory.Exists(shortcut_path))
                        {
                            NonCommandFolderItem fi = new NonCommandFolderItem(shortcut_path, true, System.IO.Path.GetFileNameWithoutExtension(filename), false);
                            fi.Path = path;
                            rval.Add(fi);
                        }
                        else
                        {
                            // it may be a shortcut to a file 
                            if (Helper.IsVideo(shortcut_path))
                            {
                                // refactor
                                NonCommandFolderItem item = new NonCommandFolderItem(shortcut_path, false, false);
                                rval.Add(item);
                                thumbMap[System.IO.Path.GetFileNameWithoutExtension(shortcut_path)] = item;
                            }
                        } 
                    }

                    if (Helper.IsVideo(filename))
                    {
                        NonCommandFolderItem item = new NonCommandFolderItem(filename, false, false);
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
                Trace.TraceInformation("Missing Dir: (Bad shortcut)" + path);
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

            InvokeChanged();
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

        public List<BaseFolderItem> nonDrilldownList;

        //public SortOrderEnum SortOrder { get; set; } 

        public void Sort(SortOrderEnum sortOrderEnum)
        {
            Trace.TraceInformation("Sort was called with " + sortOrderEnum.ToString() + " for " + this.Path); 

            lock (this)
            {
                
                //SortOrder = sortOrderEnum;

                // do not attempt to cache if we have navigated directly to a list of items (in genere case)
                if (CacheKey != null)
                {
                    Prefs.SortOrder = sortOrderEnum;
                    Prefs.Save();
                }

                if (sortOrderEnum == SortOrderEnum.Genre 
                    || sortOrderEnum == SortOrderEnum.RunTime 
                    || sortOrderEnum == SortOrderEnum.ProductionYear 
                    || sortOrderEnum == SortOrderEnum.Actor
                    || sortOrderEnum == SortOrderEnum.Director)
                {
                    AddMovieSortOptions();
                }
                sortOrders.ChosenIndex = sortOrders.Options.IndexOf(SortOrderNames.GetName(sortOrderEnum));
                sortOrders.DefaultIndex = sortOrders.ChosenIndex;

                if (sortOrderEnum == SortOrderEnum.Genre)
                {
                    GenreDrilldown();
                    this.Sort(new FolderItemSorter(sortOrderEnum));
                }
                else if (sortOrderEnum == SortOrderEnum.ProductionYear)
                {
                    ProductionYearDrilldown();
                    this.Sort(new FolderItemSorter(sortOrderEnum));
                }
                else if (sortOrderEnum == SortOrderEnum.Director)
                {
                    DirectorDrilldown();
                    this.Sort(new FolderItemSorter(sortOrderEnum));
                }
                else if (sortOrderEnum == SortOrderEnum.Actor)
                {
                    ActorDrilldown();
                    this.Sort(new FolderItemSorter(sortOrderEnum));
                }
                else
                {
                    if (nonDrilldownList != null)
                    {
                        this.Clear();
                        foreach (BaseFolderItem item in nonDrilldownList)
                        {
                            this.Add(item);
                        }
                        nonDrilldownList = null;
                    }

                    this.Sort(new FolderItemSorter(sortOrderEnum));
                }

            }

            InvokeChanged();
        }

        private void ProductionYearDrilldown()
        {
            nonDrilldownList = ActualItems;
            this.Clear();
            var productionYearRange = new Dictionary<string, List<IFolderItem>>();

            foreach (var item in nonDrilldownList)
            {
                
                if (!item.IsMovie || item.ProductionYear < 1900)
                {
                    if (!productionYearRange.ContainsKey("Unknown"))
                    {
                        productionYearRange["Unknown"] = new List<IFolderItem>();
                    }
                    productionYearRange["Unknown"].Add(item);

                }
                else 
                {
                    if (!productionYearRange.ContainsKey(item.ProductionYear.ToString()))
                    {
                        productionYearRange[item.ProductionYear.ToString()] = new List<IFolderItem>(); 
                    }
                    productionYearRange[item.ProductionYear.ToString()].Add(item); 
                }
            }

            AddDrildownItems(productionYearRange, "ProductionYearImages");
        }

        private void AddDrildownItems(Dictionary<string, List<IFolderItem>> items, string image_location)
        {
            foreach (var item in items)
            {
                FolderItem fi = new FolderItem(FolderItem.DUMMY_DIR, true, item.Key, false);
                fi.Contents = item.Value;
                var movieText = " movie";
                if (item.Value.Count > 1)
                {
                    movieText = " movies";
                }
                fi.SetTitle2(item.Value.Count.ToString() + movieText);
                fi.SetOverview(string.Format("Including: {0}", Helper.GetRandomNames(item.Value, 200)));
                if (item.Key.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0)
                {
                    string thumbPath = System.IO.Path.Combine(System.IO.Path.Combine(Helper.AppConfigPath, image_location), item.Key + ".jpg");
                    if (File.Exists(thumbPath))
                    {
                        fi.ThumbPath = thumbPath;
                    }
                }
                this.Add(fi);
            }
        }

        private void ActorDrilldown()
        {
            nonDrilldownList = ActualItems;
            var actors = new Dictionary<string, List<IFolderItem>>();
            this.Clear();
            foreach (var item in nonDrilldownList)
            {

                if (item.Actors.Count > 0)
                {
                    foreach (var actor in item.Actors)
                    {
                        if (!actors.ContainsKey(actor))
                        {
                            actors[actor] = new List<IFolderItem>();
                        }
                        actors[actor].Add(item);
                    }
                }
            }

            AddDrildownItems(actors, "ActorImages");
        }

        private void DirectorDrilldown()
        {
            nonDrilldownList = ActualItems;
            var directors = new Dictionary<string, List<IFolderItem>>();
            this.Clear();
            foreach (var item in nonDrilldownList)
            {

                if (item.Directors.Count > 0)
                {
                    foreach (var director in item.Directors)
                    {
                        if (!directors.ContainsKey(director))
                        {
                            directors[director] = new List<IFolderItem>();
                        }
                        directors[director].Add(item);
                    }
                }
            }

            AddDrildownItems(directors, "DirectorImages");
        }

        private void GenreDrilldown()
        {
            nonDrilldownList = ActualItems;
            var generes = new Dictionary<string, List<IFolderItem>>();
            this.Clear();
            foreach (var item in nonDrilldownList)
            {
               
                if (!item.IsMovie || item.Genres.Count == 0)
                {
                    if (!generes.ContainsKey("Other"))
                    {
                        generes["Other"] = new List<IFolderItem>();
                    }
                    generes["Other"].Add(item);

                }
                else
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

            AddDrildownItems(generes, "GenreImages");
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
                    if (this.Prefs != null && this.Prefs.Banners)
                    {
                        if (!string.IsNullOrEmpty(item.BannerPath))
                        {
                            System.Drawing.Image image = new Bitmap(item.BannerPath);
                            return ((float)image.Height) / ((float)image.Width);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(item.ThumbPath))
                        {
                            System.Drawing.Image image = new Bitmap(item.ThumbPath);
                            return ((float)image.Height) / ((float)image.Width);
                        }
                    }
                }
                return 1; 
            }
        }

        

        // returns a copy of the actual list of items in the folder, regardless on if we are drilling down
        private List<BaseFolderItem> ActualItems
        {
            get 
            {
                List<BaseFolderItem> items = new List<BaseFolderItem>(); 
                lock (this)
                {
                    if (nonDrilldownList != null)
                    {
                        foreach (var item in nonDrilldownList)
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
            if (nonDrilldownList != null)
            {
                nonDrilldownList.Add(item);
                // TODO : refresh genre list
            }
            else
            {
                this.Add(item);
            }
        }

        private void RemoveItem(BaseFolderItem item)
        {
            if (nonDrilldownList != null)
            {
                nonDrilldownList.Remove(item);
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

                int minMoviesToShowMovieSortOrders = (int)(0.3 * this.Count);
                // unless we have no movies at all 
                if (minMoviesToShowMovieSortOrders == 0)
                {
                    minMoviesToShowMovieSortOrders = 1;
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

                        if (!updatedSortOptions && moviesWithMetadata >= minMoviesToShowMovieSortOrders)
                        {
                            AddMovieSortOptions();
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

                        if (!updatedSortOptions && moviesWithMetadata >= minMoviesToShowMovieSortOrders)
                        {
                            AddMovieSortOptions();
                            updatedSortOptions = true;
                        }

                        itemsToCache[item.Filename] = item;
                    }
                }

                if (!cache_changed)
                {
                    return;
                }
          
                this.Sort(this.Prefs.SortOrder);

                CacheImages();
                CacheFolderXml(itemsToCache);

            }
            catch(Exception e)
            { 
                // forget about it its only the cache (could be collection modified cause we did a sort)
                Trace.TraceInformation("Caching failed!!! " + e.ToString()); 
            }
        }

        public void InvokeChanged()
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
            string imagePath = System.IO.Path.Combine(Helper.AppCachePath, CacheKey);

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
                    if (!String.IsNullOrEmpty(item.BannerPath))
                    {
                        string key = item.BannerHash;

                        if (!cachedImages.ContainsKey(key))
                        {
                            System.IO.File.Copy(item.BannerPath, System.IO.Path.Combine(imagePath, key));
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
                    if (!String.IsNullOrEmpty(ci.BannerPath))
                    {
                        string key = ci.BannerHash;
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
            CachedFolderItem.Write(CacheXmlPath, itemsToCache.Values); 
        }

        private string CacheXmlPath
        {
            get
            {
                return System.IO.Path.Combine(Helper.AppCachePath, string.Format("{0}.xml", CacheKey));
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
                    if (this.Prefs != null)
                    {
                        rval.AddRange(GetNonCommandFolderDetails(item, this.Prefs.Banners).ToArray());
                    }
                    else
                    {
                        rval.AddRange(GetNonCommandFolderDetails(item, false).ToArray());
                    }
                }
            }
            else
            {
                if (this.Prefs != null)
                {
                    rval = GetNonCommandFolderDetails(_path, this.Prefs.Banners);
                }
                else
                {
                    rval = GetNonCommandFolderDetails(_path, false);
                }
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

       



        // used for genre stuff 
        internal void Navigate(List<IFolderItem> items)
        {
            lock (this)
            {
                Clear();
                AddRange(items.ToArray());
            }

            Sort(SortOrderEnum.Name);

            InvokeChanged();
        }

        
    }
    
   

  

 }
