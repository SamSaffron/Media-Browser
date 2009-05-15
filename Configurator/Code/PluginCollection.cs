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



        public string Filename {
            get { return "bob.dll"; }
        }

    }

    public class PluginCollection : ObservableCollection<IPlugin> {

        public PluginCollection() {

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) {

                Add(new SamplePlugin() { Name = "Super Plugin", 
                    Description = "This plugin does absoulutly nothing, its actually a sample plugin for wpf to bind to."});
                Add(new SamplePlugin() { Name = "The other plugin", 
                    Description = "This plugin also does absoulutly nothing, its actually a sample plugin for wpf to bind to." });

            } 
        }
    }
}
