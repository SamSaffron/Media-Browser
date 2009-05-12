using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Logging;
using System.IO;
using System.Collections.ObjectModel;
using MediaBrowser.Library.Threading;
using MediaBrowser.Library.Extensions;
using System.Windows.Threading;
using System.Timers;
using System.Collections.Specialized;
using System.Collections;

namespace LogViewer {
    class LogMessages : ObservableCollection<LogRow> {

        string path;

        HashSet<Guid> rowHashs = new HashSet<Guid>();
        System.Threading.Timer timer;
        Dispatcher dispatcher; 

        // used by the UI
        public LogMessages() {
            Add(new LogRow()
            {
                Category = "cat1",
                Message = "This is my log message \n its a long one containing lots of info",
                Severity = LogSeverity.Verbose,
                ThreadId = 22,
                ThreadName = "thready",
                Time = DateTime.Now
            });

            Add(new LogRow()
            {
                Category = "cat1",
                Message = "This is my log message \n its a long one containing lots of info",
                Severity = LogSeverity.Info,
                ThreadId = 22,
                ThreadName = "thready",
                Time = DateTime.Now
            });

            Add(new LogRow()
            {
                Category = "cat1",
                Message = "This is my log message \n its a long one containing lots of info",
                Severity = LogSeverity.Warning,
                ThreadId = 22,
                ThreadName = "thready",
                Time = DateTime.Now
            });
            
            Add(new LogRow()
            {
                Category = "cat1",
                Message = "This is my log message \n its a long one containing lots of info",
                Severity = LogSeverity.Error,
                ThreadId = 22,
                ThreadName = "thready",
                Time = DateTime.Now
            });
        }

        public LogMessages(string path) {
            this.path = path;

            // really expensive should be a log watcher
            timer = Async.Every(2000, LoadMessages);
            dispatcher = Dispatcher.CurrentDispatcher;
        }

        private void LoadMessages() {

            var newRows = new List<LogRow>();

            foreach (var file in Directory.GetFiles(path)) {
                foreach (var line in GetLines(file)) {
                    LogRow row = LogRow.FromString(line);
                    var hash = row.ToString().GetMD5();
                    if (!rowHashs.Contains(hash)) {
                        newRows.Add(row);
                        rowHashs.Add(hash);
                    }
                }
            }

            dispatcher.Invoke((Action)(() =>
            {
                foreach (var item in newRows.OrderBy(row => row.Time))
	            {
                    this.Items.Add(item);
	            }
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }));


        }

        private IEnumerable<string> GetLines(string filename) {
            List<string> lines = new List<string>();
            try {
                using (var stream = new FileStream(filename,FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var reader = new StreamReader(stream);
                    while (!reader.EndOfStream)
                    {
                        lines.Add(reader.ReadLine());
                    } 
                }
            } catch { 
                // dont care
            }

            return lines;
        }
    }
}
