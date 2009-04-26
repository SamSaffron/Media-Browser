using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library.Providers {

    public abstract class BaseMetadataProvider : IMetadataProvider {

        public BaseItem Item {
            get; set;
        }

        public abstract void Fetch();

        public abstract bool NeedsRefresh();
        
    }
}
