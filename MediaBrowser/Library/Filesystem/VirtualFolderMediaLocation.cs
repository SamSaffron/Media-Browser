using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Library.Util;
using System.IO;
using MediaBrowser.Library.Extensions;

namespace MediaBrowser.Library.Filesystem {
    public class VirtualFolderMediaLocation : MediaLocation, IFolderMediaLocation {

        VirtualFolder virtualFolder;

        public VirtualFolder VirtualFolder { get { return virtualFolder;  } }

        public VirtualFolderMediaLocation(FileInfo info, IFolderMediaLocation parent)
            : base(info, parent) 
        {
            virtualFolder = new VirtualFolder(Contents);
            children = new Lazy<IList<IMediaLocation>>(GetChildren);
        }

        Lazy<IList<IMediaLocation>> children;

        private IList<IMediaLocation> GetChildren() {
            var children = new List<IMediaLocation>();
            foreach (var folder in virtualFolder.Folders) {

                var location = new FolderMediaLocation(new DirectoryInfo(folder).ToFileInfo(), null, this);
                foreach (var child in location.Children) {
                    children.Add(child);
                }
            }
            return children;
        }


        public IList<IMediaLocation> Children {
            get { return children.Value; }
        }

    }
}
