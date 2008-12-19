using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace MediaBrowser.Library.Providers
{
    class NndbPeopleProvider : IMetadataProvider
    {
        private static readonly string ProviderName = "Nndb";
        #region IMetadataProvider Members

        public ItemType SupportedTypes
        {
            get { return ItemType.Actor | ItemType.Director; }
        }

        public bool UsesInternet { get { return true; } }

        public void Fetch(Item item, ItemType type, MediaMetadataStore store, bool fastOnly)
        {
            if (fastOnly)
                return;
            if (store.PrimaryImage == null)
            {
                string name = item.Source.Name;
                string url = "http://search.nndb.com/search/nndb.cgi?nndb=1&omenu=unspecified&query=" + HttpUtility.UrlEncode(name);
                string doc = Fetch(url);
                if (doc != null)
                {
                    Regex searchEx = new Regex("<a  href=\"(?<fetchurl>[^\"]*)\">" + name + "</a>");
                    Match m = searchEx.Match(doc);
                    if (m.Success)
                    {
                        Trace.TraceInformation("Got basic info url for:" + name);
                        url = m.Groups["fetchurl"].Value;
                        doc = Fetch(url);
                        if (doc != null)
                        {
                            Regex imageExpr = new Regex("<img src=\"(?<imageurl>[^\"]*)\" .* alt=\"" + name + "\"");
                            Match mi = imageExpr.Match(doc);
                            if (mi.Success)
                            {
                                string imageUrl = url + mi.Groups["imageurl"].Value;
                                store.PrimaryImage = new ImageSource { OriginalSource = imageUrl };
                                Trace.TraceInformation("Got image for:" + name);
                            }
                            else
                                Trace.TraceInformation("No match for imageurl for " + name);
                        }
                    }
                    else
                        Trace.TraceInformation("No match for fetchurl for " + name);
                }
                store.ProviderData[ProviderName + ":Date"] = DateTime.Today.ToString("yyyyMMdd");
            }
        }

        public bool NeedsRefresh(Item item, ItemType type)
        {
            DateTime dt = DateTime.MinValue;
            if (item.Metadata.ProviderData.ContainsKey(ProviderName + ":Date"))
            {
                if (DateTime.Today.Subtract(item.Source.CreatedDate).TotalDays > 180)
                    return false; // don't trigger a refresh data for item that are more than 6 months old and have been refreshed before
                string date = item.Metadata.ProviderData[ProviderName + ":Date"];
                dt = DateTime.ParseExact(date, "yyyyMMdd", null);
            }
            return ((DateTime.Today.Subtract(dt).TotalDays > 14) && (item.Metadata.PrimaryImageSource==null));
        }

        #endregion
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
