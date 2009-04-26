using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.Filesystem {
    // basic file info 
    
    public class FileInfo {
        public bool IsDirectory;
        public string Path;
        public DateTime DateModified;
        public DateTime DateCreated;
    }
}
