using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Persistance;
using System.IO;
using System.Diagnostics;
using System.Xml;

namespace MediaBrowser.Library.Providers.TVDB {

    [SupportedType(typeof(Episode))]
    public class LocalEpisodeProvider : BaseMetadataProvider  {
        
        [Persist]
        string metadataFile;
        [Persist]
        DateTime metadataFileDate;

        public Episode Episode { get { return (Episode)Item; } }

        public override bool NeedsRefresh() {

            bool changed;
            string mfile = XmlLocation;

            changed = (metadataFile != mfile);

            if (!changed && mfile != null) {
                changed = (new FileInfo(mfile).LastWriteTimeUtc != metadataFileDate);
            }
            return changed;
        }

        public override void Fetch() {
            Episode episode = Episode;
            Debug.Assert(episode != null);

            // store the location so we do not fetch again 
            metadataFile = XmlLocation;
            // no data, do nothing
            if (metadataFile == null) return;

            metadataFileDate = new FileInfo(metadataFile).LastWriteTimeUtc;

            string metadataFolder = Path.GetDirectoryName(metadataFile);
            
            XmlDocument metadataDoc = new XmlDocument();
            metadataDoc.Load(metadataFile);

            var p = metadataDoc.SafeGetString("Item/filename");
            if (p != null && p.Length > 0) {
                string image = System.IO.Path.Combine(metadataFolder, System.IO.Path.GetFileName(p));
                if (File.Exists(image))
                    Item.PrimaryImagePath = image;
            }


            episode.Overview = metadataDoc.SafeGetString("Item/Overview");
            episode.EpisodeNumber = metadataDoc.SafeGetString("Item/EpisodeNumber");
            episode.Name = episode.EpisodeNumber + " - " + metadataDoc.SafeGetString("Item/EpisodeName");
            episode.SeasonNumber = metadataDoc.SafeGetString("Item/SeasonNumber");
            episode.ImdbRating = metadataDoc.SafeGetSingle("Item/Rating", (float)-1, 10);
            episode.FirstAired = metadataDoc.SafeGetString("Item/FirstAired");


            string writers = metadataDoc.SafeGetString("Item/Writer");
            if (writers != null)
                episode.Writers = new List<string>(writers.Trim('|').Split('|'));


            string directors = metadataDoc.SafeGetString("Item/Director");
            if (directors != null)
                episode.Directors = new List<string>(directors.Trim('|').Split('|'));


            var actors = ActorListFromString(metadataDoc.SafeGetString("Item/GuestStars"));
            if (actors != null) {
                if (episode.Actors == null)
                    episode.Actors = new List<Actor>();
                episode.Actors = actors;
            }
        }


        private static List<Actor> ActorListFromString(string unsplit) {

            List<Actor> actors = null;
            if (unsplit != null) {
                actors = new List<Actor>();
                foreach (string name in unsplit.Split('|')) {
                    actors.Add(new Actor { Name = name });
                }
            }
            return actors;
        }

        private string XmlLocation {
            get {

                string metadataFolder = Path.Combine(Path.GetDirectoryName(Item.Path), "metadata");
                string file = Path.GetFileNameWithoutExtension(Item.Path);
                
                var location = Path.Combine(metadataFolder, file + ".xml");
                if (!File.Exists(location)) {
                    location = null;
                }

                return location;
            }
        }

    }
}
