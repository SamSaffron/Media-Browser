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
    class MediaPersonProvider : IMetadataProvider
    {
        private static readonly string ProviderName = "MediaPersonProvider";
        #region IMetadataProvider Members

        public ItemType SupportedTypes
        {
            get { return ItemType.Actor | ItemType.Director; }
        }

        public bool UsesInternet { get { return true; } }

        public void Fetch(Item item, ItemType type, MediaMetadataStore store, bool fastOnly)
        {
            try
            {
                if (fastOnly)
                    return;
                if (store.PrimaryImage == null)
                {
                    string name = item.Source.Name;

                    // Image locations
                    string url = "http://services.tvmetadatafinder.com/services/MediaPerson/staticimages/" + name.Replace(" ","%20").Trim() +".jpg";

                    // Does it exist?  Is it for real?
                    if (DoesWebObjectExist(url))
                    {
                        store.PrimaryImage = new ImageSource { OriginalSource = url };
                        Trace.TraceInformation("Got image for:" + name);
                    }
                    else
                    {
                        Trace.TraceInformation("No match for imageurl for " + name);
                    }

                    store.ProviderData[ProviderName + ":Date"] = DateTime.Today.ToString("yyyyMMdd");
                }
            }
            catch
            {
                // Do nothing...
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


        /// <summary>
        /// Checks to see if an image or url exists
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static bool DoesWebObjectExist(string url)
        {
            bool exists = false;

            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.Method = "HEAD";

                try
                {
                    request.GetResponse();
                    exists = true;
                }
                catch
                {
                    exists = false;
                }
            }
            catch { /* Do nothing... */ }

            return exists;
        }
    }
}
