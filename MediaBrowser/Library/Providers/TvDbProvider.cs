using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Diagnostics;
using MediaBrowser.LibraryManagement;

namespace MediaBrowser.Library.Providers
{
    class TvDbProvider : IMetadataProvider
    {
        private static readonly string apiKey = "B89CE93890E9419B";
        private static readonly string roolUrl = "http://www.thetvdb.com/api/";
        private static readonly string bannerUrl = "http://www.thetvdb.com/banners/";
        private static readonly string seriesQuery = "GetSeries.php?seriesname={0}";
        private static readonly string seriesGet = "http://www.thetvdb.com/api/{0}/series/{1}/{2}.xml";
        private static readonly string episodeQuery = "http://www.thetvdb.com/api/{0}/series/{1}/default/{2}/{3}/{4}.xml";
        private static readonly string ProviderName = "TvDbProvider";

        #region IMetadataProvider Members

        public ItemType SupportedTypes
        {
            get { return ItemType.Series | ItemType.Season | ItemType.Episode; }
        }

        public bool UsesInternet { get { return true; } }

        public bool NeedsRefresh(Item item, ItemType type)
        {
            string ourSeriesId = null;
            string generalSeriesId = GetSeriesId(item, null);
            if (item.Metadata.ProviderData.ContainsKey(ProviderName + ":SeriesId"))
                ourSeriesId = item.Metadata.ProviderData[ProviderName + ":SeriesId"];
            else if ((item.PhysicalParent != null) && (item.PhysicalParent.Metadata.ProviderData.ContainsKey(ProviderName + ":SeriesId")))
                ourSeriesId = item.PhysicalParent.Metadata.ProviderData[ProviderName + ":SeriesId"];

            if (ourSeriesId != generalSeriesId) // even if we hadn't identified the series someone else now has so we might be able to get some extra data
                return true;

            if (item.Metadata.ProviderData.ContainsKey(ProviderName + ":Date"))
            {
                if (DateTime.Today.Subtract(item.Source.CreatedDate).TotalDays > 180)
                    return false; // don't trigger a refresh data for item that are more than 6 months old and have been refreshed before
                string date = item.Metadata.ProviderData[ProviderName + ":Date"];
                DateTime dt = DateTime.ParseExact(date, "yyyyMMdd", null);
                if (DateTime.Today.Subtract(dt).TotalDays < 14) // only refresh every 14 days
                    return false;
            }
            return true;
        }

        private string GetSeriesId(Item item, MediaMetadataStore store)
        {
            if ((store!=null) && (store.ProviderData.ContainsKey("TvDb:SeriesId")))
                return store.ProviderData["TvDb:SeriesId"];
            else
            {
                if (item.Metadata.ProviderData.ContainsKey("TvDb:SeriesId"))
                    return item.Metadata.ProviderData["TvDb:SeriesId"];
                else
                {
                    Item parent = item.PhysicalParent;
                    while (parent != null)
                    {
                        parent.EnsureMetadataLoaded();
                        if (parent.Metadata.ProviderData.ContainsKey("TvDb:SeriesId"))
                            return parent.Metadata.ProviderData["TvDb:SeriesId"];
                        parent = parent.PhysicalParent;
                    }
                }
            }
            Item p = item.PhysicalParent;
            while ((p != null) && (p.Source.ItemType != ItemType.Series))
                p = p.PhysicalParent;
            if (p != null)
                return FindSeries(p.Source.Name);
            return null;
        }      

        public void Fetch(Item item, ItemType type, MediaMetadataStore store, bool fastOnly)
        {
            if (fastOnly)
                return;
            string seriesId = GetSeriesId(item, store);
            /*
            if ((type==ItemType.Series) && (seriesId != "") && (!item.Metadata.ProviderData.ContainsKey(ProviderName + ":SeriesId")))
            {
                // another provider has found data but we never have, this is probably 
                // the first load of a system with lots of local xml data already present
                // for speed we won't try to update it this time, they will all happen
                // next time but it won't be so evident as most of the data will already be present in the uI
                store.ProviderData[ProviderName + ":SeriesId"] = seriesId;
                return;
            }*/
            switch (type)
            {
                case ItemType.Series:
                    FetchSeriesData(item, store, ref seriesId);
                    break;
                case ItemType.Season:
                    FetchSeasonData(item, store, seriesId);
                    break;
                case ItemType.Episode:
                    FetchEpisodeData(item, store, seriesId);
                    break;
                default:
                    return;
            }
            store.ProviderData[ProviderName + ":Date"] = DateTime.Today.ToString("yyyyMMdd");
            store.ProviderData[ProviderName + ":SeriesId"] = seriesId;
            store.ProviderData["TvDb:SeriesId"] = seriesId;
        }

