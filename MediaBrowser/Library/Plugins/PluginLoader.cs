using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaBrowser.LibraryManagement;
using System.Diagnostics;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Configuration;

namespace MediaBrowser.Library.Plugins {
    public class PluginLoader {

        public static PluginLoader Instance = new PluginLoader();

        private PluginLoader() {
        }

        IEnumerable<Plugin> plugins;

        public void Initialize(LibraryConfig config) {
            foreach (var plugin in Plugins) {
                plugin.Init(config);
            }
        }

        public IEnumerable<Plugin> Plugins {
            get {
                lock (this) {
                    if (this.plugins == null) {
                        plugins = LoadPlugins();
                    }
                    return plugins;
                }
            }
        }

        private List<Plugin> LoadPlugins() {
            List<Plugin> plugins = new List<Plugin>();
            foreach (var file in Directory.GetFiles(ApplicationPaths.AppPluginPath)) {
                if (file.ToLower().EndsWith(".dll")) {
                    try {
                        plugins.Add(new Plugin(Path.Combine(ApplicationPaths.AppPluginPath, file)));
                    } catch (Exception ex) {
                        Debug.Assert(false, "Failed to load plugin: " + ex.ToString());
                        Application.Logger.ReportException("Failed to load plugin", ex); 
                    }
                }
            }
            return plugins;
        }
    }
}
