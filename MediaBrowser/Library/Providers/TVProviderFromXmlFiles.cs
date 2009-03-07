using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using MediaBrowser.LibraryManagement;

namespace MediaBrowser.Library.Providers
{
    class TVProviderFromXmlFiles : IMetadataProvider
    {
        private static readonly string ProviderName = "TVProviderFrmXmlFiles";
        #region IMetadataProvider Members

        public ItemType SupportedTypes
        {
            get { return ItemType.Series | ItemType.Season | ItemType.Episode; }
        }

        public bool UsesInternet { get { return false; } }

        public bool NeedsRefresh(Item item, ItemType type)
        {
            string lastFile = null;
            if (item.Metadata.ProviderData.ContainsKey(ProviderName + ":File"))
                lastFile = item.Metadata.ProviderData[ProviderName + ":File"];
            string mfile = XmlLocation(item, type);
            if (!File.Exists(mfile))
                mfile = null;
            if (lastFile!=mfile)
                return true;
            if ((mfile == null) && (lastFile == null))
                return false;

            if (item.Metadata.ProviderData.ContainsKey(ProviderName + ":ModTime"))
            {
                DateTime modTime = new FileInfo(mfile).LastWriteTimeUtc;
                string lastTime = item.Metadata.ProviderData[ProviderName + ":ModTime"];
                DateTime dt = new DateTime(long.Parse(lastTime));
                if (modTime <= dt)
                    return false;
            }
            return true;
        }

        private string XmlLocation(Item item, ItemType type)
        {
            string location = item.Source.Location;
            switch (type)
            {
                case ItemType.Series:
                    return Path.Combine(location, "series.xml");
                case ItemType.Season:
                    return Path.Combine(location, "series.xml");
                case ItemType.Episode:
                    string metadataFolder = Path.Combine(Path.GetDirectoryName(location), "metadata");
                    string file = Path.GetFileNameWithoutExtension(location);
                    return Path.Combine(metadataFolder, file + ".xml");
                default:
                    throw new NotSupportedException("TVProviderFromXmlFiles does not support " + type.ToString());
            }
        }

        public void Fetch(Item item, ItemType type, MediaMetadataStore store, bool fastOnly)
        {
            switch (type)
            {
                case ItemType.Series:
                    SeriesData(item, store);
                    break;
                case ItemType.Season:
                    SeasonData(item, store);
                    break;
                case ItemType.Episode:
                    EpisodeData(item, store);
                    break;
                default:
                    throw new NotSupportedException("TVProviderFromXmlFiles does not support " + type.ToString());
            }
        }

        #endregion

        private void EpisodeData(Item item, MediaMetadataStore store)
        {
            string mfile = XmlLocation(item, ItemType.Episode);
            string metadataFolder = Path.GetDirectoryName(mfile);
            if (File.Exists(mfile))
            {
                store.ProviderData[ProviderName + ":ModTime"] = new FileInfo(mfile).LastWriteTimeUtc.Ticks.ToString();
                store.ProviderData[ProviderName + ":File"] = mfile;
                XmlDocument metadataDoc = new XmlDocument();
                metadataDoc.Load(mfile);
                if (store.PrimaryImage == null)
                {
                    var p = metadataDoc.SafeGetString("Item/filename");
                    if (p != null && p.Length > 0)
                    {
                        string image = System.IO.Path.Combine(metadataFolder, System.IO.Path.GetFileName(p));
                        if (File.Exists(image))
                            store.PrimaryImage = new ImageSource { OriginalSource = image };
                    }
                }
                if (store.Overview == null)
                    store.Overview = metadataDoc.SafeGetString("Item/Overview");
                if (store.EpisodeNumber == null)
                    store.EpisodeNumber = metadataDoc.SafeGetString("Item/EpisodeNumber");
                if (store.Name == null)
                    store.Name = store.EpisodeNumber + " - " + metadataDoc.SafeGetString("Item/EpisodeName");
                //store.Name = metadataDoc.SafeGetString("Item/ShowName");
                if (store.SeasonNumber == null)
                    store.SeasonNumber = metadataDoc.SafeGetString("Item/SeasonNumber");
                if (store.ImdbRating == null)
                    store.ImdbRating = metadataDoc.SafeGetSingle("Item/Rating", (float)-1, 10);
                if (store.FirstAired == null)
                    store.FirstAired = metadataDoc.SafeGetString("Item/FirstAired");
                if (store.Writers == null)
                {
                    string writers = metadataDoc.SafeGetString("Item/Writer");
                    if (writers != null)
                        store.Writers = new List<string>(writers.Trim('|').Split('|'));
                }
                if (store.Directors == null)
                {
                    string directors = metadataDoc.SafeGetString("Item/Director");
                    if (directors != null)
                        store.Directors = new List<string>(directors.Trim('|').Split('|'));
                }
                if (store.Actors == null)
                {
                    string actors = metadataDoc.SafeGetString("Item/GuestStars");
                    if (actors != null)
                    {
                        if (store.Actors == null)
                            store.Actors = new List<Actor>();
                        foreach (string n in actors.Split('|'))
                        {
                            store.Actors.Add(new Actor { Name = n });
                        }
                    }
                }
            }
        }

