using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Library.Sources;

namespace MediaBrowser.Library.Providers
{
    class VirtualFolderProvider : IMetadataProvider
    {
        #region IMetadataProvider Members

        public ItemType SupportedTypes
        {
            get { return ItemType.VirtualFolder; }
        }

        public void Fetch(Item item, ItemType type, MediaMetadataStore store, bool fastOnly)
        {
            VirtualFolderSource s = item.Source as VirtualFolderSource;
            if ((s != null) && (s.ImageFile != null))
                store.PrimaryImage = new ImageSource { OriginalSource = s.ImageFile };
        }

        public bool NeedsRefresh(Item item, ItemType type)
        {
            return true; // these are cheap and easy to refresh for now
        }

        public bool UsesInternet { get { return false; } }

        #endregion
    }
}
