using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    public class SortOrderNames
    {
        private static readonly string[] Names = { "name", "date", "genre", "runtime", "year", "actor", "director" };

        public static string GetName(SortOrderEnum order)
        {
            return Names[(int)order];
        }

        public static SortOrderEnum GetEnum(string name)
        {
            return (SortOrderEnum)Array.IndexOf<string>(Names, name);
        }
    }

    public class StringRef : ModelItem
    {
        private string val;
        public string Value
        {
            get { return this.val; }
            set
            {
                this.val = value;
                FirePropertyChanged("Value");
            }
        }
    }

    public enum SortOrderEnum : int
	{
	    Name = 0,
        Date = 1, 
        Genre = 2, 
        RunTime = 3,
        ProductionYear = 4,
        Actor = 5, 
        Director = 6
	} 
  
}
