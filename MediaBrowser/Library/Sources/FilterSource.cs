using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Library.Sources
{
    public delegate bool FilterMatch<FilterType>(Item itm, FilterType filter);
    public delegate string FilterName<FilterType>(FilterType filter);

    public class FilterSource<FilterType> : ItemSource
    {
        string name;
        List<Item> contents;
        FilterType filter;
        FilterMatch<FilterType> matchFunc;
        ItemType resultType;
        FilterName<FilterType> nameFunc;
        UniqueName uniqueName;

        public FilterSource( List<Item> contents, FilterType filter, FilterMatch<FilterType> matchFunc,ItemType resultType, FilterName<FilterType> nameFunc)
        {
            this.contents = contents;
            this.filter = filter;
            this.matchFunc = matchFunc;
            this.resultType = resultType;
            this.nameFunc = nameFunc;
            this.name = nameFunc(filter);
            this.uniqueName = UniqueName.Fetch("IS:" + this.resultType.ToString() + ":" + name, true); // share the unique id's with the indexing source
        }
        
        public override bool IsPlayable
        {
            get { return false; }
        }

        public override UniqueName UniqueName
        {
            get { return uniqueName; }
        }

        public override IEnumerable<ItemSource> ChildSources
        {
            get
            {
                
                foreach(Item item in contents)
                {
                    if (matchFunc(item, filter))
                        yield return new ExistingItemSource(item);
                }
            }
        }

        public override string RawName
        {
            get { return this.name; }
        }

        public override DateTime CreatedDate
        {
            get { return DateTime.Today; }
        }


        public override Item ConstructItem()
        {
            return ItemFactory.Instance.Create(this);
        }

        internal override PlayableItem PlayableItem
        {
            get { return null; }
        }

        public override ItemType ItemType
        {
            get { return this.resultType; }
        }

        public override string Location
        {
            get { return null; }
        }

        protected override void WriteStream(System.IO.BinaryWriter bw)
        {
            throw new NotImplementedException();
        }

        protected override void ReadStream(System.IO.BinaryReader br)
        {
            throw new NotImplementedException();
        }
    }
}
