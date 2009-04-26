using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Library.Util;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Library.Extensions;
using System.Collections;
using System.Diagnostics;

namespace MediaBrowser.Library.Entities {

    public class ChildrenChangedEventArgs : EventArgs {
    }

    public class Folder : BaseItem, MediaBrowser.Library.Entities.IFolder {

        public event EventHandler<ChildrenChangedEventArgs> ChildrenChanged;

        Lazy<List<BaseItem>> children;
        protected IFolderMediaLocation location;
        SortOrder sortOrder = SortOrder.Name;
        object validateChildrenLock = new object();

        public Folder() : base() {
            children = new Lazy<List<BaseItem>>(() => GetChildren(true), () => OnChildrenChanged(null));
        }

        /// <summary>
        /// By default children are loaded on first access, this operation is slow. So sometimes you may
        ///  want to force the children to load;
        /// </summary>
        public virtual void EnsureChildrenLoaded() {
            var ignore = ActualChildren;
        }

        public IFolderMediaLocation FolderMediaLocation {
            get {
                if (location == null) {
                    location = (IFolderMediaLocation)MediaLocationFactory.Create(Path);
                }
                return location;
            }
        }

        public override void Assign(IMediaLocation location, IEnumerable<InitializationParameter> parameters, Guid id) {
            base.Assign(location, parameters, id);
            this.location = location as IFolderMediaLocation;
        }


        /// <summary>
        /// Returns a safe clone of the children
        /// </summary>
        public IList<BaseItem> Children {
            get {
                // return a clone
                lock (ActualChildren) {
                    return ActualChildren.ToList();
                }
            }
        }

        public void Sort(SortOrder sortOrder) {
            Sort(sortOrder, true);
        }

        public virtual void ValidateChildren() {
            // we never want 2 threads validating children at the same time
            lock (validateChildrenLock) {
                ValidateChildrenImpl();
            }
        }

        public bool Watched {
            set {
                foreach (var item in this.EnumerateChildren()) {
                    var video = item as Video;
                    if (video != null) {
                        video.PlaybackStatus.WasPlayed = value;
                    }
                    var folder = item as Folder;
                    if (folder != null) {
                        folder.Watched = value;
                    }
                }
            }
        }

