using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Persistance;
using System.Xml;
using System.Diagnostics;

namespace MediaBrowser.Library.Providers.TVDB {


    [RequiresInternet]
    [SupportedType(typeof(Episode), SubclassBehavior.DontInclude)]
    class RemoteEpisodeProvider : BaseMetadataProvider {

        private static readonly string episodeQuery = "http://www.thetvdb.com/api/{0}/series/{1}/default/{2}/{3}/{4}.xml";
        private static readonly string absEpisodeQuery = "http://www.thetvdb.com/api/{0}/series/{1}/absolute/{3}/{4}.xml";

        [Persist]
        string seriesId;

        [Persist]
        DateTime downloadDate = DateTime.MinValue;

        Episode Episode { get { return (Episode)Item; } } 

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
                if (FetchEpisodeData()) downloadDate = DateTime.Today;
            }
        }


        private bool FetchEpisodeData() {
            var episode = Item as Episode;

            string name = Item.Name;
            string location = Item.Path;
            Application.Logger.ReportInfo("TvDbProvider: Fetching episode data: " + name);
            string epNum = TVUtils.EpisodeNumberFromFile(location);

            if (epNum == null)
                return false;
            int episodeNumber = Int32.Parse(epNum);

            episode.EpisodeNumber = episodeNumber.ToString();
            bool UsingAbsoluteData = false;

            if (string.IsNullOrEmpty(seriesId)) return false;

            string seasonNumber = "";
            if (Item.Parent is Season) {
                seasonNumber = (Item.Parent as Season).SeasonNumber;
            }

            if (string.IsNullOrEmpty(seasonNumber))
                seasonNumber = TVUtils.SeasonNumberFromEpisodeFile(location); // try and extract the season number from the file name for S1E1, 1x04 etc.

            if (!string.IsNullOrEmpty(seasonNumber)) {
                seasonNumber = seasonNumber.TrimStart('0');

                XmlDocument doc = TVUtils.Fetch(string.Format(episodeQuery, TVUtils.TVDBApiKey, seriesId, seasonNumber, episodeNumber, Config.Instance.PreferredMetaDataLanguage));
                //episode does not exist under this season, try absolute numbering.
                //still assuming it's numbered as 1x01
                //this is basicly just for anime.
                if (doc == null && Int32.Parse(seasonNumber) == 1) {
                    doc = TVUtils.Fetch(string.Format(absEpisodeQuery, TVUtils.TVDBApiKey, seriesId, seasonNumber, episodeNumber, Config.Instance.PreferredMetaDataLanguage));
                    UsingAbsoluteData = true;
                }
                if (doc != null) {

                    var p = doc.SafeGetString("//filename");
                    if (p != null)
                        Item.PrimaryImagePath = TVUtils.BannerUrl + p;


                    Item.Overview = doc.SafeGetString("//Overview");
                    if (UsingAbsoluteData)
                        episode.EpisodeNumber = doc.SafeGetString("//absolute_number");
                    if (episode.EpisodeNumber == null)
                        episode.EpisodeNumber = doc.SafeGetString("//EpisodeNumber");

                    episode.Name = episode.EpisodeNumber + " - " + doc.SafeGetString("//EpisodeName");
                    episode.SeasonNumber = doc.SafeGetString("//SeasonNumber");
                    episode.ImdbRating = doc.SafeGetSingle("//Rating", (float)-1, 10);


                    string actors = doc.SafeGetString("//GuestStars");
                    if (actors != null) {
                        episode.Actors = new List<Actor>(actors.Trim('|').Split('|')
                            .Select(str => new Actor() { Name = str })
                            );
                    }


                    string directors = doc.SafeGetString("//Director");
                    if (directors != null) {
                        episode.Directors = new List<string>(directors.Trim('|').Split('|'));
                    }


                    string writers = doc.SafeGetString("//Writer");
                    if (writers != null) {
                        episode.Writers = new List<string>(writers.Trim('|').Split('|'));
                    }

                    Application.Logger.ReportInfo("TvDbProvider: Success");
                    return true;
                }

            }

            return false;
        }



        private string GetSeriesId() {
            string seriesId = null;

            var parent = Item.Parent;
            if (parent != null && !(parent.GetType() == typeof(Series))) {
                parent = parent.Parent;
            }

            if (parent is Series) {
                seriesId = (parent as Series).TVDBSeriesId;
            }
            return seriesId;
        }      


    }
}
