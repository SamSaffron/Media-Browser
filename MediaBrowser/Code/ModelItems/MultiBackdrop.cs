using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using System.IO;
using System.Xml.XPath;
using Microsoft.MediaCenter.UI;
using System.Diagnostics;

using MediaBrowser.Library;

namespace MediaBrowser
{
    /// <summary>
    /// This provides information to the root page on-screen display. You have the option of adding
    /// one-time or recurring messages.
    /// </summary>
    public class MultiBackdrop : ModelItem
    {
        private const int cycleInterval = 8000;
        Item _item;
        Timer cycle;

        // Parameterless constructor for mcml
        public MultiBackdrop()
        {
        }

        public void BeginRotation(Item item)
        {
            this._item = item;
            cycle = new Timer(this);
            cycle.Interval = cycleInterval;
            cycle.Tick += delegate { OnRefresh(); };
            cycle.Enabled = true;
        }

        private void OnRefresh()
        {
            _item.GetNextBackDropImage();
        }

    }
}
