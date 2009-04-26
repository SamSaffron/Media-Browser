using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Providers.Attributes;
using System.Collections;
using MediaBrowser.Library.Util;
using System.Diagnostics;
using MediaBrowser.Library.Entities.Attributes;
using System.Threading;
using MediaBrowser.Library.Plugins;

namespace MediaBrowser.Library.Metadata {
    public class MetadataProviderHelper {

        #region Helper classes

        class ProviderWithId {
            public IMetadataProvider Provider { get; set; }
            public Guid Id { get; set; }
            public MetadataProvider ProviderWrapper { get; set; }
        }

        #endregion

        static object sync = new object();
 
        static List<MetadataProvider> providers = DiscoverProviders();

        static List<MetadataProvider> slowProviders =
            new List<MetadataProvider>(providers.Where(p => p.Slow || p.RequiresInternet));

        static List<MetadataProvider> fastProviders =
            new List<MetadataProvider>(providers.Where(p => !p.Slow && !p.RequiresInternet));

        
        public static Type[] ProviderTypes { 
            get { 
                return providers.Select(p => p.Type).ToArray(); 
            } 
        }

        static List<MetadataProvider> DiscoverProviders() {
            return
                Plugin.DiscoverProviders(typeof(MetadataProviderHelper).Assembly)
                .Concat( 
                    PluginLoader.Instance.Plugins.SelectMany(p => p.MetadataProviders)
                )
                .OrderBy(provider => provider.Order)
                .ToList(); 
        }


        public static bool UpdateMetadata(BaseItem item, MetadataRefreshOptions options) {

            bool force = (options & MetadataRefreshOptions.Force) == MetadataRefreshOptions.Force;
            bool fastOnly = (options & MetadataRefreshOptions.FastOnly) == MetadataRefreshOptions.FastOnly;

            bool changed = false;
            if (force) {
                ClearItem(item); 
            }
            var providers = GetSupportedProviders(item, fastOnly); 
            if (force || NeedsRefresh(providers)) {
                changed = UpdateMetadata(item, force, providers);
            }
            return changed;
        }

        /// <summary>
        /// Clear all the persistable parts of the entitiy excluding parts that are updated during initialization
        /// </summary>
        /// <param name="item"></param>
        private static void ClearItem(BaseItem item) {
            foreach (var persistable in Serializer.GetPersistables(item)) {
                if (persistable.GetAttributes<NotSourcedFromProviderAttribute>() == null) {
                    persistable.SetValue(item, null);
                }
            }
        }

        static bool NeedsRefresh(IList<ProviderWithId> supportedProviders) {
            foreach (var provider in supportedProviders) {
                try {
                    if (provider.Provider.NeedsRefresh())
                        return true;
                } catch (Exception e) {
                    Application.Logger.ReportException("Metadata provider failed during NeedsRefresh", e);
                    Debug.Assert(false, "Providers should catch all the exceptions that NeedsRefresh generates!");
                }
            }
            return false;
        }

        static IList<ProviderWithId> GetSupportedProviders(BaseItem item, bool fastOnly) {
            return (fastOnly?fastProviders:providers)
                .Where(provider => provider.Supports(item))
                .Where(provider => !provider.RequiresInternet || Config.Instance.AllowInternetMetadataProviders)
                .Select(provider => GetProviderWithId(item, provider))
                .ToList();
        }

        static ProviderWithId GetProviderWithId(BaseItem item, MetadataProvider providerWrapper) {
            Guid id = (item.Id.ToString() + providerWrapper.Type.FullName).GetMD5();
            var provider = ItemCache.Instance.RetrieveProvider(id);
            if (provider == null) {
                provider = providerWrapper.Construct();
            }
            provider.Item = (BaseItem)Serializer.Clone(item);
            // Parent is not serialized so its not cloned
            provider.Item.Parent = item.Parent;

            return new  ProviderWithId() { Provider = provider, Id = id, ProviderWrapper = providerWrapper };
        }

        static bool UpdateMetadata(
            BaseItem item,
            bool force,
            IEnumerable<ProviderWithId> providers
            ) 
        {
            bool changed = false;
            var updatedProviders = new List<ProviderWithId>();

            foreach (var pair in providers) {
                try {
                    if (force || pair.Provider.NeedsRefresh()) {
                        pair.Provider.Fetch();
                        updatedProviders.Add(pair);
                        Serializer.Merge(pair.Provider.Item, item);
                    }
                } catch (Exception e) {
                    Debug.Assert(false, "Meta data provider should not be leaking exceptions");
                    Application.Logger.ReportException("Provider failed: " + pair.Provider.GetType().ToString(), e);
                }
            }
            if (updatedProviders.Count > 0) {
                ItemCache.Instance.SaveItem(item);
                foreach (var tuple in updatedProviders) {
                    ItemCache.Instance.SaveProvider(tuple.Id, tuple.Provider);
                }
                changed = true;
            }

            return changed;
        }
    }
}
