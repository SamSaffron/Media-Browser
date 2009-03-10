using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.Collections {
    class DerivedEqualityComparer<T> : IEqualityComparer<T> {
        Func<T, T, bool> comparer;
        Func<T, int> hash;

        public DerivedEqualityComparer(Func<T, T, bool> comparer, Func<T, int> hash) {
            this.comparer = comparer;
            this.hash = hash;
        }
        public bool Equals(T x, T y)
        {
            return comparer(x, y);
        }
        public int GetHashCode(T obj)
        {
            return hash(obj);
        }
    
    }
}
