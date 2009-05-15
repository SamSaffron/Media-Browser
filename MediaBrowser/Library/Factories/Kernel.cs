using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.ImageManagement;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Library.RemoteControl;
using MediaBrowser.Library.Metadata;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Configuration;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Extensions;
using System.IO;
using System.Diagnostics;
using System.Net;

namespace MediaBrowser.Library {

    [Flags]
    public enum KernelLoadDirective { 
        None,

        /// <summary>
        /// Ensure plugin dlls are not locked 
        /// </summary>
        ShadowPlugins
    } 

    /// <summary>
    /// This is the one class that contains all the dependencies. 
    /// </summary>
    public class Kernel {

        static object sync = new object();
        static Kernel kernel;

        private static MultiLogger GetDefaultLogger(ConfigData config) {
            var logger = new MultiLogger();

            if (config.EnableTraceLogging) {
                logger.AddLogger(new FileLogger(ApplicationPaths.AppLogPath));
#if (!DEBUG)
                logger.AddLogger(new TraceLogger());
#endif
            }
#if DEBUG
            logger.AddLogger(new TraceLogger());
#endif
            return logger;
        }

        public static void Init() {
            Init(KernelLoadDirective.None);
        }

        public static void Init(ConfigData config) {
           Init(KernelLoadDirective.None, config);
        }

        public static void Init(KernelLoadDirective directives) { 
            Init(directives, ConfigData.FromFile(ApplicationPaths.ConfigFile));
        } 

        public static void Init(KernelLoadDirective directives, ConfigData config) {
            lock (sync) {
                // its critical to have the logger initialized early so initialization routines can use the right logger.
                Logger.LoggerInstance = GetDefaultLogger(config);
                var kernel = GetDefaultKernel(config, directives);
                Kernel.Instance = kernel;

                // add the podcast home
                var podcastHome = kernel.GetItem<Folder>(kernel.ConfigData.PodcastHome);
                if (podcastHome != null && podcastHome.Children.Count > 0) {
                    kernel.RootFolder.AddVirtualChild(podcastHome);
                }
            }
        } 

        private static string ResolveInitialFolder(string start) {
            if (start == Helper.MY_VIDEOS)
                start = Helper.MyVideosPath;
            return start;
        }

        private static ChainedEntityResolver DefaultResolver(ConfigData config) {
            return
                new ChainedEntityResolver() { 
                new VodCastResolver(),
                new EpisodeResolver(), 
                new SeasonResolver(), 
                new SeriesResolver(), 
                new MovieResolver(
                        config.EnableMoviePlaylists?config.PlaylistLimit:1, 
                        config.EnableNestedMovieFolders), 
                new FolderResolver(),
            };
        }

        private static List<ImageResolver> DefaultImageResolvers {
            get {
                return new List<ImageResolver>() {
                    (path) =>  { 
                        if (path != null && path.ToLower().StartsWith("http")) {
                            return new RemoteImage();
                        }
                        return null;
                    }
                };
            }
        }

        static List<IPlugin> DefaultPlugins(bool forceShadow) {
            List<IPlugin> plugins = new List<IPlugin>();
            foreach (var file in Directory.GetFiles(ApplicationPaths.AppPluginPath)) {
                if (file.ToLower().EndsWith(".dll")) {
                    try {
                        plugins.Add(new Plugin(Path.Combine(ApplicationPaths.AppPluginPath, file),forceShadow));
                    } catch (Exception ex) {
                        Debug.Assert(false, "Failed to load plugin: " + ex.ToString());
                        Logger.ReportException("Failed to load plugin", ex);
                    }
                }
            }
            return plugins;
        }

