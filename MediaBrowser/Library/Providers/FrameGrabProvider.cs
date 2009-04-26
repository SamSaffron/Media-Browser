using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Providers.Attributes;

namespace MediaBrowser.Library.Providers
{
    [SupportedType(typeof(Video))]
    class FrameGrabProvider : BaseMetadataProvider
    {
        public override void Fetch()
        {
            // TODO : we need access to the video files
         //   Item.PrimaryImagePath =  "grab://" + Item.Path;
        }

        public override bool NeedsRefresh()
        {
            return false;
            //return (Item.PrimaryImagePath == null);
        }
    }
}
