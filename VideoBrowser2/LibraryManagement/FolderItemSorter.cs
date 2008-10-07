using System;
using System.Collections.Generic;
using System.Text;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    class FolderItemSorter : IComparer<IFolderItem>
    {
        public FolderItemSorter(SortOrderEnum sortOrderEnum)
        {
            this.sortOrderEnum = sortOrderEnum;
        }

        SortOrderEnum sortOrderEnum;

        #region IComparer<IFolderItem> Members

		public int Compare(IFolderItem x, IFolderItem y)
		{
			if (x is SpecialFolderItem && !(y is SpecialFolderItem))
			{
				return -1;
			}

			if (!(x is SpecialFolderItem) && y is SpecialFolderItem)
			{
				return 1;
			}

			// TODO: I don't like this logic being here. Shouldn't the stuff that calls this call different "Sorters" instead
			// of checking for the "sortOrderEnum"?

			if (sortOrderEnum == SortOrderEnum.Name)
			{
				if (x.IsFolder && !(y.IsFolder))
				{
					return -1;
				}

				if (!(x.IsFolder) && y.IsFolder)
				{
					return 1;
				}

				string s1 = x.Description.ToLower();
				if (s1 == null)
				{
					return 0;
				}
				string s2 = y.Description.ToLower();
				if (s2 == null)
				{
					return 0;
				}

				if (Config.Instance.EnableAlphanumericSorting)
				{

					// Sanitize titles before comparing
					foreach (string search in Config.Instance.SortRemoveCharactersArray)
					{
						s1 = s1.Replace(search.ToLower(), string.Empty);
						s2 = s2.Replace(search.ToLower(), string.Empty);
					}
					foreach (string search in Config.Instance.SortReplaceCharactersArray)
					{
						s1 = s1.Replace(search.ToLower(), " ");
						s2 = s2.Replace(search.ToLower(), " ");
					}
					foreach (string search in Config.Instance.SortReplaceWordsArray)
					{
						// Remove items but only if they are followed by a space
						// Then add the removed space back in
						s1 = s1.Replace(search.ToLower() + " ", string.Empty + " ");
						s2 = s2.Replace(search.ToLower() + " ", string.Empty + " ");
					}
					s1 = s1.Trim();
					s2 = s2.Trim();

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
				else
				{
					return x.Description.CompareTo(y.Description);
				}
			}
			else if (sortOrderEnum == SortOrderEnum.Date)
			{
				// reverse order for dates
				return y.CreatedDate.CompareTo(x.CreatedDate);
			}
			else if (sortOrderEnum == SortOrderEnum.RunTime)
			{
				int xval = x.RunningTime;
				if (xval <= 0) xval = 999999;
				int yval = y.RunningTime;
				if (yval <= 0) yval = 999999;
				return xval.CompareTo(yval);
			}
			else if (sortOrderEnum == SortOrderEnum.ProductionYear)
			{
				// reverse order
				if (x.Description == "Unknown")
				{
					return 1;
				}
				if (y.Description == "Unknown")
				{
					return -1;
				}
				return y.Description.CompareTo(x.Description);
			}
			else if (sortOrderEnum == SortOrderEnum.Actor)
			{
				// a little hacky, actors with only 1 movie show up in the bottom 
				if (x.Title2.StartsWith("1") && !y.Title2.StartsWith("1"))
				{
					return 1;
				}
				if (!x.Title2.StartsWith("1") && y.Title2.StartsWith("1"))
				{
					return -1;
				}
			}

			// default is by description asc
			return x.Description.CompareTo(y.Description);

		}

        #endregion
    }
}
