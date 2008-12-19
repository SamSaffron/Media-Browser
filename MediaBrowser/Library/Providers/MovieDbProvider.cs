using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace MediaBrowser.Library.Providers
{
    class MovieDbProvider : IMetadataProvider
    {
        private static string search = @"http://api.themoviedb.org/2.0/Movie.search?title={0}&api_key={1}";
        private static string getInfo = @"http://api.themoviedb.org/2.0/Movie.getInfo?id={0}&api_key={1}";
        private static readonly string ApiKey = "f6bd687ffa63cd282b6ff2c6877f2669";
        static readonly string ProviderName = "MovieDbProvider";
        static readonly Regex[] nameMatches = new Regex[] {
            new Regex(@"(?<name>.*)\((?<year>\d{4})\)"), // matches "My Movie (2001)" and gives us the name and the year
            new Regex(@"(?<name>.*)") // last resort matches the whole string as the name
        };


        #region IMetadataProvider Members

        public ItemType SupportedTypes
        {
            get { return ItemType.Movie; }
        }

        public bool UsesInternet { get { return true; } }

        public bool NeedsRefresh(Item item, ItemType type)
        {
            if (item.Metadata.ProviderData.ContainsKey(ProviderName + ":Date"))
            {
                if (DateTime.Today.Subtract(item.Source.CreatedDate).TotalDays > 180)
                    return false; // don't trigger a refresh data for item that are more than 6 months old and have been refreshed before
                string date = item.Metadata.ProviderData[ProviderName + ":Date"];
                DateTime dt = DateTime.ParseExact(date, "yyyyMMdd", null);
                if (DateTime.Today.Subtract(dt).TotalDays < 14) // only refresh every 14 days
                    return false;
            }
            return true;
        }


        public void Fetch(Item item, ItemType type, MediaMetadataStore store, bool fastOnly)
        {
            if (fastOnly)
                return;
            switch (type)
            {
                case ItemType.Movie:
                    FetchMovieData(item, store);
                    break;
                default:
                    throw new NotSupportedException();
            }
            store.ProviderData[ProviderName + ":Date"] = DateTime.Today.ToString("yyyyMMdd");
            
        }

        private void FetchMovieData(Item item, MediaMetadataStore store)
        {
            string name = item.Source.Name.Trim();
            string year = null;
            foreach (Regex re in nameMatches)
            {
                Match m = re.Match(name);
                if (m.Success)
                {
                    name = m.Groups["name"].Value.Trim();
                    year = m.Groups["year"]!=null ? m.Groups["year"].Value : null ;
                    break;
                }
            }
            if (year=="")
                year = null;
            Trace.TraceInformation("MovieDbProvider: Fetching movie data: " + name);
            
            if (item.Metadata.ProviderData.ContainsKey(ProviderName + ":id"))
                FetchMovieData(item.Metadata.ProviderData[ProviderName + ":id"], item, store);
            else
            {
                string url = string.Format(search, HttpUtility.UrlEncode(name), ApiKey);
                XmlDocument doc = Fetch(url);
                if (doc != null)
                {
                    XmlNodeList nodes = doc.SelectNodes("//movie");
                    foreach (XmlNode node in nodes)
                    {
                        List<string> titles = new List<string>();
                        XmlNode n = node.SelectSingleNode("./title");
                        if (n != null)
                        {
                            titles.Add(n.InnerText);
                        }
                        var alt_titles = node.SelectNodes("./alternative_title");
                        {
                            foreach (XmlNode title in alt_titles)
                            {
                                titles.Add(title.InnerText);  
                            } 
                        }

                        if (titles.Count > 0)
                        {
                            string matched_title = null;
                            var comparable_name = GetComparableName(name); 
                            foreach (var title in titles)
                            {
                                if (GetComparableName(title) == comparable_name)
                                {
                                    matched_title = title;
                                    break;
                                }
                            }

                            if (matched_title != null)
                            {
                                Trace.TraceInformation("Match " + matched_title + " for " + name);
                                if (year != null)
                                {
                                    string r = node.SafeGetString("release");
                                    if (r != null)
                                    {
                                        if (!r.StartsWith(year))
                                        {
                                            Trace.TraceInformation("Result " + matched_title + " release on " + r + " did not match year " + year);
                                            continue;
                                        }
                                    }
                                }
                                string id = node.SafeGetString("./id");
                                FetchMovieData(id, item, store); // merge all the good results together
                            }
                            else
                            {
                                foreach (var title in titles)
                                {
                                    Trace.TraceInformation("Result " + title + " did not match " + name);
                                }
                            }
                        }
                    }
                }
            }
        }

        void FetchMovieData(string id, Item item, MediaMetadataStore store)
        {
            string url = string.Format(getInfo, id, ApiKey);
            XmlDocument doc = Fetch(url);
            if (doc != null)
            {
                store.ProviderData[ProviderName + ":id"] = id;
                if (store.Name==null)
                    store.Name = doc.SafeGetString("//movie/title");
                if (store.Overview == null)
                {
                    store.Overview = doc.SafeGetString("//movie/short_overview");
                    if (store.Overview != null)
                        store.Overview = store.Overview.Replace("\n\n", "\n");
                }
                if (store.ImdbRating==-1.0)
                    store.ImdbRating = doc.SafeGetFloat("//movie/rating",-1,10);
                if (store.ProductionYear == null)
                {
                    string release = doc.SafeGetString("//movie/release");
                    if (!string.IsNullOrEmpty(release))
                        store.ProductionYear = Int32.Parse(release.Substring(0, 4));
                }
                if (store.RunningTime==null)
                    store.RunningTime = doc.SafeGetInt("//movie/runtime");
                
                if (store.Directors == null)
                {
                    foreach (XmlNode n in doc.SelectNodes("//people/person[@job='director']/name"))
                    {
                        if (store.Directors == null)
                            store.Directors = new List<string>();
                        string name = n.InnerText.Trim();
                        if (!string.IsNullOrEmpty(name))
                            store.Directors.Add(name);
                    }
                }
                if (store.Writers == null)
                {
                    foreach (XmlNode n in doc.SelectNodes("//people/person[@job='author']/name"))
                    {
                        if (store.Writers == null)
                            store.Writers = new List<string>();
                        string name = n.InnerText.Trim();
                        if (!string.IsNullOrEmpty(name))
                            store.Writers.Add(name);
                    }
                }
                if (store.Actors == null)
                {
                    foreach (XmlNode n in doc.SelectNodes("//people/person[@job='actor']/name"))
                    {
                        if (store.Actors == null)
                            store.Actors = new List<Actor>();
                        string name = n.InnerText.Trim();
                        if (!string.IsNullOrEmpty(name))
                            store.Actors.Add(new Actor { Name = name });
                    }
                }
                
                if (store.PrimaryImage == null)
                {
                    string img = doc.SafeGetString("//movie/poster[@size='original']");
                    if (img != null)
                        store.PrimaryImage = new ImageSource { OriginalSource = img };
                }
                if (store.BackdropImage == null)
                {
                    string bd = doc.SafeGetString("//movie/backdrop[@size='original']");
                    if (bd != null)
                        store.BackdropImage = new ImageSource { OriginalSource = bd };
                }
                
                return;
            }
        }

        #endregion

        static string remove = "\"'!`?";
        // "Face/Off" support.
        static string spacers = "/,.:;\\(){}[]+-_=";
 
        internal static string GetComparableName(string name)
        {

            name = name.ToLower();
            name = name.Normalize(NormalizationForm.FormKD);
            StringBuilder sb = new StringBuilder();
            foreach (char c in name)
            {
                if ((int)c >= 0x2B0 && (int)c <= 0x0333)
                {
                    // skip char modifier and diacritics 
                }
                else if (remove.IndexOf(c) > -1)
                {
                    // skip chars we are removing
                }
                else if (spacers.IndexOf(c) > -1)
                {
                    sb.Append(" ");
                }
                else if (c == '&')
                {
                    sb.Append(" and ");
                }
                else
                {
                    sb.Append(c);
                } 
            }
            name = sb.ToString();
            name = name.Replace("the", " ");

            string prev_name;
            do
            {
                prev_name = name;
                name = name.Replace("  ", " ");
            } while (name.Length != prev_name.Length);
         
            return name.Trim();
        }

        private XmlDocument Fetch(string url)
        {
            
            int attempt = 0;
            while (attempt < 2)
            {
                attempt++;
                try
                {
                    WebRequest req = HttpWebRequest.Create(url);
                    req.Timeout = 60000;
                    WebResponse resp = req.GetResponse();
                    try
                    {
                        using (Stream s = resp.GetResponseStream())
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(s);
                            resp.Close();
                            s.Close();
                            return doc;
                        }
                    }
                    finally
                    {
                        resp.Close();
                    }
                }
                catch (WebException ex)
                {
                    Trace.TraceWarning("Error requesting: " + url + "\n" + ex.ToString());
                }
                catch (IOException ex)
                {
                    Trace.TraceWarning("Error requesting: " + url + "\n" + ex.ToString());
                }
            }
        
            return null;
        }

    }
}
