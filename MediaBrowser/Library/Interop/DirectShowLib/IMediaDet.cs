// From the directshow.net project http://directshownet.sourceforge.net/)

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace MediaBrowser.Library.Interop.DirectShowLib {

    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    Guid("65BD0710-24D2-4ff7-9324-ED2E5D3ABAFA")]
    public interface IMediaDet {
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

}
