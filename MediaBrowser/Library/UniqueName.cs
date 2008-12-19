using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Library
{
    public class UniqueName
    {
        private readonly string value;
        public UniqueName(string val)
        {
            value = val;
        }

        public static UniqueName Fetch( string name, bool allowCreate)
        {
            return ItemCache.Instance.GetUniqueName(name, allowCreate);
        }

        public string Value { get { return this.value; } }

        public override int GetHashCode()
        {
            return this.value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is UniqueName)
                return this.value.Equals(((UniqueName)obj).value);
            else
                return false;

        }
    }
}
