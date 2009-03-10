using System;
using System.Collections.Generic;
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
using System.IO;

namespace DotNetFrameworkUpgrader {

    using Path = System.IO.Path;
    using System.Net;
    using System.Diagnostics;
    using System.Threading;
    using System.Windows.Threading;

    public delegate void MethodInvoker();

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window {
        public Window1() {
            InitializeComponent();
            message.Text = "";
            upgradeButton.Focus();
        }

        class RequestState {
            public HttpWebRequest Request { get; set; }
            public string Filename { get; set; }
        }

        private void upgradeButton_Click(object sender, RoutedEventArgs e) {
            upgradeButton.IsEnabled = false;
            Thread downloadThread = new Thread(new ThreadStart(DownloadFramework));
            downloadThread.Start();
        }

        private void DownloadFramework() {
            SetMessage("Downloading...");

            var url = "http://www.microsoft.com/downloads/info.aspx?na=90&p=&SrcDisplayLang=en&SrcCategoryId=&SrcFamilyId=333325fd-ae52-4e35-b531-508d977d32a6&u=http%3a%2f%2fdownload.microsoft.com%2fdownload%2f7%2f0%2f3%2f703455ee-a747-4cc8-bd3e-98a615c3aedb%2fdotNetFx35setup.exe";
            string tempFile = Path.Combine(Path.GetTempPath(), "dotNetFx35setup.exe");

            var request = (HttpWebRequest)HttpWebRequest.Create(url);
            var response = request.GetResponse();

            SetMaxProgress(response.ContentLength);

            var stream = response.GetResponseStream();
            byte[] buffer = new byte[1024 * 8];

            int total = 0;

            using (var localCopy = File.Open(tempFile, FileMode.Create)) {
                int bytesRead = 0;
                while (true) {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    localCopy.Write(buffer, 0, bytesRead);
                    total += bytesRead;
                    SetProgress(total);
                }
            }

            SetMessage("Installing...");
            SetMaxProgress(100);
            int i = 0;
            Process p = Process.Start(tempFile,"/qb /norestart");
            while (!p.WaitForExit(1000)) {
                i = (i + 1) % 100;
                SetProgress(i);
            }
            SetProgress(100);
            MessageBox.Show(p.ExitCode.ToString());
        
        }

        private bool IsBackgroundThread {
            get {
                return Dispatcher.Thread != Thread.CurrentThread;
            }
        }

        private void SetMaxProgress(long size)
        {
            if (IsBackgroundThread) {
                Dispatcher.Invoke(DispatcherPriority.Normal, (MethodInvoker)delegate() {
                    SetMaxProgress(size);
                });
            } else {
                progressBar.Minimum = 0;
                progressBar.Maximum = size;
            }
        }

        private void SetMessage(string message) {
            if (IsBackgroundThread) {
                Dispatcher.Invoke(DispatcherPriority.Normal, (MethodInvoker)delegate() {
                    SetMessage(message);
                });
            } else {
                this.message.Text = message;
            }
        }


        private void SetProgress(long progress) {
            if (IsBackgroundThread) {
                Dispatcher.Invoke(DispatcherPriority.Normal, (MethodInvoker)delegate() {
                    SetProgress(progress);
                });
            } else {
                this.progressBar.Value = progress;
            }
        }

    }
}
