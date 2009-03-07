using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;

namespace MediaBrowser.Library.Providers
{
    class MovieProviderFromXml : IMetadataProvider
    {
        private static readonly string ProviderName = "MovieProviderFromXml";
        #region IMetadataProvider Members

        public ItemType SupportedTypes
        {
            get { return ItemType.Movie; }
        }

        public bool UsesInternet { get { return false; } }

        public bool NeedsRefresh(Item item, ItemType type)
        {
            string lastFile = null;
            if (item.Metadata.ProviderData.ContainsKey(ProviderName + ":File"))
                lastFile = item.Metadata.ProviderData[ProviderName + ":File"];
            string mfile = XmlLocation(item);
            if (!File.Exists(mfile))
                mfile = null;
            if (lastFile != mfile)
                return true;
            if ((mfile == null) && (lastFile == null))
                return false;

            if (item.Metadata.ProviderData.ContainsKey(ProviderName + ":ModTime"))
            {
                DateTime modTime = new FileInfo(mfile).LastWriteTimeUtc;
                string lastTime = item.Metadata.ProviderData[ProviderName + ":ModTime"];
                DateTime dt = new DateTime(long.Parse(lastTime));
                if (modTime <= dt)
                    return false;
            }
            return true;
        }

        private string XmlLocation(Item item)
        {
            string location = item.Source.Location;
            if ((File.GetAttributes(location) & FileAttributes.Directory) != FileAttributes.Directory)
                location = Path.GetDirectoryName(location);
            return Path.Combine(location, "mymovies.xml");
        }

        public void Fetch(Item item, ItemType type, MediaMetadataStore store, bool fastOnly)
        {
            string mfile = XmlLocation(item);
            string location = Path.GetDirectoryName(mfile);
            if (File.Exists(mfile))
            {

                DateTime modTime = new FileInfo(mfile).LastWriteTimeUtc;
                store.ProviderData[ProviderName + ":ModTime"] = modTime.Ticks.ToString();
                store.ProviderData[ProviderName + ":File"] = mfile;
                XmlDocument doc = new XmlDocument();
                doc.Load(mfile);

                if (store.Name == null)
                {
                    string s = doc.SafeGetString("Title/LocalTitle");
                    if ((s == null) || (s == ""))
                        s = doc.SafeGetString("Title/OriginalTitle");
                    store.Name = s;
                }
                if (store.SortName == null)
                {
                    store.SortName = doc.SafeGetString("Title/SortTitle");
                }
                if (store.Overview == null)
                {
                    store.Overview = doc.SafeGetString("Title/Description");
                    if (store.Overview != null)
                        store.Overview = store.Overview.Replace("\n\n", "\n");
                }
                if (store.PrimaryImage == null)
                {
                    string s = doc.SafeGetString("Title/Covers/Front");
                    if ((s != null) && (s.Length > 0))
                    {
                        s = Path.Combine(location, s);
                        if (File.Exists(s))
                            store.PrimaryImage = new ImageSource { OriginalSource = s };
                    }
                }
                if (store.SecondaryImage == null)
                {
                    string s = doc.SafeGetString("Title/Covers/Back");
                    if ((s != null) && (s.Length > 0))
                    {
                        s = Path.Combine(location, s);
                        if (File.Exists(s))
                            store.SecondaryImage = new ImageSource { OriginalSource = s };
                    }
                }
                if (store.RunningTime == null)
                {
                    int rt = doc.SafeGetInt32("Title/RunningTime",0);
                    if (rt > 0)
                        store.RunningTime = rt;
                }
                if (store.ProductionYear == null)
                {
                    int y = doc.SafeGetInt32("Title/ProductionYear",0);
                    if (y > 1900)
                        store.ProductionYear = y;
                }
                if (store.ImdbRating == null)
                {
                    float i = doc.SafeGetSingle("Title/IMDBrating", (float)-1, (float)10);
                    if (i >= 0)
                        store.ImdbRating = i;
                }
                if (store.MpaaRating == null)
                    store.MpaaRating = doc.SafeGetString("Title/MPAARating");

                if (store.Actors == null)
                {
                    foreach (XmlNode node in doc.SelectNodes("Title/Persons/Person[Type='Actor']"))
                    {
                        try
                        {
                            if (store.Actors == null)
                                store.Actors = new List<MediaBrowser.Library.Actor>();
                            store.Actors.Add(new MediaBrowser.Library.Actor { Name = node.SelectSingleNode("Name").InnerText, Role = node.SelectSingleNode("Role").InnerText });
                        }
                        catch
                        {
                            // fall through i dont care, one less actor
                        }
                    }
                }
                if (store.Directors == null)
                {
                    foreach (XmlNode node in doc.SelectNodes("Title/Persons/Person[Type='Director']"))
                    {
                        try
                        {
                            if (store.Directors == null)
                                store.Directors = new List<string>();
                            store.Directors.Add(node.SelectSingleNode("Name").InnerText);
                        }
                        catch
                        {
                            // fall through i dont care, one less director
                        }
                    }
                }

                foreach (XmlNode node in doc.SelectNodes("Title/Genres/Genre"))
                {
                    try
                    {
                        if (store.Genres == null)
                            store.Genres = new List<string>();
                        store.Genres.Add(node.InnerText);
                    }
                    catch
                    {
                        // fall through i dont care, one less genre
                    }
                }   

                if (store.Studios == null)
                {
                    foreach (XmlNode node in doc.SelectNodes("Title/Studios/Studio"))
                    {
                        try
                        {
                            if (store.Studios == null)
                                store.Studios = new List<MediaBrowser.Library.Studio>();
                            store.Studios.Add(new MediaBrowser.Library.Studio { Name = node.InnerText });
                        }
                        catch
                        {
                            // fall through i dont care, one less actor
                        }
                    }
                }
                if (store.TrailerPath == null)
                    store.TrailerPath = doc.SafeGetString("Title/LocalTrailer/URL");
                if (store.MpaaRating == null)
                {
                    int i = doc.SafeGetInt32("Title/ParentalRating/Value", (int)0);
                    switch (i) {
                        case -1:
                            store.MpaaRating = "NR";
                            break;
                        case 0:
                            store.MpaaRating = "NR";
                            break; 
                        case 1:
                            store.MpaaRating = "G";
                            break;
                        case 3:
                            store.MpaaRating = "PG";
                            break;
                        case 4:
                            store.MpaaRating = "PG-13";
                            break;
                        case 5:
                            store.MpaaRating = "NC-17";
                            break;
                        case 6:
                            store.MpaaRating = "R";
                            break;
                        default:
                            store.MpaaRating = null;
                            break;
                    }
                }  
            }
        }



        #endregion
    }
}
