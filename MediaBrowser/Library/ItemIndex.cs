using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Library.Sources;
using System.Collections;
using MediaBrowser.Util;
using Microsoft.MediaCenter;
using System.IO;

namespace MediaBrowser.Library
{
    public class TripleTapIndex : IComparable<TripleTapIndex>
    {
        public int Index;
        public string Name;

        #region IComparable<TripleTapIndex> Members

        public int CompareTo(TripleTapIndex other)
        {
            return this.Name.CompareTo(other.Name);
        }

        #endregion
    }

    public class ItemIndex
    {
        private List<Item> rawData;
        private List<Item> indexedData = new List<Item>();
        private List<Item> sortedAndIndexedData = new List<Item>();
        private List<Item> returnableData = new List<Item>();
        private List<TripleTapIndex> tripleTapCandidates = new List<TripleTapIndex>();
        
        private bool sortRequired = true;

        private double percentageUnknown = 0;

        public ItemIndex(List<Item> items)
        {
            this.rawData = items;
        }

        private IndexType indexby = IndexType.None;
        public IndexType IndexBy
        {
            get { return this.indexby; }
            set
            {
                if (this.indexby != value)
                {
                    this.indexby = value;
                    FlagUnsorted();
                }
            }
        }
        private SortOrder order = SortOrder.Name;
        public SortOrder SortBy 
        {
            get { return this.order; }
            set
            {
                if (this.order != value)
                {
                    this.order = value;
                    FlagUnsorted();
                }
            }
        }
        public List<Item> IndexedAndSortedData
        {
            get
            {
                if (sortRequired)
                {
                    this.ReindexAndSort();
                    if ((Config.Instance.ShowIndexWarning) && (percentageUnknown > Config.Instance.IndexWarningThreshold))
                    {
                        MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
                        DialogResult r = ev.Dialog(string.Format("{0:#%} of your items do not have the necessary metadata for the index and have been categorized as unknown.\nShow this warning in future?", this.percentageUnknown), "Metadata Warning", DialogButtons.Yes | DialogButtons.No, 60, true);
                        if (r == DialogResult.No)
                            Config.Instance.ShowIndexWarning = false;
                    }
                }
                return returnableData;
            }
        }

        public List<TripleTapIndex> TripleTapCandidates
        {
            get { return this.tripleTapCandidates; }
        }

        public void FlagUnsorted()
        {
            sortRequired = true;
        }

        private void ReindexAndSort()
        {
            sortedAndIndexedData = new List<Item>();
            switch (this.IndexBy)
            {
                case IndexType.None:
                    lock (indexedData)
                    {
                        foreach (Item i in indexedData)
                            i.Dispose();
                        indexedData.Clear();
                    }
                    lock (sortedAndIndexedData)
                    {
                        sortedAndIndexedData.Clear();
                        lock (this.rawData)
                            sortedAndIndexedData.AddRange(this.rawData);
                        sortedAndIndexedData.Sort(new ItemComparer(this.SortBy));
                    }
                    lock (tripleTapCandidates)
                    {
                        tripleTapCandidates.Clear();
                        int c = 0;
                        foreach (Item itm in sortedAndIndexedData)
                        {
                            tripleTapCandidates.Add(new TripleTapIndex { Index = c, Name = itm.Metadata.SortableName });
                            c++;
                        }
                        tripleTapCandidates.Sort();
                    }
                    break;
                default:
                    lock (rawData)
                        foreach (Item i in rawData)
                            i.EnsureMetadataLoaded();
                    CreateIndex(this.IndexBy);
                    lock (sortedAndIndexedData)
                    {
                        sortedAndIndexedData.Clear();
                        lock(indexedData)
                            sortedAndIndexedData.AddRange(this.indexedData);
                        sortedAndIndexedData.Sort(new ItemNameComparer());                   
                    }
                    lock (tripleTapCandidates)
                    {
                        tripleTapCandidates.Clear();
                        int d = 0;
                        foreach (Item i in sortedAndIndexedData)
                        {
                            tripleTapCandidates.Add(new TripleTapIndex { Index = d, Name = i.Source.RawName });
                            d++;
                        }
                        tripleTapCandidates.Sort();
                    }
                    break;                    
            }
            
            this.sortRequired = false;
            returnableData = this.sortedAndIndexedData;
        }

        private void CreateIndex(IndexType indexType)
        {
            Dictionary<string, List<Item>> index = new Dictionary<string, List<Item>>();
            int unknown = 0;
            int count = 0;
            lock (rawData)
            {
                count = rawData.Count;
                foreach (Item i in rawData)
                {
                    List<string> keys = null;
                    switch (indexType)
                    {
                        case IndexType.Actor:
                            if ((i.Metadata.Actors != null) && (i.Metadata.Actors.Count > 0))
                            {
                                keys = new List<string>();
                                foreach (Actor a in i.Metadata.Actors)
                                    keys.Add(a.Name);
                            }
                            else
                                keys = null;
                            break;
                        case IndexType.Director:
                            if ((i.Metadata.Directors!=null) && (i.Metadata.Directors.Count > 0))
                                keys = i.Metadata.Directors;
                            break;
                        case IndexType.Genre:
                            if ((i.Metadata.Genres!=null) && (i.Metadata.Genres.Count > 0))
                                keys = i.Metadata.Genres;
                            break;
                        case IndexType.Year:
                            if (i.Metadata.ProductionYear != null)
                            {
                                keys = new List<string>();
                                keys.Add(i.Metadata.ProductionYear.ToString());
                            }
                            break;
                        case IndexType.Studio:
                            if ((i.Metadata.Studios != null) && (i.Metadata.Studios.Count > 0))
                            {
                                keys = new List<string>();
                                foreach (Studio a in i.Metadata.Studios)
                                    keys.Add(a.Name);
                            }
                            else
                                keys = null;
                            break;
                        default:
                            keys = null;
                            break;
                    }
                    if (keys != null)
                        foreach (string k in keys)
                        {
                            List<Item> lst;
                            if (!index.ContainsKey(k))
                            {
                                lst = new List<Item>();
                                index[k] = lst;
                            }
                            else
                                lst = index[k];
                            lst.Add(i);
                        }
                    else
                    {
                        unknown++;
                        string k = "<Unknown>";
                        List<Item> lst;
                        if (!index.ContainsKey(k))
                        {
                            lst = new List<Item>();
                            index[k] = lst;
                        }
                        else
                            lst = index[k];
                        lst.Add(i);
                    }
                }
            }
            lock (indexedData)
            {
                foreach (Item i in indexedData)
                    i.Dispose();
                indexedData.Clear();
                this.percentageUnknown = (double)unknown / (double)count;
                foreach (KeyValuePair<string, List<Item>> kv in index)
                {
                    Item i = ItemFactory.Instance.Create(new IndexingSource(kv.Key, kv.Value, indexType));
                    indexedData.Add(i);
                }
                
            }
        }   
    }

}