        public int UnwatchedCount {
            get {
                int count = 0;

                foreach (var item in this.EnumerateChildren()) {
                    var video = item as Video;
                    if (video != null && video.PlaybackStatus.PlayCount == 0) {
                        count++;
                    } else {
                        var folder = item as Folder;
                        if (folder != null) {
                            count += folder.UnwatchedCount;
                        }
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Will search all the children recursively
        /// </summary>
        /// <param name="searchFunction"></param>
        /// <returns></returns>
        public Index Search(Func<BaseItem, bool> searchFunction, string name) {
            var items = new List<BaseItem>();

            foreach (var item in RecursiveChildren) {
                if (searchFunction(item)) {
                    items.Add(item);
                }
            }
            return new Index(name, items);
        }

        public IList<Index> IndexBy(IndexType indexType) {

            if (indexType == IndexType.None) throw new ArgumentException("Index type should not be none!");

            Func<Show, IEnumerable<string>> indexingFunction = null;

            switch (indexType) {
                case IndexType.Actor:
                    indexingFunction = show =>
                        show.Actors == null ? null : show.Actors.Select(a => a.Name);
                    break;
                case IndexType.Genre:
                    indexingFunction = show => show.Genres;
                    break;
                case IndexType.Director:
                    indexingFunction = show => show.Directors;
                    break;
                case IndexType.Year:
                    indexingFunction = show =>
                        show.ProductionYear == null ? null : new string[] { show.ProductionYear.ToString() };
                    break;
                case IndexType.Studio:
                    indexingFunction = show => show.Studios;
                    break;
                default:
                    break;
            }

            var index = new Dictionary<string, List<BaseItem>>();
            foreach (var item in RecursiveChildren) {
                Show show = item as Show;
                IEnumerable<string> subIndex = null;
                if (show != null) {
                    subIndex = indexingFunction(show);
                }
                bool added = false;

                if (subIndex != null) {
                    foreach (var str in subIndex) {
                        var cleanString = str.Trim();
                        if (cleanString.Length > 0) {
                            added = true;
                            AddItemToIndex(index, item, cleanString);
                        }
                    }
                }
                if (!added) {
                    AddItemToIndex(index, item, "<Unknown>");
                }
            }

            List<Index> sortedIndex = new List<Index>();

            sortedIndex.AddRange(
                index
                    .Select(pair => new Index(pair.Key, pair.Value))
                );


            sortedIndex.Sort((x, y) =>
            {
                if (x.Children.Count == 1 && y.Children.Count > 1) return 1;
                if (x.Children.Count > 1 && y.Children.Count == 1) return -1;
                return x.Name.CompareTo(y.Name);
            });

            return sortedIndex;
        }

        /// <summary>
        /// A recursive enumerator that walks through all the sub children 
        ///   Safe for multithreaded use, since it operates on list clones
        /// </summary>
        public IEnumerable<BaseItem> RecursiveChildren {
            get {
                foreach (var item in Children) {
                    yield return item;
                    var folder = item as Folder;
                    if (folder != null) {
                        foreach (var subitem in folder.RecursiveChildren) {
                            yield return subitem;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Protected enumeration through children, 
        ///  this has the potential to block out the item, so its not exposed publicly
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<BaseItem> EnumerateChildren() {
            lock (ActualChildren) {
                foreach (var item in ActualChildren) {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Direct access to children 
        /// </summary>
        protected virtual List<BaseItem> ActualChildren {
            get {
                return children.Value;
            }
        }

        protected void OnChildrenChanged(ChildrenChangedEventArgs args) {
            Sort(sortOrder, false);

            if (ChildrenChanged != null) {
                ChildrenChanged(this, args);
            }
        }

        void ValidateChildrenImpl() {
            location = null;
            // cache a copy of the children
            var childrenCopy = Children;

            var validChildren = GetChildren(false);
            var currentChildren = new Dictionary<Guid, BaseItem>();
            // in case some how we have a non distinc list 
            foreach (var item in childrenCopy) {
                currentChildren[item.Id] = item;
            }


            bool changed = false;
            foreach (var item in validChildren) {
                BaseItem currentChild;
                if (currentChildren.TryGetValue(item.Id, out currentChild)) {
                    changed |= currentChild.AssignFromItem(item);
                    currentChildren[item.Id] = null;
                } else {
                    changed = true;
                    lock (ActualChildren) {
                        item.Parent = this;
                        ActualChildren.Add(item);
                    }
                }
            }

            foreach (var item in currentChildren.Values.Where(item => item != null)) {
                changed = true;
                lock (ActualChildren) {
                    ActualChildren.RemoveAll(current => current.Id == item.Id);
                }
            }

            // this is a rare concurrency bug workaround - which I already fixed (it protects against regressions)
            if (!changed && childrenCopy.Count != validChildren.Count) {
                Debug.Assert(false,"For some reason we have duplicate items in our folder, fixing this up!");
                childrenCopy = childrenCopy
                    .Distinct(i => i.Id)
                    .ToList();

                lock (ActualChildren) {
                    ActualChildren.Clear(); 
                    ActualChildren.AddRange(childrenCopy);
                }

                changed = true;
            }


            if (changed) {
                SaveChildren(Children);
                OnChildrenChanged(null);
            }
        }

        List<BaseItem> GetChildren(bool allowCache) {

            List<BaseItem> items = null;
            if (allowCache) {
                items = GetCachedChildren();
            }

            if (items == null) {
                items = GetNonCachedChildren();

                if (allowCache) {
                    SaveChildren(items);
                }
            }

            SetParent(items);
            return items;
        }

        protected virtual List<BaseItem> GetNonCachedChildren() {
            List<BaseItem> items = new List<BaseItem>();

            foreach (var location in this.FolderMediaLocation.Children) {
                var item = BaseItemFactory.Create(location);
                if (item != null) {
                    items.Add(item);
                }
            }
            return items;
        }

        void SaveChildren(IList<BaseItem> items) {
            ItemCache.Instance.SaveChildren(Id, items.Select(i => i.Id));
            foreach (var item in items) {
                ItemCache.Instance.SaveItem(item);
            }
        }

        void SetParent(List<BaseItem> items) {
            foreach (var item in items) {
                item.Parent = this;
            }
        }

        void AddItemToIndex(Dictionary<string, List<BaseItem>> index, BaseItem item, string name) {
            List<BaseItem> subItems;
            if (!index.TryGetValue(name, out subItems)) {
                subItems = new List<BaseItem>();
                index[name] = subItems;
            }
            subItems.Add(item);
        }

        void Sort(SortOrder sortOrder, bool notifyChange) {
            this.sortOrder = sortOrder;
            lock (ActualChildren) {
                ActualChildren.Sort(new BaseItemComparer(sortOrder));
            }
            if (notifyChange) OnChildrenChanged(null);
        }

        List<BaseItem> GetCachedChildren() {
            List<BaseItem> items = null;

            var cached = ItemCache.Instance.RetrieveChildren(Id);
            if (cached != null) {
                items = new List<BaseItem>();
                foreach (var guid in cached) {
                    var item = ItemCache.Instance.RetrieveItem(guid);
                    if (item != null) {
                        items.Add(item);
                    }
                }
            }
            return items;
        }


      
        
    }
}
