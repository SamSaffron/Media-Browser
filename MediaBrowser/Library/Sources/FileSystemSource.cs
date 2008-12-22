using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using MediaBrowser.Library.Playables;
using MediaBrowser.LibraryManagement;
using System.Diagnostics;
using MediaBrowser.Util;

namespace MediaBrowser.Library.Sources
{
    class FileSystemSource : ItemSource, IDisposable
    {
        static readonly byte Version = 7;
        static readonly string[] ignore = { "metadata", ".metadata" };
        private string path;
        private UniqueName uniqueName;
        private ItemType itemType = ItemType.None;
        private FileSystemWatcher watcher;
        private DateTime createdDate = DateTime.MinValue;

        public FileSystemSource(UniqueName name)
        {
            this.uniqueName = name;
        }

        public FileSystemSource(string path, ItemType forcedType)
            : this(path)
        {
            this.itemType = forcedType;
        }

        public FileSystemSource(string path)
        {
            this.path = path;
            if (this.IsRoot)
            {
                ResolveItemType();
            }
            this.uniqueName = UniqueName.Fetch("FSS:" + path, true);
        }

       

        public override void PrepareToConstruct()
        {
            ResolveItemType();
			if (Helper.IsFolder(path))
                this.createdDate = GetDate(new DirectoryInfo(path));
            else
                this.createdDate = GetDate(new FileInfo(path));
        }

        public override void ValidateItemType()
        {
            //using (new Profiler(this.path))
            {
                if (ResolveItemType())
                {
                    ItemCache.Instance.SaveSource(this);
                    // would be useful to maybe trigger a metadata refresh here if the type of item has changed
                }
                base.ValidateItemType();
            }
        }

        public override bool IsPlayable
        {
            get
            {
                ItemType i = this.ItemType;
                return ((i == ItemType.Episode) || (i == ItemType.Movie));
            }
        }

        public override UniqueName UniqueName
        {
            get { return this.uniqueName; }
        }

