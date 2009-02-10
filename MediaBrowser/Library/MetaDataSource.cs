using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MediaBrowser.Library.Providers;
using System.Diagnostics;

namespace MediaBrowser.Library
{
    class MetaDataSource
    {
        private MetaDataSource()
        {
            // providers will be used in order with the first one having priority for providing data over the subsequent ones
            providers = new List<IMetadataProvider>();
            providers.Add(new ImageFromMediaLocationProvider());
            providers.Add(new ImageByNameProvider());
            providers.Add(new MovieProviderFromXml());
            providers.Add(new MovieDbProvider());
            providers.Add(new TVProviderFromXmlFiles());
            providers.Add(new TvDbProvider());
            //providers.Add(new NndbPeopleProvider());
            //providers.Add(new MediaPersonProvider());
            providers.Add(new VirtualFolderProvider());
            providers.Add(new FrameGrabProvider());
            providers.Add(new MediaInfoProvider());
        }

        private static volatile MetaDataSource instance;
        private static object lck = new object();

        public List<IMetadataProvider> providers;

        public static MetaDataSource Instance
        {
            get
            {
                if (instance == null)
                    lock (lck)
                        if (instance == null)
                            instance = new MetaDataSource();
                return instance;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        /// <remarks>When we refresh we completely wipe out the previous data and load new data form all the providers merging the data together</remarks>
        public MediaMetadataStore RefreshMetadata(Item item, bool force, bool fast)
        {
            ItemType dataTypeRequired = item.Source.ItemType;

            Debug.WriteLine("Asking about refreshing " + dataTypeRequired.ToString() + " metadata for " + item.Source.Name + " force=" + force.ToString() + " fast=" + fast.ToString());
            if (!force)
            {
                foreach (IMetadataProvider provider in providers)
                {
                    if (Config.Instance.AllowInternetMetadataProviders || (!provider.UsesInternet))
                    {
                        if ((provider.SupportedTypes & dataTypeRequired) == dataTypeRequired)
                        {
                            force = provider.NeedsRefresh(item, dataTypeRequired);
                            if (force)
                            {
                                Debug.WriteLine(provider.GetType().ToString() + " said refresh required for " + item.Source.Name);
                                break;
                            }
                        }
                    }
                }
            }
            if (force)
            {
                MediaMetadataStore result = new MediaMetadataStore(item.UniqueName);
                Debug.WriteLine("Performing refresh on " + dataTypeRequired.ToString() + " metadata for " + item.Source.Name + " force=" + force.ToString());

                foreach (IMetadataProvider provider in providers)
                {
                    if (Config.Instance.AllowInternetMetadataProviders || (!provider.UsesInternet))
                    {
                        if ((provider.SupportedTypes & dataTypeRequired) == dataTypeRequired)
                        {
                            provider.Fetch(item, dataTypeRequired, result, fast);
                        }
                    }
                }
                if ((result != null))
                {
                    if ((result.Name == null) || (result.Name.Length == 0))
                        result.Name = item.Source.Name;
                    result.UtcDataTimestamp = DateTime.UtcNow;
                }
                return result;
            }
            return null;
        }

    }
}
