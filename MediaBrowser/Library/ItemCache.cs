using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Library
{
    class ItemCache
    {
        private ItemCache()
        {
        }

        public static IItemCacheProvider Instance = new DefaultCacheProvider();
    }
}
