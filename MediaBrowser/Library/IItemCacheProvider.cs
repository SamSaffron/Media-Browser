using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Library
{
    public interface IItemCacheProvider
    {
        void SaveSource(ItemSource item);
        void RemoveSource(ItemSource item);

        Item Retrieve(UniqueName uniqueName);
        ItemSource RetrieveSource(UniqueName uniqueName);
        Item[] RetrieveChildren(UniqueName ownerName);
        void SaveChildren(UniqueName ownerName, List<Item> children);

        MediaMetadataStore RetrieveMetadata(UniqueName ownerName);
        PlayState RetrievePlayState(UniqueName ownerName);
        DisplayPreferences RetrieveDisplayPreferences(UniqueName ownerName);

        void SaveMetadata( MediaMetadataStore metadata);
        void SavePlayState( PlayState playState);
        void SaveDisplayPreferences(DisplayPreferences prefs);

        void RetrieveImage(UniqueName uniqueName);
        UniqueName SaveImage(LibraryImage image);

        UniqueName GetUniqueName(string name, bool allowCreated);
        Dictionary<string, UniqueName> UniqueNames { get; }
        void CleanCache();

        bool ClearEntireCache();
        
    }
}
