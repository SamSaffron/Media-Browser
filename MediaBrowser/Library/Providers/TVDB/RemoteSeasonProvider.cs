using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Persistance;
using System.Diagnostics;
using System.Xml;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Library.Providers.TVDB {
    [RequiresInternet]
    [SupportedType(typeof(Season), SubclassBehavior.DontInclude)]
    class RemoteSeasonProvider : BaseMetadataProvider {

        [Persist]
        string seriesId;

        [Persist]
        DateTime downloadDate = DateTime.MinValue;

        Season Season { get { return (Season)Item;  } }


        public override bool NeedsRefresh() {
            bool fetch = false;


            fetch = seriesId != GetSeriesId();
            fetch |= (
                seriesId != null &&
                DateTime.Today.Subtract(downloadDate).TotalDays > 14 &&
                DateTime.Today.Subtract(Item.DateCreated).TotalDays < 180
                );

            return fetch;
        }

        public override void Fetch() {
            seriesId = GetSeriesId();

            if (seriesId != null) {
                if (FetchSeasonData()) downloadDate = DateTime.Today;
            }
        }


        private bool FetchSeasonData() {
            Season season = Season;

            string name = Item.Name;
            Logger.ReportInfo("TvDbProvider: Fetching season data: " + name);
            string seasonNum = TVUtils.SeasonNumberFromFolderName(Item.Path);
            int seasonNumber = Int32.Parse(seasonNum);

            season.SeasonNumber = seasonNumber.ToString();

            if (!string.IsNullOrEmpty(seriesId)) {
                if ((Item.PrimaryImagePath == null) || (Item.BannerImagePath == null) || (Item.BackdropImagePath == null)) {
                    XmlDocument banners = TVUtils.Fetch(string.Format("http://www.thetvdb.com/api/" + TVUtils.TVDBApiKey + "/series/{0}/banners.xml", seriesId));


                    XmlNode n = banners.SelectSingleNode("//Banner[BannerType='season'][BannerType2='season'][Season='" + seasonNumber.ToString() + "']");
                    if (n != null) {
                        n = n.SelectSingleNode("./BannerPath");
                        if (n != null)
                            season.PrimaryImagePath = TVUtils.BannerUrl + n.InnerText;
                    }


                    n = banners.SelectSingleNode("//Banner[BannerType='season'][BannerType2='seasonwide'][Season='" + seasonNumber.ToString() + "']");
                    if (n != null) {
                        n = n.SelectSingleNode("./BannerPath");
                        if (n != null)
                            Item.BannerImagePath = TVUtils.BannerUrl + n.InnerText;
                    }


                    n = banners.SelectSingleNode("//Banner[BannerType='fanart'][Season='" + seasonNumber.ToString() + "']");
                    if (n != null) {
                        n = n.SelectSingleNode("./BannerPath");
                        if (n != null)
                            Item.BackdropImagePath = TVUtils.BannerUrl + n.InnerText;
                    } else {
                        // not necessarily accurate but will give a different bit of art to each season
                        XmlNodeList lst = banners.SelectNodes("//Banner[BannerType='fanart']");
                        if (lst.Count > 0) {
                            int num = seasonNumber % lst.Count;
                            n = lst[num];
                            n = n.SelectSingleNode("./BannerPath");
                            if (n != null)
                                Item.BackdropImagePath = TVUtils.BannerUrl + n.InnerText;
                        }
                    }

                }
                Logger.ReportInfo("TvDbProvider: Success");
                return true;
            }

            return false;
        }



        private string GetSeriesId() {
            string seriesId = null;

            // for now do not assert, this can happen in some cases. Just fail out and get no season info
          //  Debug.Assert(Season.Parent is Series);
            var parent = Season.Parent as Series;
            if (parent != null) {
                seriesId = parent.TVDBSeriesId;
            }
            return seriesId;
        }      
    }
}
