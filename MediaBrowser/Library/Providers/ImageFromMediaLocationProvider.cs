using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library.Providers
{
    [ProviderPriority(15)]
    [SupportedType(typeof(BaseItem))]
    class ImageFromMediaLocationProvider : BaseMetadataProvider
    {

        const string Primary = "folder";
        const string Banner = "banner";
        const string Backdrop = "backdrop";
        

        [Persist]
        List<string> backdropPaths = new List<string>();
        [Persist]
        string bannerPath;
        [Persist]
        string primaryPath;


        protected virtual string Location { get { return Item.Path; } }

        public override void Fetch()
        {
            if (Location == null) return;

            bool isDir = Directory.Exists(Location);
            bool isFile = File.Exists(Location);

            if (isDir || isFile)
            {
                Item.PrimaryImagePath = primaryPath = FindImage(Primary);
            }
            if (isDir)
            {
                Item.BannerImagePath = bannerPath = FindImage(Banner);
                backdropPaths = FindImages(Backdrop);
                if (backdropPaths.Count > 0) {
                    Item.BackdropImagePaths = backdropPaths;
                }
            }
        }

        private List<string> FindImages(string name) {
            var paths = new List<string>();

            string postfix = "";
            int index = 1;

            do
            {
                string currentImage = FindImage(name + postfix);
                if (currentImage == null) break;
                paths.Add(currentImage);
                postfix = index.ToString();
                index++;

            } while (true);

            return paths;
        }

        private string FindImage(string name)
        {
            string file = Path.Combine(Location, name + ".jpg");
            if (File.Exists(file))
                return file;
            
            file = Path.Combine(Location, name + ".png");
            if (File.Exists(file))
                return file;

            if (name == "folder") // we will also look for images that match by name in the same location for the primary image
            {
                var dir = Path.GetDirectoryName(Location);
                var filename_without_extension = Path.GetFileNameWithoutExtension(Location);

                // dir was null for \\10.0.0.4\dvds - workaround
                if (dir != null && filename_without_extension != null)
                {
                    file = Path.Combine(dir, filename_without_extension);
                    if (File.Exists(file + ".jpg"))
                        return file + ".jpg";
                    if (File.Exists(file + ".png"))
                        return file + ".png";
                }
            }
            return null;
        }

        Dictionary<string, string> PathLookup {
            get {
                var dict = new Dictionary<string, string>();
                dict[Primary] = primaryPath;
                dict[Banner] = bannerPath;
                return dict;
            }
        }

        public override bool NeedsRefresh()
        {
            // nothing we can do with empty location 
            if (Location == null) return false;

            // image moved or image deleted
            bool changed = FindImage(Primary) != primaryPath;
            changed |= FindImage(Banner) != bannerPath;

            var realBackdrops = FindImages(Backdrop);
            changed |= realBackdrops.Except(backdropPaths).Count() != 0;
            changed |= backdropPaths.Except(realBackdrops).Count() != 0;

            return changed;
        }

    }
}
