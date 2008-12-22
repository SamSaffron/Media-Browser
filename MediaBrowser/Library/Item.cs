using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.Xml;
using MediaBrowser.Library.Sources;
using System.Diagnostics;
using System.Collections;
using Microsoft.MediaCenter;
using MediaBrowser.Util;

namespace MediaBrowser.Library
{

    public class Item : ModelItem
    {
        private static BackgroundProcessor<Item> childVerificationProcessor = new BackgroundProcessor<Item>(ThreadPoolSizes.CHILD_VERIFICATION_THREADS, Item.ChildVerificationCallback, "ChildVerification");
        private static BackgroundProcessor<Item> childRetrievalProcessor = new BackgroundProcessor<Item>(ThreadPoolSizes.CHILD_LOAD_THREADS, Item.ProcessRetrieveChildren, "ChildRetrieval");
        private static PlayState nonPlayableItemState = new PlayState();

        private static Item blank = new EmptyItemSource().ConstructItem();
        private object lck = new object();
        private List<Item> children;
        volatile bool childrenLoaded = false;
        private ItemIndex itemIndex;
        private MediaMetadata metadata;
        private PlayState playstate;
        private ItemSource source;
        private DisplayPreferences prefs;
        SizeRef actualThumbSize = new SizeRef(new Size(1, 1));

        public Item PhysicalParent { get; private set; }

        #region Item Construction
        internal Item()
        {
        }

        internal void Assign(ItemSource source)
        {
            this.source = source;
            this.source.NewItem += new NewItemHandler(source_NewItem);
            this.source.RemoveItem += new RemoveItemHandler(source_RemoveItem);
        }


        internal void Assign(ItemSource source, MediaMetadata metadata)
        {
            Assign(source);
            this.metadata = metadata;
        }
        #endregion


        public UniqueName UniqueName { get { return this.source.UniqueName; } }

        public void NavigatingInto()
        {
            if (childLoadPending)
                childRetrievalProcessor.PullToFront(this);
            else
                this.EnsureChildrenLoaded(false);
            if (pendingVerify)
                childVerificationProcessor.PullToFront(this);
            else if (!Config.Instance.EnableFileWatching) // if we aren't doing file watching then verify the children when we navigate forwards into an item
                this.VerifyChildrenAsync();
        }

        #region Metadata

        public MediaMetadata Metadata
        {
            get
            {
                //Debug.WriteLine("Returning metadata");
                if (metadata == null)
                    LoadMetadata();
                return metadata;
            }
            private set
            {
                //Debug.WriteLine("Setting metadata");
                if (value != this.metadata)
                {
                    if (this.metadata != null)
                    {
                        this.metadata.PropertyChanged -= new PropertyChangedEventHandler(metadata_PropertyChanged);
                        this.metadata.PreferredImage.PropertyChanged -= new PropertyChangedEventHandler(PreferredImage_PropertyChanged);
                    }
                    this.metadata = value;
                    if (this.metadata != null)
                    {
                        this.metadata.PropertyChanged += new PropertyChangedEventHandler(metadata_PropertyChanged);
                        this.metadata.PreferredImage.PropertyChanged += new PropertyChangedEventHandler(PreferredImage_PropertyChanged);
                    }
                    FirePropertyChanged("Metadata");
                }
            }
        }

        void PreferredImage_PropertyChanged(IPropertyObject sender, string property)
        {
            if (property == "AspectRatio")
                FirePropertyChanged("ThumbAspectRatio");
        }

        void metadata_PropertyChanged(IPropertyObject sender, string property)
        {
            if (property == "BannerImage")
                FirePropertyChanged("BannerImage");
            if (property == "HasBannerImage")
                FirePropertyChanged("HasBannerImage");
            if (property == "BackdropImage")
                FirePropertyChanged("BackdropImage");
            if (property == "HasBackdropImage")
                FirePropertyChanged("HasBackdropImage");
            if (property == "Overview")
                FirePropertyChanged("Overview");
        }

