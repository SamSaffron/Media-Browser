using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.MediaCenter.UI;
using MediaBrowser.Library.Entities;
using System.Collections;
using MediaBrowser.Library;
using System.Diagnostics;
using System.Threading;
using MediaBrowser.Library.Threading;
using MediaBrowser.Library.Metadata;

namespace MediaBrowser.Code.ModelItems {
    public class FolderChildren : BaseModelItem, IList, ICollection, IList<Item>, IDisposable{

        static BackgroundProcessor<FolderChildren> childLoader = new BackgroundProcessor<FolderChildren>(2,LoadChildren, "Child loader");
        static BackgroundProcessor<FolderChildren> childVerifier= new BackgroundProcessor<FolderChildren>(2, VerifyChildren, "Child verifier");
        static BackgroundProcessor<BaseItem> slowMetadataRefresher = new BackgroundProcessor<BaseItem>(2, SlowMetadataRefresh, "Slow metadata refresher");
        static BackgroundProcessor<BaseItem> fastMetadataRefresher = new BackgroundProcessor<BaseItem>(2, FastMetadataRefresh, "Fast metadata refresher");

        // our global queue. 
        // 2 * fast metadata refresher 
        // 2 * slow metadata refresher 
        // 2 * child loaders
        // 2 * child verifier

        // The chain of events is as follow. 
        // Assign is called, a child loader is triggered
        //   Once done, it will trigger a child verifier
        //   Once done, it will trigger a metadata refresh

        FolderModel folderModel;
        Folder folder;
        Dictionary<Guid, Item> items = new Dictionary<Guid, Item>();
        IList<BaseItem> currentChildren = new List<BaseItem> ();
        Action onChildrenChanged;
        bool folderIsIndexed = false;
        float childImageAspect = 1;
        SortOrder sortOrder = SortOrder.Name;

        public void Assign(FolderModel folderModel, Action onChildrenChanged) {

            lock (this) {
                Debug.Assert(this.folderModel == null);
                Debug.Assert(this.folder == null);

                this.onChildrenChanged = onChildrenChanged;
                if (folderModel.Folder == this.folder && folderModel == this.folderModel) return;
                if (folder != null) StopListeningForChanges();
                this.folderModel = folderModel;
                this.folder = folderModel.Folder;

                ListenForChanges();
                childLoader.Enqueue(this);
            }

        }

        public static void LoadChildren(FolderChildren children) {
            children.folder.EnsureChildrenLoaded();
            childVerifier.Enqueue(children);
        }


        public static void VerifyChildren(FolderChildren children) {

            children.folder.ValidateChildren();

            // we may want to consider some pause APIs on the queues so we can ensure the correct ordering
            // its not a big fuss, cause it will be picked up next time around anyway

            // the reverse isn't really needed, but it means that metadata is acquired in the order the children are in. 
            foreach (var item in children.folder.Children.Reverse()) {
                fastMetadataRefresher.Inject(item);
            }

            fastMetadataRefresher.Inject(children.folder);
            bool isSeason = children.folder.GetType() == typeof(Season) && children.folder.Parent != null;
            if (isSeason) {
                fastMetadataRefresher.Inject(children.folder.Parent);
            }
        }


        public static void FastMetadataRefresh(BaseItem item) {
            item.RefreshMetadata(MetadataRefreshOptions.FastOnly);
            slowMetadataRefresher.Inject(item);
        }

        public static void SlowMetadataRefresh(BaseItem item) {
            item.RefreshMetadata(MetadataRefreshOptions.Default);
        }

        public void RefreshAsap() {
            if (!childLoader.PullToFront(this)) {
                childLoader.Inject(this);
            }
            if (!childVerifier.PullToFront(this)) {
                childVerifier.Inject(this);
            }

            Sort();
        }

        public void ListenForChanges() {
            folder.ChildrenChanged += new EventHandler<ChildrenChangedEventArgs>(folder_ChildrenChanged);
        }

        public void StopListeningForChanges() {
            folder.ChildrenChanged -= new EventHandler<ChildrenChangedEventArgs>(folder_ChildrenChanged);
        }

        /// <summary>
        /// Creates a shallow clone to trick the binder into updating the list 
        /// </summary>
        /// <returns></returns>
        public FolderChildren Clone() {
            lock (this) {
                FolderChildren clone = new FolderChildren();
                clone.folderModel = folderModel;
                clone.folder = folder;
                clone.items = items;
                clone.currentChildren = currentChildren;
                clone.onChildrenChanged = onChildrenChanged;
                clone.folderIsIndexed = folderIsIndexed;
                clone.childImageAspect = childImageAspect;
                clone.sortOrder = sortOrder;
                return clone;
            }
            
        }

