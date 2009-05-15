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
using Configurator.Code;

namespace Configurator {

    /// <summary>
    /// Interaction logic for PluginSourcesWindow.xaml
    /// </summary>
    public partial class PluginSourcesWindow : Window {

        public PluginSourcesWindow() {
            InitializeComponent();
            sourceList.ItemsSource = PluginSourceCollection.Instance;
        }

        private void addButton_Click(object sender, RoutedEventArgs e) {
            var window = new AddPluginSourceWindow();
            var result = window.ShowDialog();

            if (result != null && result.Value) {
                PluginSourceCollection.Instance.Add(window.pluginSource.Text);
            }
       
        }

        private void removeButton_Click(object sender, RoutedEventArgs e) {
            PluginSourceCollection.Instance.Remove(sourceList.SelectedItem as string);
        }
    }
}
