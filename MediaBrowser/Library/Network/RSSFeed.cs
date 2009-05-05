using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Diagnostics;
using MediaBrowser.Library.Entities;
using System.Text.RegularExpressions;
using MediaBrowser.Library.Extensions;

namespace MediaBrowser.Library.Network {
    public class RSSFeed {

        string url;
        SyndicationFeed feed;
        IEnumerable<BaseItem> children;

        public RSSFeed(string url) {
            this.url = url;
        }

        public void Refresh() {
            lock (this) {
                try {
                    using (XmlReader reader = XmlReader.Create(url)) {
                        feed = SyndicationFeed.Load(reader);
                        children = GetChildren(feed);
                    }
                } catch (Exception ex) {
                    Debug.Assert(false, "Failed to update podcast");
                    Application.Logger.ReportException("Podcast update failed.", ex);
                    throw;
                }
            }
        }

        public string ImageUrl {
            get {
                if (feed == null || feed.ImageUrl == null) return null;
                return feed.ImageUrl.AbsoluteUri;
            }
        }

        public string Title {
            get {
                if (feed == null) return "";
                return feed.Title.Text;
            }
        }

        private static IEnumerable<BaseItem> GetChildren(SyndicationFeed feed) {
            if (feed == null) yield break;

            foreach (var item in feed.Items) {
                VodcastVideo video = new VodcastVideo();
                video.DateCreated = item.PublishDate.UtcDateTime;
                video.DateModified = item.PublishDate.UtcDateTime;
                video.Name = item.Title.Text;
                video.Overview = Regex.Replace(item.Summary.Text, @"<(.|\n)*?>", string.Empty);

                var match = Regex.Match(item.Summary.Text, @"<img src=[\""\']([^\'\""]+)", RegexOptions.IgnoreCase);
                if (match != null && match.Groups.Count > 1) {
                    video.PrimaryImagePath = match.Groups[1].Value;
                }

                foreach (var link in item.Links) {
                    if (link.RelationshipType == "enclosure") {
                        video.Path = (link.Uri.AbsoluteUri);
                    }
                }
                if (video.Path != null) {
                    video.Id = video.Path.GetMD5();
                    yield return video;
                }

            }
        }

        public IEnumerable<BaseItem> Children {
            get {
                return children;
            }
        }

        // Save a basic .vodcast file that the entity framework understands 
        public void Save(string folder) {
            // find a file name based off title. 
 
            // find next available file 



        }
    }
}
