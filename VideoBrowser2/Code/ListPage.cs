using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using SamSoft.VideoBrowser.LibraryManagement;

namespace SamSoft.VideoBrowser
{
    public class ListPage : ModelItem 
    {
        public ListPage(FolderItemListMCE items)
        {
            FolderItemsMCE = items;
            items.folderItems.OnChanged += new FolderItemListModifiedDelegate(folderItems_OnChanged);
            SortOrder = items.folderItems.SortOrder;
        }

        void folderItems_OnChanged()
        {
            if (FolderItemsMCE.folderItems.SortOrder != sortOrder)
            {
                SortOrder = FolderItemsMCE.folderItems.SortOrder;
            }
        }

        public FolderItemListMCE FolderItemsMCE { get; set; }

        public bool SortOrderHasFocus
        {
            get
            {
                return sortOrderHasFocus;
            }
            set
            {
                sortOrderHasFocus = value;
                FirePropertyChanged("SortOrderHasFocus");
            }
        }

        public int ViewIndex
        {
            get
            {
                return FolderItemsMCE.folderItems.Prefs.ViewIndex; 
            }
            set
            {
                FolderItemsMCE.folderItems.Prefs.ViewIndex = value;
                FolderItemsMCE.folderItems.Prefs.Save();
                FirePropertyChanged("ViewIndex");
            }
        }

        public SortOrderEnum SortOrder
        {
            get
            {
                return sortOrder; 
            }
            set 
            {
                sortOrder = value;
                FirePropertyChanged("SortOrder");
            }
        }

        public int SortOrderInt
        {
            get
            {
                return (int)SortOrder;
            }
        }

        public float ThumbAspectRatio
        {
            get
            {
                return FolderItemsMCE.folderItems.ThumbAspectRatio;
            }
        }

        private SortOrderEnum sortOrder = SortOrderEnum.Name;
        private bool sortOrderHasFocus = false; 
     }
}
