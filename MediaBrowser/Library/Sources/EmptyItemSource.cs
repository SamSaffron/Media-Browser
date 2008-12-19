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
            // while this looks very odd it gives the desired behaviour. 
            // A simple "return null;" results in a null object which breaks foreach loops, 
            // this returns a valid enumerator hat is already at the end, meaning foreach loops
            // don't exception

            // This ensures we don't need any special handling of specific source types
            get { if (false) yield return null; } 
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
