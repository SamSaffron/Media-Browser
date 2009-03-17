using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using MediaBrowser.Library.Sources;
using MediaBrowser.Code.ModelItems;

namespace MediaBrowser.Library
{
    public class DirectorItemWrapper : BaseModelItem
    {
        public string Director{ get; private set; }
        private Item parent;
        private Item item = null;

        public DirectorItemWrapper(string director, Item parentItem)
        {
            this.Director = director;
            this.parent = parentItem;
        }

        public Item Item
        {
            get
            {
                if (item==null)
                    lock(this)
                        if (item == null)
                        {
                            FilterSource<string> source = new FilterSource<string>(parent.UnsortedChildren, this.Director,
                                                            delegate(Item itm, string director) { return itm.Metadata.Directors.Find(a => a == director) != null; },
                                                            ItemType.Director,
                                                            delegate(string director) { return director; });
                            item = source.ConstructItem();
                        }
                return item;
            }
        }
    }
}
