using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Diagnostics;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Entities;
using System.Web;
using MediaBrowser.Library.Providers.Attributes;


namespace MediaBrowser.Library.Providers
{
    [RequiresInternet]
    [SupportedType(typeof(Person))]
    public class NndbPeopleProvider : BaseMetadataProvider 
    {
        [Persist]
        DateTime lastFetched = DateTime.MinValue;

        public override void Fetch() {

            lastFetched = DateTime.Now;
            string name = Item.Name;
            string url = "http://search.nndb.com/search/nndb.cgi?nndb=1&omenu=unspecified&query=" + HttpUtility.UrlEncode(name);
            string doc = Fetch(url);
            if (doc == null) return;


            Regex searchEx = new Regex("<a  href=\"(?<fetchurl>[^\"]*)\">" + name + "</a>");
            Match m = searchEx.Match(doc);
            if (m.Success) {
                Trace.TraceInformation("Got basic info url for:" + name);
                url = m.Groups["fetchurl"].Value;
                Trace.WriteLine(url);
                doc = Fetch(url);
                if (doc != null) {
                    Regex imageExpr = new Regex("<img src=\"(?<imageurl>[^\"]*)\" [^>]* alt=\"" + name + "\"");
                    Match mi = imageExpr.Match(doc);
                    if (mi.Success) {
                        string imageUrl = url + mi.Groups["imageurl"].Value;
                        Item.PrimaryImagePath = imageUrl;
                        Trace.TraceInformation("Got image for:" + name);
                    } else
                        Trace.TraceInformation("No match for imageurl for " + name);
                }
            } else {
                Trace.TraceInformation("No match for fetchurl for " + name);
            }
        }

        public override bool NeedsRefresh()
        {
            // only fetch missing images 
            return Item.PrimaryImagePath == null && (DateTime.Now - lastFetched).Days > 30;
        }

        private static object fetchLock = new object();
        private string Fetch(string url)
        {
            int attempt = 0;
            while (attempt < 2)
            {
                attempt++;
                try
                {
                    lock (fetchLock) // nndb doesn't cope well with being hit too hard
                    {
                        WebRequest req = HttpWebRequest.Create(url);
                        req.Timeout = 60000;
                        WebResponse resp = req.GetResponse();
                        try
                        {
                            using (Stream s = resp.GetResponseStream())
                            {
                                StreamReader sr = new StreamReader(s);
                                return sr.ReadToEnd();
                            }
                        }
                        finally
                        {
                            resp.Close();
                        }
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
