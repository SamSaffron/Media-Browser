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
            if (item.Metadata.ProviderData.ContainsKey(ProviderName + ":id"))
                FetchMovieData(item.Metadata.ProviderData[ProviderName + ":id"], item, store);
            else
            {
                string id;
                string matchedName;
                string[] possibles;
                id = FindId(item.Source.Name, out matchedName, out possibles);
                if (id != null)
                    FetchMovieData(id, item, store);
            }
        }

        public static string FindId(string name, out string matchedName, out string[] possibles)
        {
            name = name.Replace(".", " ");
            name = name.Replace("  ", " ");
            string year = null;
            foreach (Regex re in nameMatches)
            {
                Match m = re.Match(name);
                if (m.Success)
                {
                    name = m.Groups["name"].Value.Trim();
                    year = m.Groups["year"] != null ? m.Groups["year"].Value : null;
                    break;
                }
            }
            if (year == "")
                year = null;
            Trace.TraceInformation("MovieDbProvider: Finding id for movie data: " + name);

            string id = null;
            string url = string.Format(search, HttpUtility.UrlEncode(name).Replace("'", "%27"), ApiKey);
            XmlDocument doc = Fetch(url);
            List<string> possibleTitles = new List<string>();
            if (doc != null)
            {
                XmlNodeList nodes = doc.SelectNodes("//movie");
                foreach (XmlNode node in nodes)
                {
                    matchedName = null;
                    id = null;
                    List<string> titles = new List<string>();
                    string mainTitle = null;
                    XmlNode n = node.SelectSingleNode("./title");
                    if (n != null)
                    {
                        titles.Add(n.InnerText);
                        mainTitle = n.InnerText;
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

                        var comparable_name = GetComparableName(name);
                        foreach (var title in titles)
                        {
                            if (GetComparableName(title) == comparable_name)
                            {
                                matchedName = mainTitle;
                                break;
                            }
                        }

                        if (matchedName != null)
                        {
                            Trace.TraceInformation("Match " + matchedName + " for " + name);
                            if (year != null)
                            {
                                string r = node.SafeGetString("release");
                                if (r != null)
                                {
                                    if (!r.StartsWith(year))
                                    {
                                        Trace.TraceInformation("Result " + matchedName + " release on " + r + " did not match year " + year);
                                        continue;
                                    }
                                }
                            }
                            id = node.SafeGetString("./id");
                            possibles = null;
                            return id;

                        }
                        else
                        {
                            foreach (var title in titles)
                            {
                                possibleTitles.Add(title);
                                Trace.TraceInformation("Result " + title + " did not match " + name);
                            }
                        }
                    }
                }
            }
            possibles = possibleTitles.ToArray();
            matchedName = null;
            return null;
        }

        void FetchMovieData(string id, Item item, MediaMetadataStore store)
        {
            string url = string.Format(getInfo, id, ApiKey);
            XmlDocument doc = Fetch(url);
            if (doc != null)
            {
                store.ProviderData[ProviderName + ":id"] = id;
                if (store.Name == null)
                    store.Name = doc.SafeGetString("//movie/title");
                if (store.Overview == null)
                {
                    store.Overview = doc.SafeGetString("//movie/short_overview");
                    if (store.Overview != null)
                        store.Overview = store.Overview.Replace("\n\n", "\n");
                }
                if (store.ImdbRating == -1.0)
                    store.ImdbRating = doc.SafeGetFloat("//movie/rating", -1, 10);
                if (store.ProductionYear == null)
                {
                    string release = doc.SafeGetString("//movie/release");
                    if (!string.IsNullOrEmpty(release))
                        store.ProductionYear = Int32.Parse(release.Substring(0, 4));
                }
                if (store.RunningTime == null)
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
                if (store.Genres == null)
                {
                    XmlNodeList nodes = doc.SelectNodes("//category/name");
                    List<string> genres = new List<string>();
                    foreach (XmlNode node in nodes)
                    {
                        string n = MapGenre(node.InnerText);
                        if ((!string.IsNullOrEmpty(n)) && (!genres.Contains(n)))
                            genres.Add(n);
                    }
                    store.Genres = genres;
                }

                return;
            }
        }

        #endregion

        private static readonly Dictionary<string, string> genreMap = CreateGenreMap();

        private static Dictionary<string, string> CreateGenreMap()
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            // some of the genres in the moviedb may be deamed too specific/detailed
            // they certainly don't align to those of other sources 
            // this collection will let us map them to alternative names or "" to ignore them
            /* these are the imdb genres that should probably be our common targets
                Action
                Adventure
                Animation
                Biography
                Comedy
                Crime
                Documentary
                Drama
                Family Fantasy
                Film-Noir
                Game-Show 
                History
                Horror
                Music
                Musical 
                Mystery
                News
                Reality-TV
                Romance 
                Sci-Fi
                Short
                Sport
                Talk-Show 
                Thriller
                War
                Western
             */
            ret.Add("Action Film"       , "Action");
            ret.Add("Adventure Film"    , "Adventure");
            ret.Add("Animation Film"    , "Animation");
            ret.Add("Comedy"            , "Comedy");
            ret.Add("Crime Film"        , "Crime");
            ret.Add("Disaster Film"     , "Disaster");
            ret.Add("Documentary Film"  , "Documentary");
            ret.Add("Drama Film"        , "Drama");
            ret.Add("Eastern"           , "Eastern");
            ret.Add("Environmental"     , "Environmental");
            ret.Add("Erotic Film"       , "Erotic");
            ret.Add("Fantasy Film"      , "Family Fantasy");
            ret.Add("Historical Film"   , "History");
            ret.Add("Horror Film"       , "Horror");
            ret.Add("Musical Film"      , "Musical ");
            ret.Add("Mystery"           , "Mystery");
            ret.Add("Mystery Film"      , "Mystery");
            ret.Add("Road Movie"        , "Road Movie");
            ret.Add("Science Fiction Film", "Sci-Fi");
            ret.Add("Thriller"          , "Thriller");
            ret.Add("Western"           , "Western ");

            return ret;
        }

        private string MapGenre(string g)
        {
            if (genreMap.ContainsKey(g))
                return genreMap[g];
            else
            {
                Trace.WriteLine("Tmdb category not mapped to genre: " + g);
                return "";
            }
        }

        static string remove = "\"'!`?";
        // "Face/Off" support.
        static string spacers = "/,.:;\\(){}[]+-_=–";  // (there are not actually two - in the they are different char codes)

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

        private static XmlDocument Fetch(string url)
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
