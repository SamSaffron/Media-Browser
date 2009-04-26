using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.Entities {

    /// <summary>
    /// This is special entity for root folders. It aggregates the physical root folder with a virtual list of items that are provided by plugins 
    /// </summary>
    public class AggregateFolder : Folder {

        List<BaseItem> virtualChildren = new List<BaseItem>();

        public void AddVirtualChild(BaseItem child) {
            virtualChildren.Add(child);
        }

        protected override List<BaseItem> GetNonCachedChildren() {
            var list =  base.GetNonCachedChildren();
            list.AddRange(virtualChildren);
            return list;
        }
    }
}
