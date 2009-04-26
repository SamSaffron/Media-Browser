using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using MediaBrowser.Code.ModelItems;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library
{
    public class StudioItemWrapper : BaseModelItem
    {
        public Studio Studio { get; private set; }
        private FolderModel parent;
        private Item item = null;

        public StudioItemWrapper(Studio studio, FolderModel parent)
        {
            this.Studio = studio;
            this.parent = parent;
        }

        public Item Item
        {
            get
            {
                return null;
                /*
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
                 * 
                 */
            }
        }
    }
}