        private void FetchEpisodeData(Item item, MediaMetadataStore store, string seriesId)
        {
            string name = item.Source.Name;
            string location = item.Source.Location;
            Trace.TraceInformation("TvDbProvider: Fetching episode data: " + name);
            string epNum = Helper.EpisodeNumberFromFile(location);
            if (epNum == null)
                return;
            int episodeNumber = Int32.Parse(epNum);
            
            if (!string.IsNullOrEmpty(seriesId))
            {
                string seasonNumber = item.PhysicalParent.Metadata.SeasonNumber;
                if (string.IsNullOrEmpty(seasonNumber))
                    seasonNumber = Helper.SeasonNumberFromEpisodeFile(location); // try and extract the season number from the file name for S1E1, 1x04 etc.
                if (!string.IsNullOrEmpty(seasonNumber))
                {
                    XmlDocument doc = Fetch(string.Format(episodeQuery, apiKey, seriesId, seasonNumber, episodeNumber, Config.Instance.PreferredMetaDataLanguage));
                    if (doc != null)
                    {
                        if (store.PrimaryImage == null)
                        {
                            var p = doc.SafeGetString("//filename");
                            if (p != null)
                                store.PrimaryImage = new ImageSource { OriginalSource = bannerUrl + p };
                        }
                        if (store.Overview == null)
                            store.Overview = doc.SafeGetString("//Overview");
                        if (store.EpisodeNumber == null)
                            store.EpisodeNumber = doc.SafeGetString("//EpisodeNumber");
                        if (store.Name == null)
                            store.Name = store.EpisodeNumber + " - " + doc.SafeGetString("//EpisodeName");
                        if (store.SeasonNumber == null)
                            store.SeasonNumber = doc.SafeGetString("//SeasonNumber");
                        if (store.ImdbRating == null)
                            store.ImdbRating = doc.SafeGetFloat("//Rating", (float)-1, 10);
                        if (store.Actors == null)
                        {
                            string actors = doc.SafeGetString("//GuestStars");
                            if (actors != null)
                            {
                                string[] a = actors.Trim('|').Split('|');
                                foreach (string actor in a)
                                {
                                    if (store.Actors == null)
                                        store.Actors = new List<Actor>();
                                    store.Actors.Add(new Actor { Name = actor });
                                }
                            }
                        }
                        if (store.Directors == null)
                        {
                            string directors = doc.SafeGetString("//Director");
                            if (directors != null)
                            {
                                string[] d = directors.Trim('|').Split('|');
                                if (d.Length > 0)
                                    store.Directors = new List<string>(d);
                            }
                        }
                        if (store.Writers == null)
                        {
                            string writers = doc.SafeGetString("//Writer");
                            if (writers != null)
                            {
                                string[] w = writers.Trim('|').Split('|');
                                if (w.Length > 0)
                                    store.Writers = new List<string>(w);
                            }
                        }
                        Trace.TraceInformation("TvDbProvider: Success");
                    }
                }
            }
            if (store.EpisodeNumber == null)
                store.EpisodeNumber = episodeNumber.ToString();
            if (store.Name == null)
                store.Name = item.Source.Name;

        }

        
        private void FetchSeasonData(Item item, MediaMetadataStore store, string seriesId)
        {
            string name = item.Source.Name;
            Trace.TraceInformation("TvDbProvider: Fetching season data: " + name);
            string seasonNum = Helper.SeasonNumberFromFolderName(item.Source.Location);
            int seasonNumber = Int32.Parse(seasonNum);

            if (store.Name == null)
                store.Name = item.Source.Name;
            if (store.SeasonNumber == null)
                store.SeasonNumber = seasonNumber.ToString();
            if (item.PhysicalParent == null)
                return;
            if (!string.IsNullOrEmpty(seriesId))
            {
                if ((store.PrimaryImage == null) || (store.BannerImage==null) || (store.BackdropImage==null))
                {
                    XmlDocument banners = Fetch(string.Format("http://www.thetvdb.com/api/" + apiKey + "/series/{0}/banners.xml", seriesId));
                    if (store.PrimaryImage == null)
                    {
                        XmlNode n = banners.SelectSingleNode("//Banner[BannerType='season'][BannerType2='season'][Season='" + seasonNumber.ToString() + "']");
                        if (n != null)
                        {
                            n = n.SelectSingleNode("./BannerPath");
                            if (n != null)
                                store.PrimaryImage = new ImageSource { OriginalSource = bannerUrl + n.InnerText };
                        }
                    }
                    if (store.BannerImage == null)
                    {
                        XmlNode n = banners.SelectSingleNode("//Banner[BannerType='season'][BannerType2='seasonwide'][Season='" + seasonNumber.ToString() + "']");
                        if (n != null)
                        {
                            n = n.SelectSingleNode("./BannerPath");
                            if (n != null)
                                store.BannerImage = new ImageSource { OriginalSource = bannerUrl + n.InnerText };
                        }
                    }
                    if (store.BackdropImage == null)
                    {
                        XmlNode n = banners.SelectSingleNode("//Banner[BannerType='fanart'][Season='" + seasonNumber.ToString() + "']");
                        if (n != null)
                        {
                            n = n.SelectSingleNode("./BannerPath");
                            if (n != null)
                                store.BackdropImage = new ImageSource { OriginalSource = bannerUrl + n.InnerText };
                        }
                        else
                        {
                            // not necessarily accurate but will give a different bit of art to each season
                            XmlNodeList lst = banners.SelectNodes("//Banner[BannerType='fanart']");
                            if (lst.Count > 0)
                            {
                                int num = seasonNumber % lst.Count;
                                n = lst[num];
                                n = n.SelectSingleNode("./BannerPath");
                                if (n != null)
                                    store.BackdropImage = new ImageSource { OriginalSource = bannerUrl + n.InnerText };
                            }
                        }
                    }
                }
                Trace.TraceInformation("TvDbProvider: Success");
            }
        }

