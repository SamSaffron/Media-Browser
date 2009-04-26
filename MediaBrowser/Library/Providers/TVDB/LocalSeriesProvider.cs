using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Persistance;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace MediaBrowser.Library.Providers.TVDB {

    [SupportedType(typeof(Series))]
    public class LocalSeriesProvider : BaseMetadataProvider {

        [Persist]
        string metadataFile;
        [Persist]
        DateTime metadataFileDate;

        public override bool NeedsRefresh() {

            bool changed = false;

            string current = XmlLocation;
            changed = (metadataFile != current);
            changed |= current != null && (new FileInfo(current).LastWriteTimeUtc != metadataFileDate);

            return changed;
        }


        private string XmlLocation {
            get {
                string location = Path.Combine(Item.Path, "series.xml");
                if (!File.Exists(location)) {
                    location = null;
                }
                return location;
            }
        }

        public override void Fetch() {
            string location = Item.Path;
            metadataFile = XmlLocation;
            if (location == null || metadataFile == null) return;

            var series = Item as Series;
            metadataFileDate = new FileInfo(metadataFile).LastWriteTimeUtc;

            XmlDocument metadataDoc = new XmlDocument();
            metadataDoc.Load(metadataFile);

            var seriesNode = metadataDoc.SelectSingleNode("Series");
            if (seriesNode == null) {
                // support for sams metadata scraper 
                seriesNode = metadataDoc.SelectSingleNode("Item");
            }

            // exit if we have no data. 
            if (seriesNode == null) {
                return;
            }

            string id = seriesNode.SafeGetString("id");

            var p = seriesNode.SafeGetString("banner");
            if (p != null) {
                string bannerFile = System.IO.Path.Combine(location, System.IO.Path.GetFileName(p));
                if (File.Exists(bannerFile))
                    Item.BannerImagePath = bannerFile;
                else {
                    // we don't have the banner file!
                }
            }


            Item.Overview = seriesNode.SafeGetString("Overview");
            Item.Name = seriesNode.SafeGetString("SeriesName");


            string actors = seriesNode.SafeGetString("Actors");
            if (actors != null) {

                series.Actors = new List<Actor>();
                foreach (string n in actors.Split('|')) {
                    series.Actors.Add(new Actor { Name = n });
                }
            }


            string genres = seriesNode.SafeGetString("Genre");
            if (genres != null)
                series.Genres = new List<string>(genres.Trim('|').Split('|'));

            series.MpaaRating = seriesNode.SafeGetString("ContentRating");

            string runtimeString = seriesNode.SafeGetString("Runtime");
            if (!string.IsNullOrEmpty(runtimeString)) {

                int runtime;
                if (int.TryParse(runtimeString.Split(' ')[0], out runtime))
                    series.RunningTime = runtime;
            }


            string ratingString = seriesNode.SafeGetString("Rating");
            if (ratingString != null) {
                float imdbRating;
                if (float.TryParse(ratingString, out imdbRating)) {
                    series.ImdbRating = imdbRating;
                }
            }

            series.Status = seriesNode.SafeGetString("Status");

            string studios = seriesNode.SafeGetString("Network");
            if (studios != null) {
                series.Studios = new List<string>(studios.Split('|'));
            }


            // Some XML files may have incorrect series ids so do not try to set the item, 
            // this would really mess up the internet provid

        }
    }
}
