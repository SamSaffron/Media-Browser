using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Filesystem;

namespace MediaBrowser.Library.Interfaces {
    public interface IBaseItemFactory {
        BaseItem Create(IMediaLocation location);
    }
}
