using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Library.Util;
using System.IO;
using System.Linq;
using MediaBrowser.Library.Extensions;

namespace MediaBrowser.Library.Filesystem {
    public class VirtualFolderMediaLocation : FolderMediaLocation {

        VirtualFolderContents virtualFolder;

        public VirtualFolderContents VirtualFolder { get { return virtualFolder;  } }

        public VirtualFolderMediaLocation(FileInfo info, IFolderMediaLocation parent)
            : base(info, parent) 
        {
            virtualFolder = new VirtualFolderContents(Contents);
        }

        protected override void SetName() {
            Name = System.IO.Path.GetFileNameWithoutExtension(Path);
        }

        protected override IList<IMediaLocation> GetChildren() {
            var children = new List<IMediaLocation>();
            foreach (var folder in virtualFolder.Folders) {

                var location = new FolderMediaLocation(new DirectoryInfo(folder).ToFileInfo(), null, this);
                foreach (var child in location.Children) {
                    children.Add(child);
                }
            }
            return children;
        }


    }
}