        /// <summary>
        /// The metadata overview if there is one otherwise a list of some of the children
        /// </summary>
        public string Overview
        {
            get
            {
                if ((this.Metadata.Overview == null) || (this.Metadata.Overview == ""))
                {
                    List<Item> c = this.UnsortedChildren;
                    if (c == null)
                        return "";
                    StringBuilder sb = new StringBuilder();
                    lock (c)
                    {
                        int num = c.Count > 20 ? 20 : c.Count;
                        for (int i = 0; i < num; ++i)
                        {
                            sb.Append(c[i].Metadata.Name);
                            if (i < num - 1)
                                sb.Append(" ... ");
                        }
                    }
                    return sb.ToString();
                }
                else
                    return this.Metadata.Overview.Replace("\r\n", "\n").Replace("\n\n", "\n");
            }
        }

        #endregion

        #region Images

        public bool HasBannerImage
        {
            get
            {
                if (this.Metadata.HasBannerImage)
                    return true;
                else if (this.PhysicalParent != null)
                    return this.PhysicalParent.HasBannerImage;
                else
                    return false;
            }
        }

        public LibraryImage BannerImage
        {
            get
            {
                if (this.Metadata.HasBannerImage)
                    return this.Metadata.BannerImage;
                else if (this.PhysicalParent != null)
                    return this.PhysicalParent.BannerImage;
                else
                    return BlankLibraryImage.Instance;
            }
        }

        public bool HasBackdropImage
        {
            get
            {
                if (this.Metadata.HasBackdropImage)
                    return true;
                else if (this.PhysicalParent != null)
                    return this.PhysicalParent.HasBackdropImage;
                else
                    return false;
            }
        }

        public LibraryImage BackdropImage
        {
            get
            {
                if (this.Metadata.HasBackdropImage)
                    return this.Metadata.BackdropImage;
                else if (this.PhysicalParent != null)
                    return this.PhysicalParent.BackdropImage;
                else
                    return BlankLibraryImage.Instance;
            }
        }

        public Item BlankItem
        {
            get { return blank; }
        }

        #endregion

        ICommand command;
        public ICommand Command
        {
            get
            {
                if (command == null)
                {
                    lock (lck)
                    {
                        if (command == null)
                            command = new Command();
                    }
                }
                return command;
            }
        }

        #region Playback

        public void Play()
        {
            try
            {
                if (this.Source.IsPlayable)
                    this.Source.PlayableItem.Play(this.PlayState, false);
            }
            catch (Exception)
            {
                MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
                ev.Dialog("There was a problem playing the content. Check location exists\n" + this.source.Location, "Content Error", DialogButtons.Ok, 60, true);
            }
        }

        public void Resume()
        {
            try
            {
                if (this.Source.IsPlayable)
                    this.Source.PlayableItem.Play(this.PlayState, true);
            }
            catch (Exception)
            {
                MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
                ev.Dialog("There was a problem playing the content. Check location exists\n" + this.source.Location, "Content Error", DialogButtons.Ok, 60, true);
            }
        }

        public PlayState PlayState
        {
            get
            {
                if (playstate == null)
                {
                    //Debug.WriteLine("Sync load of playstate");
                    LoadPlayState();
                }
                return this.playstate;
            }
            private set
            {
                //Debug.WriteLine("Setting playstate");
                if (value != playstate)
                {
                    if (this.playstate != null)
                        this.playstate.PropertyChanged -= new PropertyChangedEventHandler(playstate_PropertyChanged);
                    this.playstate = value;
                    this.playstate.PropertyChanged += new PropertyChangedEventHandler(playstate_PropertyChanged);
                    FirePropertyChanged("PlayState");
                    lock (watchLock)
                        unwatchedCountCache = -1;
                    FirePropertyChanged("HaveWatched");
                    FirePropertyChanged("UnwatchedCount");
                    FirePropertyChanged("ShowUnwatched");
                    FirePropertyChanged("UnwatchedCountSting");
                }
            }
        }

