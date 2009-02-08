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
                        /*
                        switch (this.SortBy)
                        {
                            case SortOrder.Unwatched:
                                sortedAndIndexedData.Sort(new ItemComparer(this.SortBy));
                                break;
                            default:
                                sortedAndIndexedData.Sort(new ItemNameComparer()); // simple non-metadata based name sorting when indexed
                                break;
                        }      */                      
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

    internal class ItemNameComparer : IComparer<Item>
    {
        public ItemNameComparer()
        {
           
        }

        #region IComparer<Item> Members

        public int Compare(Item x, Item y)
        {
            if (x.Source.Name == null)
                if (y.Source.Name == null)
                    return 0;
                else
                    return 1;
            if (Config.Instance.EnableAlphanumericSorting)
                return ItemComparer.AlphaNumericCompare(x.Source.Name, y.Source.Name);
            else
                return x.Source.Name.CompareTo(y.Source.Name);
        }
        #endregion
    }

    internal class ItemComparer : IComparer<Item>
    {
        private SortOrder order;
        public ItemComparer(SortOrder order)
        {
            this.order = order;
        }

        #region IComparer<Item> Members

        public int Compare(Item x, Item y)
        {
            int compare; 

            switch (this.order)
            {
                case SortOrder.Name:

                    if (NullCompare(x.Metadata.Name, y.Metadata.Name, out compare))
                        return compare;
                    if (Config.Instance.EnableAlphanumericSorting)
                        return AlphaNumericCompare(x.Metadata.SortableName, y.Metadata.SortableName);
                    else
                        return x.Metadata.SortableName.CompareTo(y.Metadata.SortableName);
                case SortOrder.NameOnDisk:
                    string xn = Path.GetFileNameWithoutExtension(x.Source.Location);
                    string yn = Path.GetFileNameWithoutExtension(y.Source.Location);
                    if (Config.Instance.EnableAlphanumericSorting)
                        return AlphaNumericCompare(xn, yn);
                    else
                        return xn.CompareTo(yn);
                case SortOrder.Date:
                    return -x.Source.CreatedDate.CompareTo(y.Source.CreatedDate);
                
                case SortOrder.Year:
                    if (NullCompare(x.Metadata.ProductionYear, y.Metadata.ProductionYear, out compare))
                        return compare;
                    return -x.Metadata.ProductionYear.Value.CompareTo(y.Metadata.ProductionYear.Value);

                case SortOrder.Rating:
                    if (NullCompare(x.Metadata.ImdbRating, y.Metadata.ImdbRating, out compare))
                        return compare;
                    return -x.Metadata.ImdbRating.Value.CompareTo(y.Metadata.ImdbRating.Value);
                    
                case SortOrder.Runtime:
                    if (NullCompare(x.Metadata.RunningTime,y.Metadata.RunningTime, out compare))
                        return compare;
                    return x.Metadata.RunningTime.Value.CompareTo(y.Metadata.RunningTime.Value);
                case SortOrder.Unwatched:
                    int i = -x.UnwatchedCount.CompareTo(y.UnwatchedCount);
                    if (i != 0)
                        return i;
                    else
                    {
                        if (NullCompare(x.Metadata.Name, y.Metadata.Name, out compare))
                            return compare;
                        if (Config.Instance.EnableAlphanumericSorting)
                            return AlphaNumericCompare(x.Metadata.SortableName, y.Metadata.SortableName);
                        else
                            return x.Metadata.SortableName.CompareTo(y.Metadata.SortableName);
                    }
                default:
                    return 0;
            }
        }

        #endregion

        private static bool NullCompare(object o1, object o2, out int compare)
        {
            compare = 0;
            if (o1 == null || o2 == null)
            {
                if (o1 == null && o2 == null)
                {
                    compare = 0;
                }
                else if (o1 == null)
                {
                    compare = -1; 
                }
                else if (o2 == null)
                {
                    compare = 1;
                }
                return true; 
            }
            return false;
        }

        public static int AlphaNumericCompare(string s1, string s2)
        {
            // http://dotnetperls.com/Content/Alphanumeric-Sorting.aspx

					int len1 = s1.Length;
					int len2 = s2.Length;
					int marker1 = 0;
					int marker2 = 0;

					// Walk through two the strings with two markers.
					while (marker1 < len1 && marker2 < len2)
					{
						char ch1 = s1[marker1];
						char ch2 = s2[marker2];

						// Some buffers we can build up characters in for each chunk.
						char[] space1 = new char[len1];
						int loc1 = 0;
						char[] space2 = new char[len2];
						int loc2 = 0;

						// Walk through all following characters that are digits or
						// characters in BOTH strings starting at the appropriate marker.
						// Collect char arrays.
						do
						{
							space1[loc1++] = ch1;
							marker1++;

							if (marker1 < len1)
							{
								ch1 = s1[marker1];
							}
							else
							{
								break;
							}
						} while (char.IsDigit(ch1) == char.IsDigit(space1[0]));

						do
						{
							space2[loc2++] = ch2;
							marker2++;

							if (marker2 < len2)
							{
								ch2 = s2[marker2];
							}
							else
							{
								break;
							}
						} while (char.IsDigit(ch2) == char.IsDigit(space2[0]));

						// If we have collected numbers, compare them numerically.
						// Otherwise, if we have strings, compare them alphabetically.
						string str1 = new string(space1);
						string str2 = new string(space2);

						int result;

						if (char.IsDigit(space1[0]) && char.IsDigit(space2[0]))
						{
							int thisNumericChunk = int.Parse(str1);
							int thatNumericChunk = int.Parse(str2);
							result = thisNumericChunk.CompareTo(thatNumericChunk);
						}
						else
						{
							result = str1.CompareTo(str2);
						}

						if (result != 0)
						{
							return result;
						}
					}
					return len1 - len2;
        }
    }
}
