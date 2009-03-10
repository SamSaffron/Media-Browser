using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.LibraryManagement;
using System.IO;
using System.Diagnostics;
using System.Threading;
using MediaBrowser.Util;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Security.Cryptography;

namespace MediaBrowser.Library
{
    class ItemRepository : IItemRepository, IDisposable
    {
        public ItemRepository()
        {
            try
            {
                cacheMutex = Mutex.OpenExisting(MUTEX_NAME, MutexRights.Synchronize | MutexRights.Modify); 
            }
            catch
            {
                bool cn;
                MutexSecurity mSec = new MutexSecurity();

                SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                MutexAccessRule rule = new MutexAccessRule(sid, MutexRights.FullControl, AccessControlType.Allow);
                mSec.AddAccessRule(rule);
                cacheMutex = new Mutex(false, MUTEX_NAME, out cn, mSec);
            }
            LoadUniqueNameIndex();
            LoadSources();
            ThreadPool.QueueUserWorkItem(CacheExistsingMetadata);
        }
        
        private const string MUTEX_NAME = "MEDIABROWSER_DEFAULTCACHEPROVIDER";
        private Mutex cacheMutex; // provides cross process cache synchonization
        private string rootPath = Helper.AppCachePath;

        #region IItemCacheProvider Members
        private DateTime sourcesDate = DateTime.MinValue;
        private Dictionary<UniqueName, ItemSource> sources = new Dictionary<UniqueName, ItemSource>();
        private DateTime uniqueNamesDate = DateTime.MinValue;
        private Dictionary<string, UniqueName> uniqueNames = new Dictionary<string, UniqueName>();

        private class MutexWrapper : IDisposable
        {
            Mutex m;
            private FileStream fileLock = null;
            public MutexWrapper(Mutex m)
            {
                this.m = m;
                try
                {
                    m.WaitOne();
                }
                catch (AbandonedMutexException)
                { }
            }

            #region IDisposable Members

            public void Dispose()
            {
                if (fileLock != null)
                {
                    fileLock.Close();
                    fileLock.Dispose();
                    fileLock = null;
                }
                m.ReleaseMutex();
                GC.SuppressFinalize(this);
            }

            #endregion
        }

        private bool CentralisedSources
        {
            get { return false; }
        }

        private bool CentralisedImages
        {
            get { return false; }
        }

        private bool CentralisedMetadata
        {
            get { return false; }
        }

        private bool CentralisedDisplayPrefs
        {
            get { return false; }
        }

        private bool CentralisedChildren
        {
            get { return false; }
        }

        private bool CentralisedPlaystate
        {
            get { return Config.Instance.CentralisedCache != null; }
        }


        public void SaveSource(ItemSource source)
        {
            if ((source != null) && (source.UniqueName == null))
                return;
            using (new MutexWrapper(cacheMutex))
            {
                lock (this.sources)
                    if (!sources.ContainsKey(source.UniqueName))
                    {
                        sources.Add(source.UniqueName, source);
                        AppendSource(source.UniqueName, source);
                    }
                    else
                    {
                        sources[source.UniqueName] = source;
                        SaveSources();
                    }
            }
        }

        private void SaveSources()
        {
            using (new MutexWrapper(cacheMutex))
            {
                VerifySources();
                if (!Directory.Exists(rootPath))
                    Directory.CreateDirectory(rootPath);
                string file = Path.Combine(rootPath, "sources");
                using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    BinaryWriter bw = new BinaryWriter(fs);
                    lock (this.sources)
                    {
                        foreach (KeyValuePair<UniqueName, ItemSource> kv in sources)
                        {
                            bw.Write(kv.Key.Value);
                            kv.Value.WriteToStream(bw);
                        }
                        fs.Flush();
                        fs.Close();
                    }
                }
                sourcesDate = new FileInfo(file).LastWriteTimeUtc;
            }
        }

