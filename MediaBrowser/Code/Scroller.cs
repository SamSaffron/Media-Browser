using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;

namespace MediaBrowser {
    public class Scroller : ModelItem {
        public Scroller() {
        }

        public Size ComputeSize(Size size, Single currentPage, Single totalPages) {
            int height = size.Height;
            Single temp = currentPage / (totalPages);
            if (temp > 1)
                temp = 1;
            if (temp < 0)
                temp = 0;
            size.Height = (int)((height * temp) - 1);
            return size;
        }

    }
}
