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


using MediaBrowser;
using MediaBrowser.LibraryManagement;
using System.IO;
using Microsoft.Win32;

namespace Configurator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            infoPanel.Visibility = Visibility.Hidden;

            // first time the wizard has run 
            if (Config.Instance.InitialFolder != Helper.AppInitialDirPath) {
                try {
                    MigrateOldInitialFolder();
                } catch {
                    MessageBox.Show("For some reason we were not able to migrate your old initial path, you are going to have to start from scratch.");
                }
            }

            Config.Instance.InitialFolder = Helper.AppInitialDirPath;

            RefreshItems();

            enableTranscode360.IsChecked = Config.Instance.EnableTranscode360;
            useAutoPlay.IsChecked = Config.Instance.UseAutoPlayForIso;

            for (char c = 'D'; c <= 'Z'; c++) {
                daemonToolsDrive.Items.Add(c.ToString()); 
            }

            try {
                daemonToolsDrive.SelectedValue = Config.Instance.DaemonToolsDrive;
            } catch { 
                // someone bodged up the config
            }

            daemonToolsLocation.Content = Config.Instance.DaemonToolsLocation;
            RefreshExtenderFormats();

        }

        private void RefreshExtenderFormats() {
            extenderFormats.Items.Clear();
            foreach (var format in Config.Instance.ExtenderNativeTypes.Split(',')) {
                extenderFormats.Items.Add(format);
            }
        }


        private void RefreshItems() {

            folderList.Items.Clear();

            foreach (var filename in Directory.GetFiles(Config.Instance.InitialFolder)) {
                try {
                    folderList.Items.Add(new VirtualFolder(filename));
                } catch (Exception e) {
                    MessageBox.Show("Invalid file detected in the initial folder!" + e.ToString());
                    // TODO : alert about dodgy VFs and delete them
                }
            }
        }

        private static void MigrateOldInitialFolder() {
            var path = Config.Instance.InitialFolder;
            if (Config.Instance.InitialFolder == Helper.MY_VIDEOS) {
                path = Helper.MyVideosPath;
            }

            foreach (var file in Directory.GetFiles(path)) {
                if (file.ToLower().EndsWith(".vf")) {
                    File.Copy(file, System.IO.Path.Combine(Helper.AppInitialDirPath, System.IO.Path.GetFileName(file)), true);
                } else if (file.ToLower().EndsWith(".lnk")) {
                    WriteVirtualFolder(Helper.ResolveShortcut(file));
                }
            }

            foreach (var dir in Directory.GetDirectories(path)) {

                WriteVirtualFolder(dir);
            }
        }

        private static void WriteVirtualFolder(string dir) {
            var imagePath = FindImage(dir);
            string vf = string.Format(
@"
folder: {0}
{1}
", dir, imagePath);
            var destination = System.IO.Path.Combine(Helper.AppInitialDirPath, System.IO.Path.GetFileName(dir) + ".vf");

            File.WriteAllText(destination,
                vf.Trim());
        }

        private static string FindImage(string dir) {
            string imagePath = "";
            foreach (var file in new string[] { "folder.png", "folder.jpeg", "folder.jpg" })
                if (File.Exists(System.IO.Path.Combine(dir, file))) {
                    imagePath = "image: " + System.IO.Path.Combine(dir, file);
                }
            return imagePath;
        }

        private void btnAddFolder_Click(object sender, RoutedEventArgs e) {
            FolderBrowser browser = new FolderBrowser();
            var result = browser.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK) {
                WriteVirtualFolder(browser.DirectoryPath);
                RefreshItems();
            }
        }

        private void btnRename_Click(object sender, RoutedEventArgs e) {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder != null) {
                var form = new RenameForm(virtualFolder.Name);
                form.Owner = this;
                var result = form.ShowDialog();
                if (result == true) {
                    virtualFolder.Name = form.tbxName.Text;

                    RefreshItems();

                    foreach (VirtualFolder item in folderList.Items) {
                        if (item.Name == virtualFolder.Name) {
                            folderList.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
        }

        private void btnRemoveFolder_Click(object sender, RoutedEventArgs e) {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder != null) {

                var message = "About to remove the folder \"" + virtualFolder.Name + "\" from the menu.\nAre you sure?";
                if (
                   MessageBox.Show(message, "Remove folder", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes) {

                    File.Delete(virtualFolder.Path);
                    folderList.Items.Remove(virtualFolder);
                }
            }
            infoPanel.Visibility = Visibility.Hidden;
        }

        private void btnChangeImage_Click(object sender, RoutedEventArgs e) {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder == null) return;

            var dialog = new OpenFileDialog();
            dialog.Title = "Select your image";
            dialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;
            var result = dialog.ShowDialog(this);
            if (result == true) {
                virtualFolder.ImagePath = dialog.FileName;
                folderImage.Source = new BitmapImage(new Uri(virtualFolder.ImagePath));
            }
        }

        private void btnAddSubFolder_Click(object sender, RoutedEventArgs e) {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder == null) return;

            FolderBrowser browser = new FolderBrowser();
            var result = browser.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                virtualFolder.AddFolder(browser.DirectoryPath);
                folderList_SelectionChanged(this, null);
            }
        }

        private void btnRemoveSubFolder_Click(object sender, RoutedEventArgs e) {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder == null) return;

            var path = internalFolder.SelectedItem as string;
            if (path != null) {
                var message = "Remove \"" + path + "\"?";
                if (
                  MessageBox.Show(message, "Remove folder", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes) {
                    virtualFolder.RemoveFolder(path);
                    folderList_SelectionChanged(this, null);
                }
            }
        }

        private void folderList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            internalFolder.Items.Clear();

            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder != null) {
                foreach (var folder in virtualFolder.Folders) {
                    internalFolder.Items.Add(folder);
                }

                if (!string.IsNullOrEmpty(virtualFolder.ImagePath)) {
                    folderImage.Source = new BitmapImage(new Uri(virtualFolder.ImagePath));
                } else {
                    folderImage.Source = null;
                }

                infoPanel.Visibility = Visibility.Visible;
            }
        }


        private void enableTranscode360_Click(object sender, RoutedEventArgs e) {
            Config.Instance.EnableTranscode360 = (bool)enableTranscode360.IsChecked;
        }

        private void addExtenderFormat_Click(object sender, RoutedEventArgs e) {
            var form = new AddExtenderFormat();
            form.Owner = this;
            var result = form.ShowDialog();
            if (result == true) {
                var parser = new FormatParser(Config.Instance.ExtenderNativeTypes); 
                parser.Add(form.formatName.Text);
                Config.Instance.ExtenderNativeTypes = parser.ToString();
                RefreshExtenderFormats();
            }
        }

        private void removeExtenderFormat_Click(object sender, RoutedEventArgs e) {
            var format = extenderFormats.SelectedItem as string;
            if (format != null) {
                var message = "Remove \"" + format + "\"?";
                if (
                  MessageBox.Show(message, "Remove folder", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes) {
                    var parser = new FormatParser(Config.Instance.ExtenderNativeTypes);
                    parser.Remove(format);
                    Config.Instance.ExtenderNativeTypes = parser.ToString();
                    RefreshExtenderFormats();
                }
            }
        }

        private void changeDaemonToolsLocation_Click(object sender, RoutedEventArgs e) {

            var dialog = new OpenFileDialog();
            dialog.Filter = "*.exe|*.exe";
            var result = dialog.ShowDialog();
            if (result == true) {
                Config.Instance.DaemonToolsLocation = dialog.FileName;
                daemonToolsLocation.Content = Config.Instance.DaemonToolsLocation;
            }
        }


        private void useAutoPlay_Click(object sender, RoutedEventArgs e) {
            Config.Instance.UseAutoPlayForIso = (bool)useAutoPlay.IsChecked ;
        }

        private void daemonToolsDrive_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Config.Instance.DaemonToolsDrive = (string)daemonToolsDrive.SelectedValue;
        }
    }

    class FormatParser {

        List<string> currentFormats = new List<string>();

        public FormatParser(string value) {
            currentFormats.AddRange(value.Split(','));
        }

        public void Add(string format) {
            format = format.Trim();
            if (!format.StartsWith(".")) {
                format = "." + format;
            }
            format = format.ToLower();

            if (format.Length > 1) {
                if (!currentFormats.Contains(format)) {
                    currentFormats.Add(format);
                }
            }  
        }

        public void Remove(string format) {
            currentFormats.Remove(format);
        }

        public override string ToString() {
            return String.Join(",", currentFormats.ToArray());
        }


    }
   
}
