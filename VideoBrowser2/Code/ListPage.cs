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
            FolderItems = items;
            items.folderItems.OnChanged += new FolderItemListModifiedDelegate(folderItems_OnChanged);
            SortOrder = items.folderItems.SortOrder;
        }

        void folderItems_OnChanged()
        {
            if (FolderItems.folderItems.SortOrder != sortOrder)
            {
                SortOrder = FolderItems.folderItems.SortOrder;
            }
        }

        public FolderItemListMCE FolderItems { get; set; }

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
                return FolderItems.folderItems.Prefs.ViewIndex; 
            }
            set
            {
                FolderItems.folderItems.Prefs.ViewIndex = value;
                FolderItems.folderItems.Prefs.Save();
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

        private SortOrderEnum sortOrder = SortOrderEnum.Name;
        private bool sortOrderHasFocus = false; 
     }
}
