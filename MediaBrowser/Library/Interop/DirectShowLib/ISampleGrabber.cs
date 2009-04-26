// From the directshow.net project http://directshownet.sourceforge.net/)

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace MediaBrowser.Library.Interop.DirectShowLib {
    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
        Guid("6B652FFF-11FE-4fce-92AD-0266B5D7C78F"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISampleGrabber {
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
}
