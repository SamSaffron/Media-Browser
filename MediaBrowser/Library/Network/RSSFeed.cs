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
using MediaBrowser.LibraryManagement;
using System.IO;
using MediaBrowser.Library.Filesystem;

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

        public string Description {
            get {
                if (feed == null) return null;
                return feed.Description.Text;
            } 
        } 

        private static IEnumerable<BaseItem> GetChildren(SyndicationFeed feed) {
            if (feed == null) yield break;

            foreach (var item in feed.Items) {
                VodCastVideo video = new VodCastVideo();
                video.DateCreated = item.PublishDate.UtcDateTime;
                video.DateModified = item.PublishDate.UtcDateTime;
                video.Name = item.Title.Text;

                // itunes podcasts sometimes don't have a summary 
                if (item.Summary != null && item.Summary.Text != null) {
                    video.Overview = Regex.Replace(item.Summary.Text, @"<(.|\n)*?>", string.Empty);

                    var match = Regex.Match(item.Summary.Text, @"<img src=[\""\']([^\'\""]+)", RegexOptions.IgnoreCase);
                    if (match != null && match.Groups.Count > 1) {
                        video.PrimaryImagePath = match.Groups[1].Value;
                    }
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
            string name = Helper.RemoveInvalidFileChars(Title); 
            string filename = Path.Combine(folder, name + ".vodcast");

            if (!File.Exists(filename)) {
                VodcastContents generator = new VodcastContents();
                generator.Url = url;
                generator.FilesToRetain = -1;
                generator.DownloadPolicy = DownloadPolicy.Stream;
                File.WriteAllText(filename, generator.Contents);
            } else {
                throw new ApplicationException("Looks like we already have this podcast!");
            }

        }
    }
}