        private void AppendSource(UniqueName name, ItemSource source)
        {
            using (new MutexWrapper(cacheMutex))
            {
                VerifySources();
                if (!Directory.Exists(rootPath))
                    Directory.CreateDirectory(rootPath);
                string file = Path.Combine(rootPath, "sources");
                using (FileStream fs = new FileStream(file, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    BinaryWriter bw = new BinaryWriter(fs);
                    fs.Seek(0, SeekOrigin.End);
                    bw.Write(name.Value);
                    source.WriteToStream(bw);
                    fs.Flush();
                    fs.Close();
                }
                sourcesDate = new FileInfo(file).LastWriteTimeUtc;
            }
        }

        private bool VerifySources()
        {
            string file = Path.Combine(rootPath, "sources");
            if (this.sourcesDate != new FileInfo(file).LastWriteTimeUtc)
                using (new MutexWrapper(cacheMutex))
                {
                    LoadSources();
                    return true;
                }
            return false;
        }

        private void LoadSources()
        {
            using (new MutexWrapper(cacheMutex))
            {
                try
                {
                    Debug.WriteLine("Loading Sources");
                    lock (this.sources)
                    {
                        sources.Clear();
                        if (!Directory.Exists(rootPath))
                            Directory.CreateDirectory(rootPath);
                        string file = Path.Combine(rootPath, "sources");
                        if (File.Exists(file))
                        {
                            FileStream fs = null;
                            try
                            {
                                using (fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    BinaryReader br = new BinaryReader(fs);
                                    while (fs.Position < fs.Length)
                                    {
                                        UniqueName n = new UniqueName(br.ReadString());
                                        ItemSource itm = ItemSource.ReadFromStream(n, br);
                                        if (itm != null)
                                            sources[n] = itm;
                                    }
                                    fs.Close();
                                }
                                sourcesDate = new FileInfo(file).LastWriteTimeUtc;
                            }
                            catch
                            {
                                Trace.TraceError("Corrupt sources files, sources will be rediscovered but loading may be slower");
                                if (fs != null)
                                    fs.Close();
                                File.Delete(file);
                                SaveSources();
                            }
                        }
                    }
                    Debug.WriteLine("Sources loaded");
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error reading source.\n" + ex.ToString());
                }
            }
        }

        public void RemoveSource(ItemSource source)
        {
            if ((source != null) && (source.UniqueName == null))
                return;
            using (new MutexWrapper(cacheMutex))
            {
                lock (this.sources)
                    if (sources.ContainsKey(source.UniqueName))
                    {
                        sources.Remove(source.UniqueName);
                        SaveSources();
                        // todo we should cleanup the rest of the cache as well
                        // remove unique name
                        // delete metadata file
                        // delete prefs file
                        // delete playstate file
                        // should try and cleanup and images referenced by the metadata
                    }
            }

        }

        public Item Retrieve(UniqueName uniqueName)
        {
            if (uniqueName == null)
                return null;

            ItemSource source = null; 

            lock (this.sources)
            {
                if (sources.ContainsKey(uniqueName))
                {
                    source = sources[uniqueName];
                }
            } 

            if (source == null && VerifySources())
            {
                lock (this.sources)
                    if (sources.ContainsKey(uniqueName))
                        source = sources[uniqueName];
            }

            // it is critical to call construct item out of the lock. 
            // overwise you can get deadlocked against the mutex.
            return source==null?null:source.ConstructItem();
        }

        public ItemSource RetrieveSource(UniqueName uniqueName)
        {
            if (uniqueName == null)
                return null;
            lock (this.sources)
                if (sources.ContainsKey(uniqueName))
                    return sources[uniqueName];
            if (VerifySources())
                lock (this.sources)
                    if (sources.ContainsKey(uniqueName))
                        return sources[uniqueName];
            return null;
        }

        public Item[] RetrieveChildren(UniqueName ownerName)
        {
            if (ownerName == null)
                return null;
            string file = GetFile("children", ownerName);

            List<UniqueName> itemsToRetrieve = null;  
            
            using (new MutexWrapper(this.cacheMutex))
            {
                if (File.Exists(file))
                {
                    itemsToRetrieve = new List<UniqueName>();

                    
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        BinaryReader br = new BinaryReader(fs);
                        int count = br.ReadInt32();
                        for (int i = 0; i < count; ++i)
                        {
                            string n = br.SafeReadString();
                            if (n != null)
                            {
                                itemsToRetrieve.Add(new UniqueName(n)); 
                            }
                        }
                        fs.Close();
                    } 
                }
            }

            // item retrival can trigger the ui thread which can cause our mutex to hang  
            if (itemsToRetrieve != null)
            {
                var items = new List<Item>();
                foreach (var uniqueName in itemsToRetrieve)
                {
                    Item item = Retrieve(uniqueName);
                    if (item != null)
                        items.Add(item);
                }
                return items.ToArray();
            }
            else
            {
                return null;
            }
        }

