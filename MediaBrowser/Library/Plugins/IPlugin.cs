using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Factories;

namespace MediaBrowser.Library.Plugins {
    /// <summary>
    /// This interface can be implemented by plugin to provide rich information about the plugin
    ///  It also provides plugins with a place to place initialization code
    /// </summary>
    public interface IPlugin {
        void Init(Kernel kernel);
        string Name { get; }
        string Description { get; }
        System.Version Version { get; }
        System.Version LatestVersion { get; }
    }
}
