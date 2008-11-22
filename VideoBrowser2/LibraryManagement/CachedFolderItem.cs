using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.MediaCenter.UI;
using System.IO;

namespace SamSoft.VideoBrowser.LibraryManagement
{
     public static class MyExtension
    {

        public static string SafeGetString(this XmlElement elem, string path)
        {
            return SafeGetString(elem, path, ""); 
        }

        public static string SafeGetString(this XmlElement elem, string path, string defaultString)
        {
            XmlNode rvalNode = elem.SelectSingleNode(path);
            if (rvalNode != null && rvalNode.InnerText.Length > 0)
            {
                return rvalNode.InnerText;
            }
            return defaultString;
        }

        public static void WriteList(this XmlWriter writer, string node, List<string> list)
        {
            if (list != null && list.Count > 0)
            {
                writer.WriteStartElement(node + "s");
                foreach (var item in list)
                {
                    writer.WriteElementString(node, item);
                }
                writer.WriteEndElement();
            }
        }
    }



    // This class is used for the xml caching stuff 
    public class CachedFolderItem : BaseFolderItem
    {
        FolderItemList parent; 

        // needed for mcml 
        public CachedFolderItem()
        { }


        public static System.Version Version = new System.Version(1,0);

        internal static void Write(string CacheXmlPath, IEnumerable<BaseFolderItem> items)
        {
            // save this in a cache file ... 
            MemoryStream ms = new MemoryStream();
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Encoding = Encoding.UTF8;
			settings.Indent = true;
			settings.IndentChars = "\t";
			XmlWriter writer = XmlWriter.Create(ms, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("Items");
            writer.WriteAttributeString("Version", Version.ToString()); 
            foreach (BaseFolderItem item in items)
            {
                writer.WriteStartElement("Item");
                writer.WriteElementString("Filename", item.Filename);
                writer.WriteElementString("IsFolder", item.IsFolder.ToString());
                writer.WriteElementString("IsVideo", item.IsVideo.ToString());
                writer.WriteElementString("IsMovie", item.IsMovie.ToString());
                writer.WriteElementString("Description", item.Description);
				writer.WriteElementString("SortableDescription", item.SortableDescription);
				if (item is CachedFolderItem || !String.IsNullOrEmpty(((FolderItem)item).ThumbPath))
                {
                    writer.WriteElementString("ThumbHash", item.ThumbHash);
                }
                if (item is CachedFolderItem || !String.IsNullOrEmpty(((FolderItem)item).BannerPath))
                {
                    writer.WriteElementString("BannerHash", item.BannerHash);
                }
                writer.WriteElementString("Title1", item.Title1);
                writer.WriteElementString("Title2", item.Title2);
                writer.WriteElementString("Overview", item.Overview);
                if (item.IMDBRating>=0)
                    writer.WriteElementString("IMDBRating", item.IMDBRating.ToString());
                if (item.IsMovie)
                {
                    writer.WriteElementString("RunningTime", item.RunningTime.ToString());
                    writer.WriteElementString("ProductionYear", item.ProductionYear.ToString());
                }
                if ((item.Genres!=null) && (item.Genres.Count>0))
                    writer.WriteList("Genre", item.Genres);
                if ((item.Actors != null) && (item.Actors.Count > 0))
                    writer.WriteList("Actor", item.Actors);
                if (item.IsMovie)
                    writer.WriteList("Director", item.Directors); 
                writer.WriteStartElement("CreatedDate");
                writer.WriteValue(item.CreatedDate);
                writer.WriteEndElement();
                writer.WriteStartElement("ModifiedDate");
                writer.WriteValue(item.ModifiedDate);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
            ms.Flush();

            File.WriteAllBytes(CacheXmlPath, ms.ToArray());
        }

        // load the item from the element
        public CachedFolderItem(XmlElement elem, string folderHash, FolderItemList parent)
        {
            this.parent = parent;
            this.folderHash = folderHash;
            filename = elem.SafeGetString("Filename");
            Description = elem.SafeGetString("Description");
			sortableDescription = elem.SafeGetString("SortableDescription");
            if ((sortableDescription == null) || (sortableDescription.Length == 0))
                sortableDescription = FolderItem.GetSortableDescription(this.Description);
			thumbHash = elem.SafeGetString("ThumbHash");
            bannerHash = elem.SafeGetString("BannerHash");
            title1 = elem.SafeGetString("Title1");
            title2 = elem.SafeGetString("Title2");
            isFolder = Boolean.Parse(elem.SafeGetString("IsFolder"));
            isVideo = Boolean.Parse(elem.SafeGetString("IsVideo"));
            isMovie = Boolean.Parse(elem.SafeGetString("IsMovie"));
            UseBanners = Boolean.Parse(elem.SafeGetString("UseBanners"));
            overview = elem.SafeGetString("Overview");
            createdDate = DateTime.Parse(elem.SafeGetString("CreatedDate"));
            modifiedDate = DateTime.Parse(elem.SafeGetString("ModifiedDate"));
            var runtime = elem.SafeGetString("RunningTime");
            if (!String.IsNullOrEmpty(runtime))
            {
                runningTime = Int32.Parse(runtime); 
            }
            var p = elem.SafeGetString("ProductionYear");
            if (!String.IsNullOrEmpty(p))
            {
                productionYear = Int32.Parse(p);
            }

            var rating = elem.SafeGetString("IMDBRating");
            if (!String.IsNullOrEmpty(rating))
            {
                iMDBRating = float.Parse(rating);
            }
            else
            {
                iMDBRating = -1;
            }

            foreach (XmlNode item in elem.SelectNodes("Directors/Director"))
            {
                try
                {
                    directors.Add(item.InnerText);
                }
                catch
                {
                    // fall through i dont care, one less actor/director
                }
            }

            foreach (XmlNode item in elem.SelectNodes("Genres/Genre"))
            {
                try
                {
                    genres.Add(item.InnerText);
                }
                catch
                {
                    // fall through i dont care, one less actor/director
                }
            }

            foreach (XmlNode item in elem.SelectNodes("Actors/Actor"))
            {
                try
                {
                    actors.Add(item.InnerText);
                }
                catch
                {
                    // fall through i dont care, one less actor/director
                }
            }
        }

        List<string> genres = new List<string>();

        private string folderHash;

        private bool isVideo;
        public override bool IsVideo { get { return isVideo; } }

        private bool isMovie;
        public override bool IsMovie { get { return isMovie; } }

        private bool isFolder;
        public override bool IsFolder { get { return isFolder; } }

        private string filename;
        public override string Filename { get {return filename;} }
        
        private string thumbHash;
        public override string ThumbHash { get { return thumbHash; } }

        private string bannerHash;
        public override string BannerHash { get { return bannerHash; } }

        private string title1;
        public override string Title1 { get { return title1; } }

        private string title2;
        public override string Title2 { get { return title2; } }

        private string overview;
        public override string Overview { get { return overview; } }

		private string sortableDescription;
		public override string SortableDescription { get { return sortableDescription; } }
		
		private int runningTime;
        public override int RunningTime { get { return runningTime;} }

        private int productionYear;
        public override int ProductionYear { get { return productionYear; } }

        private float iMDBRating;
        public override float IMDBRating { get { return iMDBRating; } }

        private List<string> directors = new List<string>();
        public override List<string> Directors
        {
            get { return directors; }
        }

        private List<string> actors = new List<string>();
        public override List<string> Actors
        {
            get 
            { 
                return actors; 
            }
        }

        public override List<string> Genres {
            get
            {
                return genres; 
            }
        }

        public override string GenresString
        {
            get
            {
                if (Genres.Count > 0)
                {
                    string returnStr = string.Empty;
                    
                    foreach (string g in Genres)
                    {
                        returnStr += g + ", ";
                    }
                    return returnStr;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        private DateTime createdDate;
        public override DateTime CreatedDate { get { return createdDate; } }

        private DateTime modifiedDate;
        public override DateTime ModifiedDate { get { return modifiedDate; } }

        private string _thumbPath = null; 
        public override string ThumbPath
        {
            get
            {
                if (_thumbPath == null)
                {
                    string path = "";
                    if (!string.IsNullOrEmpty(ThumbHash))
                    {
                        path = System.IO.Path.Combine(Helper.AppCachePath, folderHash);
                        path = System.IO.Path.Combine(path, ThumbHash);
                        if (!File.Exists(path))
                        {
                            parent.DestroyCache();
                            var realItem = new FolderItem(Filename, IsFolder, Description, UseBanners);
                            path = realItem.ThumbPath;
                        }

                        _thumbPath = path;
                    }
                }
                return _thumbPath;
            }
            set
            {
                throw new NotImplementedException();
            }
        }


        private string _bannerPath = null;
        public override string BannerPath
        {
            get
            {
                if (_bannerPath == null)
                {
                    string path = "";
                    if (!string.IsNullOrEmpty(BannerHash))
                    {
                        path = System.IO.Path.Combine(Helper.AppCachePath, folderHash);
                        path = System.IO.Path.Combine(path, BannerHash);
                        if (!File.Exists(path))
                        {
                            parent.DestroyCache();
                            var realItem = new FolderItem(Filename, IsFolder, Description, UseBanners);
                            path = realItem.BannerPath;
                        }

                        _bannerPath = path;
                    }
                }
                return _bannerPath;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override string RunningTimeString
        {
            get 
            {
                if (RunningTime > 0)
                {
                    return RunningTime.ToString() + " minutes";
                }
                else
                {
                    return ""; 
                }
            }
        }

        public override string IMDBRatingString
        {
            get 
            {
                return IMDBRating.ToString();
            }
        }


        
    }
}
