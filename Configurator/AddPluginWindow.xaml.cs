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
using Configurator.Code;

namespace Configurator {
    /// <summary>
    /// Interaction logic for AddPluginWindow.xaml
    /// </summary>
    public partial class AddPluginWindow : Window {
        public AddPluginWindow() {
            InitializeComponent();
            pluginList.ItemsSource = PluginSourceCollection.Instance.AvailablePlugins;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e) {
            PluginSourcesWindow window = new PluginSourcesWindow();
            window.ShowDialog();
        }
    }
}