        public override IEnumerable<ItemSource> ChildSources
        {
            get
            {
                if (Helper.IsFolder(path) && !IsPlayable)
                {
                    InitializeWatcher();
                    /* this slowed down verification ofr season folers dramatically, so for files we will stick with just getting names not full info back
                    FileSystemInfo[] infos = new DirectoryInfo(path).GetFileSystemInfos();
                    if (infos!=null)
                        foreach (FileSystemInfo f in infos)
                        {
                            if ((f.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                            {
                                ItemSource s = GetChildSource(f.FullName, Helper.IsFolder(f));
                                if (s != null)
                                    yield return s;
                            }
                        }
                    */
                    DirectoryInfo[] folders = new DirectoryInfo(path).GetDirectories();
                    if (folders != null)
                        foreach (DirectoryInfo f in folders)
                        {
                            if ((f.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                            {
                                ItemSource s = GetChildSource(f.FullName, true);
                                if (s != null)
                                    yield return s;
                            }
                        }
                    string[] files = Directory.GetFiles(path);
                    if (files != null)
                        foreach (string file in files)
                        {
                            ItemSource s = GetChildSource(file, false);
                            if (s != null)
                                yield return s;
                        }
                }
            }
        }

        private ItemSource GetChildSource(string path, bool isFolder)
        {
            //using (new Profiler(path))
            {
                if (isFolder)
                {
                    if (Array.IndexOf<string>(ignore, Path.GetFileName(path).ToLower()) < 0)
                        return new FileSystemSource(path);
                    else
                        return null;
                }
                else
                {
                    if (Helper.IsShortcut(path))
                        return new ShortcutSource(path);
                    else if (Array.IndexOf<string>(ignore, Path.GetFileName(path).ToLower()) < 0)
                    {
                        if (Helper.IsVirtualFolder(path))
                            return new VirtualFolderSource(path);
                        else if (Helper.IsVideo(path) || Helper.IsIso(path))
                            return new FileSystemSource(path);
                    }
                }
                return null;
            }
        }

        private ItemSource GetDeleteChildSource(string path)
        {
            if (Helper.IsShortcut(path))
                return new ShortcutSource(path);
            else if (Array.IndexOf<string>(ignore, Path.GetFileName(path).ToLower()) < 0)
            {
                if (Helper.IsVirtualFolder(path))
                    return new VirtualFolderSource(path);
                else
                {
                    UniqueName n = UniqueName.Fetch("FSS:" + path, false);
                    if (n != null)
                        return new DummyDeleteItemSource(n);
                }
            }
            return null;
        }

        public override string RawName
        {
            get
            {
                if (Helper.IsFolder(path))
                    return Path.GetFileName(path);
                else
                    return Path.GetFileNameWithoutExtension(path);
            }
        }
        public override DateTime CreatedDate
        {
            get
            {
                return createdDate;
            }
        }

        private static DateTime GetDate(string path)
        {
            return GetDate(new FileInfo(path));
        }

        private static DateTime GetDate(FileSystemInfo fi)
        {
            bool isFolder = Helper.IsFolder(fi);
            if (!isFolder && (!fi.Exists))
                return DateTime.Today;
            if (!isFolder)
                return fi.CreationTimeUtc;
            else
            {
                try
                {
                    DirectoryInfo di = fi as DirectoryInfo;
                    if (di == null)
                        di = new DirectoryInfo(fi.FullName);
                    
                    FileInfo[] files = di.GetFiles();
                    //string[] files = Directory.GetFiles(fi.FullName);
                    if ((files != null) && (files.Length > 0))
                    {
                        DateTime oldest = DateTime.MaxValue;
                        foreach (FileInfo f in files)
                        {
                            DateTime dt = f.CreationTimeUtc;
                            if (dt < oldest)
                                oldest = dt;
                        }
                        return oldest;
                    }
                    else
                        return fi.CreationTimeUtc; 
                }
                catch
                {
                    // can happen when a folder is being created
                    return DateTime.Now;
                }
            }
        }

        public override Item ConstructItem()
        {
            return ItemFactory.Instance.Create(this);
        }

        PlayableItem playable = null;

        internal override PlayableItem PlayableItem
        {
            get
            {
                if (!IsPlayable)
                    return null;
                if (!(File.Exists(path) || Directory.Exists(path)))
                    return null;
                if (playable == null)
                    lock (this)
                        if (playable == null)
                        {
                            if (PlayableVideoFile.CanPlay(this.path))
                                playable = new PlayableVideoFile(this.path);
                            else if (PlayableFolder.CanPlay(this.path))
                                playable = new PlayableFolder(this.path);
                            else if (PlayableIso.CanPlay(this.path))
                                playable = new PlayableIso(this.path);
                            else if (PlayableDvd.CanPlay(this.path))
                                playable = new PlayableDvd(this.path);
                            else
                            {
                                Trace.TraceError("There is no valid way of playing: " + this.path + " as itemtype is currently " + this.itemType.ToString() + " going to re-resolve item type");
                                ResolveItemType();
                                ItemCache.Instance.SaveSource(this);
                            }
                        }
                return playable;
            }
        }



        public override ItemType ItemType
        {
            get
            {
                if (itemType == ItemType.None)
                {
                    if (File.Exists(path) || Directory.Exists(path))
                    {
                        ResolveItemType();
                        ItemCache.Instance.SaveSource(this);
                    }
                }
                return itemType;
            }
        }

        private bool ResolveItemType()
        {
            // Debug.WriteLine("Resolving type of " + this.path);
            //using (Profiler prof = new Profiler(this.path))
            {
                bool isFolder = Helper.IsFolder(this.path);
                bool isVideo = (!isFolder) && Helper.IsVideo(path);
                ItemType old = this.itemType;

                if (IsRoot)
                    itemType = ItemType.Folder;
                else if (isFolder)
                {
                    if (Helper.IsSeasonFolder(path))
                        itemType = ItemType.Season;
                    else
                    {
                        string[] folders;
                        string[] files;

                        folders = Directory.GetDirectories(path);
                        files = Directory.GetFiles(path);
                        if (Helper.IsSeriesFolder(path, files, folders))
                            itemType = ItemType.Series;
                        else
                        {
                            int iso = Helper.IsoCount(path, files);
                            if (iso > 1)
                                itemType = ItemType.Folder;
                            else if (Helper.HasNoAutoPlaylistFile(path, files))
                                itemType = ItemType.Folder;
                            else if (iso == 1)
                                itemType = ItemType.Movie;
                            else if (Helper.IsDvDFolder(path, files, folders))
                                itemType = ItemType.Movie;
                            else if (Helper.ContainsSingleMovie(path, files, folders))
                                itemType = ItemType.Movie;
                            else if (files.Length + folders.Length > 0)
                                itemType = ItemType.Folder;
                            else
                                itemType = ItemType.Other; // we cannot determine what an empty folder might be or become

                        }
                    }
                }
                else
                {
                    if (Helper.IsEpisode(path))
                        itemType = ItemType.Episode;
                    else if (isVideo)
                        itemType = ItemType.Movie;
                    else if (Helper.IsIso(path))
                        itemType = ItemType.Movie;
                    else
                        itemType = ItemType.Other;
                }
				//Debug.WriteLine(this.path + " is item type: " + itemType.ToString());
                return itemType != old;
            }
        }

        public override string Location
        {
            get { return path; }
        }

        protected override void WriteStream(BinaryWriter bw)
        {
            bw.Write(Version);
            bw.SafeWriteString(this.path);
            bw.Write(this.itemType.ToString());
            bw.Write(this.createdDate.Ticks);
        }

        protected override void ReadStream(BinaryReader br)
        {
            byte v = br.ReadByte();
            this.path = br.SafeReadString();
            this.itemType = (ItemType)Enum.Parse(typeof(ItemType), br.ReadString());
            if (v > 5) 
                this.createdDate = new DateTime(br.ReadInt64());
            
            if ((v != Version) || (this.itemType == ItemType.Other)) // we were uncertain of what it was last time
                this.itemType = ItemType.None; // force reevaluation of what this is
            //Debug.WriteLine("Loaded " + this.Name + " as " + this.itemType.ToString());
        }

        private void InitializeWatcher()
        {
            if ((watcher == null) && (Config.Instance.EnableFileWatching))
            {
                lock (this.path)
                    if (watcher == null)
                    {
                        watcher = new FileSystemWatcher(this.path);
                        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
                        watcher.Created += new FileSystemEventHandler(watcher_Created);
                        watcher.Renamed += new RenamedEventHandler(watcher_Renamed);
                        watcher.Deleted += new FileSystemEventHandler(watcher_Deleted);
                        watcher.EnableRaisingEvents = true;
                    }
            }
        }

        private void CheckReResolveType()
        {
            if (itemType == ItemType.Movie || itemType == ItemType.Folder || itemType == ItemType.Other)
            {
                ValidateItemType();
            }
        }

        void watcher_Renamed(object sender, RenamedEventArgs e)
        {
            ItemSource s = GetDeleteChildSource(e.OldFullPath);

            if (s != null)
            {
                // need to potentially re-evaluate our type - we may become a movie, or we may cease to be recognised as a movie
                CheckReResolveType();
                FireRemoveItem(s);
            }
            if (File.Exists(e.FullPath) || Directory.Exists(e.FullPath))
            {
                s = GetChildSource(e.FullPath, Helper.IsFolder(e.FullPath));
                if (s != null)
                {
                    CheckReResolveType();
                    FireNewItem(s);
                }
            }
        }

        void watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            ItemSource s = GetDeleteChildSource(e.FullPath);

            if (s != null)
            {
                CheckReResolveType();
                FireRemoveItem(s);
            }
        }

        void watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath) || Directory.Exists(e.FullPath))
            {
                ItemSource s = GetChildSource(e.FullPath, Helper.IsFolder(e.FullPath));
                if (s != null)
                {
                    CheckReResolveType();
                    FireNewItem(s);
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
          /*  if (watcher != null)
            {
                watcher.Dispose();
                watcher = null;
            }
           */
            GC.SuppressFinalize(this);
        }

        ~FileSystemSource()
        {
            Dispose();
        }

        #endregion
    }
}
