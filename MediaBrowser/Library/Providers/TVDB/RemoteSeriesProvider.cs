using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Persistance;
using System.Xml;
using System.Web;
using System.Diagnostics;

namespace MediaBrowser.Library.Providers.TVDB {
    [RequiresInternet]
    [SupportedType(typeof(Series), SubclassBehavior.DontInclude)]
    class RemoteSeriesProvider : BaseMetadataProvider {

        private static readonly string rootUrl = "http://www.thetvdb.com/api/";
        private static readonly string seriesQuery = "GetSeries.php?seriesname={0}";
        private static readonly string seriesGet = "http://www.thetvdb.com/api/{0}/series/{1}/{2}.xml";

        [Persist]
        string seriesId;

        [Persist]
        DateTime downloadDate = DateTime.MinValue;

        Series Series { get { return (Series)Item; } }


        public override bool NeedsRefresh() {
            bool fetch = false;

            if (!HasCompleteMetadata()) {
                fetch = seriesId != GetSeriesId();
                fetch |= (
                    seriesId != null &&
                    DateTime.Today.Subtract(downloadDate).TotalDays > 14 &&
                    DateTime.Today.Subtract(Item.DateCreated).TotalDays < 180
                    );
            }

            return fetch;
        }


        public override void Fetch() {
            seriesId = GetSeriesId();

            // we may want to consider giving up on this item if we find no series id 

            if (!string.IsNullOrEmpty(seriesId)) {
                if (!HasCompleteMetadata() && FetchSeriesData()) {
                    downloadDate = DateTime.Today;
                    Series.TVDBSeriesId = seriesId;
                } else {
                    if (!HasCompleteMetadata()) {
                        seriesId = null;
                    }
                }
            }
        }

        private bool FetchSeriesData() {
            bool success = false;
            Series series = Item as Series;

            string name = Item.Name;
            Application.Logger.ReportInfo("TvDbProvider: Fetching series data: " + name);

            if (string.IsNullOrEmpty(seriesId)) {
                seriesId = FindSeries(name);
            }

            if (!string.IsNullOrEmpty(seriesId)) {
      
                string url = string.Format(seriesGet, TVUtils.TVDBApiKey, seriesId, Config.Instance.PreferredMetaDataLanguage);
                XmlDocument doc = TVUtils.Fetch(url);
                if (doc != null) {

                    success = true;

                    series.Name = doc.SafeGetString("//SeriesName");
                    series.Overview = doc.SafeGetString("//Overview");
                    series.ImdbRating = doc.SafeGetSingle("//Rating", 0, 10);

                    string n = doc.SafeGetString("//banner");
                    if ((n != null) && (n.Length > 0))
                        series.BannerImagePath = TVUtils.BannerUrl + n;


                    string actors = doc.SafeGetString("//Actors");
                    if (actors != null) {
                        string[] a = actors.Trim('|').Split('|');
                        if (a.Length > 0) {
                            series.Actors = new List<Actor>();
                            series.Actors.AddRange(
                                a.Select(actor => new Actor { Name = actor }));
                        }
                    }

                    series.MpaaRating = doc.SafeGetString("//ContentRating");

                    string g = doc.SafeGetString("//Genre");

                    if (g != null) {
                        string[] genres = g.Trim('|').Split('|');
                        if (g.Length > 0) {
                            series.Genres = new List<string>();
                            series.Genres.AddRange(genres);
                        }
                    }
                }
                
            }
            if ((!string.IsNullOrEmpty(seriesId)) && ((series.PrimaryImagePath == null) || (series.BackdropImagePath == null))) {
                XmlDocument banners = TVUtils.Fetch(string.Format("http://www.thetvdb.com/api/" + TVUtils.TVDBApiKey + "/series/{0}/banners.xml", seriesId));
                if (banners != null) {

                    XmlNode n = banners.SelectSingleNode("//Banner[BannerType='poster']");
                    if (n != null) {
                        n = n.SelectSingleNode("./BannerPath");
                        if (n != null)
                            series.PrimaryImagePath = TVUtils.BannerUrl + n.InnerText;
                    }


                    n = banners.SelectSingleNode("//Banner[BannerType='fanart']");
                    if (n != null) {
                        n = n.SelectSingleNode("./BannerPath");
                        if (n != null)
                            series.BackdropImagePath = TVUtils.BannerUrl + n.InnerText;

                    }
                }
            }

            return success;
        }

        private bool HasCompleteMetadata() {
            return (Series.BannerImagePath != null) && (Series.ImdbRating != null)
                                && (Series.Overview != null) && (Series.Name != null) && (Series.Actors != null)
                                && (Series.Genres != null) && (Series.MpaaRating != null) && (Series.TVDBSeriesId != null);
        }


        private string GetSeriesId() {
            string seriesId = Series.TVDBSeriesId;
            if (string.IsNullOrEmpty(seriesId)) {
                seriesId = FindSeries(Series.Name);
            }
            return seriesId;
        }


        public static string FindSeries(string name) {
            string url = string.Format(rootUrl + seriesQuery, HttpUtility.UrlEncode(name));
            XmlDocument doc = TVUtils.Fetch(url);
            XmlNodeList nodes = doc.SelectNodes("//Series");
            string comparableName = GetComparableName(name);
            foreach (XmlNode node in nodes) {
                XmlNode n = node.SelectSingleNode("./SeriesName");
                if (GetComparableName(n.InnerText) == comparableName) {
                    n = node.SelectSingleNode("./seriesid");
                    if (n != null)
                        return n.InnerText;
                }
            }
            return null;
        }

        static string remove = "\"'!`?";
        static string spacers = "/,.:;\\(){}[]+-_=–*";  // (there are not actually two - in the they are different char codes)

        internal static string GetComparableName(string name) {
            name = name.ToLower();
            name = name.Normalize(NormalizationForm.FormKD);
            StringBuilder sb = new StringBuilder();
            foreach (char c in name) {
                if ((int)c >= 0x2B0 && (int)c <= 0x0333) {
                    // skip char modifier and diacritics 
                } else if (remove.IndexOf(c) > -1) {
                    // skip chars we are removing
                } else if (spacers.IndexOf(c) > -1) {
                    sb.Append(" ");
                } else if (c == '&') {
                    sb.Append(" and ");
                } else {
                    sb.Append(c);
                }
            }
            name = sb.ToString();
            name = name.Replace("the", " ");

            string prev_name;
            do {
                prev_name = name;
                name = name.Replace("  ", " ");
            } while (name.Length != prev_name.Length);

            return name.Trim();
        }

      

    }
}