        void playstate_PropertyChanged(IPropertyObject sender, string property)
        {
            if (property == "HaveWatched")
            {
                lock (watchLock)
                    unwatchedCountCache = -1;
                FirePropertyChanged("HaveWatched");
                FirePropertyChanged("UnwatchedCount");
                FirePropertyChanged("ShowUnwatched");
                FirePropertyChanged("UnwatchedCountSting");
            }
        }
        #endregion



        public ItemSource Source
        {
            get
            {
                return this.source;
            }
        }

        #region Display Prefs
        public DisplayPreferences DisplayPrefs
        {
            get
            {
                if (this.prefs == null)
                    lock (lck)
                        if (this.prefs == null)
                            LoadDisplayPreferences();
                return this.prefs;
            }
            private set
            {
                if (this.prefs != null)
                    throw new NotSupportedException("Attempt to set displayPrefs twice");
                this.prefs = value;
                if (this.prefs != null)
                {
                    this.prefs.ThumbConstraint.PropertyChanged += new PropertyChangedEventHandler(ThumbConstraint_PropertyChanged);
                    this.prefs.ShowLabels.PropertyChanged += new PropertyChangedEventHandler(ShowLabels_PropertyChanged);
                    this.prefs.SortOrders.ChosenChanged += new EventHandler(SortOrders_ChosenChanged);
                    this.prefs.IndexByChoice.ChosenChanged += new EventHandler(IndexByChoice_ChosenChanged);
                    IndexByChoice_ChosenChanged(null, null);
                    SortOrders_ChosenChanged(null, null);
                    ShowLabels_PropertyChanged(null, null);
                    ThumbConstraint_PropertyChanged(null, null);
                }
                FirePropertyChanged("DisplayPrefs");
            }
        }


        void IndexByChoice_ChosenChanged(object sender, EventArgs e)
        {
            if (this.itemIndex != null)
            {
                this.itemIndex.IndexBy = this.prefs.IndexBy;
                this.selectedchildIndex = -1;
                if (this.itemIndex.IndexedAndSortedData.Count > 0)
                    this.SelectedChildIndex = 0;
            }
            FirePropertyChanged("Children");
            FirePropertyChanged("TripleTapCandidates");
        }

        void SortOrders_ChosenChanged(object sender, EventArgs e)
        {
            if (this.itemIndex != null)
                this.itemIndex.SortBy = this.prefs.SortOrder;
            FirePropertyChanged("Children");
            FirePropertyChanged("TripleTapCandidates");
        }

        #endregion

        #region Children
        public List<Item> UnsortedChildren
        {
            get
            {
                //Debug.WriteLine("Returning Children");
                EnsureChildrenLoaded(true);
                return children;
            }
        }

        public List<Item> Children
        {
            get
            {
                //Debug.WriteLine("Returning Children");
                EnsureChildrenLoaded(true);
                return itemIndex.IndexedAndSortedData;
            }
        }



        private int selectedchildIndex = -1;
        public int SelectedChildIndex
        {
            get
            {
                if (this.selectedchildIndex > this.Children.Count)
                    this.selectedchildIndex = -1;
                //Debug.WriteLine("Returning SelectedChildIndex");
                return this.selectedchildIndex;
            }
            set
            {
                //Debug.WriteLine("Setting SelectedChildIndex");
                if (this.selectedchildIndex != value)
                {
                    this.selectedchildIndex = value;
                    FirePropertyChanged("SelectedChildIndex");
                    FirePropertyChanged("SelectedChild");
                }
            }
        }

        public Item SelectedChild
        {
            get
            {
                //Debug.WriteLine("Returning SelectedChild");
                if ((this.SelectedChildIndex < 0) || (this.selectedchildIndex >= this.Children.Count))
                    return blank;
                return this.Children[this.SelectedChildIndex];
            }
        }
        #endregion


        #region Triple Tap Support

        private int jilShift = -1;
        public int JILShift
        {
            get
            {
                return this.jilShift;
            }
            set
            {
                this.jilShift = value;
                FirePropertyChanged("JILShift");
            }
        }

