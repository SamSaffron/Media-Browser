using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using MediaBrowser.Library.Sources;
using MediaBrowser.Code.ModelItems;

namespace MediaBrowser.Library
{
    public class StudioItemWrapper : BaseModelItem
    {
        public Studio Studio { get; private set; }
        private Item parent;
        private Item item = null;

        public StudioItemWrapper(Studio studio, Item parentItem)
        {
            this.Studio = studio;
            this.parent = parentItem;
        }

        public Item Item
        {
            get
            {
                if (item == null)
                    lock (this)
                        if (item == null)
                        {
                            FilterSource<Studio> source = new FilterSource<Studio>(parent.UnsortedChildren, this.Studio,
                                                            delegate(Item itm, Studio studio) { return itm.Metadata.Studios.Find(a => a.Name == studio.Name) != null; },
                                                            ItemType.Studio,
                                                            delegate(Studio studio) { return studio.Name; });
                            item = source.ConstructItem();
                        }
                return item;
            }
        }
    }
}
