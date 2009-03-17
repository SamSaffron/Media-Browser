using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.Xml;
using MediaBrowser.Library.Sources;
using System.Diagnostics;
using System.Collections;
using Microsoft.MediaCenter;
using MediaBrowser.Util;
using MediaBrowser.Library.Collections;
using System.Linq;
using MediaBrowser.Library.LinqExtensions;


namespace MediaBrowser.Library {

    public class Folder : Item {

        /*
        public override void NavigatingInto() {
            base.NavigatingInto();

            if (childLoadPending)
                childRetrievalProcessor.PullToFront(this);
            else
                this.EnsureChildrenLoaded(false);
            if (pendingVerify)
                childVerificationProcessor.PullToFront(this);
            else if (!Config.Instance.EnableFileWatching) // if we aren't doing file watching then verify the children when we navigate forwards into an item
                this.VerifyChildrenAsync();
        }
         */
    }
}