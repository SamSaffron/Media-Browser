using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Library.Sources
{
    public class IndexingSource : ItemSource
    {
        string name;
        List<Item> contents;
        ItemType type;
        private UniqueName uniqueName;

        public IndexingSource(string name, List<Item> contents, IndexType type)
        {
            this.name = name;
            this.contents = contents;
            switch (type)
            {
                case IndexType.Actor:
                    this.type = ItemType.Actor;
                    break;
                case IndexType.Director:
                    this.type = ItemType.Director;
                    break;
                case IndexType.Genre:
                    this.type = ItemType.Genre;
                    break;
                case IndexType.Year:
                    this.type = ItemType.Year;
                    break;
                default:
                    throw new NotSupportedException("IndexingSource does not understand IndexType: " + type.ToString());
            }
            this.uniqueName = UniqueName.Fetch("IS:" + this.type.ToString() + ":" + name, true);
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
                foreach (Item i in contents)
                {
                    yield return new ExistingItemSource(i);
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
            get { return this.type; }
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
