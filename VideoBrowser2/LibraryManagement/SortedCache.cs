using System;
using System.Collections.Generic;
using System.Text;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    class SortedCache<T> 
    {
        int maxSize;
        IComparer<T> comparer;
        List<T> contents = new List<T>();

        public SortedCache(int maxSize, IComparer<T> comparer)
        {
            this.maxSize = maxSize;
            this.comparer = comparer; 
        }

        public void Add(T item)
        {
            int index = contents.BinarySearch(item, comparer);

            if (index >= 0)
            {
                contents.Insert(index, item);
            }
            else
            {
                // BinarySearch return the inverse of the place it last looked at
                contents.Insert(~index, item);
            }


            // if larger than maxSize drop off the last one  
            if (contents.Count > maxSize)
            {
                contents.RemoveAt(contents.Count - 1); 
            } 

        } 

        public List<T> Contents
        {
            get
            {
                return contents; 
            }
        }
    }
}
