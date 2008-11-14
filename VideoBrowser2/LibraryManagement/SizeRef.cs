using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    public class SizeRef : ModelItem
    {
        public SizeRef()
        {
        }

        public SizeRef(Size s)
        {
            this.val = s;
        }
        private Size val;
        public Size Value
        {
            get { return this.val; }
            set
            {
                if (this.val != value)
                {
                    this.val = value;
                    FirePropertyChanged("Value");
                }
            }
        }
    }
}
