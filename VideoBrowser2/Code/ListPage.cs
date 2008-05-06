using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using SamSoft.VideoBrowser.LibraryManagement;

namespace SamSoft.VideoBrowser
{
    public class ListPage : ModelItem 
    {
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

        private bool sortOrderHasFocus = false; 
     }
}
