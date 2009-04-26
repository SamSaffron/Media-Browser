using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Persistance;

namespace MediaBrowser.Library.Providers
{
    [RequiresInternet]
    [SupportedType(typeof(Movie))]
    class MovieDbProvider : BaseMetadataProvider
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

        [Persist]
        string moviedbId;

        [Persist]
        DateTime downloadDate = DateTime.MinValue;

        public override bool NeedsRefresh()
        {
            if (DateTime.Today.Subtract(Item.DateCreated).TotalDays > 180 && downloadDate != DateTime.MinValue)
                return false; // don't trigger a refresh data for item that are more than 6 months old and have been refreshed before

            if (DateTime.Today.Subtract(downloadDate).TotalDays < 14) // only refresh every 14 days
                return false;

            return true;
        }


        public override void Fetch()
        {
            FetchMovieData();
            downloadDate = DateTime.Today;
        }

        private void FetchMovieData()
        {
            string id;
            string matchedName;
            string[] possibles;
            id = FindId(Item.Name, out matchedName, out possibles);
            if (id != null)
            {
                Item.Name = matchedName;
                FetchMovieData(id);
            }
        }

        public static string FindId(string name, out string matchedName, out string[] possibles)
        {
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
            Application.Logger.ReportInfo("MovieDbProvider: Finding id for movie data: " + name);
            string id = AttemptFindId(name, year, out matchedName, out possibles);
            if (id == null)
            {
                // try with dot turned to space
                name = name.Replace(".", " ");
                name = name.Replace("  ", " ");
                matchedName = null;
                possibles = null;
                return AttemptFindId(name, year, out matchedName, out possibles);
            }
            else
                return id;
        }

