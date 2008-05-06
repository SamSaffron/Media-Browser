using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.MediaCenter.UI;

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
    }



    // This class is used for the xml caching stuff 
    public class CachedFolderItem : BaseFolderItem
    {
        // needed for mcml 
        public CachedFolderItem()
        { }

        // load the item from the element
        public CachedFolderItem(XmlElement elem, string folderHash)
        {
            this.folderHash = folderHash;
            filename = elem.SafeGetString("Filename");
            Description = elem.SafeGetString("Description");
            thumbHash = elem.SafeGetString("ThumbHash");
            title1 = elem.SafeGetString("Title1");
            title2 = elem.SafeGetString("Title2");
            isFolder = Boolean.Parse(elem.SafeGetString("IsFolder"));
            isVideo = Boolean.Parse(elem.SafeGetString("IsVideo"));
            isMovie = Boolean.Parse(elem.SafeGetString("IsMovie"));
            overview = elem.SafeGetString("Overview");
            createdDate = DateTime.Parse(elem.SafeGetString("CreatedDate"));
            modifiedDate = DateTime.Parse(elem.SafeGetString("ModifiedDate"));
            var runtime = elem.SafeGetString("RunningTime");
            if (!String.IsNullOrEmpty(runtime))
            {
                runningTime = Int32.Parse(runtime); 
            }

            var rating = elem.SafeGetString("IMDBRating");
            if (!String.IsNullOrEmpty(rating))
            {
                iMDBRating = float.Parse(rating);
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

        private string title1;
        public override string Title1 { get { return title1; } }

        private string title2;
        public override string Title2 { get { return title2; } }

        private string overview;
        public override string Overview { get { return overview; } }

        private int runningTime;
        public override int RunningTime { get { return runningTime;} }

        private float iMDBRating;
        public override float IMDBRating { get { return iMDBRating; } }

        public override List<string> Genres {
            get
            {
                return genres; 
            }
        }

        private DateTime createdDate;
        public override DateTime CreatedDate { get { return createdDate; } }

        private DateTime modifiedDate;
        public override DateTime ModifiedDate { get { return modifiedDate; } }


        public override Microsoft.MediaCenter.UI.Image MCMLThumb
        {
            get 
            {
                string path = "";
                if (!string.IsNullOrEmpty(ThumbHash))
                {
                    path = System.IO.Path.Combine(Helper.AppDataPath, folderHash);
                    path = System.IO.Path.Combine(path, ThumbHash); 
                }
                return Helper.GetMCMLThumb(path, IsVideo);
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
