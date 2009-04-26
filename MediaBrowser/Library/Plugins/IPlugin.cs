using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library.Plugins {
    /// <summary>
    /// This interface can be implemented by plugin to provide rich information about the plugin
    ///  It also provides plugins with a place to place initialization code
    /// </summary>
    public interface IPlugin {
        void Init(LibraryConfig config);
    }
}