        public string TripleTapSelect
        {
            set
            {
                if ((value != null) && (value != ""))
                {
                    TripleTapIndex proto = new TripleTapIndex { Index = -1, Name = value };
                    lock (itemIndex.TripleTapCandidates)
                    {
                        int index = itemIndex.TripleTapCandidates.BinarySearch(proto);
                        if (index < 0)
                        {
                            index = ~index;
                            if (index >= itemIndex.TripleTapCandidates.Count)
                                index = itemIndex.TripleTapCandidates.Count - 1;
                        }
                        this.JILShift = itemIndex.TripleTapCandidates[index].Index - this.SelectedChildIndex;
                    }
                }
            }
        }
        #endregion

        #region watch tracking


        public bool HaveWatched
        {
            get
            {
                return this.UnwatchedCount == 0;
            }
        }

        public bool ShowUnwatched
        {
            get { return this.UnwatchedCountString.Length > 0; }
        }

        public string UnwatchedCountString
        {
            get
            {
                if (this.source.IsPlayable)
                    return "";
                int i = this.UnwatchedCount;
                return (i == 0) ? "" : i.ToString();
            }
        }
        object watchLock = new object();
        volatile int unwatchedCountCache = -1;
        public int UnwatchedCount
        {
            get
            {
                if (unwatchedCountCache > -1)
                    return unwatchedCountCache;
                if (this.Source.IsPlayable)
                    return this.PlayState.HaveWatched ? 0 : 1;

                lock (watchLock)
                {
                    if (this.UnsortedChildren != null)
                    {
                        int c = 0;
                        lock (this.UnsortedChildren)
                            foreach (Item i in this.UnsortedChildren)
                                c += i.UnwatchedCount;
                        unwatchedCountCache = c;
                    }
                    else unwatchedCountCache = 0;
                    return unwatchedCountCache;
                }
            }
        }

        public void ToggleWatched()
        {
            SetWatched(!this.HaveWatched);
            lock (watchLock)
                unwatchedCountCache = -1;
            FirePropertyChanged("HaveWatched");
            FirePropertyChanged("UnwatchedCount");
            FirePropertyChanged("ShowUnwatched");
            FirePropertyChanged("UnwatchedCountSting");
        }

        private void SetWatched(bool value)
        {
            if (this.Source.IsPlayable)
            {
                if (value != this.HaveWatched)
                {
                    if (value)
                    {
                        if (PlayState.PlayCount == 0)
                            PlayState.PlayCount = 1;
                    }
                    else
                        PlayState.PlayCount = 0;
                    lock (watchLock)
                        unwatchedCountCache = -1;
                }
            }
            else if (this.UnsortedChildren != null)
            {
                lock (this.UnsortedChildren)
                    foreach (Item i in this.UnsortedChildren)
                        if (i.HaveWatched != value)
                            i.SetWatched(value);
            }
        }

        public int FirstUnwatchedIndex
        {
            get
            {
                if (Config.Instance.DefaultToFirstUnwatched)
                {
                    lock (this.Children)
                        for (int i = 0; i < this.Children.Count; ++i)
                            if (!this.Children[i].HaveWatched)
                                return i;

                }
                return 0;
            }
        }

        #endregion

        #region thumbnail sizing
        private float ThumbAspectRatio
        {
            get
            {
                //Debug.WriteLine("Returning ThumbAspectRation");
                if (this.Metadata.HasPreferredImage)
                    return this.Metadata.PreferredImage.AspectRatio;
                else
                    return 0;
            }
        }

        public SizeRef ActualThumbSize
        {
            get
            {
                if (this.actualThumbSize.Value.Height == 1)
                    UpdateActualThumbSize();
                //Debug.WriteLine("Returning ActualThumbSize");
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
                //Debug.WriteLine("Returning ReferenceSize");
                Size s = this.ActualThumbSize.Value;
                if (this.DisplayPrefs.ShowLabels.Value)
                    s.Height += 40;
                return s;
            }
        }

