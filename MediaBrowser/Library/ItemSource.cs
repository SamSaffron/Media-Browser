using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MediaBrowser.Library.Sources;
using MediaBrowser.LibraryManagement;

namespace MediaBrowser.Library
{
    public delegate void NewItemHandler(ItemSource newItem);
    public delegate void RemoveItemHandler(ItemSource removeItem);
    public abstract class ItemSource
    {
        public abstract bool IsPlayable { get;}
        public abstract UniqueName UniqueName { get;}
        public abstract IEnumerable<ItemSource> ChildSources { get; }
        public abstract string RawName { get; }
        /// <summary>
        /// The name with and "comment", i.e. pieces enclosed by [ ] removed
        /// </summary>
        public virtual string Name { get { return Helper.RemoveCommentsFromName(this.RawName); } }
        public abstract string Location { get;}
        public abstract DateTime CreatedDate { get; }
        internal abstract PlayableItem PlayableItem { get; }
        public abstract ItemType ItemType { get; }
        public abstract Item ConstructItem();
        public event NewItemHandler NewItem;
        public event RemoveItemHandler RemoveItem;
        public string ItemTypeString { get { return this.ItemType.ToString(); } }
        public string ItemMediaTypeString { get { return MediaType(this.Location); } }

        /// <summary>
        /// Does any caching not requiredfor item verification but that should be done async before
        /// we construct the item for the first time and also before we save this source for the first time
        /// </summary>
        public virtual void PrepareToConstruct()
        {
        }

        public virtual void ValidateItemType()
        {

        }
        bool isRootInitialized = false; 
        bool isRoot; 
        public bool IsRoot { 
            get 
            { 
                if (!isRootInitialized)
                {
                    isRoot = Helper.IsRoot(this.Location);
                    isRootInitialized = true;
                }
                
                return isRoot;
            }  
        }


        protected void FireRemoveItem(ItemSource item)
        {
            if (this.RemoveItem != null)
                this.RemoveItem(item);
        }

        protected void FireNewItem(ItemSource item)
        {
            if (this.NewItem != null)
                this.NewItem(item);
        }

        public void WriteToStream(BinaryWriter bw)
        {
            if (this is FileSystemSource)
                bw.Write("FSS");
            else if (this is VirtualFolderSource)
                bw.Write("VFS");
            else if (this is ShortcutSource)
                bw.Write("SCS");
            else
                throw new NotSupportedException("ItemSource of type " + this.GetType().ToString() + " cannot be saved");
            this.WriteStream(bw);
        }

        public static ItemSource ReadFromStream(UniqueName name,  BinaryReader br)
        {
            ItemSource source;
            string type = br.ReadString();
            switch (type)
            {
                case "FSS":
                    source = new FileSystemSource(name);
                    break;
                case "VFS":
                    source = new VirtualFolderSource(name);
                    break;
                case "SCS":
                    source = new ShortcutSource(name);
                    break;
                default:
                    return null;
            }
            
            source.ReadStream(br);
            return source;
        }

        protected abstract void WriteStream(BinaryWriter bw);
        protected abstract void ReadStream( BinaryReader br);


        //HACK: This is a real hack job in order to get the Media Type of an item.
        // It looks at the first file in the directory path, and uses that as the
        // basis for the Item's MediaType.
        private static string MediaType(string path)
        {
            
            if (path==null)
                return "unknown";
            try
            {
                path = path.ToLower();
                DirectoryInfo di = new DirectoryInfo(path);
                FileInfo[] fi = di.GetFiles();
                path = fi[0].ToString().ToLower();
            }
            catch(Exception)
            {
                return "unknown";
            }
            if (path.Contains("video_ts"))
                return "DVD";
            if (path.EndsWith(".avi"))
                return "Avi";
            if (path.EndsWith(".mpg"))
                return "Mpg";
            if (path.EndsWith(".mkv"))
                return "Mkv";
            if (path.Contains("bdmv"))
                return "BluRay";
            if (path.Contains("hvdvd_ts"))
                return "HDDVD";
            if (Directory.Exists(Path.Combine(path, "VIDEO_TS")))
                return "DVD";
            if (Directory.Exists(Path.Combine(path, "BDMV")))
                return "BluRay";
            if (Directory.Exists(Path.Combine(path, "HVDVD_TS")))
                return "HDDVD";
           
            return "unknown";
        }
    }
}
