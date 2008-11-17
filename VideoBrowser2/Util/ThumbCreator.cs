using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SamSoft.VideoBrowser.Util.VideoProcessing
{
    class ThumbCreator
    {

        #region interop (from the direct show lib project)

        [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
   Guid("0579154A-2B53-4994-B0D0-E773148EFF85"),
   InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ISampleGrabberCB
        {
            /// <summary>
            /// When called, callee must release pSample
            /// </summary>
            [PreserveSig]
            int SampleCB(double SampleTime, IMediaSample pSample);

            [PreserveSig]
            int BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen);
        }

        [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("56a8689a-0ad4-11ce-b03a-0020af0ba770"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IMediaSample
        {
            [PreserveSig]
            int GetPointer([Out] out IntPtr ppBuffer); // BYTE **

            [PreserveSig]
            int GetSize();

            [PreserveSig]
            int GetTime(
                [Out] out long pTimeStart,
                [Out] out long pTimeEnd
                );

            [PreserveSig]
            int SetTime(
                [In, MarshalAs(UnmanagedType.LPStruct)] DsLong pTimeStart,
                [In, MarshalAs(UnmanagedType.LPStruct)] DsLong pTimeEnd
                );

            [PreserveSig]
            int IsSyncPoint();

            [PreserveSig]
            int SetSyncPoint([In, MarshalAs(UnmanagedType.Bool)] bool bIsSyncPoint);

            [PreserveSig]
            int IsPreroll();

            [PreserveSig]
            int SetPreroll([In, MarshalAs(UnmanagedType.Bool)] bool bIsPreroll);

            [PreserveSig]
            int GetActualDataLength();

            [PreserveSig]
            int SetActualDataLength([In] int len);

            /// <summary>
            /// Returned object must be released with DsUtils.FreeAMMediaType()
            /// </summary>
            [PreserveSig]
            int GetMediaType([Out, MarshalAs(UnmanagedType.LPStruct)] out AMMediaType ppMediaType);

            [PreserveSig]
            int SetMediaType([In, MarshalAs(UnmanagedType.LPStruct)] AMMediaType pMediaType);

            [PreserveSig]
            int IsDiscontinuity();

            [PreserveSig]
            int SetDiscontinuity([In, MarshalAs(UnmanagedType.Bool)] bool bDiscontinuity);

            [PreserveSig]
            int GetMediaTime(
                [Out] out long pTimeStart,
                [Out] out long pTimeEnd
                );

            [PreserveSig]
            int SetMediaTime(
                [In, MarshalAs(UnmanagedType.LPStruct)] DsLong pTimeStart,
                [In, MarshalAs(UnmanagedType.LPStruct)] DsLong pTimeEnd
                );
        }

        [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("6B652FFF-11FE-4fce-92AD-0266B5D7C78F"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ISampleGrabber
        {
            [PreserveSig]
            int SetOneShot(
                [In, MarshalAs(UnmanagedType.Bool)] bool OneShot);

            [PreserveSig]
            int SetMediaType(
                [In, MarshalAs(UnmanagedType.LPStruct)] AMMediaType pmt);

            [PreserveSig]
            int GetConnectedMediaType(
                [Out, MarshalAs(UnmanagedType.LPStruct)] AMMediaType pmt);

            [PreserveSig]
            int SetBufferSamples(
                [In, MarshalAs(UnmanagedType.Bool)] bool BufferThem);

            [PreserveSig]
            int GetCurrentBuffer(ref int pBufferSize, IntPtr pBuffer);

            [PreserveSig]
            int GetCurrentSample(out IMediaSample ppSample);

            [PreserveSig]
            int SetCallback(ISampleGrabberCB pCallback, int WhichMethodToCallback);
        }

        [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    Guid("65BD0710-24D2-4ff7-9324-ED2E5D3ABAFA")]
        public interface IMediaDet
        {
            [PreserveSig]
            int get_Filter(
                [MarshalAs(UnmanagedType.IUnknown)] out object pVal
                );

            [PreserveSig]
            int put_Filter(
                [MarshalAs(UnmanagedType.IUnknown)] object newVal
                );

            [PreserveSig]
            int get_OutputStreams(
                out int pVal
                );

            [PreserveSig]
            int get_CurrentStream(
                out int pVal
                );

            [PreserveSig]
            int put_CurrentStream(
                int newVal
                );

            [PreserveSig]
            int get_StreamType(
                out Guid pVal
                );

            [PreserveSig]
            int get_StreamTypeB(
                [MarshalAs(UnmanagedType.BStr)] out string pVal
                );

            [PreserveSig]
            int get_StreamLength(
                out double pVal
                );

            [PreserveSig]
            int get_Filename(
                [MarshalAs(UnmanagedType.BStr)] out string pVal
                );

            [PreserveSig]
            int put_Filename(
                [MarshalAs(UnmanagedType.BStr)] string newVal
                );

            [PreserveSig]
            int GetBitmapBits(
                double StreamTime,
                out int pBufferSize,
                [In] IntPtr pBuffer,
                int Width,
                int Height
                );

            [PreserveSig]
            int WriteBitmapBits(
                double StreamTime,
                int Width,
                int Height,
                [In, MarshalAs(UnmanagedType.BStr)] string Filename);

            [PreserveSig]
            int get_StreamMediaType(
                [Out, MarshalAs(UnmanagedType.LPStruct)] AMMediaType pVal);

            [PreserveSig]
            int GetSampleGrabber(
                out ISampleGrabber ppVal);

            [PreserveSig]
            int get_FrameRate(
                out double pVal);

            [PreserveSig]
            int EnterBitmapGrabMode(
                double SeekTime);
        }

        /// <summary>
        /// From AM_MEDIA_TYPE - When you are done with an instance of this class,
        /// it should be released with FreeAMMediaType() to avoid leaking
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public class AMMediaType
        {
            public Guid majorType;
            public Guid subType;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fixedSizeSamples;
            [MarshalAs(UnmanagedType.Bool)]
            public bool temporalCompression;
            public int sampleSize;
            public Guid formatType;
            public IntPtr unkPtr; // IUnknown Pointer
            public int formatSize;
            public IntPtr formatPtr; // Pointer to a buff determined by formatType
        }

        [StructLayout(LayoutKind.Sequential)]
        public class DsRect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            /// <summary>
            /// Empty contructor. Initialize all fields to 0
            /// </summary>
            public DsRect()
            {
                this.left = 0;
                this.top = 0;
                this.right = 0;
                this.bottom = 0;
            }

            /// <summary>
            /// A parametred constructor. Initialize fields with given values.
            /// </summary>
            /// <param name="left">the left value</param>
            /// <param name="top">the top value</param>
            /// <param name="right">the right value</param>
            /// <param name="bottom">the bottom value</param>
            public DsRect(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            /// <summary>
            /// A parametred constructor. Initialize fields with a given <see cref="System.Drawing.Rectangle"/>.
            /// </summary>
            /// <param name="rectangle">A <see cref="System.Drawing.Rectangle"/></param>
            /// <remarks>
            /// Warning, DsRect define a rectangle by defining two of his corners and <see cref="System.Drawing.Rectangle"/> define a rectangle with his upper/left corner, his width and his height.
            /// </remarks>
            public DsRect(Rectangle rectangle)
            {
                this.left = rectangle.Left;
                this.top = rectangle.Top;
                this.right = rectangle.Right;
                this.bottom = rectangle.Bottom;
            }

            /// <summary>
            /// Provide de string representation of this DsRect instance
            /// </summary>
            /// <returns>A string formated like this : [left, top - right, bottom]</returns>
            public override string ToString()
            {
                return string.Format("[{0}, {1} - {2}, {3}]", this.left, this.top, this.right, this.bottom);
            }

            public override int GetHashCode()
            {
                return this.left.GetHashCode() |
                    this.top.GetHashCode() |
                    this.right.GetHashCode() |
                    this.bottom.GetHashCode();
            }

            /// <summary>
            /// Define implicit cast between DirectShowLib.DsRect and System.Drawing.Rectangle for languages supporting this feature.
            /// VB.Net doesn't support implicit cast. <see cref="DirectShowLib.DsRect.ToRectangle"/> for similar functionality.
            /// <code>
            ///   // Define a new Rectangle instance
            ///   Rectangle r = new Rectangle(0, 0, 100, 100);
            ///   // Do implicit cast between Rectangle and DsRect
            ///   DsRect dsR = r;
            ///
            ///   Console.WriteLine(dsR.ToString());
            /// </code>
            /// </summary>
            /// <param name="r">a DsRect to be cast</param>
            /// <returns>A casted System.Drawing.Rectangle</returns>
            public static implicit operator Rectangle(DsRect r)
            {
                return r.ToRectangle();
            }

            /// <summary>
            /// Define implicit cast between System.Drawing.Rectangle and DirectShowLib.DsRect for languages supporting this feature.
            /// VB.Net doesn't support implicit cast. <see cref="DirectShowLib.DsRect.FromRectangle"/> for similar functionality.
            /// <code>
            ///   // Define a new DsRect instance
            ///   DsRect dsR = new DsRect(0, 0, 100, 100);
            ///   // Do implicit cast between DsRect and Rectangle
            ///   Rectangle r = dsR;
            ///
            ///   Console.WriteLine(r.ToString());
            /// </code>
            /// </summary>
            /// <param name="r">A System.Drawing.Rectangle to be cast</param>
            /// <returns>A casted DsRect</returns>
            public static implicit operator DsRect(Rectangle r)
            {
                return new DsRect(r);
            }

            /// <summary>
            /// Get the System.Drawing.Rectangle equivalent to this DirectShowLib.DsRect instance.
            /// </summary>
            /// <returns>A System.Drawing.Rectangle</returns>
            public Rectangle ToRectangle()
            {
                return new Rectangle(this.left, this.top, (this.right - this.left), (this.bottom - this.top));
            }

            /// <summary>
            /// Get a new DirectShowLib.DsRect instance for a given <see cref="System.Drawing.Rectangle"/>
            /// </summary>
            /// <param name="r">The <see cref="System.Drawing.Rectangle"/> used to initialize this new DirectShowLib.DsGuid</param>
            /// <returns>A new instance of DirectShowLib.DsGuid</returns>
            public static DsRect FromRectangle(Rectangle r)
            {
                return new DsRect(r);
            }
        }

        /// <summary>
        /// From BITMAPINFOHEADER
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public class BitmapInfoHeader
        {
            public int Size;
            public int Width;
            public int Height;
            public short Planes;
            public short BitCount;
            public int Compression;
            public int ImageSize;
            public int XPelsPerMeter;
            public int YPelsPerMeter;
            public int ClrUsed;
            public int ClrImportant;
        }

        /// <summary>
        /// From VIDEOINFOHEADER
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public class VideoInfoHeader
        {
            public DsRect SrcRect;
            public DsRect TargetRect;
            public int BitRate;
            public int BitErrorRate;
            public long AvgTimePerFrame;
            public BitmapInfoHeader BmiHeader;
        }

        /// <summary>
        /// DirectShowLib.DsLong is a wrapper class around a <see cref="System.Int64"/> value type.
        /// </summary>
        /// <remarks>
        /// This class is necessary to enable null paramters passing.
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        public class DsLong
        {
            private long Value;

            /// <summary>
            /// Constructor
            /// Initialize a new instance of DirectShowLib.DsLong with the Value parameter
            /// </summary>
            /// <param name="Value">Value to assign to this new instance</param>
            public DsLong(long Value)
            {
                this.Value = Value;
            }

            /// <summary>
            /// Get a string representation of this DirectShowLib.DsLong Instance.
            /// </summary>
            /// <returns>A string representing this instance</returns>
            public override string ToString()
            {
                return this.Value.ToString();
            }

            public override int GetHashCode()
            {
                return this.Value.GetHashCode();
            }

            /// <summary>
            /// Define implicit cast between DirectShowLib.DsLong and System.Int64 for languages supporting this feature.
            /// VB.Net doesn't support implicit cast. <see cref="DirectShowLib.DsLong.ToInt64"/> for similar functionality.
            /// <code>
            ///   // Define a new DsLong instance
            ///   DsLong dsL = new DsLong(9876543210);
            ///   // Do implicit cast between DsLong and Int64
            ///   long l = dsL;
            ///
            ///   Console.WriteLine(l.ToString());
            /// </code>
            /// </summary>
            /// <param name="g">DirectShowLib.DsLong to be cast</param>
            /// <returns>A casted System.Int64</returns>
            public static implicit operator long(DsLong l)
            {
                return l.Value;
            }

            /// <summary>
            /// Define implicit cast between System.Int64 and DirectShowLib.DsLong for languages supporting this feature.
            /// VB.Net doesn't support implicit cast. <see cref="DirectShowLib.DsGuid.FromInt64"/> for similar functionality.
            /// <code>
            ///   // Define a new Int64 instance
            ///   long l = 9876543210;
            ///   // Do implicit cast between Int64 and DsLong
            ///   DsLong dsl = l;
            ///
            ///   Console.WriteLine(dsl.ToString());
            /// </code>
            /// </summary>
            /// <param name="g">System.Int64 to be cast</param>
            /// <returns>A casted DirectShowLib.DsLong</returns>
            public static implicit operator DsLong(long l)
            {
                return new DsLong(l);
            }

            /// <summary>
            /// Get the System.Int64 equivalent to this DirectShowLib.DsLong instance.
            /// </summary>
            /// <returns>A System.Int64</returns>
            public long ToInt64()
            {
                return this.Value;
            }

            /// <summary>
            /// Get a new DirectShowLib.DsLong instance for a given System.Int64
            /// </summary>
            /// <param name="g">The System.Int64 to wrap into a DirectShowLib.DsLong</param>
            /// <returns>A new instance of DirectShowLib.DsLong</returns>
            public static DsLong FromInt64(long l)
            {
                return new DsLong(l);
            }
        }

        /// <summary>
        /// From CLSID_MediaDet
        /// </summary>
        [ComImport, Guid("65BD0711-24D2-4ff7-9324-ED2E5D3ABAFA")]
        public class MediaDet
        {
        }

        #endregion

        static Guid videoType = new
                  System.Guid("73646976-0000-0010-8000-00AA00389B71");

        public static bool CreateThumb(string videoFilename, string thumbFilename, double positionPercent)
        {
            bool rval = false;
            IMediaDet m = new MediaDet() as IMediaDet;
            m.put_Filename(videoFilename);

            int streamCount;
            m.get_OutputStreams(out streamCount);

            AMMediaType media_type = new AMMediaType();

            for (int i = 0; i < streamCount; i++)
            {
                m.get_StreamMediaType(media_type);

                /*
                 * TODO: Its proing really hard to find out what the stream is major type doesn't work sometimes
                 * Cause I only seem to get audio streams for some videos, current approach is to assume we have 
                 * a video stream and then just stop if the width is 0 
                if (media_type.majorType != videoType)
                {
                    continue;
                }
                 */

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
