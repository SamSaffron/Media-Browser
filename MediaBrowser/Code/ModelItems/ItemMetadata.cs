using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using Microsoft.MediaCenter.UI;
using System.Threading;
using MediaBrowser.Library.Threading;
using MediaBrowser.Library.Metadata;

namespace MediaBrowser.Library {
    partial class Item {

        public static Item BlankItem {
            get { return blank; }
        }

        public string FirstAired {
            get {
                string firstAired = null;
                var episode = baseItem as Episode;
                if (episode != null) {
                    firstAired = episode.FirstAired;
                }
                return firstAired ?? "";
            }
        }

        public string Status {
            get {
                string status = null;
                var series = baseItem as Series;
                if (series != null) {
                    status = series.Status;
                }
                return status ?? "";
            }
        }

        public bool IsHD {
            get {
                return ((this.MediaInfo.Width >= 1280) || (this.MediaInfo.Height >= 720));
            }
        }

        public int HDType {
            get {
                if ((this.MediaInfo.Width >= 1920) || (this.MediaInfo.Height >= 1080))
                    return 1080;
                else if (IsHD)
                    return 720;
                else
                    return 0;
            }
        }

        public bool HasMediaInfo {
            get {
                return MediaInfo != MediaInfoData.Empty;
            }
        }

        public MediaInfoData MediaInfo { 
            get {
                var video = baseItem as Video;
                if (video != null && video.MediaInfo != null) {
                    return video.MediaInfo;
                }
                return MediaInfoData.Empty;
            }  
        }

        public string DirectorString {
            get { return string.Join(", ", this.Directors.ToArray()); }
        }

        public string WritersString {
            get { return string.Join(", ", this.Writers.ToArray()); }
        }

        public List<string> Directors {
            get {
                List<string> directors = null;
                var show = baseItem as Show;
                if (show != null && show.Directors != null) {
                    directors = show.Directors;
                }
                return directors ?? new List<string>();
            }
        }

        public List<string> Writers {
            get {
                List<string> writers = null; 
                var episode = baseItem as Episode;
                if (episode != null) {
                    writers = episode.Writers;
                }
                return writers ?? new List<string>();
            }
        }

        public List<string> Genres {
            get {
                var show = baseItem as IShow;
                if (show != null && show.Genres != null) {
                    return show.Genres;
                }
                return new List<string>();
            }
        }
        public string TrailerPath {
            get {
                var movie = baseItem as Movie;
                if (movie != null && movie.TrailerPath != null) {
                    return movie.TrailerPath;
                }
                return "";
            }
        }

        public string RunningTimeString {
            get {
                string runtime = "";
                var show = baseItem as IShow;
                if (show != null) {
                    runtime = show.RunningTime==null ? "" : show.RunningTime.ToString() + " mins";
                }
                return runtime;
            }
        }

        public int ProductionYear {
            get {
                int productionYear = -1;
                var show = baseItem as Show;
                if (show != null) {
                    productionYear = show.ProductionYear ?? -1;
                }
                return productionYear;
            }
        }

        public string ProductionYearString {
            get { return ProductionYear == -1 ? "" : ProductionYear.ToString(); }
        }

        public float ImdbRating {
            get {
                float rating = -1;
                var show = baseItem as IShow;
                if (show != null) {
                    rating = show.ImdbRating ?? -1;
                }
                return rating;
            }
        }

        public string ImdbRatingString {
            get { return (ImdbRating).ToString("0.##"); }
        }

        public string MpaaRating {
            get {
                IShow show = baseItem as IShow;
                return show != null ? show.MpaaRating ?? "" : "";
            }
        }

        public List<ActorItemWrapper> Actors {
            get {
                List<ActorItemWrapper> actors = new List<ActorItemWrapper>();
                var show = baseItem as Show;
                if (show != null && show.Actors != null) {
                    foreach (var actor in show.Actors) {
                        actors.Add(new ActorItemWrapper(actor, this.PhysicalParent));
                    }
                    Async.Queue(() =>
                    {
                        foreach (var actor in show.Actors) {
                            if (actor.Person.RefreshMetadata(MetadataRefreshOptions.FastOnly)) {
                                ItemCache.Instance.SaveItem(actor.Person);
                            }
                        }

                        foreach (var actor in show.Actors) {
                            if (actor.Person.RefreshMetadata()) {
                                ItemCache.Instance.SaveItem(actor.Person);
                            }
                        }
                    });
   
                }
                return actors;
            }
        }

        public bool HasDataForDetailPage {
            get {

                var movie = baseItem as Movie;
                if (movie == null) return false;

                int score = 0;
                if (Actors.Count > 0)
                    score += 2;
                if (movie.Studios != null && movie.Studios.Count > 0)
                    score += 2;
                if (Genres.Count > 0)
                    score += 2;
                if (Directors.Count > 0)
                    score += 2;
                if (Writers.Count > 0)
                    score += 2;
                if (movie.Overview != null)
                    score += 2;
                if (movie.MpaaRating != null)
                    score += 1;
                if (movie.ImdbRating != null)
                    score += 1;
                if (movie.ProductionYear != null)
                    score += 1;
                if (movie.RunningTime != null)
                    score += 1;
                return score > 5;
            }
        }

        public List<StudioItemWrapper> StudioItems {
            get {
                return null;
                /*
                var items = this.Studios
                    .Select(s => new StudioItemWrapper(s, this.PhysicalParent))
                    .OrderBy(x => x.Studio.Name);
                return new List<StudioItemWrapper>(items);
                 * */

            }
        }

        public List<DirectorItemWrapper> DirectorItems {
            get {
                var items = this.Directors
                    .Select(s => new DirectorItemWrapper(s, this.PhysicalParent))
                    .OrderBy(x => x.Director);
                return new List<DirectorItemWrapper>(items);
            }
        }


        /// <summary>
        /// The metadata overview if there is one.
        /// </summary>
        public virtual string Overview {
            get {
                string overview = this.BaseItem.Overview;
                if (!string.IsNullOrEmpty(overview)) {
                    overview = overview.Replace("\r\n", "\n").Replace("\n\n", "\n");
                } else {
                    overview = "";
                }
                return overview;
            }
        }


        public bool HasSubTitle {
            get {
                return !string.IsNullOrEmpty(baseItem.SubTitle);
            }
        }
        public string SubTitle {
            get { return baseItem.SubTitle; }
        }


        private void MetadataChanged(object sender, MetadataChangedEventArgs args) {
            if (!Microsoft.MediaCenter.UI.Application.IsApplicationThread) {
                Microsoft.MediaCenter.UI.Application.DeferredInvoke( _ => MetadataChanged(sender, args));
                return;
            }

            FirePropertyChanged("Name");
            FirePropertyChanged("Overview");
            FirePropertyChanged("PrimaryImage"); 
            FirePropertyChanged("PrimaryImageSmall"); 
            FirePropertyChanged("PrimaryImage"); 
            FirePropertyChanged("PreferredImageSmall"); 
        }
    }
}
