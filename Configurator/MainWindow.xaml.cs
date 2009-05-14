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
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Network;
using MediaBrowser.Library.Logging;

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
            LoadComboBoxes();

            config = ConfigData.FromFile(ApplicationPaths.ConfigFile);

            infoPanel.Visibility = Visibility.Hidden;
            infoPlayerPanel.Visibility = Visibility.Hidden;

            // first time the wizard has run 
            if (config.InitialFolder != ApplicationPaths.AppInitialDirPath)
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

            config.InitialFolder = ApplicationPaths.AppInitialDirPath;
            RefreshItems();
            RefreshPodcasts();
            RefreshPlayers();
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

            
            RefreshExtenderFormats();
            RefreshDisplaySettings();

            podcastsPath.Content = config.PodcastHome;
            podcastDetails.Visibility = Visibility.Hidden;
            SaveConfig();

        }

        private void RefreshPodcasts() {
            var podcasts = Kernel.Instance.GetItem<Folder>(config.PodcastHome);
            podcasts.ValidateChildren();

            podcastList.Items.Clear();

            foreach (var item in podcasts.Children) {
                if (item is VodCast) {
                    (item as VodCast).ValidateChildren();
                    podcastList.Items.Add(item);
                }
            }
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
            cbxRootPage.IsChecked                  = config.EnableRootPage;
            if (config.MaximumAspectRatioDistortion == Constants.MAX_ASPECT_RATIO_STRETCH)
                cbxOptionAspectRatio.IsChecked = true;
            else
                cbxOptionAspectRatio.IsChecked = false;
            
            
            ddlOptionViewTheme.SelectedItem = config.ViewTheme;
            ddlOptionThemeColor.SelectedItem = config.Theme;
            ddlOptionThemeFont.SelectedItem = config.FontTheme;

            tbxWeatherID.Text = config.YahooWeatherFeed;
            if (config.YahooWeatherUnit.ToLower() == "f")
                ddlWeatherUnits.SelectedItem = "Farenheit";
            else
                ddlWeatherUnits.SelectedItem = "Celsius";
        }

        private void SaveConfig()
        {
            config.Save(ApplicationPaths.ConfigFile);
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
            // Weather Units
            ddlWeatherUnits.Items.Add("Celsius");
            ddlWeatherUnits.Items.Add("Farenheit");
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
                    //else
                    //    throw new ArgumentException("Invalid virtual folder file extension: " + filename);
                }
                catch (ArgumentException)
                {
                    Logger.ReportWarning("Ignored file: " + filename);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Invalid file detected in the initial folder!" + e.ToString());
                    // TODO : alert about dodgy VFs and delete them
                }
            }
        }

        private void RefreshPlayers()
        {
            lstExternalPlayers.Items.Clear();
            foreach (ConfigData.ExternalPlayer item in config.ExternalPlayers)
                lstExternalPlayers.Items.Add(item);
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
                    File.Copy(file, System.IO.Path.Combine(ApplicationPaths.AppInitialDirPath, System.IO.Path.GetFileName(file)), true);
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
            var destination = System.IO.Path.Combine(ApplicationPaths.AppInitialDirPath, System.IO.Path.GetFileName(dir) + ".vf");

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
                    if (File.Exists(virtualFolder.ImagePath)) {
                        folderImage.Source = new BitmapImage(new Uri(virtualFolder.ImagePath));
                    }
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

        private void btnAddPlayer_Click(object sender, RoutedEventArgs e)
        {
            List<MediaType> list = new List<MediaType>();
            // Provide a list of media types that haven't been used. This is to filter out the selection available to the end user.
            // Don't display media types for players that we already have. 
            //
            // This also makes this scalable, we shouldn't have to adjust this code for new media types.
            Boolean found;
            foreach (MediaType item in Enum.GetValues(typeof(MediaType)))
            {
                // See if an external player has been configured for this media type.
                found = false;
                foreach (ConfigData.ExternalPlayer player in lstExternalPlayers.Items)
                    if (player.MediaType == item) {
                        found = true;
                        break;
                    }
                // If a player hasn't been configured then make it an available option to be added
                if (!found)
                    list.Add(item);
            }

            var form = new SelectMediaTypeForm(list);
            form.Owner = this;

            if (form.ShowDialog() == true)
            {
                ConfigData.ExternalPlayer player = new ConfigData.ExternalPlayer();
                player.MediaType = (MediaType)form.cbMediaType.SelectedItem;
                player.Args = "\"{0}\""; // Assign a default parameter
                config.ExternalPlayers.Add(player);
                lstExternalPlayers.Items.Add(player);
                lstExternalPlayers.SelectedItem = player;
                SaveConfig();
            }
        }

        private void btnRemovePlayer_Click(object sender, RoutedEventArgs e)
        {
            var mediaPlayer = lstExternalPlayers.SelectedItem as ConfigData.ExternalPlayer;
            if (mediaPlayer != null)
            {
                var message = "About to remove the media type \"" + lstExternalPlayers.SelectedItem.ToString() + "\" from the external players.\nAre you sure?";
                if (MessageBox.Show(message, "Remove Player", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    config.ExternalPlayers.Remove(mediaPlayer);
                    lstExternalPlayers.Items.Remove(mediaPlayer);
                    SaveConfig();
                    infoPlayerPanel.Visibility = Visibility.Hidden;
                }
            }
        }

        private void btnPlayerCommand_Click(object sender, RoutedEventArgs e)
        {
            var mediaPlayer = lstExternalPlayers.SelectedItem as ConfigData.ExternalPlayer;
            if (mediaPlayer != null)
            {
                var dialog = new OpenFileDialog();
                dialog.Filter = "*.exe|*.exe";
                if (mediaPlayer.Command != string.Empty)
                    dialog.FileName = mediaPlayer.Command;

                if (dialog.ShowDialog() == true)
                {
                    mediaPlayer.Command = dialog.FileName;
                    txtPlayerCommand.Text = mediaPlayer.Command;
                    SaveConfig();
                }
            }
        }

        private void btnPlayerArgs_Click(object sender, RoutedEventArgs e)
        {
            var mediaPlayer = lstExternalPlayers.SelectedItem as ConfigData.ExternalPlayer;
            if (mediaPlayer != null)
            {
                var form = new PlayerArgsForm(mediaPlayer.Args);
                form.Owner = this;
                if (form.ShowDialog() == true)
                {
                    mediaPlayer.Args = form.txtArgs.Text;
                    lblPlayerArgs.Text = mediaPlayer.Args;
                    SaveConfig();
                }
            }
        }

        private void lstExternalPlayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstExternalPlayers.SelectedIndex >= 0)
            {
                var mediaPlayer = lstExternalPlayers.SelectedItem as ConfigData.ExternalPlayer;
                if (mediaPlayer != null)
                {
                    txtPlayerCommand.Text = mediaPlayer.Command;
                    lblPlayerArgs.Text = mediaPlayer.Args;
                    infoPlayerPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    txtPlayerCommand.Text = string.Empty;
                    lblPlayerArgs.Text = string.Empty;
                    infoPlayerPanel.Visibility = Visibility.Hidden;
                }
            }
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
        private void cbxRootPage_Click(object sender, RoutedEventArgs e)
        {
            config.EnableRootPage = (bool)cbxRootPage.IsChecked;
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
            externalPlayersTab.Visibility = externalPlayersTab.Visibility  = extendersTab.Visibility = Visibility.Collapsed;
        }

        private void hdrAdvanced_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetHeader(hdrAdvanced);
            externalPlayersTab.Visibility = externalPlayersTab.Visibility = extendersTab.Visibility = Visibility.Visible;
        }

        private void ClearHeaders()
        {
            hdrAdvanced.Foreground = hdrBasic.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Gray);
            hdrAdvanced.FontWeight = hdrBasic.FontWeight = FontWeights.Normal;
            tabControl1.SelectedIndex = 0;
        }
        private void SetHeader(Label label)
        {
            ClearHeaders();
            label.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Black);
            label.FontWeight = FontWeights.Bold;
        }
        #endregion

        private void btnWeatherID_Click(object sender, RoutedEventArgs e)
        {
            if (ddlWeatherUnits.SelectedItem.ToString() == "Farenheit")
                config.YahooWeatherUnit = "f";
            else
                config.YahooWeatherUnit = "c";
            config.YahooWeatherFeed = tbxWeatherID.Text;
            SaveConfig();
        }

        private void ChangePodcastPathClick(object sender, RoutedEventArgs e) {
            BrowseForFolderDialog dlg = new BrowseForFolderDialog();

            if (true == dlg.ShowDialog(this)) {
                config.PodcastHome = dlg.SelectedFolder;
                podcastsPath.Content = dlg.SelectedFolder;
                SaveConfig();
            }
        }

        private void addPodcast_Click(object sender, RoutedEventArgs e) {
            var form = new AddPodcastForm();
            form.Owner = this;
            var result = form.ShowDialog();
            if (result == true) {
                form.RSSFeed.Save(config.PodcastHome);
                RefreshPodcasts();
            } 

        }

        private void podcastList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            VodCast vodcast = podcastList.SelectedItem as VodCast;
            if (vodcast != null) {
                podcastDetails.Visibility = Visibility.Visible;
                podcastUrl.Text = vodcast.Url;
                podcastName.Content = vodcast.Name;
                podcastDescription.Text = vodcast.Overview;
            }
        }

        private void removePodcast_Click(object sender, RoutedEventArgs e) {
            VodCast vodcast = podcastList.SelectedItem as VodCast;
            if (vodcast != null) {
                var message = "Remove \"" + vodcast.Name + "\"?";
                if (
                  MessageBox.Show(message, "Remove folder", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes) {
                    File.Delete(vodcast.Path);
                    vodcast.Parent.ValidateChildren();
                    podcastDetails.Visibility = Visibility.Hidden;
                    RefreshPodcasts();
                }
            }
        }

        private void renamePodcast_Click(object sender, RoutedEventArgs e) {
            VodCast vodcast = podcastList.SelectedItem as VodCast;
            if (vodcast != null) {
                var form = new RenameForm(vodcast.Name);
                form.Owner = this;
                var result = form.ShowDialog();
                if (result == true) {
                    vodcast.Name = form.tbxName.Text;
                    ItemCache.Instance.SaveItem(vodcast);

                    RefreshPodcasts();

                    foreach (VodCast item in podcastList.Items) {
                        if (item.Name == vodcast.Name) {
                            podcastList.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
        }


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
