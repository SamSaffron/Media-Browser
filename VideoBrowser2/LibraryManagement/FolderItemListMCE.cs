using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.Diagnostics;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    public class FolderItemListMCE : VirtualList
    {
        internal FolderItemList folderItems;

        public void Sort(int sortOrder)
        {
            folderItems.Sort((SortOrderEnum)sortOrder);
        }

        public FolderItemListMCE()
        {
            folderItems = new FolderItemList();
            folderItems.OnChanged += new FolderItemListModifiedDelegate(InternalListChanged);
            // retarded mce does not allow cross thread signalling
            //  folderItems.OnSortOrdersChanged += new SortOrdersModifiedDelegate(SortOrderChanged);
        }

        void InternalListChanged()
        {
            this.Count = 0;
            this.Count = folderItems.Count;
        }
        void SortOrderChanged()
        {
            RefreshSortOrder();
        }

        protected override void OnRequestItem(int index, ItemRequestCallback callback)
        {
            callback(this, index, folderItems[index]); 
            
        }

        internal void Navigate(List<IFolderItem> items)
        {
            folderItems.Navigate(items);
            Count = folderItems.Count;
        } 

        internal void Navigate(string path)
        {
            folderItems.Navigate(path);
            Count = folderItems.Count;
        }

        internal void CacheMetadata()
        {
            folderItems.CacheMetadata();
        }

        ArrayListDataSet sortOrderList;  
        public ArrayListDataSet SortOrderList
        {
            get
            {
                if (sortOrderList == null)
                {
                    sortOrderList = new ArrayListDataSet();
                    RefreshSortOrder();
                }
                return sortOrderList;
            }
        }

        public void RefreshSortOrder()
        {
            try
            {
                if (sortOrderList.Count != folderItems.SortOrders.Count)
                {
                    sortOrderList.Clear();
                    foreach (var item in folderItems.SortOrders)
                    {
                        sortOrderList.Add(item);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.Write(e.ToString());
            }
        }


        internal void Navigate(VirtualFolder virtualFolder)
        {
            folderItems.Navigate(virtualFolder);
            Count = folderItems.Count;
        }
    }
}
