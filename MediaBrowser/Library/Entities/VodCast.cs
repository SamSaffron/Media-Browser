using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Entities.Attributes;
using MediaBrowser.Library.Extensions;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MediaBrowser.Library.Entities {
    // move me out of here ... 
    class RSSFeed {

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
                    // error, do we exception out ? do we retry ? 
                    Debug.Assert(false,"Failed to update podcast");
                    Application.Logger.ReportException("Podcast update failed.", ex);
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
                video.Overview =  Regex.Replace(item.Summary.Text,@"<(.|\n)*?>",string.Empty);

                var match = Regex.Match(item.Summary.Text, @"<img src=[\""\']([^\'\""]+)",RegexOptions.IgnoreCase);
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
    }

    public class VodCast : Folder {

        // update the vodcast every 60 minutes
        const int UpdateMinuteInterval = 60;

        [NotSourcedFromProvider]
        [Persist]
        string url;

        [Persist]
        List<BaseItem> children = new List<BaseItem>();

        [Persist]
        DateTime lastUpdated = DateTime.MinValue;

        public override void Assign(IMediaLocation location, IEnumerable<InitializationParameter> parameters, Guid id) {
            this.url = location.Contents;
            base.Assign(location, parameters, id);
        }

        public override void ValidateChildren() {
            if (Math.Abs((lastUpdated - DateTime.Now).TotalMinutes) < UpdateMinuteInterval) return;
            lastUpdated = DateTime.Now;
            this.children = GetNonCachedChildren();
            this.OnChildrenChanged(null);
            ItemCache.Instance.SaveItem(this);
        }

        protected override List<BaseItem> ActualChildren {
            get {
                if (lastUpdated == DateTime.MinValue) {
                    ValidateChildren();
                }
                return children;
            }
        }

        protected override List<BaseItem> GetNonCachedChildren() {
            RSSFeed feed = new RSSFeed(url);
            feed.Refresh();
            PrimaryImagePath = feed.ImageUrl;
            return feed.Children.Distinct(key => key.Id).ToList();
        }

        public override bool RefreshMetadata(MediaBrowser.Library.Metadata.MetadataRefreshOptions options) {
            // metadata should not be acquired through the provider framework. 
            // its all done during item validation
            return false;
        }
    }
}
