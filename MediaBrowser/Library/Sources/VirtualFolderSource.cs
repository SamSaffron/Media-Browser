using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace MediaBrowser.Library.Sources
{
    public class VirtualFolderSource : ItemSource
    {
        private static readonly byte Version = 2;
        UniqueName uniqueName;
        string vfFile;
        List<string> paths = new List<string>();
        string thumbPath = null;
        DateTime createdDate = DateTime.MinValue;
        List<FileSystemSource> sources = new List<FileSystemSource>();

        public VirtualFolderSource(UniqueName name)
        {
            this.uniqueName = name;
        }

        public VirtualFolderSource(string vfFile)
        {
            this.vfFile = vfFile;
            this.uniqueName = UniqueName.Fetch("VFF:" + vfFile, true);
            LoadVfFile(vfFile);
        }

        private void LoadVfFile(string vfFile)
        {
            var fileInfo = new FileInfo(this.vfFile);
            DateTime dt = fileInfo.LastWriteTimeUtc;
            if (createdDate != dt)
            {
                createdDate = dt;
                lock (sources)
                {
                    List<string> foundPaths = new List<string>();
                    foreach (var line in File.ReadAllLines(vfFile))
                    {
                        if (line.StartsWith("image:"))
                        {
                            // TODO: test if it is a valid image
                            string thumbPath = line.Substring(6).Trim();
                            if ((thumbPath.StartsWith(@".\")) || (thumbPath.StartsWith(@"..\")))
                            {
                                thumbPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(vfFile), thumbPath);
                                thumbPath = System.IO.Path.GetFullPath(thumbPath);
                            }
                            if (!IsValidPath(thumbPath) || !File.Exists(thumbPath))
                            {
                                Application.DialogBoxViaReflection("Invalid virtual folder thumbnail path: " + thumbPath);
                            }
                            else
                            {
                                this.thumbPath = thumbPath;
                            }
                        }
                        else if (line.StartsWith("folder:"))
                        {
                            string folderPath = line.Substring(7).Trim();
                            if ((folderPath.StartsWith(@".\")) || (folderPath.StartsWith(@"..\")))
                            {
                                folderPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(vfFile), folderPath);
                                folderPath = System.IO.Path.GetFullPath(folderPath);
                            }

                            if (!IsValidPath(folderPath) || !Directory.Exists(folderPath))
                            {
                                Application.DialogBoxViaReflection("Invalid virtual folder path: " + folderPath);
                            }
                            else
                            {

                                foundPaths.Add(folderPath);
                                if (!paths.Contains(folderPath))
                                {
                                    paths.Add(folderPath);
                                    FileSystemSource s = new FileSystemSource(folderPath);
                                    s.NewItem += new NewItemHandler(s_NewItem);
                                    s.RemoveItem += new RemoveItemHandler(s_RemoveItem);
                                    sources.Add(s);
                                }
                            }
                        }
                    }
                    for (int i = 0 ; i < paths.Count ; ++i)
                    {
                        if (!foundPaths.Contains(paths[i]))
                        {
                            string path = paths[i];
                            paths.RemoveAt(i);
                            i--;
                            for (int j = 0 ; j < this.sources.Count ; ++j)
                                if (this.sources[j].Path == path)
                                {
                                    sources.RemoveAt(j);
                                    break;
                                }
                        }
                    }
                }
                ItemCache.Instance.SaveSource(this);
            }
        }

        void s_RemoveItem(ItemSource removeItem)
        {
            this.FireNewItem(removeItem);
        }

        void s_NewItem(ItemSource newItem)
        {
            this.FireRemoveItem(newItem);
        }

        public string ImageFile
        {
            get { return this.thumbPath; }
        }

        /// <summary>
        /// Gets whether the specified path is a valid absolute file path.
        /// http://www.csharp411.com/check-valid-file-path-in-c/
        /// </summary>
        /// <param name="path">Any path. OK if null or empty.</param>
        static private bool IsValidPath(string path)
        {
            Regex r = new Regex(@"^(([a-zA-Z]\:)|(\\))(\\{1}|((\\{1})[^\\]([^/:*?<>""|]*))+)$");
            return r.IsMatch(path);
        }

        
        public override bool IsPlayable
        {
            get { return false; }
        }

        public override UniqueName UniqueName
        {
            get { return this.uniqueName; }
        }

        public override IEnumerable<ItemSource> ChildSources
        {
            get 
            {
                lock (sources)
                {
                    LoadVfFile(this.vfFile);
                    if (sources.Count == 0)
                        foreach (string path in paths)
                            sources.Add(new FileSystemSource(path, ItemType.Folder));
                    foreach (ItemSource source in sources)
                        foreach (ItemSource s in source.ChildSources)
                            yield return s;
                }
            }
        }

        public override string RawName
        {
            get { return Path.GetFileNameWithoutExtension(vfFile); }
        }

        public override DateTime CreatedDate
        {
            get
            {
                return createdDate;
            }
        }


        public override Item ConstructItem()
        {
            Item itm = ItemFactory.Instance.Create(this);
            
            return itm;
        }

        internal override PlayableItem PlayableItem
        {
            get { return null; }
        }

        public override ItemType ItemType
        {
            get { return ItemType.VirtualFolder; }
        }

        public override string Location
        {
            get { return vfFile; }
        }

        protected override void WriteStream(System.IO.BinaryWriter bw)
        {
            bw.Write(Version);
            bw.Write(vfFile);
            bw.SafeWriteString(thumbPath);
            bw.Write(paths.Count);
            bw.Write(this.createdDate.Ticks);
            foreach (string s in paths)
                bw.Write(s);
            
        }

        protected override void ReadStream(System.IO.BinaryReader br)
        {
            byte v = br.ReadByte();
            vfFile = br.ReadString();
            thumbPath = br.SafeReadString();
            int len = br.ReadInt32();
            createdDate = new DateTime(br.ReadInt64());
            for (int i = 0; i < len; ++i)
            {
                string n = br.ReadString();
                this.paths.Add(n);
            }
            
        }
    }
}
