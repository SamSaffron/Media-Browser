using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Filesystem;

namespace MediaBrowser.Library.Extensions {
    static class DirectoryInfoExtensions {
        public static FileInfo ToFileInfo (this System.IO.FileSystemInfo info) {
            return new FileInfo()
            {
                IsDirectory = info is System.IO.DirectoryInfo,
                Path = info.FullName,
                DateCreated = info.CreationTimeUtc,
                DateModified = info.LastWriteTimeUtc
            };
        }
    }
}
