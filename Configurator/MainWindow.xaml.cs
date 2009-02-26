﻿using System;
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
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            infoPanel.Visibility = Visibility.Hidden;

            // first time the wizard has run 
            if (Config.Instance.InitialFolder != Helper.AppInitialDirPath)
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

            // TODO : migrate any user created shortcuts to VFs 

            Config.Instance.InitialFolder = Helper.AppInitialDirPath;

            RefreshItems();
        }

        private void RefreshItems()
        {

            folderList.Items.Clear();

            foreach (var filename in Directory.GetFiles(Config.Instance.InitialFolder))
            {
                try
                {
                    folderList.Items.Add(new VirtualFolder(filename));
                }
                catch (Exception e)
                {
                    MessageBox.Show("Invalid file detected in the initial folder!" + e.ToString());
                    // TODO : alert about dodgy VFs and delete them
                }
            }
        }

        private static void MigrateOldInitialFolder()
        {
            var path = Config.Instance.InitialFolder;
            if (Config.Instance.InitialFolder == Helper.MY_VIDEOS)
            {
                path = Helper.MyVideosPath;
            }

            foreach (var file in Directory.GetFiles(path))
            {
                if (file.ToLower().EndsWith(".vf") || file.ToLower().EndsWith(".lnk"))
                {

                    File.Copy(file, System.IO.Path.Combine(Helper.AppInitialDirPath, System.IO.Path.GetFileName(file)), true);
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

        private void btnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            //var browser = new System.Windows.Forms.FolderBrowserDialog();
            //var result = browser.ShowDialog();
            //if (result == System.Windows.Forms.DialogResult.OK)
            //{
            //    WriteVirtualFolder(browser.SelectedPath);
            //    RefreshItems();
            //}
            FolderBrowser browser = new FolderBrowser();
            var result = browser.ShowDialog();
            
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                WriteVirtualFolder(browser.DirectoryPath);
                RefreshItems();
            }
        }

        private void btnRename_Click(object sender, RoutedEventArgs e)
        {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder != null)
            {
                var form = new RenameForm(virtualFolder.Name);
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
                }
            }
            infoPanel.Visibility = Visibility.Hidden;
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
                folderImage.Source = new BitmapImage ( new Uri ( virtualFolder.ImagePath ));
            }
        }

        private void btnAddSubFolder_Click(object sender, RoutedEventArgs e)
        {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder == null) return;

            //var browser = new System.Windows.Forms.FolderBrowserDialog();
            //var result = browser.ShowDialog();
            //if (result == System.Windows.Forms.DialogResult.OK)
            //{
            //    virtualFolder.AddFolder(browser.SelectedPath);
            //    folderList_SelectionChanged(this, null);
            //}

            FolderBrowser browser = new FolderBrowser();
            var result = browser.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                virtualFolder.AddFolder(browser.DirectoryPath);
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
                    folderImage.Source = new BitmapImage( new Uri( virtualFolder.ImagePath ));
                }
                else
                {
                    folderImage.Source = null;
                }

                infoPanel.Visibility = Visibility.Visible;
            }
        }

        private void internalFolder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }







    }


    // we really should be reusing this code. 
    public class VirtualFolder
    {

        List<string> folders = new List<string>();
        string image;

        string path;

        public VirtualFolder(string path)
        {
            this.path = path;
            foreach (var line in File.ReadAllLines(path))
            {
                var colonPos = line.IndexOf(':');
                if (colonPos <= 0)
                {
                    continue;
                }

                var type = line.Substring(0, colonPos);
                var filename = line.Substring(colonPos + 1).Trim();

                if ((!File.Exists(filename) && type == "image") || (!Directory.Exists(filename) && type == "folder"))
                {
                    MessageBox.Show(string.Format("Ignoring invalid file {0} in virtual folder {1}", filename, path));
                    continue;
                }
                if (type == "image")
                {
                    image = filename;
                }
                else if (type == "folder")
                {
                    folders.Add(filename);
                }

            }
        }

        public string Path { get { return path; } }

        public void RemoveFolder(string folder)
        {
            folders.Remove(folder);
            Save();
        }

        public void AddFolder(string folder)
        {
            folders.Add(folder);
            Save();
        }

        public void Save()
        {

            StringBuilder contents = new StringBuilder();
            if (image != null && File.Exists(image))
            {
                contents.AppendLine("image: " + image);
            }

            foreach (var folder in folders)
            {
                if (Directory.Exists(folder))
                {
                    contents.AppendLine("folder: " + folder);
                }
            }

            File.WriteAllText(path, contents.ToString());
        }

        public List<string> Folders { get { return folders; } }

        public string ImagePath
        {
            get { return image; }
            set { image = value; Save(); }
        }

        public string Name
        {
            get { return System.IO.Path.GetFileNameWithoutExtension(path); }
            set
            {
                string newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), value + ".vf");
                File.Move(path, newPath);
                path = newPath;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }

}
