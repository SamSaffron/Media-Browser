using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Library.Sources
{
    public class ExistingItemSource : ItemSource
    {
        private Item item;
        public ExistingItemSource(Item item)
        {
            this.item = item;
        }

        public override bool IsPlayable
        {
            get { return item.Source.IsPlayable; }
        }

        public override UniqueName UniqueName
        {
            get { return item.UniqueName;  }
        }

        public override IEnumerable<ItemSource> ChildSources
        {
            get { return item.Source.ChildSources; }
        }

        public override string RawName
        {
            get { return item.Source.RawName; }
        }

        public override DateTime CreatedDate
        {
            get { return item.Source.CreatedDate; }
        }


        public override Item ConstructItem()
        {
            return item;
        }

        internal override PlayableItem PlayableItem
        {
            get { return item.Source.PlayableItem; }
        }

        public override ItemType ItemType
        {
            get { return item.Source.ItemType;  }
        }

        public override string Location
        {
            get { return item.Source.Location; }
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