        public static string AttemptFindId(string name, string year, out string matchedName, out string[] possibles)
        {

            string id = null;
            string url = string.Format(search, UrlEncode(name), ApiKey);
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
                                matchedName = title;
                                break;
                            }
                        }

                        if (matchedName != null)
                        {
                            Application.Logger.ReportInfo("Match " + matchedName + " for " + name);
                            if (year != null)
                            {
                                string r = node.SafeGetString("release");
                                if ((r != null) && r.Length >= 4)
                                {
                                    int db;
                                    if (Int32.TryParse(r.Substring(0, 4), out db))
                                    {
                                        int y;
                                        if (Int32.TryParse(year, out y))
                                        {
                                            if (Math.Abs(db - y) > 1) // allow a 1 year tollerance on release date
                                            {
                                                Application.Logger.ReportInfo("Result " + matchedName + " release on " + r + " did not match year " + year);
                                                continue;
                                            }
                                        }
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
                                Application.Logger.ReportInfo("Result " + title + " did not match " + name);
                            }
                        }
                    }
                }
            }
            possibles = possibleTitles.ToArray();
            matchedName = null;
            return null;
        }

        private static string UrlEncode(string name)
        {
            return HttpUtility.UrlEncode(name);
        }

        void FetchMovieData(string id)
        {
            Movie movie = Item as Movie;

            string url = string.Format(getInfo, id, ApiKey);
            XmlDocument doc = Fetch(url);
            if (doc != null)
            {
                moviedbId = id;
                // This is problamatic for forign films we want to keep the alt title. 
                //if (store.Name == null)
                //    store.Name = doc.SafeGetString("//movie/title");

                movie.Overview = doc.SafeGetString("//movie/short_overview");
                if (movie.Overview != null)
                    movie.Overview = movie.Overview.Replace("\n\n", "\n");

                movie.ImdbRating = doc.SafeGetSingle("//movie/rating", -1, 10);

                string release = doc.SafeGetString("//movie/release");
                if (!string.IsNullOrEmpty(release))
                    movie.ProductionYear = Int32.Parse(release.Substring(0, 4));

                movie.RunningTime = doc.SafeGetInt32("//movie/runtime");


                movie.Directors = null;
                foreach (XmlNode n in doc.SelectNodes("//people/person[@job='director']/name"))
                {
                    if (movie.Directors == null)
                        movie.Directors = new List<string>();
                    string name = n.InnerText.Trim();
                    if (!string.IsNullOrEmpty(name))
                        movie.Directors.Add(name);
                }

                movie.Writers = null;
                foreach (XmlNode n in doc.SelectNodes("//people/person[@job='author']/name"))
                {
                    if (movie.Writers == null)
                        movie.Writers = new List<string>();
                    string name = n.InnerText.Trim();
                    if (!string.IsNullOrEmpty(name))
                        movie.Writers.Add(name);
                }


                movie.Actors = null;
                foreach (XmlNode n in doc.SelectNodes("//people/person[@job='actor']"))
                {
                    if (movie.Actors == null)
                        movie.Actors = new List<Actor>();
                    string name = n.SafeGetString("name");
                    string role = n.SafeGetString("role");
                    if (!string.IsNullOrEmpty(name))
                        movie.Actors.Add(new Actor { Name = name, Role = role });
                }


                string img = doc.SafeGetString("//movie/poster[@size='original']");
                if (img != null)
                    movie.PrimaryImagePath = img;

                movie.BackdropImagePaths = new List<string>();
                foreach (XmlNode n in doc.SelectNodes("//movie/backdrop[@size='original']"))
                {
                    movie.BackdropImagePaths.Add(n.InnerText);
                }

                XmlNodeList nodes = doc.SelectNodes("//category/name");
                List<string> genres = new List<string>();
                foreach (XmlNode node in nodes)
                {
                    string n = MapGenre(node.InnerText);
                    if ((!string.IsNullOrEmpty(n)) && (!genres.Contains(n)))
                        genres.Add(n);
                }
                movie.Genres = genres;


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
            ret.Add("Action Film", "Action");
            ret.Add("Adventure Film", "Adventure");
            ret.Add("Animation Film", "Animation");
            ret.Add("Comedy", "Comedy");
            ret.Add("Crime Film", "Crime");
            ret.Add("Disaster Film", "Disaster");
            ret.Add("Documentary Film", "Documentary");
            ret.Add("Drama Film", "Drama");
            ret.Add("Eastern", "Eastern");
            ret.Add("Environmental", "Environmental");
            ret.Add("Erotic Film", "Erotic");
            ret.Add("Fantasy Film", "Family Fantasy");
            ret.Add("Historical Film", "History");
            ret.Add("Horror Film", "Horror");
            ret.Add("Musical Film", "Musical ");
            ret.Add("Mystery", "Mystery");
            ret.Add("Mystery Film", "Mystery");
            ret.Add("Road Movie", "Road Movie");
            ret.Add("Science Fiction Film", "Sci-Fi");
            ret.Add("Thriller", "Thriller");
            ret.Add("Western", "Western ");

            return ret;
        }

        private string MapGenre(string g)
        {
            if (genreMap.ContainsKey(g))
                return genreMap[g];
            else
            {
                Application.Logger.ReportWarning("Tmdb category not mapped to genre: " + g);
                return "";
            }
        }

        static string remove = "\"'!`?";
        // "Face/Off" support.
        static string spacers = "/,.:;\\(){}[]+-_=–*";  // (there are not actually two - in the they are different char codes)

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
            try
            {

                int attempt = 0;
                while (attempt < 2)
                {
                    attempt++;
                    try
                    {
                        WebRequest req = HttpWebRequest.Create(url);
                        req.Timeout = 60000;

                        using (WebResponse resp = req.GetResponse())
                            try
                            {
                                using (Stream s = resp.GetResponseStream())
                                {
                                    XmlDocument doc = new XmlDocument();
                                    doc.Load(s);
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
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Failed to fetch url: " + url + "\n" + ex.ToString());
            }

            return null;
        }

    }
}
