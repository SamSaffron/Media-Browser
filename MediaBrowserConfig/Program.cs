using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MediaBrowserConfig {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            //MediaBrowser.Config.Instance.InitialFolder = "MyVideos";
            if (MediaBrowser.Config.Instance.InitialFolder.ToLower().EndsWith(".vf")) {
                MessageBox.Show("The configuration utility does not support a virtual folder as the initial folder!");
                System.Windows.Forms.Application.Exit();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