        private void SeasonData(Item item, MediaMetadataStore store)
        {
            string seasonNum = Helper.SeasonNumberFromFolderName(item.Source.Location);
            SeriesData(item, store);
            if (!string.IsNullOrEmpty(seasonNum))
            {
                if (store.SeasonNumber == null)
                    store.SeasonNumber = seasonNum;
                if (store.Name == null)
                    store.Name = "Season " + seasonNum;
            }
        }

        private void SeriesData(Item item, MediaMetadataStore store)
        {
            string location = item.Source.Location;
            string tmpString;
            if (location != null)
            {
                string file = XmlLocation(item, ItemType.Series);
                if (File.Exists(file))
                {
                    store.ProviderData[ProviderName + ":ModTime"] = new FileInfo(file).LastWriteTimeUtc.Ticks.ToString();
                    store.ProviderData[ProviderName + ":File"] = file;
                    XmlDocument metadataDoc = new XmlDocument();
                    metadataDoc.Load(file);

                    var seriesNode = metadataDoc.SelectSingleNode("Series");
                    if (seriesNode == null)
                    {
                        // support for sams metadata scraper 
                        seriesNode = metadataDoc.SelectSingleNode("Item");
                    }

                    // exit if we have no data. 
                    if (seriesNode == null)
                    {
                        return; 
                    }

                    string id = seriesNode.SafeGetString("id");

                    if (id != null)
                    {
                        store.ProviderData["TvDb:SeriesId"] = id;  // caching this means the TvDbProvider can then fill in season and episode data automatically
                        item.Metadata.ProviderData["TvDb:SeriesId"] = id; // not strictly what we should be doing here but helps later providers in the flow
                    }
                    if (store.BannerImage == null)
                    {
                        var p = seriesNode.SafeGetString("banner");
                        if (p != null)
                        {
                            string bannerFile = System.IO.Path.Combine(location, System.IO.Path.GetFileName(p));
                            if (File.Exists(bannerFile))
                                store.BannerImage = new ImageSource { OriginalSource = bannerFile };
                            else
                            {
                                // we don;t have the banner file!
                            }
                        }
                    }
                    if (store.Overview == null)
                        store.Overview = seriesNode.SafeGetString("Overview");
                    if (store.Name == null)
                        store.Name = seriesNode.SafeGetString("SeriesName");
                    if (store.Actors == null)
                    {
                        string actors = seriesNode.SafeGetString("Actors");
                        if (actors != null)
                        {
                            if (store.Actors == null)
                                store.Actors = new List<Actor>();
                            foreach (string n in actors.Split('|'))
                            {
                                store.Actors.Add(new Actor { Name = n });
                            }
                        }
                    }
                    if (store.Genres == null)
                    {
                        string genres = seriesNode.SafeGetString("Genre");
                        if (genres != null)
                            store.Genres = new List<string>(genres.Trim('|').Split('|'));
                    }
                    if (store.MpaaRating == null)
                        store.MpaaRating = seriesNode.SafeGetString("ContentRating");
                    if (store.RunningTime == null) {
                        tmpString = seriesNode.SafeGetString("Runtime");
                        if (tmpString != null)
                          store.RunningTime = int.Parse(tmpString);
                    }
                    if (store.ImdbRating == null) {
                        tmpString = seriesNode.SafeGetString("Rating");
                        if (tmpString != null)  store.ImdbRating = float.Parse(tmpString);
                    }
                    if (store.DataSource == null)
                        store.DataSource = seriesNode.SafeGetString("Network");
                    if (store.Status == null)
                        store.Status = seriesNode.SafeGetString("Status");

                    if (store.Studios == null)
                    {
                        string studios = seriesNode.SafeGetString("Network");
                        if (studios != null)
                        {
                            if (store.Studios == null)
                                store.Studios = new List<Studio>();
                            foreach (string n in studios.Split('|'))
                            {
                                store.Studios.Add(new Studio { Name = n });
                            }
                        }
                    }
                }
            }
        }
    }
}