        private void UpdateActualThumbSize()
        {
            //Debug.WriteLine("Updating ActualThumbSize");
            Size s = this.DisplayPrefs.ThumbConstraint.Value;
            float f = 0;
            if (this.UnsortedChildren != null)
                lock (this.UnsortedChildren)
                    foreach (Item i in this.UnsortedChildren)
                    {
                        f = i.ThumbAspectRatio;
                        if (f > 0)
                            break;
                    }
            if (f == 0)
                f = 1;
            float maxAspect = s.Height / s.Width;
            if (f > maxAspect)
                s.Width = (int)(s.Height / f);
            else
                s.Height = (int)(s.Width * f);
            if (this.actualThumbSize.Value != s)
            {
                this.actualThumbSize.Value = s;
                FirePropertyChanged("ReferenceSize");
                FirePropertyChanged("PosterZoom");
            }
        }

        public Vector3 PosterZoom
        {
            get
            {
                //Debug.WriteLine("Returning PosterZoom");
                Size s = this.ReferenceSize;
                float x = Math.Max(s.Height, s.Width);
                if (x == 1)
                    return new Vector3(1.15F, 1.15F, 1); // default if we haven't be set yet
                float z = (float)((-0.007 * x) + 2.5);
                if (z < 1.15)
                    z = 1.15F;
                if (z > 1.9F)
                    z = 1.9F; // above this the navigation arrows start going in strange directions!
                return new Vector3(z, z, 1);
            }
        }

        #endregion

        #region Child Loading

        private void CreateChildIndex()
        {
            if (this.itemIndex == null)
                lock (lck)
                    if (this.itemIndex == null)
                    {
                        this.children = new List<Item>();
                        itemIndex = new ItemIndex(children);
                        if (this.prefs != null)
                        {
                            itemIndex.SortBy = this.DisplayPrefs.SortOrder;
                            itemIndex.IndexBy = this.DisplayPrefs.IndexBy;
                        }
                    }
        }

