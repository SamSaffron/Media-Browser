using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using System.Globalization;
using System.Net;
using System.Xml;
using System.IO;
using MediaBrowser;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Extensions;

namespace ITunesTrailers {
    public class ITunesTrailerFolder : Folder {
        // update once a day
        const int UpdateMinuteInterval = 60 * 24;
        const string LoFiUrl = @"http://www.apple.com/trailers/home/xml/current.xml";
        const string HiFiUrl = @"http://www.apple.com/trailers/home/xml/current_720p.xml";

        [Persist]
        List<BaseItem> children = new List<BaseItem>();

        [Persist]
        DateTime lastUpdated = DateTime.MinValue;

        public override bool RefreshMetadata(MediaBrowser.Library.Metadata.MetadataRefreshOptions options) {
            // no metadata for itunes folder please
            return false;
        }

        protected override List<BaseItem> ActualChildren {
            get {
                if (lastUpdated == DateTime.MinValue) {
                    ValidateChildren();
                }
                return children;
            }
        }

        public override void  ValidateChildren()
        {
            if (Math.Abs((lastUpdated - DateTime.Now).TotalMinutes) < UpdateMinuteInterval) return;

            try {
                // load the xml
                children = GetTrailerChildren();

                lastUpdated = DateTime.Now;
            } catch (Exception err) {
                Plugin.Logger.ReportException("Failed to update trailers", err);
            }
        }

        private List<BaseItem> GetTrailerChildren() {

            List<BaseItem> children = new List<BaseItem>();

            using (WebClient client = new WebClient()) {
                using (Stream stream = client.OpenRead(HiFiUrl)) {
                    XmlTextReader reader = new XmlTextReader(stream);

                    reader.Read();

                    ITunesTrailer trailer = null;

                    DateTimeFormatInfo dateFormat = new DateTimeFormatInfo();
                    dateFormat.ShortDatePattern = "yyyy-MM-dd";

                    while (reader.Read()) {
                        if (reader.NodeType == XmlNodeType.Element) {
                            switch (reader.Name) {
                                case "movieinfo":
                                    if (trailer != null)
                                        children.Add(trailer);

                                    trailer = new ITunesTrailer();

                                    // trailer.Id = reader.GetAttribute(0);
                                    break;

                                case "title":
                                    trailer.Name = ReadToValue(reader);
                                    trailer.Id = trailer.Name.GetMD5();
                                    break;

                                case "runtime":
                                    // trailer.RunningTime = ReadToValue(reader);
                                    break;

                                case "rating":
                                    trailer.MpaaRating = ReadToValue(reader);
                                    break;

                                case "studio":
                                    trailer.Studios = new List<string>() { ReadToValue(reader) };
                                    break;

                                case "postdate":
                                    trailer.DateCreated = DateTime.Parse(ReadToValue(reader), dateFormat);
                                    break;

                                case "releasedate":
                                    //  trailer.ProductionYear = DateTime.Parse(ReadToValue(reader), dateFormat).Year;
                                    break;

                                case "director":
                                    trailer.Directors = new List<string>() { ReadToValue(reader) };
                                    break;

                                case "description":
                                    trailer.Overview = ReadToValue(reader);
                                    break;

                                case "cast":
                                    while (reader.Read()) {
                                        if (reader.NodeType == XmlNodeType.EndElement &&
                                            reader.Name == "cast")
                                            break;

                                        if (reader.Name == "name") {
                                            if (trailer.Actors == null) {
                                                trailer.Actors = new List<Actor>();
                                            }
                                            trailer.Actors.Add(new Actor() { Name = ReadToValue(reader) });
                                        }
                                    }
                                    break;

                                case "genre":
                                    while (reader.Read()) {
                                        if (reader.NodeType == XmlNodeType.EndElement &&
                                            reader.Name == "genre")
                                            break;

                                        if (reader.Name == "name") {
                                            if (trailer.Genres == null) {
                                                trailer.Genres = new List<string>();
                                            }
                                            trailer.Genres.Add(ReadToValue(reader));
                                        }
                                    }
                                    break;

                                case "poster":
                                    while (reader.Read()) {
                                        if (reader.NodeType == XmlNodeType.EndElement &&
                                            reader.Name == "poster")
                                            break;

                                        if (reader.Name == "xlarge") {
                                            trailer.PrimaryImagePath = ReadToValue(reader);
                                        }
                                    }
                                    break;

                                case "preview":
                                    while (reader.Read()) {
                                        if (reader.NodeType == XmlNodeType.EndElement &&
                                            reader.Name == "preview")
                                            break;

                                        if (reader.Name == "large") {
                                            trailer.Path = ReadToValue(reader);
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            return children;
        }

        private static string ReadToValue(XmlTextReader reader) {
            while (reader.Read())
                if (reader.NodeType == XmlNodeType.Text)
                    break;

            return reader.ReadContentAsString();
        }
    }
}
