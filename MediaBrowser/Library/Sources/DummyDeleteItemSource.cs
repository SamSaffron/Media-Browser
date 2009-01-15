using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Library.Sources
{
    class DummyDeleteItemSource : ItemSource
    {
        UniqueName name;
        public DummyDeleteItemSource(UniqueName name)
        {
            this.name = name;
        }
        public override bool IsPlayable
        {
            get { throw new NotImplementedException(); }
        }

        public override UniqueName UniqueName
        {
            get { return name; }
        }

        public override IEnumerable<ItemSource> ChildSources
        {
            get { throw new NotImplementedException(); }
        }

        public override string RawName
        {
            get { throw new NotImplementedException(); }
        }

        public override string Location
        {
            get { throw new NotImplementedException(); }
        }

        public override DateTime CreatedDate
        {
            get { throw new NotImplementedException(); }
        }

        internal override PlayableItem PlayableItem
        {
            get { throw new NotImplementedException(); }
        }

        public override ItemType ItemType
        {
            get { throw new NotImplementedException(); }
        }

        public override Item ConstructItem()
        {
            throw new NotImplementedException();
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
