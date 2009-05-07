using System;
using System.Collections.Generic;
namespace MediaBrowser.Library.Filesystem {
    public interface IMediaLocation {
        IFolderMediaLocation Parent { get; }
        string Path { get; }
        string Name { get; }
        string Contents { get; set; }
        DateTime DateModified { get; }
        DateTime DateCreated { get; }
    }
}
