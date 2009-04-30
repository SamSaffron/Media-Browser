using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library.Providers {

    public abstract class BaseMetadataProvider : IMetadataProvider {

        static MetadataProviderFactory factory;

        public BaseItem Item {
            get; set;
        }

        public abstract void Fetch();
        public abstract bool NeedsRefresh();
        
        public virtual bool IsSlow {
            get {
                SetFactory();
                return factory.Slow;
            }
        }

        public virtual bool RequiresInternet { 
            get {
                SetFactory();
                return factory.RequiresInternet;
            }
        }

        void SetFactory() {
            if (factory == null) { 
                factory = new MetadataProviderFactory(this.GetType());
            }
        }
    }
}
