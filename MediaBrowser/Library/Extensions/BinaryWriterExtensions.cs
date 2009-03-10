using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace System.IO
{
    public static class BinaryWriterExtensions
    {
        public static void SafeWriteString(this BinaryWriter bw, string val)
        {
            bw.Write(val != null);
            if (val != null)
                bw.Write(val);
        }
    }

}
