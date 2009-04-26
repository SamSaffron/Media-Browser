using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Interfaces;

namespace MediaBrowser.Library
{
    public interface IItemRepository
    {
        IMetadataProvider RetrieveProvider(Guid guid);
        void SaveProvider(Guid guid, IMetadataProvider provider);

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
