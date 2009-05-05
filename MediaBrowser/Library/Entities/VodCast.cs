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
using MediaBrowser.Library.Network;

namespace MediaBrowser.Library.Entities {


    public class VodCast : Folder {

        // update the vodcast every 60 minutes
        const int UpdateMinuteInterval = 60;

        [NotSourcedFromProvider]
        [Persist]
        string url;

        [Persist]
        string localPath;

        [Persist]
        int maximumLocalVodcastsToKeep = -1; 

        [Persist]
        DownloadPolicy downloadPolicy; 
        
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
