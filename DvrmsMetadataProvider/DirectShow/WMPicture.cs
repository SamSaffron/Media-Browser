using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace DvrmsMetadataProvider.DirectShow {

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct WMPicture {
        public IntPtr pwszMIMEType;
        public byte bPictureType;
        public IntPtr pwszDescription;
        [MarshalAs(UnmanagedType.U4)]
        public int dwDataLen;
        public IntPtr pbData;
    }
}
