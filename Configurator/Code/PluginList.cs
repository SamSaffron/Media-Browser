using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using MediaBrowser.Library.Logging;

namespace Configurator.Code {

    // sample data 
    public class SamplePlugin : IPlugin {

        public void Init(Kernel kernel) {
        }

        public string Name {
            get; set;
        }

        public string Description {
            get; set;
        }

        public System.Version Version {
            get { return new System.Version(1, 2, 3, 4);  }
        }

        public System.Version LatestVersion {
            get { return null; }
        }
    }

    public class PluginList : ObservableCollection<IPlugin> {

        public PluginList() {

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) {

                Add(new SamplePlugin() { Name = "Super Plugin", 
                    Description = "This plugin does absoulutly nothing, its actually a sample plugin for wpf to bind to."});
                Add(new SamplePlugin() { Name = "The other plugin", 
                    Description = "This plugin also does absoulutly nothing, its actually a sample plugin for wpf to bind to." });

            } else {
                foreach (var plugin in Kernel.Instance.Plugins) {
                    Add(plugin);
                }
            }
        }

        protected override void RemoveItem(int index) {

            try {
                Kernel.Instance.DeletePlugin(Items[index]);
                base.RemoveItem(index);
            } catch (Exception e) {
                MessageBox.Show("Failed to delete the plugin, ensure no one has a lock on the plugin file!");
                Logger.ReportException("Failed to delete plugin", e);
            }
        }
    }
}
