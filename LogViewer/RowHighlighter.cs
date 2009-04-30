using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using MediaBrowser.Library.Logging;
using System.Windows.Controls;


namespace LogViewer {
    class RowHighlighter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {

            Brush brush = Brushes.Black;

            var item = (ListViewItem)value;
            ListView listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
            int index = listView.ItemContainerGenerator.IndexFromContainer(item);

            if (!(listView.Items[index] is LogRow)) return Brushes.Black;

            var row = (LogRow)listView.Items[index];

            switch (row.Severity) {
                case LogSeverity.Verbose:
                    brush = Brushes.Gray;
                    break;
                case LogSeverity.Info:
                    brush = Brushes.Black;
                    break;
                case LogSeverity.Warning:
                    brush = Brushes.Blue;
                    break;
                case LogSeverity.Error:
                    brush = Brushes.Red;
                    break;
                default:
                    break;
            }

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return null;
        }

        #endregion
    }
}
