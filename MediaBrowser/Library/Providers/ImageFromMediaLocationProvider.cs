using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MediaBrowser.Library.Providers
{
    class ImageFromMediaLocationProvider : IMetadataProvider
    {
        
        #region IMetadataProvider Members

        public virtual ItemType SupportedTypes
        {
            get { return ItemType.Movie | ItemType.Series | ItemType.Folder | ItemType.Season | ItemType.Episode; }
        }

        protected virtual string ProviderName
        {
            get { return "ImageFromMediaLocation"; }
        }

        protected virtual string GetLocation(Item item)
        {
            return item.Source.Location;
        }

        public bool UsesInternet { get { return false; } }

        public void Fetch(Item item, ItemType type, MediaMetadataStore store, bool fastOnly)
        {
            string location = GetLocation(item);
            if (Directory.Exists(location))
            {
                if ((File.GetAttributes(location) & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    string file;
                    if (store.PrimaryImage == null)
                    {
                        file = GetImage(store, location, "folder");
                        if (file != null)
                            store.PrimaryImage = new ImageSource { OriginalSource = file };
                    }
                    if (store.BannerImage == null)
                    {
                        file = GetImage(store, location, "banner");
                        if (file != null)
                            store.BannerImage = new ImageSource { OriginalSource = file };
                    }
                    if (store.BackdropImage == null)
                    {
                        file = GetImage(store, location, "backdrop");
                        if (file != null)
                            store.BackdropImage = new ImageSource { OriginalSource = file };
                    }
                }
            }
            else if (File.Exists(location))
            {
                if (store.PrimaryImage == null)
                {
                    string file = GetImage(store, location, "folder");
                    if (file != null)
                        store.PrimaryImage = new ImageSource { OriginalSource = file };
                }
            }
        }

        private string GetImage(MediaMetadataStore store, string location, string name)
        {
            string file = FindImage(location, name);
            if (file != null)
            {
                store.ProviderData[ProviderName + ":" + name] = file;
                store.ProviderData[ProviderName + ":" + name + ":mod"] = new FileInfo(file).LastWriteTimeUtc.Ticks.ToString();
            }
            return file;
        }

        private string FindImage(string location, string name)
        {
            string file = Path.Combine(location, name + ".jpg");
            if (File.Exists(file))
                return file;
            
            file = Path.Combine(location, name + ".png");
            if (File.Exists(file))
                return file;

            if (name == "folder") // we will also look for images that match by name in the same location for the primary image
            {
                var dir = Path.GetDirectoryName(location);
                var filename_without_extension = Path.GetFileNameWithoutExtension(location);

                // dir was null for \\10.0.0.4\dvds - workaround
                if (dir != null && filename_without_extension != null)
                {
                    file = Path.Combine(dir, filename_without_extension);
                    if (File.Exists(file + ".jpg"))
                        return file + ".jpg";
                    if (File.Exists(file + ".png"))
                        return file + ".png";
                }
            }
            return null;
        }

        private bool CheckImage(Dictionary<string, string> store, ImageSource current, string location, string name)
        {
            if (store.ContainsKey(ProviderName + ":" + name))
            {
                string lastFile = store[ProviderName + ":" + name];
                if (current.OriginalSource != lastFile)
                    return false; // we are currently overridden by another provider anyway
                if (!File.Exists(lastFile))
                    return true;
                string file = FindImage(location, name);
                if (lastFile != file)
                    return true;
                /* this is checked by the LibraryImage class, no need to cause a full metadata refresh
                DateTime dt = new DateTime(long.Parse(store[ProviderName + ":" + name + ":mod"]));
                if (dt != new FileInfo(file).LastWriteTimeUtc)
                    return true;
                 * */
            }
            else
            {
                if ((current==null) && (FindImage(location, name) != null))
                    return true;
            }
            return false;
        }

        public bool NeedsRefresh(Item item, ItemType type)
        {
            string location = GetLocation(item);
            
            if (CheckImage(item.Metadata.ProviderData, item.Metadata.PrimaryImageSource, location, "folder"))
                return true;
            if (CheckImage(item.Metadata.ProviderData, item.Metadata.BannerImageSource, location, "banner"))
                return true;
            if (CheckImage(item.Metadata.ProviderData, item.Metadata.BackdropImageSource, location, "backdrop"))
                return true;
            return false;
        }

        #endregion
    }
}
