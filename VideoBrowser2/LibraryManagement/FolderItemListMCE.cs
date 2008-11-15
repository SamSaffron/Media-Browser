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
        private FolderItemListMCE parent;
        string breadcrumb;

        public string Breadcrumb
        {
            get
            {
                FolderItemListMCE list = this;
                List<string> breadcrumbs = new List<string>();

                for (int i = 0; i < Config.Instance.BreadcrumbCountLimit; i++)
                {
                    if (list.breadcrumb != null)
                    {
                        breadcrumbs.Insert(0, list.breadcrumb);
                    }

                    list = list.parent;
                    if (list == null)
                    {
                        break;
                    }
                }

                if (breadcrumbs.Count == 0)
                {
                    return "Video Library";
                }

                return String.Join(" | ", breadcrumbs.ToArray());
                
            }
        }
       
        public FolderItemListMCE(FolderItemListMCE parent, string breadcrumb)
        {
            this.parent = parent;
            this.breadcrumb = breadcrumb;
            folderItems = new FolderItemList();
            folderItems.OnChanged += new FolderItemListModifiedDelegate(InternalListChanged);
            selectedIndex = new IntRangedValue();
            selectedIndex.MinValue = -1;
            selectedIndex.MaxValue = 20000;
            selectedIndex.Value = -1;
            selectedIndex.PropertyChanged += new PropertyChangedEventHandler(selectedIndex_PropertyChanged);
            
            //VisualReleaseBehavior = ReleaseBehavior.Dispose;
            
            // TODO : decide if it makes sense to discard of screen visuals, will require 
            // DiscardOffscreenVisuals="true" in the repeater 
            
            //EnableSlowDataRequests = true;


            //  mce does not allow cross thread signalling
            //  folderItems.OnSortOrdersChanged += new SortOrdersModifiedDelegate(SortOrderChanged);
        }

        void selectedIndex_PropertyChanged(IPropertyObject sender, string property)
        {
            FirePropertyChanged("SelectedItem");
        }

        

        public IFolderItem SelectedItem
        {
            get
            {
                if (selectedIndex.Value == -1)
                {
                    return new FolderItem();
                }

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

        static FolderItem blank = new FolderItem("",true);
        public IFolderItem BlankItem
        {
            get { return blank; }
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

        SizeRef actualThumbSize = new SizeRef(new Size(10, 10));
        public SizeRef ActualThumbSize
        {
            get
            {
                return actualThumbSize;
            }
        }

        /// <summary>
        /// Determines the size the grid layout gives to each item, without this it bases it off the first item.
        /// We need this as without it under some circustance when labels are showing and the first item is in 
        /// focus things get upset and all the other posters dissappear
        /// It seems to be something todo with what happens when the text box gets scaled
        /// </summary>
        public Size ReferenceSize
        {
            get
            {
                Size s = this.ActualThumbSize.Value;
                if (this.folderItems.Prefs.ShowLabels)
                    s.Height += 40;
                return s;
            }
        }

        void ThumbConstraint_PropertyChanged(IPropertyObject sender, string property)
        {
            UpdateActualThumbSize();
            FirePropertyChanged("ReferenceSize");
        }

        private void UpdateActualThumbSize()
        {
            Size s = folderItems.Prefs.ThumbConstraint.Value;
            float f = this.ThumbAspectRatio;
            float maxAspect = s.Height / s.Width;
            if (f > maxAspect)
                s.Width = (int)(s.Height / f);
            else
                s.Height = (int)(s.Width * f);
            this.ActualThumbSize.Value = s;
        }

        

        void InternalListChanged()
        {
            try
            {
                UpdateActualThumbSize();
                this.Count = 0;
                this.Count = folderItems.Count;
            }
            catch
            {
                // fall through, we may need to ensure we are on the UI thread
            }
        }

        private void InitializePrefListening()
        {
            folderItems.Prefs.ThumbConstraint.PropertyChanged += new PropertyChangedEventHandler(ThumbConstraint_PropertyChanged);
            folderItems.Prefs.PropertyChanged += new PropertyChangedEventHandler(Prefs_PropertyChanged);
            UpdateActualThumbSize();
        }

        void Prefs_PropertyChanged(IPropertyObject sender, string property)
        {
            if (property == "ShowLabels")
                FirePropertyChanged("ReferenceSize");
        }
        
        private void SetSelectedItem()
        {
            this.SelectedIndex.Value = 0;
            if (Config.Instance.DefaultToFirstUnwatched)
            {
                for (int i = 0; i < this.folderItems.Count; ++i)
                    if (!folderItems[i].HaveWatched)
                    {
                        this.SelectedIndex.Value = i;
                        break;
                    }
            }
        }

        internal void Navigate(List<IFolderItem> items)
        {
            folderItems.Navigate(items);
            Count = folderItems.Count;
            InitializePrefListening();
            SetSelectedItem();
        }

        internal void Navigate(string path)
        {
            folderItems.Navigate(path);
            Count = folderItems.Count;
            InitializePrefListening();
            SetSelectedItem();
        }

        internal void Navigate(VirtualFolder virtualFolder)
        {
            folderItems.Navigate(virtualFolder);
            Count = folderItems.Count;
            InitializePrefListening();
            SetSelectedItem();
        }

        internal void CacheMetadata()
        {
            folderItems.CacheMetadata();
        }

        
        public Choice SortOrders
        {
            get { return this.folderItems.SortOrders; }
        }

        public void RefreshSortOrder()
        {
            this.folderItems.RefreshSortOrder();
        }

        

        #region Speed optimisations for poster view

        protected override void OnRequestItem(int index, ItemRequestCallback callback)
        {
          //  Trace.TraceInformation("RequestItem " + index.ToString());
            callback(this, index, folderItems[index]);
        }

        protected override void OnRequestSlowData(int index)
        {
         //   Trace.TraceInformation("OnRequestSlowData " + index.ToString());
            base.OnRequestSlowData(index);
        }

        protected override void OnVisualsCreated(int index)
        {
         //   Trace.TraceInformation("OnVisualsCreated " + index.ToString());
            base.OnVisualsCreated(index);
        }

        protected override void OnVisualsReleased(int index)
        {
         //   Trace.TraceInformation("OnVisualsReleased " + index.ToString());
            base.OnVisualsReleased(index);
        }

        #endregion 
    }
}
