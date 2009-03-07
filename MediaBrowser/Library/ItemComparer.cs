using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Library {
    internal class ItemComparer : IComparer<Item> {
        private SortOrder order;
        public ItemComparer(SortOrder order) {
            this.order = order;
        }

        #region IComparer<Item> Members

        public int Compare(Item x, Item y) {
            int compare;

            switch (this.order) {
                case SortOrder.Name:

                    if (NullCompare(x.Metadata.Name, y.Metadata.Name, out compare))
                        return compare;
                    if (Config.Instance.EnableAlphanumericSorting)
                        return AlphaNumericCompare(x.Metadata.SortableName, y.Metadata.SortableName);
                    else
                        return x.Metadata.SortableName.CompareTo(y.Metadata.SortableName);
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
                    if (NullCompare(x.Metadata.RunningTime, y.Metadata.RunningTime, out compare))
                        return compare;
                    return x.Metadata.RunningTime.Value.CompareTo(y.Metadata.RunningTime.Value);
                case SortOrder.Unwatched:
                    int i = -x.UnwatchedCount.CompareTo(y.UnwatchedCount);
                    if (i != 0)
                        return i;
                    else {
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

        private static bool NullCompare(object o1, object o2, out int compare) {
            compare = 0;
            if (o1 == null || o2 == null) {
                if (o1 == null && o2 == null) {
                    compare = 0;
                } else if (o1 == null) {
                    compare = -1;
                } else if (o2 == null) {
                    compare = 1;
                }
                return true;
            }
            return false;
        }

        public static int AlphaNumericCompare(string s1, string s2) {
            // http://dotnetperls.com/Content/Alphanumeric-Sorting.aspx

            int len1 = s1.Length;
            int len2 = s2.Length;
            int marker1 = 0;
            int marker2 = 0;

            // Walk through two the strings with two markers.
            while (marker1 < len1 && marker2 < len2) {
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
                do {
                    space1[loc1++] = ch1;
                    marker1++;

                    if (marker1 < len1) {
                        ch1 = s1[marker1];
                    } else {
                        break;
                    }
                } while (char.IsDigit(ch1) == char.IsDigit(space1[0]));

                do {
                    space2[loc2++] = ch2;
                    marker2++;

                    if (marker2 < len2) {
                        ch2 = s2[marker2];
                    } else {
                        break;
                    }
                } while (char.IsDigit(ch2) == char.IsDigit(space2[0]));

                // If we have collected numbers, compare them numerically.
                // Otherwise, if we have strings, compare them alphabetically.
                string str1 = new string(space1);
                string str2 = new string(space2);

                int result;

                if (char.IsDigit(space1[0]) && char.IsDigit(space2[0])) {
                    int thisNumericChunk = int.Parse(str1);
                    int thatNumericChunk = int.Parse(str2);
                    result = thisNumericChunk.CompareTo(thatNumericChunk);
                } else {
                    result = str1.CompareTo(str2);
                }

                if (result != 0) {
                    return result;
                }
            }
            return len1 - len2;
        }
    }
}