        private void FetchSeriesData(Item item, MediaMetadataStore store, ref string seriesId)
        {
            string name = item.Source.Name;
            Trace.TraceInformation("TvDbProvider: Fetching series data: " + name);
            
            if (string.IsNullOrEmpty(seriesId))
            {
                seriesId = FindSeries(name);
                if (seriesId!=null)
                    Trace.TraceInformation("TvDbProvider: Success");
            }
            if (!string.IsNullOrEmpty(seriesId))
            {
                if ((store.BannerImage == null) || (store.ImdbRating == null)
                    || (store.Overview == null) || (store.Name == null) || (store.Actors == null)
                    || (store.Genres == null) || (store.MpaaRating == null))
                {
                    string url = string.Format(seriesGet, apiKey, seriesId, Config.Instance.PreferredMetaDataLanguage);
                    XmlDocument doc = Fetch(url);
                    if (doc != null)
                    {
                        if (store.Name == null)
                            store.Name = doc.SafeGetString("//SeriesName");
                        if (store.Overview == null)
                            store.Overview = doc.SafeGetString("//Overview");
                        if (store.ImdbRating == null)
                            store.ImdbRating = doc.SafeGetFloat("//Rating", 0, 10);
                        if (store.BannerImage == null)
                        {
                            string n = doc.SafeGetString("//banner");
                            if ((n != null) && (n.Length > 0))
                                store.BannerImage = new ImageSource { OriginalSource = bannerUrl + n };
                        }
                        if (store.Actors == null)
                        {
                            string actors = doc.SafeGetString("//Actors");
                            if (actors != null)
                            {
                                string[] a = actors.Trim('|').Split('|');
                                foreach (string actor in a)
                                {
                                    if (store.Actors == null)
                                        store.Actors = new List<Actor>();
                                    store.Actors.Add(new Actor { Name = actor });
                                }
                            }
                        }
                        if (store.MpaaRating == null)
                            store.MpaaRating = doc.SafeGetString("//ContentRating");
                        if (store.Genres == null)
                        {
                            string g = doc.SafeGetString("//Genre");
                            if (g != null)
                            {
                                string[] genres = g.Trim('|').Split('|');
                                if (g.Length > 0)
                                {
                                    store.Genres = new List<string>();
                                    store.Genres.AddRange(genres);
                                }
                            }
                        }
                    }
                }
            }
            if ((!string.IsNullOrEmpty(seriesId)) && ((store.PrimaryImage == null) || (store.BackdropImage == null)))
            {
                XmlDocument banners = Fetch(string.Format("http://www.thetvdb.com/api/" + apiKey + "/series/{0}/banners.xml", seriesId));
                if (banners != null)
                {
                    if (store.PrimaryImage == null)
                    {
                        XmlNode n = banners.SelectSingleNode("//Banner[BannerType='poster']");
                        if (n != null)
                        {
                            n = n.SelectSingleNode("./BannerPath");
                            if (n != null)
                                store.PrimaryImage = new ImageSource { OriginalSource = bannerUrl + n.InnerText };
                        }
                    }
                    if (store.BackdropImage == null)
                    {
                        XmlNode n = banners.SelectSingleNode("//Banner[BannerType='fanart']");
                        if (n != null)
                        {
                            n = n.SelectSingleNode("./BannerPath");
                            if (n != null)
                                store.BackdropImage = new ImageSource { OriginalSource = bannerUrl + n.InnerText };

                        }
                    }
                }
            }
        }

