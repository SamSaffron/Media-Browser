using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace MediaBrowser.Library
{
    class MediaMetadataFactory
    {
        public static readonly MediaMetadataFactory Instance = new MediaMetadataFactory();

        private MediaMetadataFactory()
        {
        }

        public MediaMetadata Create(MediaMetadataStore store, ItemType type)
        {
            MediaMetadata mine = new MediaMetadata();
            mine.Assign(store,type);
            return mine;
        }

        public MediaMetadata Create(UniqueName ownerName, ItemType type)
        {
            MediaMetadata mine = new MediaMetadata();
            mine.Assign(ownerName, type);
            return mine;
        }
    }
}
