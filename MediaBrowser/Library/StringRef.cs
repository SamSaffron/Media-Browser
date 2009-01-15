using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;

namespace MediaBrowser.Library
{
    public class StringRef : ModelItem
    {
        private string val;
        public string Value
        {
            get { return this.val; }
            set
            {
                this.val = value;
                FirePropertyChanged("Value");
            }
        }
    }
}
