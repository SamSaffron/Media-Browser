using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.LibraryManagement;
using System.IO;

namespace MediaBrowser.Library.Sources
{
    class ShortcutSource : FileSystemSource
    {
        static readonly byte Version = 1;
        private string path;
        

        public ShortcutSource(UniqueName name)
            : base(name)
        {
            
        }

        public ShortcutSource(string path)
            : base(Helper.ResolveShortcut(path))
        {
            this.path = path;
        }

        public override string RawName
        {
            get
            {
                return System.IO.Path.GetFileNameWithoutExtension(this.path);
            }
        }
        protected override void WriteStream(BinaryWriter bw)
        {
            base.WriteStream(bw);
            bw.Write(Version);
            bw.SafeWriteString(this.path);
        }

        protected override void ReadStream(BinaryReader br)
        {
            base.ReadStream(br);
            byte v = br.ReadByte();
            this.path = br.SafeReadString();
        }
    }
}
