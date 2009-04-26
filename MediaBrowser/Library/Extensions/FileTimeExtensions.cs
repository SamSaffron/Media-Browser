using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.Extensions {
    static class FileTimeExtensions {
        public static DateTime ToDateTime(this System.Runtime.InteropServices.ComTypes.FILETIME filetime) {
            long highBits = filetime.dwHighDateTime;
            highBits = highBits << 32;
            return DateTime.FromFileTimeUtc(highBits + (long)filetime.dwLowDateTime);
        }
    }
}