        static Kernel GetDefaultKernel(ConfigData config, KernelLoadDirective loadDirective) {

            var kernel = new Kernel()
            {
                PlaybackControllers = new List<IPlaybackController>(),
                MetadataProviderFactories = MetadataProviderHelper.DefaultProviders(),
                ImageResolvers = DefaultImageResolvers,
                ConfigData = config,
                ItemRepository = new SafeItemRepository(new ItemRepository()),
                MediaLocationFactory = new MediaBrowser.Library.Factories.MediaLocationFactory()
            };


            kernel.EntityResolver = DefaultResolver(kernel.ConfigData);
            kernel.RootFolder = kernel.GetItem<AggregateFolder>(ResolveInitialFolder(kernel.ConfigData.InitialFolder));

            kernel.Plugins = DefaultPlugins((loadDirective & KernelLoadDirective.ShadowPlugins) == KernelLoadDirective.ShadowPlugins);

            // initialize our plugins (maybe we should add a kernel.init ? )
            // The ToList enables us to remove stuff from the list if there is a failure
            foreach (var plugin in kernel.Plugins.ToList()) {
                try {
                    plugin.Init(kernel);
                } catch (Exception e) {
                    Logger.ReportException("Failed to initialize Plugin : " + plugin.Name, e);
                    kernel.Plugins.Remove(plugin);
                }
            }

            return kernel;

        }

        public static Kernel Instance {
            get {
                if (kernel != null) return kernel;
                lock (sync) {
                    if (kernel != null) return kernel;
                    Init(); 
                }
                return kernel;
            }
            set {
                lock (sync) {
                    kernel = value;
                }
            }
        }


        public AggregateFolder RootFolder { get; set; }
        public List<IPlugin> Plugins { get; set; }
        public List<IPlaybackController> PlaybackControllers { get; set; }
        public List<MetadataProviderFactory> MetadataProviderFactories { get; set; }
        public List<ImageResolver> ImageResolvers { get; set; }
        public ChainedEntityResolver EntityResolver { get; set; }
        public ConfigData ConfigData { get; set; }
        public IItemRepository ItemRepository { get; set; }
        public IMediaLocationFactory MediaLocationFactory { get; set; }

        public T GetItem<T>(string path) where T : BaseItem {
            return GetItem<T>(GetLocation<IMediaLocation>(path));
        }

        public T GetItem<T>(IMediaLocation location) where T : BaseItem {
            BaseItem item = null;

            BaseItemFactory factory;
            IEnumerable<InitializationParameter> setup;

            EntityResolver.ResolveEntity(location, out factory, out setup);
            if (factory != null) {
                item = factory.CreateInstance(location, setup);
            }
            return item as T;
        }

        public BaseItem GetItem(IMediaLocation location) {
            return GetItem<BaseItem>(location);
        }

        public T GetLocation<T>(string path) where T : class, IMediaLocation {
            return MediaLocationFactory.Create(path) as T;
        }

        public IMediaLocation GetLocation(string path) {
            return GetLocation<IMediaLocation>(path);
        }

        public LibraryImage GetImage(string path) {
            return LibraryImageFactory.Instance.GetImage(path);
        }

        public void DeletePlugin(IPlugin plugin) {
            if (!(plugin is Plugin)) {
                Logger.ReportWarning("Attempting to remove a plugin that we have no location for!");
                throw new ApplicationException("Attempting to remove a plugin that we have no location for!");
            }

            (plugin as Plugin).Delete();
            Plugins.Remove(plugin);
        }

        public void InstallPlugin(string path) {
            string target = Path.Combine(ApplicationPaths.AppPluginPath, Path.GetFileName(path));

            if (path.ToLower().StartsWith("http")) {
                WebRequest request = WebRequest.Create(path);
                using (var response = request.GetResponse()) {
                    using (var stream = response.GetResponseStream()) {
                        File.WriteAllBytes(target, stream.ReadAllBytes());
                    }
                }
            } else {
                File.Copy(path, target);
            }

            var plugin = Plugin.FromFile(target, true);
            plugin.Init(this);
            Plugins.Add(plugin);
        }
    }
}
