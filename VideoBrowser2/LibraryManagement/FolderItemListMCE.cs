using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.Diagnostics;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    // This is our model

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
            selectedIndex = new IntRangedValue();
            selectedIndex.MinValue = -1;
            selectedIndex.MaxValue = 20000;
            selectedIndex.Value = -1; 
            //  mce does not allow cross thread signalling
            //  folderItems.OnSortOrdersChanged += new SortOrdersModifiedDelegate(SortOrderChanged);
        }

        public IFolderItem SelectedItem
        {
            get
            {
                try
                {
                    return folderItems[selectedIndex.Value];
                }
                catch
                {
                    // fall through return an empty item 
                    return new FolderItem(); 
                }
            }
        }

        IntRangedValue selectedIndex;
        public IntRangedValue SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
        }

        public float ThumbAspectRatio
        {
            get
            {
                return folderItems.ThumbAspectRatio;
            }
        }

        // Cause its easier to do this in code than transformers, guess the height between 0.27 and 0.9
        public float GuessHeight
        {
            get
            {
                float f = ThumbAspectRatio / (float)2.1;
                f += (float)0.27;

                if (f > 0.9)
                {
                    f = (float)0.9; 
                }
                return f;
            }
        }


        void InternalListChanged()
        {
            try
            {
                this.Count = 0;
                this.Count = folderItems.Count;
            }
            catch
            {
                // fall through, we may need to ensure we are on the UI thread
            }
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
