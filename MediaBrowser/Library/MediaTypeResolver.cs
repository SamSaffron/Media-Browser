using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MediaBrowser.Library {
    static class MediaTypeResolver {
        public static MediaType DetermineType(string path) {
            path = path.ToLower();
            if (path.Contains("video_ts"))
                return MediaType.DVD;
            if (path.EndsWith(".avi"))
                return MediaType.Avi;
            if (path.EndsWith(".mpg"))
                return MediaType.Mpg;
            if (path.EndsWith(".mkv"))
                return MediaType.Mkv;
            if (path.EndsWith(".mp4"))
                return MediaType.Mp4;
            if (path.EndsWith(".pls"))
                return MediaType.PlayList;
            if (path.EndsWith(".ts") || path.EndsWith(".m2ts"))
                return MediaType.TS;
            if (path.Contains("bdmv"))
                return MediaType.BluRay;
            if (path.Contains("hvdvd_ts"))
                return MediaType.HDDVD;
            if (Directory.Exists(Path.Combine(path, "VIDEO_TS")))
                return MediaType.DVD;
            if (Directory.Exists(Path.Combine(path, "BDMV")))
                return MediaType.BluRay;
            if (Directory.Exists(Path.Combine(path, "HVDVD_TS")))
                return MediaType.HDDVD;
            return MediaType.Unknown;
        }
    }
}
