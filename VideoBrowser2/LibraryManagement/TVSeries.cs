using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    class TVSeries
    {
        /// <summary>
        /// Create a tv show object based of the metadata file 
        /// </summary>
        /// <param name="path"></param>
        public TVSeries(string path)
        {
            try
            {
                LoadMetaData(path);
            }
            catch (Exception e)
            {
                Trace.TraceInformation("bodgy series.xml: " + path + " " + e.ToString());
                // bad metadata :( 
            }
        }

        public string ThumbPath { get; private set; }
        public string Overview { get; private set; }
        public string SeriesName { get; private set; }
        public List<string> Actors { get; private set; }
        public List<string> Genres { get; private set; }

        private void LoadMetaData(string path)
        {
            XmlDocument metadataDoc = new XmlDocument();
            metadataDoc.Load(path);

            var p = metadataDoc.SafeGetString("Series/banner");
            if (p.Length > 0)
            {
                ThumbPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileName(p));
                if (!System.IO.File.Exists(ThumbPath))
                {
                    ThumbPath = null;
                }
            }

            Overview = metadataDoc.SafeGetString("Series/Overview");
            SeriesName = metadataDoc.SafeGetString("Series/SeriesName");
            string actors = metadataDoc.SafeGetString("Series/Actors");
            if (actors.Length > 0)
                this.Actors = new List<string>(actors.Split('|'));
            string genres = metadataDoc.SafeGetString("Series/Genre");
            if (genres.Length > 0)
                this.Genres = new List<string>(genres.Trim('|').Split('|'));
            
        }
    }
}
