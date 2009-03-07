using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Library {
    internal class ItemNameComparer : IComparer<Item> {
        public ItemNameComparer() {

        }

        #region IComparer<Item> Members

        public int Compare(Item x, Item y) {
            if (x.Source.Name == null)
                if (y.Source.Name == null)
                    return 0;
                else
                    return 1;
            if (Config.Instance.EnableAlphanumericSorting)
                return ItemComparer.AlphaNumericCompare(x.Source.Name, y.Source.Name);
            else
                return x.Source.Name.CompareTo(y.Source.Name);
        }
        #endregion
    }

}
