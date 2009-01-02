using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Library.Providers
{
    class FrameGrabProvider : IMetadataProvider
    {
        #region IMetadataProvider Members

        public ItemType SupportedTypes
        {
            get { return ItemType.Episode; }
        }

        public void Fetch(Item item, ItemType type, MediaMetadataStore store, bool fastOnly)
        {
            if ((!fastOnly) && (store.PrimaryImage==null))
                store.PrimaryImage = new ImageSource { OriginalSource = "grab://" + item.Source.PlayableItem.Filename };
        }

        public bool NeedsRefresh(Item item, ItemType type)
        {
            return (item.Metadata.PrimaryImageSource == null);
        }

        public bool UsesInternet
        {
            get { return false; }
        }

        #endregion
    }
}
