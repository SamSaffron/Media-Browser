using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MediaBrowser.Library
{
    class ItemFactory 
    {
        public static readonly ItemFactory Instance = new ItemFactory();

        public Item Create(ItemSource source)
        {
            Item mine = new Item();
            mine.Assign(source);
            return mine;
        }

        public Item Create(ItemSource source, MediaMetadata metadata)
        {
            Item mine = new Item();
            mine.Assign(source, metadata);
            return mine;
        }
    }
}
