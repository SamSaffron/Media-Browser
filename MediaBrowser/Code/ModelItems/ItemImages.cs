using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library;
using Microsoft.MediaCenter.UI;
using System.Threading;
using MediaBrowser.Library.ImageManagement;
using System.Reflection;
using System.IO;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Factories;
using System.Diagnostics;
using MediaBrowser.Code.ModelItems;
using MediaBrowser.Library.Threading;
using System.Runtime.InteropServices;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Library {
    public partial class Item {



        public bool HasBannerImage {
            get {
                return (BaseItem.BannerImagePath != null) ||
                    (PhysicalParent != null ? PhysicalParent.HasBannerImage : false);
            }
        }

        AsyncImageLoader bannerImage = null;
        public Image BannerImage {
            get {
                if (!HasBannerImage) {
                    if (PhysicalParent != null) {
                        return PhysicalParent.BannerImage;
                    } else {
                        return null;
                    }

                }

                if (bannerImage == null) {
                    bannerImage = new AsyncImageLoader(
                        () => baseItem.BannerImage,
                        null,
                        () => this.FirePropertyChanged("BannerImage"));

                }

                return bannerImage.Image;
            }
        }

        public bool HasBackdropImage {
            get {
                return baseItem.BackdropImagePath != null;
            }
        }

        AsyncImageLoader backdropImage = null;
        public Image BackdropImage {
            get {

                if (!HasBackdropImage) {
                    return null;
                }

                if (backdropImage == null) {
                    backdropImage = new AsyncImageLoader(
                        () => baseItem.BackdropImage,
                        null,
                        () => this.FirePropertyChanged("BackdropImage"));
                }
                return backdropImage.Image;
            }
        }

        List<AsyncImageLoader> backdropImages = null;
        public List<Image> BackdropImages {
            get {
                if (!HasBackdropImage) {
                    return null;
                }

                if (backdropImages == null) {
                    EnsureAllBackdropsAreLoaded();
                }

                lock (backdropImages) {
                    return backdropImages.Select(async => async.Image).ToList();
                }
            }
        }

        private void EnsureAllBackdropsAreLoaded() {
            if (backdropImages == null) {
                backdropImages = new List<AsyncImageLoader>();

                // we need to do this on the thread pool ... 
                Async.Queue(() =>
                {
                    foreach (var image in baseItem.BackdropImages) {
                        // this is really subtle, we need to capture the image otherwise they will all be the same
                        var captureImage = image;
                        var backdropImage = new AsyncImageLoader(
                             () => captureImage,
                             null,
                             () => this.FirePropertiesChanged("BackdropImages", "BackdropImage"));

                        lock (backdropImages) {
                            backdropImages.Add(backdropImage);
                            // trigger a load
                            var ignore = backdropImage.Image;
                        }
                    }
                });
            }
        }

        int backdropImageIndex = 0;
        public void GetNextBackDropImage() {
            backdropImageIndex++;
            EnsureAllBackdropsAreLoaded();
            var images = new List<AsyncImageLoader>();
            lock (backdropImages) {
                images.AddRange(backdropImages);
            }
           
            if (images != null && images.Count > 0) {
                backdropImageIndex = backdropImageIndex % images.Count;
                if (images[backdropImageIndex].Image != null) {
                    backdropImage = images[backdropImageIndex];
                    FirePropertyChanged("BackdropImage");
                }
            }
        }

        AsyncImageLoader primaryImage = null;
        public Image PrimaryImage {
            get {
                if (baseItem.PrimaryImagePath == null) {
                    return DefaultImage;
                }
                EnsurePrimaryImageIsSet();
                return primaryImage.Image;
            }
        }

        private void EnsurePrimaryImageIsSet() {
            if (primaryImage == null) {
                primaryImage = new AsyncImageLoader(
                    () => baseItem.PrimaryImage,
                    DefaultImage,
                    PrimaryImageChanged);
                var ignore = primaryImage.Image;
            }
        }

        void PrimaryImageChanged() {
            FirePropertiesChanged("PrimaryImage", "PreferredImage", "PrimaryImageSmall", "PreferredImageSmall");
        }

        AsyncImageLoader primaryImageSmall = null;
        // these all come in from the ui thread so no sync is required. 
        public Image PrimaryImageSmall {
            get {

                if (baseItem.PrimaryImagePath == null) {
                    return DefaultImage;
                }
                EnsurePrimaryImageIsSet();
                if (!primaryImage.IsLoaded ||
                    preferredImageSmallSize == null ||
                    preferredImageSmallSize.Width < 1 ||
                    preferredImageSmallSize.Height < 1) {
                    // we have no aspect ratio... so small image may be bodge. 
                    return DefaultImage;
                }

                if (primaryImageSmall == null) {
                    float aspect = primaryImage.Size.Height / (float)primaryImage.Size.Width;
                    float constraintAspect = preferredImageSmallSize.Height / (float)preferredImageSmallSize.Width;

                    primaryImageSmall = new AsyncImageLoader(
                        () => baseItem.PrimaryImage,
                        DefaultImage,
                        PrimaryImageChanged);

                    smallImageIsDistorted = Math.Abs(aspect - constraintAspect) < Config.Instance.MaximumAspectRatioDistortion;

                    if (smallImageIsDistorted) {
                        primaryImageSmall.Size = preferredImageSmallSize;
                    } else {

                        int width = preferredImageSmallSize.Width;
                        int height = preferredImageSmallSize.Height;

                        if (aspect > constraintAspect) {
                            width = (int)((float)height / aspect);
                        } else {
                            height = (int)((float)width * aspect);
                        }

                        primaryImageSmall.Size = new Size(width, height);
                    }

                    FirePropertyChanged("SmallImageIsDistorted");
                }

                return primaryImageSmall.Image;
            }
        }

        bool smallImageIsDistorted = false;
        public bool SmallImageIsDistorted {
            get {
                return smallImageIsDistorted;
            }
        }

        public Image PreferredImage {
            get {
                return preferBanner ? BannerImage : PrimaryImage;
            }
        }


        public Image PreferredImageSmall {
            get {
                return preferBanner ? BannerImage : PrimaryImageSmall;
            }
        }

        Microsoft.MediaCenter.UI.Size preferredImageSmallSize;
        public Microsoft.MediaCenter.UI.Size PreferredImageSmallSize {
            get {
                return preferredImageSmallSize;
            }
            set {
                if (value != preferredImageSmallSize) {
                    preferredImageSmallSize = value;
                    FirePropertyChanged("PreferredImageSmall");
                    FirePropertyChanged("PrimaryImageSmall");
                }
            }
        }


        public bool HasPrimaryImage {
            get { return baseItem.PrimaryImagePath != null; }
        }

        public bool HasPreferredImage {
            get { return (PreferBanner ? HasBannerImage : HasPrimaryImage); }
        }

        bool preferBanner;
        public bool PreferBanner {
            get {
                return preferBanner;
            }
            set {
                preferBanner = value;
                FirePropertyChanged("HasPreferredImage");
                FirePropertyChanged("PreferredImage");
            }
        }


        internal float PrimaryImageAspect {
            get {
                return GetAspectRatio(baseItem.PrimaryImagePath);
            }
        }

        internal float BannerImageAspect {
            get {
                return GetAspectRatio(baseItem.BannerImagePath);
            }
        }

        float GetAspectRatio(string path) {

            float aspect = 0;
            if (path != null) {
                var image = LibraryImageFactory.Instance.GetImage(path);
                aspect = ((float)image.Height) / (float)image.Width;
            }
            return aspect;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);


        // I can not figure out any way to pass the size of an element to the code
        // so I cheat 
        public void SetPreferredImageSmallToEstimatedScreenSize() {

            var folder = this as FolderModel;
            if (folder == null) return;

            Size size = new Size(200, 200);

            try {

                // find ehshell 
                var ehshell = Process.GetProcessesByName("ehshell").First().MainWindowHandle;

                if (ehshell != IntPtr.Zero) {

                    RECT windowSize;
                    GetWindowRect(ehshell, out windowSize);

                    // why 3, well the large images are in general a 3rd the size of the screen. 
                    size = new Size(
                        (windowSize.Right - windowSize.Left) / 3,
                        (windowSize.Bottom - windowSize.Top) / 3
                        );



                }
            } catch (Exception e) {
                Logger.ReportException("Failed to gather size information, made a guess ", e);
            }

            foreach (var item in folder.Children) {
                item.PreferredImageSmallSize = size;
            }

        }


        static Image DefaultVideoImage = new Image("res://ehres!MOVIE.ICON.DEFAULT.PNG");
        static Image DefaultActorImage = new Image("resx://MediaBrowser/MediaBrowser.Resources/MissingPerson");
        static Image DefaultStudioImage = new Image("resx://MediaBrowser/MediaBrowser.Resources/BlankGraphic");
        static Image DefaultFolderImage = new Image("resx://MediaBrowser/MediaBrowser.Resources/folder");

        public Image DefaultImage {
            get {
                Image image = DefaultFolderImage;

                if (baseItem is Video) {
                    image = DefaultVideoImage;
                } else if (baseItem is Person) {
                    image = DefaultActorImage;
                } else if (baseItem is Studio) {
                    image = DefaultStudioImage;
                }

                return image;
            }
        }

    }
}
