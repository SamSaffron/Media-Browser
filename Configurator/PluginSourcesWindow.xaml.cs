using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Configurator.Properties;
using System.Collections.ObjectModel;

namespace Configurator {

    /// <summary>
    /// Interaction logic for PluginSourcesWindow.xaml
    /// </summary>
    public partial class PluginSourcesWindow : Window {

        class SourceCollection : ObservableCollection<string> {

            bool initializing; 

            public SourceCollection() {
                foreach (var item in Settings.Default.Repositories) {
                    Items.Add(item);
                }
            }

            protected override void InsertItem(int index, string item) {
                base.InsertItem(index, item);
                Settings.Default.Repositories.Add(item);
                Settings.Default.Save(); 
            }

            protected override void RemoveItem(int index) {
                Settings.Default.Repositories.Remove(this[index]);
                Settings.Default.Save();
                base.RemoveItem(index);
            }
        }

        SourceCollection sources = new SourceCollection();

        public PluginSourcesWindow() {
            InitializeComponent();
            sourceList.ItemsSource = sources;
        }

        private void addButton_Click(object sender, RoutedEventArgs e) {
            var window = new AddPluginSourceWindow();
            var result = window.ShowDialog();

            if (result != null && result.Value) {
                sources.Add(window.pluginSource.Text);
            }
       
        }

        private void removeButton_Click(object sender, RoutedEventArgs e) {
            sources.Remove(sourceList.SelectedItem as string);
        }
    }
}
