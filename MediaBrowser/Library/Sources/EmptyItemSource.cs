using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Library.Sources
{
    public class EmptyItemSource : ItemSource
    {
        
        public override bool IsPlayable
        {
            get { return false; }
        }

        public override UniqueName UniqueName
        {
            get { return null; }
        }

        public override IEnumerable<ItemSource> ChildSources
        {
            get { yield break; } 
        }

        public override string RawName
        {
            get { return ""; }
        }

        public override DateTime CreatedDate
        {
            get { return DateTime.Today; }
        }

        public override Item ConstructItem()
        {
            MediaMetadata md = MediaMetadataFactory.Instance.Create((UniqueName)null, this.ItemType);
            return ItemFactory.Instance.Create(this, md);
        }

        internal override PlayableItem PlayableItem
        {
            get { return null; }
        }

        public override ItemType ItemType
        {
            get { return ItemType.Other;  }
        }

        public override string Location
        {
            get { return null; }
        }

        protected override void WriteStream(System.IO.BinaryWriter bw)
        {
           
        }

        protected override void ReadStream(System.IO.BinaryReader br)
        {
            
        }
    }
}