        public static string FindSeries(string name)
        {
            string url = string.Format(roolUrl + seriesQuery, HttpUtility.UrlEncode(name));
            XmlDocument doc = Fetch(url);
            XmlNodeList nodes = doc.SelectNodes("//Series");
            string comparableName = GetComparableName(name);
            foreach (XmlNode node in nodes)
            {
                XmlNode n = node.SelectSingleNode("./SeriesName");
                if (GetComparableName(n.InnerText) == comparableName)
                {
                    n = node.SelectSingleNode("./seriesid");
                    if (n != null)
                         return n.InnerText;
                }
            }
            return "";
        }

        static string remove = "\"'!`?";
        static string spacers = "/,.:;\\(){}[]+-_=–*";  // (there are not actually two - in the they are different char codes)

        internal static string GetComparableName(string name)
        {
            name = name.ToLower();
            name = name.Normalize(NormalizationForm.FormKD);
            StringBuilder sb = new StringBuilder();
            foreach (char c in name)
            {
                if ((int)c >= 0x2B0 && (int)c <= 0x0333)
                {
                    // skip char modifier and diacritics 
                }
                else if (remove.IndexOf(c) > -1)
                {
                    // skip chars we are removing
                }
                else if (spacers.IndexOf(c) > -1)
                {
                    sb.Append(" ");
                }
                else if (c == '&')
                {
                    sb.Append(" and ");
                }
                else
                {
                    sb.Append(c);
                }
            }
            name = sb.ToString();
            name = name.Replace("the", " ");

            string prev_name;
            do
            {
                prev_name = name;
                name = name.Replace("  ", " ");
            } while (name.Length != prev_name.Length);

            return name.Trim();
        }

        private static XmlDocument Fetch(string url)
        {
            int attempt = 0;
            while (attempt < 2)
            {
                attempt++;
                try
                {
                    WebRequest req = HttpWebRequest.Create(url);
                    req.Timeout = 60000;
                    WebResponse resp = req.GetResponse();
                    try
                    {
                        using (Stream s = resp.GetResponseStream())
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(s);
                            resp.Close();
                            s.Close();
                            return doc;
                        }
                    }
                    finally
                    {
                        resp.Close();
                    }
                }
                catch (WebException ex)
                {
                    Trace.TraceWarning("Error requesting: " + url + "\n" + ex.ToString());
                }
                catch (IOException ex)
                {
                    Trace.TraceWarning("Error requesting: " + url + "\n" + ex.ToString());
                }
            }
            return null;
        }

        #endregion





    }
}
