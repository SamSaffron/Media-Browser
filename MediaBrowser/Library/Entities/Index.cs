using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 This is a rough index implementation, this name can be an actor or genre or year. 
   Its not persisted, but will call the metadata provider to extract things like pictures
 */

namespace MediaBrowser.Library.Entities {
    public class Index : Folder {

        // if we are not newable the serializer can not serialize us
        public Index() {
        }

        List<BaseItem> children;

        public Index(string name, List<BaseItem> children) {
            this.children = children;
            this.Name = name;
            this.Id = Guid.NewGuid();
        }

        protected override List<BaseItem> ActualChildren { get { return children; } }

        public override void ValidateChildren() {
            // do nothing
        }

        public override void EnsureChildrenLoaded() {
            // do nothing
        }

    }
}
