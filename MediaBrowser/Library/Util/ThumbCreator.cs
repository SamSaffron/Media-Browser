using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using MediaBrowser.Library.Interop.DirectShowLib;

namespace MediaBrowser.Util.VideoProcessing
{
    class ThumbCreator {

        static readonly Guid videoType = new
                  System.Guid("73646976-0000-0010-8000-00AA00389B71");

        public static bool CreateThumb(string videoFilename, string thumbFilename, double positionPercent)
        {
            Application.Logger.ReportInfo("Creating thumb for " + videoFilename);
            bool rval = false;
            IMediaDet m = new MediaDet() as IMediaDet;
            m.put_Filename(videoFilename);

            int streamCount;
            m.get_OutputStreams(out streamCount);

            AMMediaType media_type = new AMMediaType();

            for (int i = 0; i < streamCount; i++)
            {
                m.get_StreamMediaType(media_type);

                VideoInfoHeader vih = (VideoInfoHeader)Marshal.PtrToStructure(media_type.formatPtr, typeof(VideoInfoHeader));

                if (vih == null)
                {
                    continue;
                }

                double pos;
                m.get_StreamLength(out pos);
                pos = (int)(pos * positionPercent);

                int width = vih.BmiHeader.Width;
                int height = vih.BmiHeader.Height;

                if (height < 10 || width < 10)
                {
                    continue;
                }

                string tempfile = Path.GetTempFileName() + ".bmp";

                m.WriteBitmapBits(pos, width, height, tempfile);

                if (File.Exists(tempfile))
                {
                    using (var bitmap = new Bitmap(tempfile))
                    {
                        bitmap.Save(thumbFilename, ImageFormat.Png);
                    }

                    File.Delete(tempfile);
                    rval = true;
                }

                break;
            }

            Marshal.ReleaseComObject(m);
            return rval;

        }
    }
}
