using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Entities;
using System.Drawing;
using System.Diagnostics;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Logging;
using System.IO;

namespace MediaBrowser.Library.Plugins {
    public class Plugin : IPlugin {
        string filename;
        Assembly assembly;
        IPlugin pluginInterface;

        public static Plugin FromFile(string filename, bool forceShadow) {
            return new Plugin(filename, forceShadow);
        }

        internal Plugin(string filename, bool forceShadow) {
            this.filename = filename;
#if DEBUG
            // This will allow us to step through plugins
            if (forceShadow) {
                assembly = Assembly.Load(System.IO.File.ReadAllBytes(filename));
            } else {
                assembly = Assembly.LoadFile(filename);
            }
#else 
            // This will reduce the locking on the plugins files
            assembly = Assembly.Load(System.IO.File.ReadAllBytes(filename)); 
#endif
            pluginInterface = FindPluginInterface(assembly);

        }


        public IPlugin FindPluginInterface(Assembly assembly) {

            IPlugin pluginInterface = null;

            var plugin = assembly.GetTypes().Where(type => typeof(IPlugin).IsAssignableFrom(type)).FirstOrDefault();
            if (plugin != null) {
                try {
                    pluginInterface = plugin.GetConstructor(Type.EmptyTypes).Invoke(null) as IPlugin;
                } catch (Exception e) {
                    Logger.ReportException ("Failed to initialize plugin: ", e);
                    Debug.Assert(false);
                    throw;
                }
            }

            if (pluginInterface == null) {
                throw new ApplicationException("The following assembly is not a valid Plugin : " + assembly.FullName);
            }

            return pluginInterface;
        }

        public void Init(Kernel config) {
            pluginInterface.Init(config);
        }


        public string Name {
            get { return pluginInterface.Name; }
        }

        public string Description {
            get { return pluginInterface.Description; }
        }

        public System.Version Version {
            get { return pluginInterface.Version; }
        }

        public string Filename {
            get { return Path.GetFileName(filename); } 
        }

        public void Delete() {
            File.Delete(filename);
        }
    }
}
