using System;
using System.Collections.Generic;
using System.Text;


namespace MediaBrowser.Library.Sources
{
    public delegate bool FilterMatch<FilterType>(Item itm, FilterType filter);
    public delegate string FilterName<FilterType>(FilterType filter);
    public class IndexFilteringSource<FilterType> : ItemSource
    {
        string name;
        List<Item> contents;
        ItemType type;
        UniqueName uniqueName;
        List<FilterType> validEntires;
        FilterMatch<FilterType> matchFunc;
        ItemType resultType;
        FilterName<FilterType> nameFunc;

        public IndexFilteringSource(string name, List<Item> contents, ItemType resultType, List<FilterType> validEntires, FilterMatch<FilterType> matchFunc, FilterName<FilterType> nameFunc)
        {
            this.name = name;
            this.contents = contents;
            this.validEntires = validEntires;
            this.matchFunc = matchFunc;
            this.nameFunc = nameFunc;
            this.resultType = resultType;

            this.uniqueName = UniqueName.Fetch("IFS:" + name, true);
        }
        
        public override bool IsPlayable
        {
            get { return false; }
        }

        public override UniqueName UniqueName
        {
            get { return this.uniqueName; }
        }

        public override IEnumerable<ItemSource> ChildSources
        {
            get
            {
                foreach(FilterType filter in validEntires)
                {
                    yield return new FilterSource<FilterType>(contents, filter, matchFunc, resultType, nameFunc);
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
            throw new NotSupportedException("IndexFilteringSources are not designed to be constructed");
            /*
            MediaMetadata md = MediaMetadataFactory.Instance.Create((UniqueName)null, this.ItemType);
            md.Name = this.name;
            return ItemFactory.Instance.Create(this, md);
             */
        }

        internal override PlayableItem PlayableItem
        {
            get { return null; }
        }

        public override ItemType ItemType
        {
            get { return ItemType.None; }
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
