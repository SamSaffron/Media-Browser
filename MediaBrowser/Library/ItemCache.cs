using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Library.Persistance;

namespace MediaBrowser.Library
{
    public class ItemCache
    {
        private ItemCache()
        {
        }

        public static IItemRepository Instance = new SafeItemRepository(new ItemRepository());
    }
}
