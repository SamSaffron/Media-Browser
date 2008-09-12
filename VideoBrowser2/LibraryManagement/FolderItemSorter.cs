using System;
using System.Collections.Generic;
using System.Text;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    class FolderItemSorter : IComparer<IFolderItem>
    {
        public FolderItemSorter(SortOrderEnum sortOrderEnum)
        {
            this.sortOrderEnum = sortOrderEnum;
        }

        SortOrderEnum sortOrderEnum;

        #region IComparer<IFolderItem> Members

        public int Compare(IFolderItem x, IFolderItem y)
        {
            if (x is SpecialFolderItem && !(y is SpecialFolderItem))
            {
                return -1;
            }

            if (!(x is SpecialFolderItem) && y is SpecialFolderItem)
            {
                return 1;
            }

            if (sortOrderEnum == SortOrderEnum.Name)
            {
                if (x.IsFolder && !(y.IsFolder))
                {
                    return -1;
                }

                if (!(x.IsFolder) && y.IsFolder)
                {
                    return 1;
                }

                return x.Description.CompareTo(y.Description);
            }
            else if (sortOrderEnum == SortOrderEnum.Date)
            {
                // reverse order for dates
                return y.CreatedDate.CompareTo(x.CreatedDate);
            }
            else if (sortOrderEnum == SortOrderEnum.RunTime)
            {
                int xval = x.RunningTime;
                if (xval <= 0) xval = 999999;
                int yval = y.RunningTime;
                if (yval <= 0) yval = 999999;
                return xval.CompareTo(yval);
            }
            else if (sortOrderEnum == SortOrderEnum.ProductionYear)
            {
                // reverse order
                if (x.Description == "Unknown")
                {
                    return 1;
                }
                if (y.Description == "Unknown" )
                {
                    return -1;
                }
                return y.Description.CompareTo(x.Description);
            }
            else
            {
                // genre sort etc...
                return x.Description.CompareTo(y.Description);
            }
        }

        #endregion
    }
}
