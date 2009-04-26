// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>The IFileSinkFilter interface is implemented on filters that write media streams to a file.</summary>
	[ComImport]
	[Guid("A2104830-7C70-11CF-8BCE-00AA00A3F1A6")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IFileSinkFilter
	{
		/// <summary>The SetFileName method sets the name of the file into which media samples will be written.</summary>
		/// <param name="pszFileName">Pointer to the name of the file to receive the media samples.</param>
		/// <param name="pmt">Pointer to the type of media samples to be written to the file, and the media type of the sink filter's input pin.</param>
		void SetFileName(
			[In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName, 
			[In, MarshalAs(UnmanagedType.LPStruct)] AmMediaType pmt);
	
		/// <summary>The GetCurFile method retrieves the name and media type of the current file.</summary>
		/// <param name="ppszFileName">Address of a pointer that receives the name of the file, as an OLESTR type.</param>
		/// <param name="pmt">Pointer to an AM_MEDIA_TYPE structure that receives the media type.</param>
		void GetCurFile(
			[Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName, 
			[Out, MarshalAs(UnmanagedType.LPStruct)] AmMediaType pmt);
	}
}