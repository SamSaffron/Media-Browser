// From the directshow.net project http://directshownet.sourceforge.net/)

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace MediaBrowser.Library.Interop.DirectShowLib {
    /// <summary>
    /// From VIDEOINFOHEADER
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class VideoInfoHeader {
        public DsRect SrcRect;
        public DsRect TargetRect;
        public int BitRate;
        public int BitErrorRate;
        public long AvgTimePerFrame;
        public BitmapInfoHeader BmiHeader;
    }
}
