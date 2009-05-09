using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using System.Net;
using System.Xml.XPath;

using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library;
using MediaBrowser.Library.Extensions;
using MediaBrowser;
using MediaBrowser.Library.Configuration;


namespace FilmTrailerPlugin
{
    public class FilmTrailerFolder : Folder
    {
        private static readonly string FileName = "filmtrailers.xml";
        private readonly string DownloadToFilePath = System.IO.Path.Combine(ApplicationPaths.AppRSSPath, FileName);
        private readonly string Feed = @"http://uk.feed.filmtrailer.com/v2.0/?ListType=Latest30InCinema&channel_user_id=441100001-1";
        //private readonly string Feed = @"http://uk.feed.filmtrailer.com/v2.0/?ListType=AllCinemaMovies&channel_user_id=441100001-1";

        const int RefreshIntervalHrs = 24;  // once a day

        #region Base Item methods

        [Persist]
        List<BaseItem> trailers = new List<BaseItem>();

        [Persist]
        DateTime lastUpdated = DateTime.MinValue;


        public override bool RefreshMetadata(MediaBrowser.Library.Metadata.MetadataRefreshOptions options)
        {
            return false;
            // do nothing .. we control the metadata ... providers do nothing
        }

        // The critical override, you need to override this to take control of the children 
        protected override List<BaseItem> ActualChildren
        {
            get
            {
                return trailers;
            }
        }

        // validation is overidden so it can do nothing if a period of time has not elapsed
        public override void ValidateChildren()
        {
#if (!DEBUG)
            if (Math.Abs((lastUpdated - DateTime.Now).TotalMinutes) < (RefreshIntervalHrs * 60)) return;
#endif
            lastUpdated = DateTime.Now;
            this.trailers = GetTrailers();
            this.OnChildrenChanged(null);
            // cache the children
            ItemCache.Instance.SaveItem(this);
        }
        #endregion

        #region Parse Feed

        List<BaseItem> GetTrailers()
        {
            var trailers = new List<BaseItem>();
            WebClient client = new WebClient();
            XmlDocument xDoc = new XmlDocument();
            try
            {
                if (IsRefreshRequired())
                {
                    client.DownloadFile(Feed, DownloadToFilePath);
                    Stream strm = client.OpenRead(Feed);
                    StreamReader sr = new StreamReader(strm);
                    string strXml = sr.ReadToEnd();
                    xDoc.LoadXml(strXml);
                }
                else
                {
                    xDoc.Load(DownloadToFilePath);
                }
                trailers = ParseDocument(xDoc);
            }
            catch (Exception e)
            {
                Plugin.Logger.ReportException("Failed to update trailers", e);
            }
            finally
            {
                client.Dispose();
            }

            lastUpdated = DateTime.Now;

            return trailers;
        }

        private bool IsRefreshRequired()
        {
            if (File.Exists(DownloadToFilePath))
            {
                FileInfo fi = new FileInfo(DownloadToFilePath);
                if (fi.LastWriteTime < DateTime.Now.AddHours(-(RefreshIntervalHrs)))
                    return true;
                else
                    return false;
            }
            // If we get to this stage that means the file does not exists, and we should force a refresh
            return true;
        }

        private List<BaseItem> ParseDocument(XmlDocument xDoc)
        {
            List<BaseItem> trailers = new List<BaseItem>();
            XmlNodeList movieTrailers = xDoc.GetElementsByTagName("movie");

            foreach (XmlNode movie in movieTrailers)
            {
                try
                {
                    var currentTrailer = new FilmTrailer();
                    var x = movie;


                    foreach (XmlNode node in movie.ChildNodes)
                    {
                        if (node.Name == "original_title")
                        {
                            currentTrailer.Name = node.InnerText;
                        }
                        if (node.Name == "movie_duration")
                        {
                            currentTrailer.RunningTime = Int32.Parse(node.InnerText);
                        }
                        if (node.Name == "production_year")
                        {
                            currentTrailer.ProductionYear = Int32.Parse(node.InnerText);
                        }
                        if (node.Name == "actors")
                        {
                            var actors = node.SelectNodes("./actor");
                            if (currentTrailer.Actors == null)
                                currentTrailer.Actors = new List<Actor>();
                            foreach (XmlNode anode in actors)
                            {
                                 string actorName = anode.InnerText;
                                 if (!string.IsNullOrEmpty(actorName))
                                     currentTrailer.Actors.Add(new Actor { Name = actorName, Role = "" });
                            }
                        }
                        if (node.Name == "directors")
                        {
                            if (currentTrailer.Directors == null)
                                currentTrailer.Directors = new List<string>();
                            var directors = node.SelectNodes("./director");
                            if (directors.Count > 0)
                            {
                                foreach (XmlNode dnode in directors)
                                {
                                    currentTrailer.Directors.Add(dnode.InnerText);
                                }
                            }
                        }

                        if (node.Name == "regions")
                        {
                            currentTrailer.Overview = node.SelectSingleNode("./region/products/product/description").InnerText;
                            
                            currentTrailer.DateCreated = DateTime.Parse(node.SelectSingleNode("./region/products/product/pub_date").InnerText);                                
                            currentTrailer.DateModified = currentTrailer.DateCreated;
                            currentTrailer.PrimaryImagePath = node.SelectSingleNode("./region/pictures/picture/url").InnerText;

                            var genres = node.SelectNodes("./region/categories/categorie");
                            if (currentTrailer.Genres == null)
                                currentTrailer.Genres = new List<string>();
                            foreach (XmlNode gnode in genres)
                            {
                                currentTrailer.Genres.Add(gnode.InnerText);
                            }

                            var files = node.SelectNodes("./region/products/product/clips/clip/files/file");
                            foreach (XmlNode file in files)
                            {
                                if ((file.Attributes["format"].Value == "wmv" && file.Attributes["size"].Value == "xlarge") ||
                                    (file.Attributes["format"].Value == "wmv" && file.Attributes["size"].Value == "xxlarge"))
                                {
                                    foreach (XmlNode nodeFile in file)
                                    {
                                        if (nodeFile.Name == "url")
                                        {
                                            string[] pathUrl = nodeFile.InnerText.Split('?');

                                            currentTrailer.Path = pathUrl[0];
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    }
                    currentTrailer.Id = currentTrailer.Path.GetMD5();
                    currentTrailer.SubTitle = "presented by FilmTrailer.com";
                    trailers.Add(currentTrailer);
                    //Plugin.Logger.ReportInfo("FilmTrailer added trailer: " + currentTrailer.Name);
                }
                catch (Exception e)
                {
                    Plugin.Logger.ReportException("Failed to parse trailer document", e);
                }
            }
            return trailers;
        }

        private string GetChildNodesValue(XPathNavigator nav, string nodeName)
        {
            string value = string.Empty;
            if (nav.MoveToChild(nodeName, ""))
            {
                value = nav.Value;
                nav.MoveToParent();
            }
            return value;
        }

        #endregion
    }
}
