using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Entities;
using System.Drawing;
using System.Diagnostics;

namespace MediaBrowser.Library.Plugins {
    public class Plugin {
        string filename;
        Assembly assembly;

        IEnumerable<MetadataProvider> providers;
        IPlugin pluginInterface;

        public Plugin(string filename) {
            this.filename = filename;
#if DEBUG
            // This will allow us to step through plugins
            assembly = Assembly.LoadFile(filename);
#else 
            // This will reduce the locking on the plugins files
            assembly = Assembly.Load(System.IO.File.ReadAllBytes(filename)); 
#endif
            providers = DiscoverProviders(assembly);
            pluginInterface = FindPluginInterface(assembly);
        }

        public IEnumerable<MetadataProvider> MetadataProviders {
            get {
                return providers;
            }
        }

        public static IEnumerable<MetadataProvider> DiscoverProviders(Assembly assembly) {
            return new List<MetadataProvider>(
             assembly
             .GetTypes()
             .Where(type => typeof(IMetadataProvider).IsAssignableFrom(type))
             .Where(type => type.IsClass)
             .Where(type => !type.IsAbstract)
             .Select(type => new MetadataProvider(type))
         );
        }

        public IPlugin FindPluginInterface(Assembly assembly) {

            IPlugin pluginInterface = null;

            var plugin = assembly.GetTypes().Where(type => typeof(IPlugin).IsAssignableFrom(type)).FirstOrDefault();
            if (plugin != null) {
                try {
                    pluginInterface = plugin.GetConstructor(Type.EmptyTypes).Invoke(null) as IPlugin;
                } catch (Exception e) {
                    Application.Logger.ReportException ("Failed to initialize plugin: ", e);
                    Debug.Assert(false);
                }
            }

            return pluginInterface;
        }

        public void Init(LibraryConfig config) {
            if (pluginInterface != null) {
                pluginInterface.Init(config);
            }
        }


    }
}
