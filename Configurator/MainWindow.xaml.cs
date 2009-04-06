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
using MediaBrowser.Code.ShadowTypes;
using System.Xml.Serialization;
using MediaBrowser.Library;

namespace Configurator
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        ConfigData config;

        public MainWindow()
        {
            InitializeComponent();

            config = ConfigData.FromFile(Helper.ConfigFile);

            infoPanel.Visibility = Visibility.Hidden;

            // first time the wizard has run 
            if (config.InitialFolder != Helper.AppInitialDirPath)
            {
                try
                {
                    MigrateOldInitialFolder();
                }
                catch
                {
                    MessageBox.Show("For some reason we were not able to migrate your old initial path, you are going to have to start from scratch.");
                }
            }

            config.InitialFolder = Helper.AppInitialDirPath;
            RefreshItems();
            LoadConfigurationSettings();            

            for (char c = 'D'; c <= 'Z'; c++)
            {
                daemonToolsDrive.Items.Add(c.ToString());
            }

            try
            {
                daemonToolsDrive.SelectedValue = config.DaemonToolsDrive;
            }
            catch
            {
                // someone bodged up the config
            }

            daemonToolsLocation.Content = config.DaemonToolsLocation;

            LoadComboBoxes();
            RefreshExtenderFormats();
            RefreshDisplaySettings();

            SaveConfig();
        }

        #region Config Loading / Saving        
        private void LoadConfigurationSettings()
        {
            enableTranscode360.IsChecked = config.EnableTranscode360;
            useAutoPlay.IsChecked = config.UseAutoPlayForIso;

            cbxOptionClock.IsChecked = config.ShowClock;            
            cbxOptionTransparent.IsChecked = config.TransparentBackground;
            cbxOptionIndexing.IsChecked = config.RememberIndexing;
            cbxOptionDimPoster.IsChecked = config.DimUnselectedPosters;

            cbxOptionUnwatchedCount.IsChecked      = config.ShowUnwatchedCount;
            cbxOptionUnwatchedOnFolder.IsChecked   = config.ShowWatchedTickOnFolders;
            cbxOptionUnwatchedOnVideo.IsChecked    = config.ShowWatchTickInPosterView;
            cbxOptionUnwatchedDetailView.IsChecked = config.EnableListViewTicks;
            cbxOptionDefaultToUnwatched.IsChecked  = config.DefaultToFirstUnwatched;
            if (config.MaximumAspectRatioDistortion == Constants.MAX_ASPECT_RATIO_STRETCH)
                cbxOptionAspectRatio.IsChecked = true;
            else
                cbxOptionAspectRatio.IsChecked = false;
            
            
            ddlOptionViewTheme.SelectedItem = config.ViewTheme;
            ddlOptionThemeColor.SelectedItem = config.Theme;
            ddlOptionThemeFont.SelectedItem = config.FontTheme;
        }

        private void SaveConfig()
        {
            config.Save(Helper.ConfigFile);
        }

        private void LoadComboBoxes()
        {
            // Themes
            ddlOptionViewTheme.Items.Add("Default");
            ddlOptionViewTheme.Items.Add("Classic");
            ddlOptionViewTheme.Items.Add("Vanilla");
            // Colors
            ddlOptionThemeColor.Items.Add("Default");
            ddlOptionThemeColor.Items.Add("Black");
            ddlOptionThemeColor.Items.Add("Extender Default");
            ddlOptionThemeColor.Items.Add("Extender Black");
            // Fonts 
            ddlOptionThemeFont.Items.Add("Default");
            ddlOptionThemeFont.Items.Add("Small");
        }
        #endregion

        private void RefreshExtenderFormats()
        {
            extenderFormats.Items.Clear();
            foreach (var format in config.ExtenderNativeTypes.Split(','))
            {
                extenderFormats.Items.Add(format);
            }
        }

        private void RefreshDisplaySettings()
        {
            extenderFormats.Items.Clear();
            foreach (var format in config.ExtenderNativeTypes.Split(','))
            {
                extenderFormats.Items.Add(format);
            }
        }

        private void RefreshItems()
        {

            folderList.Items.Clear();

            foreach (var filename in Directory.GetFiles(config.InitialFolder))
            {
                try
                {
                    if (filename.ToLowerInvariant().EndsWith(".vf") ||
                        filename.ToLowerInvariant().EndsWith(".lnk"))
                        folderList.Items.Add(new VirtualFolder(filename));
                    else
                        throw new ArgumentException("Invalid virtual folder file extension: " + filename);
                }
                catch (ArgumentException e)
                {
                    // LOG invalid filename object.
                }
                catch (Exception e)
                {
                    MessageBox.Show("Invalid file detected in the initial folder!" + e.ToString());
                    // TODO : alert about dodgy VFs and delete them
                }
            }
        }

        #region Media Collection methods

        private void MigrateOldInitialFolder()
        {
            var path = config.InitialFolder;
            if (config.InitialFolder == Helper.MY_VIDEOS)
            {
                path = Helper.MyVideosPath;
            }

            foreach (var file in Directory.GetFiles(path))
            {
                if (file.ToLower().EndsWith(".vf"))
                {
                    File.Copy(file, System.IO.Path.Combine(Helper.AppInitialDirPath, System.IO.Path.GetFileName(file)), true);
                }
                else if (file.ToLower().EndsWith(".lnk"))
                {
                    WriteVirtualFolder(Helper.ResolveShortcut(file));
                }
            }

            foreach (var dir in Directory.GetDirectories(path))
            {

                WriteVirtualFolder(dir);
            }
        }

        private static void WriteVirtualFolder(string dir)
        {
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

        private static string FindImage(string dir)
        {
            string imagePath = "";
            foreach (var file in new string[] { "folder.png", "folder.jpeg", "folder.jpg" })
                if (File.Exists(System.IO.Path.Combine(dir, file)))
                {
                    imagePath = "image: " + System.IO.Path.Combine(dir, file);
                }
            return imagePath;
        }

        #endregion

        #region events
        private void btnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            BrowseForFolderDialog dlg = new BrowseForFolderDialog();

            if (true == dlg.ShowDialog(this))
            {
                WriteVirtualFolder(dlg.SelectedFolder);
                RefreshItems();
            }

            /* OLDFolderBrowser
            FolderBrowser browser = new FolderBrowser();
            browser.OnlyFilesystem = false;

            var result = browser.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                WriteVirtualFolder(browser.DirectoryPath);
                RefreshItems();
            }
            */
        }

        private void btnRename_Click(object sender, RoutedEventArgs e)
        {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder != null)
            {
                var form = new RenameForm(virtualFolder.Name);
                form.Owner = this;
                var result = form.ShowDialog();
                if (result == true)
                {
                    virtualFolder.Name = form.tbxName.Text;

                    RefreshItems();

                    foreach (VirtualFolder item in folderList.Items)
                    {
                        if (item.Name == virtualFolder.Name)
                        {
                            folderList.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
        }

        private void btnRemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder != null)
            {

                var message = "About to remove the folder \"" + virtualFolder.Name + "\" from the menu.\nAre you sure?";
                if (
                   MessageBox.Show(message, "Remove folder", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                {

                    File.Delete(virtualFolder.Path);
                    folderList.Items.Remove(virtualFolder);
                    infoPanel.Visibility = Visibility.Hidden;
                }
            }            
        }

        private void btnChangeImage_Click(object sender, RoutedEventArgs e)
        {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder == null) return;

            var dialog = new OpenFileDialog();
            dialog.Title = "Select your image";
            dialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;
            var result = dialog.ShowDialog(this);
            if (result == true)
            {
                virtualFolder.ImagePath = dialog.FileName;
                folderImage.Source = new BitmapImage(new Uri(virtualFolder.ImagePath));
            }
        }

        private void btnAddSubFolder_Click(object sender, RoutedEventArgs e)
        {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder == null) return;

            BrowseForFolderDialog dlg = new BrowseForFolderDialog();
            
            if (true == dlg.ShowDialog(this))
            {
                virtualFolder.AddFolder(dlg.SelectedFolder);
                folderList_SelectionChanged(this, null);
            }

            /* OLDFolderBrowser
            FolderBrowser browser = new FolderBrowser();
            var result = browser.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                virtualFolder.AddFolder(browser.DirectoryPath);
                folderList_SelectionChanged(this, null);
            }
            */
        }

        private void btnRemoveSubFolder_Click(object sender, RoutedEventArgs e)
        {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder == null) return;

            var path = internalFolder.SelectedItem as string;
            if (path != null)
            {
                var message = "Remove \"" + path + "\"?";
                if (
                  MessageBox.Show(message, "Remove folder", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                {
                    virtualFolder.RemoveFolder(path);
                    folderList_SelectionChanged(this, null);
                }
            }
        }

        private void folderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            internalFolder.Items.Clear();

            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder != null)
            {
                foreach (var folder in virtualFolder.Folders)
                {
                    internalFolder.Items.Add(folder);
                }

                if (!string.IsNullOrEmpty(virtualFolder.ImagePath))
                {
                    folderImage.Source = new BitmapImage(new Uri(virtualFolder.ImagePath));
                }
                else
                {
                    folderImage.Source = null;
                }

                infoPanel.Visibility = Visibility.Visible;
            }
        }

        private void addExtenderFormat_Click(object sender, RoutedEventArgs e)
        {
            var form = new AddExtenderFormat();
            form.Owner = this;
            var result = form.ShowDialog();
            if (result == true)
            {
                var parser = new FormatParser(config.ExtenderNativeTypes);
                parser.Add(form.formatName.Text);
                config.ExtenderNativeTypes = parser.ToString();
                RefreshExtenderFormats();
                SaveConfig();
            }
        }

        private void removeExtenderFormat_Click(object sender, RoutedEventArgs e)
        {
            var format = extenderFormats.SelectedItem as string;
            if (format != null)
            {
                var message = "Remove \"" + format + "\"?";
                if (
                  MessageBox.Show(message, "Remove folder", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                {
                    var parser = new FormatParser(config.ExtenderNativeTypes);
                    parser.Remove(format);
                    config.ExtenderNativeTypes = parser.ToString();
                    RefreshExtenderFormats();
                    SaveConfig();
                }
            }
        }

        private void changeDaemonToolsLocation_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "*.exe|*.exe";
            var result = dialog.ShowDialog();
            if (result == true)
            {
                config.DaemonToolsLocation = dialog.FileName;
                daemonToolsLocation.Content = config.DaemonToolsLocation;
                SaveConfig();
            }
        }

        private void daemonToolsDrive_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (daemonToolsDrive.SelectedValue != null)
            {
                config.DaemonToolsDrive = (string)daemonToolsDrive.SelectedValue;
            }
            SaveConfig();
        }
        #endregion

        #region CheckBox Events

        private void useAutoPlay_Click(object sender, RoutedEventArgs e)
        {
            config.UseAutoPlayForIso = (bool)useAutoPlay.IsChecked;
            SaveConfig();
        }
        private void enableTranscode360_Click(object sender, RoutedEventArgs e)
        {
            config.EnableTranscode360 = (bool)enableTranscode360.IsChecked;
            SaveConfig();
        }

        private void cbxOptionClock_Click(object sender, RoutedEventArgs e)
        {
            config.ShowClock = (bool)cbxOptionClock.IsChecked;
            SaveConfig();
        }

        private void cbxOptionTransparent_Click(object sender, RoutedEventArgs e)
        {
            config.TransparentBackground = (bool)cbxOptionTransparent.IsChecked;
            SaveConfig();
        }

        private void cbxOptionIndexing_Click(object sender, RoutedEventArgs e)
        {
            config.RememberIndexing = (bool)cbxOptionIndexing.IsChecked;
            SaveConfig();
        }

        private void cbxOptionDimPoster_Click(object sender, RoutedEventArgs e)
        {
            config.DimUnselectedPosters = (bool)cbxOptionDimPoster.IsChecked;
            SaveConfig();
        }

        private void cbxOptionUnwatchedCount_Click(object sender, RoutedEventArgs e)
        {
            config.ShowUnwatchedCount = (bool)cbxOptionUnwatchedCount.IsChecked;
            SaveConfig();
        }

        private void cbxOptionUnwatchedOnFolder_Click(object sender, RoutedEventArgs e)
        {
            config.ShowWatchedTickOnFolders = (bool)cbxOptionUnwatchedOnFolder.IsChecked;
            SaveConfig();
        }

        private void cbxOptionUnwatchedOnVideo_Click(object sender, RoutedEventArgs e)
        {
            config.ShowWatchTickInPosterView = (bool)cbxOptionUnwatchedOnVideo.IsChecked;
            SaveConfig();
        }

        private void cbxOptionUnwatchedDetailView_Click(object sender, RoutedEventArgs e)
        {
            config.EnableListViewTicks = (bool)cbxOptionUnwatchedDetailView.IsChecked;
            SaveConfig();
        }

        private void cbxOptionDefaultToUnwatched_Click(object sender, RoutedEventArgs e)
        {
            config.DefaultToFirstUnwatched = (bool)cbxOptionDefaultToUnwatched.IsChecked;
            SaveConfig();
        }

        private void cbxOptionAspectRatio_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)cbxOptionAspectRatio.IsChecked)
            {
                config.MaximumAspectRatioDistortion = Constants.MAX_ASPECT_RATIO_STRETCH;
            }
            else
            {
                config.MaximumAspectRatioDistortion = Constants.MAX_ASPECT_RATIO_DEFAULT;
            }

            SaveConfig();
        }
        #endregion

        #region ComboBox Events
        private void ddlOptionViewTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ddlOptionViewTheme.SelectedValue != null)
            {
                config.ViewTheme = ddlOptionViewTheme.SelectedValue.ToString();
            }
            SaveConfig();
        }

        private void ddlOptionThemeColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ddlOptionThemeColor.SelectedValue != null)
            {
                config.Theme = ddlOptionThemeColor.SelectedValue.ToString();
            }
            SaveConfig();
        }

        private void ddlOptionThemeFont_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ddlOptionThemeFont.SelectedValue != null)
            {
                config.FontTheme = ddlOptionThemeFont.SelectedValue.ToString();
            }
            SaveConfig();
        }
        #endregion

        #region Header Selection Methods
        private void hdrBasic_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetHeader(hdrBasic);
            tabItem4.Visibility = Visibility.Collapsed;
        }

        private void hdrAdvanced_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetHeader(hdrAdvanced);
            tabItem4.Visibility = Visibility.Visible;
        }

        private void ClearHeaders()
        {
            hdrAdvanced.Foreground = hdrBasic.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Gray);
            tabControl1.SelectedIndex = 0;
        }
        private void SetHeader(Label label)
        {
            ClearHeaders();
            label.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Black);
        }
        #endregion


    }
    #region FormatParser Class
    class FormatParser
    {

        List<string> currentFormats = new List<string>();

        public FormatParser(string value)
        {
            currentFormats.AddRange(value.Split(','));
        }

        public void Add(string format)
        {
            format = format.Trim();
            if (!format.StartsWith("."))
            {
                format = "." + format;
            }
            format = format.ToLower();

            if (format.Length > 1)
            {
                if (!currentFormats.Contains(format))
                {
                    currentFormats.Add(format);
                }
            }
        }

        public void Remove(string format)
        {
            currentFormats.Remove(format);
        }

        public override string ToString()
        {
            return String.Join(",", currentFormats.ToArray());
        }


    }
    #endregion
}