        public void EnsureChildrenLoaded(bool allowAsync)
        {
            if (this.source.IsPlayable)
                return;
            if (childLoadPending && allowAsync)
                return;
            if (!childrenLoaded)
                lock (lck)
                {
                    if (!childrenLoaded)
                    {
                        if (allowAsync)
                        {
                            if (!childLoadPending)
                            {
                                childLoadPending = true;
                                if (allowAsync)
                                    childRetrievalProcessor.Inject(this);
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Sync child collection create:" + this.source.Location);
                            CreateChildIndex();
                            childrenLoaded = true;
                            if (!childLoadPending)
                                childLoadPending = true;
                        }
                    }
                }
            if ((!allowAsync) && childLoadPending)
            {
                Debug.WriteLine("Sync retrieve children:" + this.source.RawName);
                RetrieveChildren();
            }
        }

        private static void ProcessRetrieveChildren(Item itm)
        {
            itm.RetrieveChildren();
        }

        private volatile bool childLoadPending = false;
        Item[] pendingChildren = null;

        private void RetrieveChildren()
        {
            Debug.WriteLine("Retrieving Children:" + this.source.Location);

            lock (lck)
                if (!childrenLoaded)
                {
                    Debug.WriteLine("Async child collection create:" + this.source.Location);
                    CreateChildIndex();
                    childrenLoaded = true;
                }
            lock (lck)
                if (!childLoadPending)
                    return;
                else
                    childLoadPending = false;
            if (!(this.Source is IndexingSource))
                pendingChildren = ItemCache.Instance.RetrieveChildren(this.UniqueName);
            else
                pendingChildren = null;
            if (pendingChildren != null)
            {
                foreach (Item i in pendingChildren)
                {
                    i.LoadPlayState();
                    i.PropertyChanged += new PropertyChangedEventHandler(child_PropertyChanged);
                    if (i.PhysicalParent == null)
                        i.PhysicalParent = this;
                }
            }
            if (Microsoft.MediaCenter.UI.Application.IsApplicationThread)
                RetrieveChildrenFinished(null);
            else
                Microsoft.MediaCenter.UI.Application.DeferredInvoke(RetrieveChildrenFinished);
        }

        private void RetrieveChildrenFinished(object nothing)
        {
            if (pendingChildren != null)
            {
                lock (this.children)
                    children.AddRange(pendingChildren);
                pendingChildren = null;
                itemIndex.FlagUnsorted();
            }
            FireChildrenChangedEvents();
            VerifyChildrenAsync();
        }
        #endregion

        #region Child Verification

        void source_NewItem(ItemSource newItem)
        {
            if (childrenLoaded)
                Microsoft.MediaCenter.UI.Application.DeferredInvoke(AddChild, newItem);
        }

        private void AddChild(object newItemSource)
        {
            Item itm = ((ItemSource)newItemSource).ConstructItem();
            lock (this.children)
            {
                this.children.Add(itm);
                this.itemIndex.FlagUnsorted();
            }
            FireChildrenChangedEvents();
            if (!(this.Source is IndexingSource))
                lock (this.children)
                    ItemCache.Instance.SaveChildren(this.UniqueName, this.children);
        }

        void source_RemoveItem(ItemSource newItem)
        {
            if (childrenLoaded)
                Microsoft.MediaCenter.UI.Application.DeferredInvoke(RemoveChild, newItem);
        }

        private void RemoveChild(object removedItemSource)
        {
            lock (this.children)
            {
                int i = 0;
                foreach (Item itm in this.children)
                {
                    if (itm.source.UniqueName.Equals(((ItemSource)removedItemSource).UniqueName))
                        break;
                    i++;
                }
                if (i < this.children.Count)
                    this.children.RemoveAt(i);
                ItemCache.Instance.RemoveSource((ItemSource)removedItemSource);
                this.itemIndex.FlagUnsorted();
            }
            FireChildrenChangedEvents();
            if (!(this.Source is IndexingSource))
                lock (this.children)
                    ItemCache.Instance.SaveChildren(this.UniqueName, this.children);
        }

        object verifyLock = new object();
        volatile bool pendingVerify = false;
        public void VerifyChildrenAsync()
        {
            if (!childrenLoaded)
                return;
            if (!pendingVerify)
                lock (verifyLock)
                    if (!pendingVerify)
                    {
                        pendingVerify = true;
                        childVerificationProcessor.Inject(this);
                    }
        }

        private List<ItemSource> childrenToAdd = null;

        private static void ChildVerificationCallback(Item item)
        {
            if (item.VerifyChildren())
                Microsoft.MediaCenter.UI.Application.DeferredInvoke(item.VerifyChildrenComplete);
            else
                lock (item.verifyLock)
                    item.pendingVerify = false;
        }

        private bool VerifyChildren()
        {
            // todo should also verify on navigate into folder
            Debug.WriteLine("Verifying children:" + this.source.RawName);
            //using (new Profiler(this.source.RawName))
            {
                bool changed = false;
                Item[] clist;
                lock (this.children)
                    clist = this.children.ToArray();
                if (clist.Length > 0)
                {
					// TODO: Deal with network shares that have gone online/offline since last session
                    Dictionary<UniqueName, Item> itemsToRemove = new Dictionary<UniqueName, Item>();
                    foreach (Item i in clist)
                    {
                        // index the current items
                        itemsToRemove[i.UniqueName] = i;
                    }
                    foreach (ItemSource s in source.ChildSources)
                    {
                        if (!itemsToRemove.ContainsKey(s.UniqueName))
                        {
                            // add new item
                            Trace.TraceInformation("New item found: " + s.Location);
                            if (childrenToAdd == null)
                                childrenToAdd = new List<ItemSource>();
                            s.PrepareToConstruct();
							// Skip ItemType.Other since we can't do anything with it
							if (s.ItemType != ItemType.Other)
								childrenToAdd.Add(s);
							changed = true;
						}
                        else
                        {
                            // revalidation of type only happens on navigation
                            itemsToRemove.Remove(s.UniqueName);
                        }
                    }
                    foreach (Item i in itemsToRemove.Values)
                    {
                        // remove items no longer present
                        changed = true;
                        lock (this.children)
                        {
                            this.children.Remove(i);
                            i.PropertyChanged -= new PropertyChangedEventHandler(child_PropertyChanged);
                            if (i.PhysicalParent == this)
                                i.PhysicalParent = null;
                        }
                        ItemCache.Instance.RemoveSource(i.Source);
                    }
                }
                else
                {
                    foreach (ItemSource s in source.ChildSources)
                    {
                        if (childrenToAdd == null)
                            childrenToAdd = new List<ItemSource>();
                        s.PrepareToConstruct();
						// Skip ItemType.Other since we can't do anything with it
						if (s.ItemType != ItemType.Other)
							childrenToAdd.Add(s);
						changed = true;
					}
                }
                return changed;
            }
        }

        private void VerifyChildrenComplete(object nothing)
        {
            Debug.WriteLine("Finishing children verification:" + this.source.RawName);
            if (childrenToAdd != null)
            {
                foreach (ItemSource s in childrenToAdd)
                {
                    Item itm = s.ConstructItem();
                    if (itm.PhysicalParent == null) // genuine new item not one from a ExistingItem wrapper etc.
                        ItemCache.Instance.SaveSource(itm.Source);
                    lock (this.children)
                        this.children.Add(itm);
                    itm.PropertyChanged += new PropertyChangedEventHandler(child_PropertyChanged);
                    if (itm.PhysicalParent == null)
                        itm.PhysicalParent = this;
                }
                childrenToAdd = null;
            }
            itemIndex.FlagUnsorted();
            lock (verifyLock)
                pendingVerify = false;
            FireChildrenChangedEvents();
            if (!(this.Source is IndexingSource))
                lock (this.children)
                    ItemCache.Instance.SaveChildren(this.UniqueName, this.children);

            //Debug.WriteLine("Finished children verification:" + this.source.Name);
        }

        #endregion

        private void FireChildrenChangedEvents()
        {
            FirePropertyChanged("Children");
            FirePropertyChanged("TripleTapCandidates");
            FirePropertyChanged("UnsortedChildren");
            lock (watchLock)
                unwatchedCountCache = -1;
            FirePropertyChanged("HaveWatched");
            FirePropertyChanged("UnwatchedCount");
            FirePropertyChanged("ShowUnwatched");
            FirePropertyChanged("UnwatchedCountSting");
        }

        #region Metadata loading and refresh

        private void LoadMetadata()
        {
            if (this.metadata == null)
                lock (lck)
                    if (this.metadata == null)
                    {
                        //using (Profiler p = new Profiler())
                        {
                            Debug.WriteLine("Loading metadata for " + this.Source.Location);
                            MediaMetadataStore data = ItemCache.Instance.RetrieveMetadata(this.UniqueName);
                            if (data != null)
                            {
                                //Debug.WriteLine("Loaded cached metadata:" + this.UniqueName.Value);
                                this.Metadata = MediaMetadataFactory.Instance.Create(data, this.Source.ItemType);
                                this.Metadata.SaveEnabled = true;
                                this.metadata.RefreshAsync(this, false, false);
                            }
                            else
                            {
                                //Debug.WriteLine("Fetching new metadata:" + this.UniqueName.Value);
                                //data = MetaDataSource.Instance.GetMetadata(this);
                                //if (data == null)
                                data = new MediaMetadataStore(this.UniqueName) { Name = this.Source.Name };
                                this.Metadata = MediaMetadataFactory.Instance.Create(data, this.Source.ItemType);
                                this.Metadata.SaveEnabled = true;
                                this.Metadata.Save();
                                this.Metadata.RefreshAsync(this, true, true);
                            }
                        }
                    }
        }

        public void RefreshMetadata()
        {
            this.Metadata.RefreshAsync(this, true, false);
        }
        #endregion

        private void LoadPlayState()
        {
            if (this.Source.IsPlayable)
            {
                PlayState p = ItemCache.Instance.RetrievePlayState(this.UniqueName);
                if (p == null)
                {
                    p = PlayStateFactory.Instance.Create(this.UniqueName); // initialise an empty version that items can bind to
                    if (source.CreatedDate <= Config.Instance.AssumeWatchedBefore)
                        p.PlayCount = 1;
                    else
                        p.PlayCount = 0; // making this assignment forces a save
                }
                this.PlayState = p;
            }
            else
                this.playstate = nonPlayableItemState;
        }

        private void LoadDisplayPreferences()
        {
            Debug.WriteLine("Loading display prefs for " + this.Source.Location);
            DisplayPreferences dp = ItemCache.Instance.RetrieveDisplayPreferences(this.UniqueName);
            if (dp == null)
            {
                dp = new DisplayPreferences(this.UniqueName);
                dp.LoadDefaults();
                if ((this.PhysicalParent != null) && (Config.Instance.InheritDefaultView))
                {
                    // inherit some of the display properties from our parent the first time we are visited
                    DisplayPreferences pt = this.PhysicalParent.DisplayPrefs;
                    dp.ViewType.Chosen = pt.ViewType.Chosen;
                    dp.ShowLabels.Value = pt.ShowLabels.Value;
                    // after some use, carrying the sort order forward doesn;t feel right - for seasons especially it can be confusing
                    // dp.SortOrder = pt.SortOrder;
                    dp.VerticalScroll.Value = pt.VerticalScroll.Value;
                }
            }
            this.DisplayPrefs = dp;
        }

        void ShowLabels_PropertyChanged(IPropertyObject sender, string property)
        {
            FirePropertyChanged("ReferenceSize");
            FirePropertyChanged("PosterZoom");
        }

        void ThumbConstraint_PropertyChanged(IPropertyObject sender, string property)
        {
            UpdateActualThumbSize();
            FirePropertyChanged("ReferenceSize");
            FirePropertyChanged("PosterZoom");
        }

        protected override void Dispose(bool disposing)
        {
            if (this.metadata != null)
                this.metadata.Dispose();
            if (this.playstate != null)
                this.playstate.Dispose();
            if (this.prefs != null)
                this.prefs.Dispose();
            if (this.children != null)
                lock (this.children)
                    foreach (Item i in this.children)
                    {
                        i.PropertyChanged -= new PropertyChangedEventHandler(child_PropertyChanged);
                        if (i.PhysicalParent == this)
                            i.PhysicalParent = null;
                    }
            base.Dispose(disposing);
        }

        void child_PropertyChanged(IPropertyObject sender, string property)
        {
            /*if (property == "HaveWatched")
            {
                FirePropertyChanged(property);
                // note: need tobe careful this doesn't trigger the load of the prefs 
                // that can then trigger a cascade that loads metadata, prefs should only be loaded by 
                // functions that are required when the item is the current item displayed
                if ((this.prefs != null) && (this.DisplayPrefs.SortOrder == SortOrder.Unwatched))
                {
                    this.itemIndex.FlagUnsorted();
                    FirePropertyChanged("Children");
                }
            }
            else*/
            if (property == "UnwatchedCount")
            {
                lock (watchLock)
                    unwatchedCountCache = -1;
                FirePropertyChanged("HaveWatched");
                FirePropertyChanged("UnwatchedCount");
                FirePropertyChanged("ShowUnwatched");
                FirePropertyChanged("UnwatchedCountSting");
                // note: need to be careful this doesn't trigger the load of the prefs 
                // that can then trigger a cascade that loads metadata, prefs should only be loaded by 
                // functions that are required when the item is the current item displayed
                if ((this.prefs != null) && (this.DisplayPrefs.SortOrder == SortOrder.Unwatched))
                {
                    this.itemIndex.FlagUnsorted();
                    FirePropertyChanged("Children");
                }
            }
            else if (property == "ThumbAspectRatio")
                UpdateActualThumbSize();
        }

        internal void EnsureMetadataLoaded()
        {
            Debug.WriteLine("Ensuring metadata loaded :" + this.UniqueName.Value + " : " + this.Source.Name);
            LoadMetadata();
        }
    }


}
