using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Library
{
    public interface IMetadataProvider
    {
        ItemType SupportedTypes { get; }
        
        /// <summary>
        /// Fetches metadata for the specified item and puts it in the "store"
        /// </summary>
        /// <param name="item">The item to find data for</param>
        /// <param name="type">The type of item</param>
        /// <param name="store">The resulting metadata</param>
        /// <remarks>
        /// It is the aim of each successive MetadataProvider to improve the data held in "store". 
        /// Typically a provider should not overwrite non-null values.
        /// </remarks>
        void Fetch(Item item, ItemType type, MediaMetadataStore store, bool fastOnly);

        /// <summary>
        /// Determines whether the item needs a metadata refresh - i.e. if any files have changed, 
        /// it is over x days since we last did a refresh etc.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="type"></param>
        /// <remarks>If one provider returns true for an item, the current metadata is
        /// wiped out and all providers are called to provide new data. With the data being merged 
        /// in MetadataSource.RefreshMetadata
        /// </remarks>
        /// <returns></returns>
        bool NeedsRefresh(Item item, ItemType type);

        bool UsesInternet { get; }
    }

    
}
