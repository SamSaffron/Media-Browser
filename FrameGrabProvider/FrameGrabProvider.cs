using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Providers;


namespace FrameGrabProvider
{
    // we mark this as slow, so we ensure it only runs at the end of the chain
    [SlowProvider]
    [SupportedType(typeof(Video))]
    class FrameGrabProvider : BaseMetadataProvider
    {

        public override void Fetch()
        {
            var video = Item as Video;

            string path = video.VideoFiles.First();
            if (!path.ToLower().StartsWith("http")) {
                Item.PrimaryImagePath = "grab://" + path;
            }
        }

        public override bool NeedsRefresh()
        {
            return (Item.PrimaryImagePath == null);
        }
    }
}
