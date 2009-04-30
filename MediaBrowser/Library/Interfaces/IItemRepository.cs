using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Interfaces;

namespace MediaBrowser.Library
{
    public interface IItemRepository
    {
        IEnumerable<IMetadataProvider> RetrieveProviders(Guid guid);
        void SaveProviders(Guid guid, IEnumerable<IMetadataProvider> providers);

        void SaveItem(BaseItem item);
        BaseItem RetrieveItem(Guid name);
        void SaveChildren(Guid ownerName, IEnumerable<Guid> children);
        IEnumerable<Guid> RetrieveChildren(Guid id);

        PlaybackStatus RetrievePlayState(Guid id);
        DisplayPreferences RetrieveDisplayPreferences(Guid id);

        void SavePlayState( PlaybackStatus playState);
        void SaveDisplayPreferences(DisplayPreferences prefs);


        void CleanCache();

        bool ClearEntireCache();
        
    }
}
