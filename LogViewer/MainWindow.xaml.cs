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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Logging;

namespace LogViewer {
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window {

        LogMessages logMessages = new LogMessages(ApplicationPaths.AppLogPath);

        public Window1() {
            InitializeComponent();
            listView.ItemsSource = logMessages;

            listView.SelectionChanged += new SelectionChangedEventHandler(ListViewSelectionChanged);
            logMessages.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(LogMessagesCollectionChanged);
        }

        void LogMessagesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            listView.ScrollIntoView(listView.Items[listView.Items.Count - 1]);
        }

        void ListViewSelectionChanged(object sender, SelectionChangedEventArgs e) {
            textBox.Text = ((LogRow)listView.SelectedItem).FullDescription; 
        }
    }
}
