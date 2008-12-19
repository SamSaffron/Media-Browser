using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MediaBrowser.Library
{
    public class ImageSource
    {
        public string OriginalSource { get; set; }
        public string LocalSource { get; set; }

        public void WriteToStream(BinaryWriter bw)
        {
            bw.SafeWriteString(this.OriginalSource);
            bw.SafeWriteString(this.LocalSource);
        }

        public static ImageSource ReadFromStream(BinaryReader br)
        {
            ImageSource i = new ImageSource();
            i.OriginalSource = br.SafeReadString();
            i.LocalSource = br.SafeReadString();
            return i;
        }
    }
}
