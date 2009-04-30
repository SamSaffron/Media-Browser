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
using System.Linq;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Util;
using System.Reflection;
using System.Drawing;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Configuration;

namespace MediaBrowser.Library {
    class ItemRepository : IItemRepository, IDisposable {
        public ItemRepository() {
            playbackStatus = new FileBasedDictionary<PlaybackStatus>(GetPath("playstate", userSettingPath));
        }

        string rootPath = ApplicationPaths.AppCachePath;
        string userSettingPath = ApplicationPaths.AppUserSettingsPath;

        FileBasedDictionary<PlaybackStatus> playbackStatus;


        

        #region IItemCacheProvider Members

        public void SaveChildren(Guid id, IEnumerable<Guid> children) {
            string file = GetChildrenFilename(id);

            Guid[] childrenCopy;
            lock (children) {
                childrenCopy = children.ToArray();
            }

            using (Stream fs = WriteExclusiveFileStream(file)) {
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(childrenCopy.Length);
                foreach (var guid in childrenCopy) {
                    bw.Write(guid);
                }
            }

        }

        public IEnumerable<Guid> RetrieveChildren(Guid id) {

            List<Guid> children = new List<Guid>();
            string file = GetChildrenFilename(id);
            if (!File.Exists(file)) return null;

            try {

                using (Stream fs = ReadFileStream(file)) {
                    BinaryReader br = new BinaryReader(fs);
                    lock (children) {
                        var count = br.ReadInt32();
                        var itemsRead = 0;
                        while (itemsRead < count) {
                            children.Add(br.ReadGuid());
                            itemsRead++;
                        }
                    }
                }
            } catch (Exception e) {
                Application.Logger.ReportException("Failed to retrieve children:", e);
#if DEBUG
                throw;
#else 
                return null;
#endif

            }

            return children.Count == 0 ? null : children;
        }


        public PlaybackStatus RetrievePlayState(Guid id) {
            return playbackStatus[id]; 
        }

        public DisplayPreferences RetrieveDisplayPreferences(Guid id) {
            string file = GetDisplayPrefsFile(id);

            if (File.Exists(file)) {
                using (Stream fs = ReadFileStream(file)) {
                    DisplayPreferences dp = DisplayPreferences.ReadFromStream(id, new BinaryReader(fs));
                    return dp;
                }
            } 

            return null;
        }

        public void SavePlayState(PlaybackStatus playState) {
            playbackStatus[playState.Id] = playState;
        }

        public void SaveDisplayPreferences(DisplayPreferences prefs) {
            string file = GetDisplayPrefsFile(prefs.Id);
            using (Stream fs = WriteExclusiveFileStream(file)) {
                prefs.WriteToStream(new BinaryWriter(fs));
            }
        }

        public BaseItem RetrieveItem(Guid id) {
            BaseItem item = null;
            string file = GetItemFilename(id);

            try {
                using (Stream fs = ReadFileStream(file)) {
                    BinaryReader reader = new BinaryReader(fs);
                    item = Serializer.Deserialize<BaseItem>(fs);
                }
            } catch (FileNotFoundException) { 
                // we expect to be called with unknown items sometimes
            }

            return item;
        }

        public void SaveItem(BaseItem item) {
            string file = GetItemFilename(item.Id);
            using (Stream fs = WriteExclusiveFileStream(file)) {
                BinaryWriter bw = new BinaryWriter(fs);
                Serializer.Serialize(bw.BaseStream, item);
            }
        }


        // TODO implement IEnumerable serialization
        class MetadataProviderSearilizationWrapper {
            [Persist]
            public List<IMetadataProvider> Providers {get; set;}
        }

        public IEnumerable<IMetadataProvider> RetrieveProviders(Guid guid) {
            MetadataProviderSearilizationWrapper data = null;
            string file = GetProviderFilename(guid);

            try {
                using (Stream fs = ReadFileStream(file)) {
                    BinaryReader reader = new BinaryReader(fs);
                    data = (MetadataProviderSearilizationWrapper)Serializer.Deserialize<object>(fs);
                }
            } catch (FileNotFoundException) { return null; }

            return data.Providers;
        }

        public void SaveProviders(Guid guid, IEnumerable<IMetadataProvider> providers) {
            string file = GetProviderFilename(guid);
            using (Stream fs = WriteExclusiveFileStream(file)) {
                BinaryWriter bw = new BinaryWriter(fs);
                Serializer.Serialize<object>(bw.BaseStream,
                    new MetadataProviderSearilizationWrapper() { Providers = providers.ToList() });
            }
        }


        private static Stream WriteExclusiveFileStream(string file) {
            return ProtectedFileStream.OpenExclusiveWriter(file);
        }

        private static Stream ReadFileStream(string file) {
            return ProtectedFileStream.OpenSharedReader(file);
        }

        public void CleanCache() {

        }

        private string GetChildrenFilename(Guid id) {
            return GetFile("children", id);
        }

        private string GetItemFilename(Guid id) {
            return GetFile("items", id);
        }

        private string GetProviderFilename(Guid id) {
            return GetFile("providerdata", id);
        }

        private string GetDisplayPrefsFile(Guid id) {
            return GetFile("display", id, this.userSettingPath);
        }

        private string GetPlaystateFile(Guid id) {
            return GetFile("playstate", id, this.userSettingPath);
        }


        private string GetFile(string type, Guid id) {
            return GetFile(type, id, rootPath);
        }


        private string GetFile(string type, Guid id, string root) {

            return Path.Combine(GetPath(type,root), id.ToString("N"));
        }

        private string GetPath(string type, string root) {
            string path = Path.Combine(root, type);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }


        public bool ClearEntireCache() {
            bool success = true;
            
            // we are going to need a semaphore here.
            //lock (ProtectedFileStream.GlobalLock) {
                success &= DeleteFolder(Path.Combine(ApplicationPaths.AppCachePath, "items"));
                success &= DeleteFolder(Path.Combine(ApplicationPaths.AppCachePath, "providerdata"));
                success &= DeleteFolder(Path.Combine(ApplicationPaths.AppCachePath, "images"));
                success &= DeleteFolder(Path.Combine(ApplicationPaths.AppCachePath, "autoplaylists"));
                success &= DeleteFolder(Path.Combine(ApplicationPaths.AppCachePath, "children"));
            //}
            return success;
        }

        private bool DeleteFolder(string p) {
            try {
                Directory.Delete(p, true);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose() {
            GC.SuppressFinalize(this);
        }

        #endregion




    }
}
