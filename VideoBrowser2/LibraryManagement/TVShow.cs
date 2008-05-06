using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    class TVShow
    {
        /// <summary>
        /// Create a tv show object based of the metadata file 
        /// </summary>
        /// <param name="path"></param>
        public TVShow(string path)
        {
            try
            {
                LoadMetaData(path);
            }
            catch (Exception e)
            {
                Trace.WriteLine("bodgy metadata: " + path + " " + e.ToString());
                // bad metadata :( 
            }
        }

        public string ThumbPath { get; private set; }
        public string Overview { get; private set; }
        public string EpisodeNumber { get; private set; }
        public string ShowName { get; private set; }
        public string EpisodeName { get; private set; }
        public string SeasonNumber { get; private set; }

        public string LongSeriesName
        {
            get
            {
                string rval = string.Empty;

                if (ShowName.Length > 0)
                {
                    rval = ShowName;

                    if (SeasonNumber.Length > 0)
                    {
                        rval += " - Season " + SeasonNumber;
                    }
                }

                return rval;
            }
        }

        private void LoadMetaData(string path)
        {
            XmlDocument metadataDoc = new XmlDocument();
            metadataDoc.Load(path);

            var p = metadataDoc.SafeGetString("Item/filename");
            if (p.Length > 0)
            {
                // TODO : check for file? 
                ThumbPath = System.IO.Path.Combine(path, System.IO.Path.GetFileName(p));
            }

            Overview = metadataDoc.SafeGetString("Item/Overview");
            EpisodeNumber = metadataDoc.SafeGetString("Item/EpisodeNumber");
            EpisodeName = metadataDoc.SafeGetString("Item/EpisodeName");
            ShowName = metadataDoc.SafeGetString("Item/ShowName");
            SeasonNumber = metadataDoc.SafeGetString("Item/SeasonNumber");

        }

    }
}
