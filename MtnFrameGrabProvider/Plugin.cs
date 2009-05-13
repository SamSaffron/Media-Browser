using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library;
using System.IO;
using MediaBrowser.Library.Configuration;
using System.Reflection;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using ICSharpCode.SharpZipLib.Zip;
using MediaBrowser.Library.Extensions;

namespace MtnFrameGrabProvider {
    public class Plugin : IPlugin {

        public static ILogger Logger { get; private set; }

        public static readonly string MtnPath = Path.Combine(ApplicationPaths.AppPluginPath, "mtn");
        public static readonly string MtnExe = Path.Combine(MtnPath, "mtn.exe");
        public static readonly string FrameGrabsPath = Path.Combine(MtnPath, "FrameGrabs");

        public void Init(LibraryConfig config) {

            EnsureMtnIsExtracted();

            Logger = config.Logger;

            config.Providers.Add(new MetadataProviderFactory(typeof(FrameGrabProvider)));

            config.ImageResolvers.Add(path =>
            {
                if (path.ToLower().StartsWith("mtn")) {
                    return new GrabImage();
                }
                return null;
            });
        }

        public string Name {
            get { return "High Quality Thumbnails"; }
        }

        public string Description {
            get { return "High quality automatic thumbnails powered by the mtn project. http://moviethumbnail.sourceforge.net"; }
        }

        public static void EnsureMtnIsExtracted() {
           
            if (!Directory.Exists(MtnPath)) {
                Directory.CreateDirectory(MtnPath);
                Directory.CreateDirectory(FrameGrabsPath);
                

                string name = "MtnFrameGrabProvider.mtn.zip";
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name); 
                
                using (var zip = new ZipInputStream(stream)) {

                    ZipEntry entry; 
                    while ((entry = zip.GetNextEntry()) != null) {
                        string destination = Path.Combine(MtnPath, entry.Name);
                        File.WriteAllBytes(destination, zip.ReadAllBytes());
                    }
                }
            }
        }


    }
}