        public void SaveChildren(UniqueName ownerName, List<Item> children)
        {
            if (ownerName == null)
                return;
            string file = GetFile("children", ownerName);
            using (new MutexWrapper(this.cacheMutex))
            {
                using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    BinaryWriter bw = new BinaryWriter(fs);
                    lock (children)
                    {
                        bw.Write(children.Count);
                        foreach (Item i in children)
                            bw.SafeWriteString(i.UniqueName == null ? null : i.UniqueName.Value);
                    }
                    fs.Close();
                }
            }
        }

        public MediaMetadataStore RetrieveMetadata(UniqueName ownerName)
        {
            if (ownerName == null)
                return null;
            byte[] bytes = null;
            lock (metadataCache)
            {
                if (metadataCache.ContainsKey(ownerName.Value))
                    return metadataCache[ownerName.Value];
                else
                {
                    string file = GetFile("metadata", ownerName);
                    using (new MutexWrapper(this.cacheMutex))
                    {
                        if (File.Exists(file))
                            bytes = File.ReadAllBytes(file);
                    }
                    if (bytes != null)
                    {
                        try
                        {
                            using (MemoryStream ms = new MemoryStream(bytes))
                            {
                                // need to do this outside of the mutex in case otherwise it can deadlock with the IncreaseBuffer call in the factory
                                // which needs to callback onto the UI thread which could be waiting on the mutex
                                MediaMetadataStore dp = MediaMetadataStore.ReadFromStream(ownerName, new BinaryReader(ms));
                                metadataCache[ownerName.Value] = dp;
                                return dp;
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error loading metadata.\n" + ex.ToString());
                            return null;
                        }
                    }
                }
            }

            return null;
        }

        Dictionary<string, MediaMetadataStore> metadataCache = new Dictionary<string, MediaMetadataStore>();
        private void CacheExistsingMetadata(object nothing)
        {
            //using (Profiler p = new Profiler())
            {
                string folder = Path.Combine(Helper.AppCachePath, "metadata");
                if (Directory.Exists(folder))
                {
                    string[] files = Directory.GetFiles(folder);
                    using (new MutexWrapper(this.cacheMutex))
                    {
                        foreach (string file in files)
                        {
                            string name = Path.GetFileName(file);
                            UniqueName ownerName = new UniqueName(name);
                            lock (metadataCache)
                            {
                                if (!metadataCache.ContainsKey(name))
                                {
                                    try
                                    {
                                        using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                                        {
                                            MediaMetadataStore dp = MediaMetadataStore.ReadFromStream(ownerName, new BinaryReader(fs));
                                            fs.Close();
                                            metadataCache[name] = dp;
                                        }
                                    }
                                    catch
                                    {
                                        try
                                        {
                                            Trace.WriteLine("Deleting corrupt metadata file " + file);
                                            File.Delete(file);
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public PlayState RetrievePlayState(UniqueName ownerName)
        {
            if (ownerName == null)
                return null;
            string file = GetFile("playstate", ownerName);
            byte[] bytes = null;
            using (new MutexWrapper(this.cacheMutex))
            {
                if (File.Exists(file))
                {
                    int attempt = 0;
                    while (attempt < 2)
                    {
                        try
                        {
                            attempt++;
                            bytes = File.ReadAllBytes(file);
                        }
                        catch (IOException ex)
                        {
                            Thread.Sleep(200);
                            Trace.TraceWarning("Failed to read " + file + "\n" + ex.ToString());
                        }
                    }
                }
            }
            if (bytes != null)
            {
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    // need to do this outside of the mutex in case otherwise it can deadlock with the IncreaseBuffer call in the factory
                    // which needs to callback onto the UI thread which could be waiting on the mutex
                    PlayState ps = PlayState.ReadFromStream(ownerName, new BinaryReader(ms));
                    return ps;
                }
            }
            return null;
        }

        public DisplayPreferences RetrieveDisplayPreferences(UniqueName ownerName)
        {
            if (ownerName == null)
                return null;
            string file = GetFile("display", ownerName);
            using (new MutexWrapper(this.cacheMutex))
            {
                if (File.Exists(file))
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        DisplayPreferences dp = DisplayPreferences.ReadFromStream(ownerName, new BinaryReader(fs));
                        fs.Close();
                        return dp;
                    }
                }
            }
            return null;
        }

        public void SaveMetadata(MediaMetadataStore metadata)
        {
            if (metadata.OwnerName == null)
                return;
            string file = GetFile("metadata", metadata.OwnerName);
            using (new MutexWrapper(this.cacheMutex))
            {
                using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    metadata.WriteToStream(new BinaryWriter(fs));
                    fs.Close();
                }
                lock (metadataCache)
                    metadataCache[metadata.OwnerName.Value] = metadata;
            }
        }

        public void SavePlayState(PlayState playState)
        {
            if (playState.OwnerName == null)
                return;
            string file = GetFile("playstate", playState.OwnerName);
            using (new MutexWrapper(this.cacheMutex))
            {
                try
                {
                    using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        playState.WriteToStream(new BinaryWriter(fs));
                        fs.Close();
                    }
                }
                catch (IOException ex)
                {
                    Trace.TraceWarning("Failed to write " + file + "\n" + ex.ToString());
                }
            }
        }

        public void SaveDisplayPreferences(DisplayPreferences prefs)
        {
            if (prefs.OwnerName == null)
                return;

            string file = GetFile("display", prefs.OwnerName);
            using (new MutexWrapper(this.cacheMutex))
            {
                using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    prefs.WriteToStream(new BinaryWriter(fs));
                    fs.Close();
                }
            }
        }

        public void CleanCache()
        {

        }

        public UniqueName GetUniqueName(string name, bool allowCreate)
        {
            lock (this.uniqueNames)
                if (uniqueNames.ContainsKey(name))
                    return uniqueNames[name];
            if (allowCreate)
            {
                using (new MutexWrapper(this.cacheMutex))
                {
                    VerifyUniqueNames();
                    lock (this.uniqueNames)
                        if (uniqueNames.ContainsKey(name))
                            return uniqueNames[name];
    
                    SHA1 newSHA1 = new SHA1CryptoServiceProvider();
                    UnicodeEncoding ByteMaker = new UnicodeEncoding();
                    StringBuilder Digest = new StringBuilder();

                    Byte[] ByteString = ByteMaker.GetBytes(name);
                    Byte[] SHA1Hash = newSHA1.ComputeHash(ByteString);
                    
                    foreach(byte nchar in SHA1Hash)
                        Digest.Append(nchar.ToString("x2"));

                    UniqueName n = new UniqueName(Digest.ToString());

                    uniqueNames[name] = n;
                    AppendUniqueName(name, n);
                    return n;
                }
            }
            else
                return null;            
        }

        public Dictionary<string, UniqueName> UniqueNames
        {
            get { return this.uniqueNames; }
        }

        private void SaveUniqueNameIndex()
        {
            using (new MutexWrapper(this.cacheMutex))
            {
                string p = rootPath;
                if (!Directory.Exists(p))
                    Directory.CreateDirectory(p);
                string file = Path.Combine(p, "uniqueNames");
                using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    BinaryWriter bw = new BinaryWriter(fs);
                    lock (this.uniqueNames)
                    {
                        foreach (KeyValuePair<string, UniqueName> kv in uniqueNames)
                        {
                            bw.Write(kv.Key);
                            bw.Write(kv.Value.Value);
                        }
                        fs.Flush();
                        fs.Close();
                    }
                }
                uniqueNamesDate = new FileInfo(file).LastWriteTimeUtc;
            }
        }

        private void AppendUniqueName(string key, UniqueName name)
        {
            using (new MutexWrapper(this.cacheMutex))
            {
                string p = rootPath;
                if (!Directory.Exists(p))
                    Directory.CreateDirectory(p);
                string file = Path.Combine(p, "uniqueNames");

                using (FileStream fs = new FileStream(file, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    BinaryWriter bw = new BinaryWriter(fs);
                    fs.Seek(0, SeekOrigin.End);
                    bw.Write(key);
                    bw.Write(name.Value);
                    fs.Flush();
                    fs.Close();
                }
                uniqueNamesDate = new FileInfo(file).LastWriteTimeUtc;
            }
        }

        private bool VerifyUniqueNames()
        {
            string p = rootPath;
            string file = Path.Combine(p, "uniqueNames");
            if (this.uniqueNamesDate != new FileInfo(file).LastWriteTimeUtc)
                using (new MutexWrapper(cacheMutex))
                {
                    LoadUniqueNameIndex();
                    return true;
                }
            return false;
        }

        private void LoadUniqueNameIndex()
        {
            Debug.WriteLine("Loading UniqueNames");
            using (new MutexWrapper(this.cacheMutex))
            {
                lock (this.uniqueNames)
                {
                    this.uniqueNames.Clear();
                    string p = rootPath;
                    if (!Directory.Exists(p))
                        Directory.CreateDirectory(p);
                    string file = Path.Combine(p, "uniqueNames");
                    if (File.Exists(file))
                    {
                        FileStream fs = null;
                        try
                        {
                            using (fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                BinaryReader br = new BinaryReader(fs);
                                while (fs.Position < fs.Length)
                                    this.uniqueNames[br.ReadString()] = new UniqueName(br.ReadString());
                                fs.Close();
                            }
                            uniqueNamesDate = new FileInfo(file).LastWriteTimeUtc;
                        }
                        catch (Exception)
                        {
                            Trace.TraceError("Corrupt uniquenames file, some entries may have been lost and this may lead to orphan data in the cache.");
                            if (fs != null)
                                fs.Close();
                            File.Delete(file);
                            SaveUniqueNameIndex();
                        }
                    }
                }
            }
            Debug.WriteLine("UniqueNames Loaded");
        }

        private string GetFile(string type, UniqueName ownerName)
        {
            string root = this.rootPath;
            switch (type)
            {
                case "playstate":
                    if (this.CentralisedPlaystate)
                        root = Config.Instance.CentralisedCache;
                    break;
            }
            string path = Path.Combine(root, type);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return Path.Combine(path, ownerName.Value);
        }



        public bool ClearEntireCache()
        {
            using (new MutexWrapper(this.cacheMutex))
            {
                bool success = true;
                success &= DeleteFolder(Path.Combine(Helper.AppCachePath, "metadata"));
                success &= DeleteFolder(Path.Combine(Helper.AppCachePath, "images"));
                success &= DeleteFolder(Path.Combine(Helper.AppCachePath, "autoplaylists"));
                success &= DeleteFolder(Path.Combine(Helper.AppCachePath, "children"));
                try
                {
                    lock (this.sources)
                    {
                        sources.Clear();
                        File.Delete(Path.Combine(Helper.AppCachePath, "sources"));
                    }
                }
                catch
                {
                    success = false;
                }
                return success;
            }
        }

        private bool DeleteFolder(string p)
        {
            try
            {
                Directory.Delete(p, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose() {
            if (cacheMutex != null) {
                cacheMutex.Close();
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