        void folder_ChildrenChanged(object sender, ChildrenChangedEventArgs e) {
            if (!folderIsIndexed) {
                lock (this) {
                    currentChildren = folder.Children;
                }
            }

            if (onChildrenChanged != null) onChildrenChanged();
        }

        // trigger a re-sort
        public void Sort() {
            Sort(sortOrder);
        }

        public void Sort(SortOrder sortOrder) {
            if (folder != null && !folderIsIndexed) {
                this.sortOrder = sortOrder;
                Async.Queue(() => folder.Sort(sortOrder));
            }
        }

        public Item this[int index] {
            get {
                BaseItem baseItem;
                lock (this) {
                    baseItem = currentChildren[index];
                }
                return GetItem(baseItem);
            }
            set {
                throw new NotImplementedException();
            }
        }

        private Item GetItem(BaseItem baseItem) {
            Guid guid = baseItem.Id;
            Item item;
            if (!items.TryGetValue(guid, out item)) {
                item = ItemFactory.Instance.Create(baseItem);
                item.PhysicalParent = folderModel;
                items[guid] = item;
            }
            return item;
        }

        public void IndexBy(IndexType indexType) {

            if (folder == null) return;

            if (indexType == IndexType.None) {
                folderIsIndexed = false;
                lock (this) {
                    currentChildren = folder.Children;
                }
            } else {
                folderIsIndexed = true;
                lock (this) {
                    currentChildren = folder.IndexBy(indexType).Select(i => (BaseItem)i).ToList();
                }
            }
            
            folder_ChildrenChanged(this, null);
        }

        public int Count {
            get {
                lock (this) {
                    return currentChildren.Count;
                }
            }
        }

        public IEnumerator<Item> GetEnumerator() {
            if (folder != null) {
                lock (this) {
                    foreach (var baseItem in currentChildren) {
                        yield return GetItem(baseItem);
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        int ICollection.Count {
            get { return this.Count; }
        }

        object IList.this[int index] {
            get {
                return this[index];
            }
            set {
                throw new NotImplementedException();
            }
        }

        
        public float GetChildAspect(bool useBanner) {
            Async.Queue(() => CalculateChildAspect(useBanner));
            return childImageAspect;
        }

        private float CalculateChildAspect(bool useBanner) {

            Func<BaseItem, float> calcAspect;
            if (useBanner) {
                calcAspect = i => i.BannerImage != null ? i.BannerImage.Aspect : 0;
            } else {
                calcAspect = i => i.PrimaryImage != null ? i.PrimaryImage.Aspect : 0;
            }

            var aspects = this.folder
                .Children
                .Select(calcAspect)
                .Where(ratio => ratio > 0)
                .Take(4).ToArray();

            float oldAspect = childImageAspect;
            if (aspects.Length > 0) {
                childImageAspect = aspects.Average();
            }
            if (childImageAspect != oldAspect) {
                folder_ChildrenChanged(this, null);
            }

            return childImageAspect;
        }



        #region Uninmplemented interfaces that are not supported

        public int IndexOf(Item item) {
            throw new NotImplementedException();
        }

        public void Insert(int index, Item item) {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index) {
            throw new NotImplementedException();
        }


        public void Add(Item item) {
            throw new NotImplementedException();
        }

        public void Clear() {
            throw new NotImplementedException();
        }

        public bool Contains(Item item) {
            throw new NotImplementedException();
        }

        public void CopyTo(Item[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        public bool IsReadOnly {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(Item item) {
            throw new NotImplementedException();
        }


        void ICollection.CopyTo(Array array, int index) {
            throw new NotImplementedException();
        }


        bool ICollection.IsSynchronized {
            get { throw new NotImplementedException(); }
        }

        object ICollection.SyncRoot {
            get { throw new NotImplementedException(); }
        }

        int IList.Add(object value) {
            throw new NotImplementedException();
        }

        void IList.Clear() {
            throw new NotImplementedException();
        }

        bool IList.Contains(object value) {
            throw new NotImplementedException();
        }

        int IList.IndexOf(object value) {
            throw new NotImplementedException();
        }

        void IList.Insert(int index, object value) {
            throw new NotImplementedException();
        }

        bool IList.IsFixedSize {
            get { throw new NotImplementedException(); }
        }

        bool IList.IsReadOnly {
            get { throw new NotImplementedException(); }
        }

        void IList.Remove(object value) {
            throw new NotImplementedException();
        }

        void IList.RemoveAt(int index) {
            throw new NotImplementedException();
        }


        #endregion

        void IDisposable.Dispose() {
            
        }

    }
}
