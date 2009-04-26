// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>The IFileSourceFilter interface is implemented on filters that read media streams from a file.</summary>
	[ComImport]
	[Guid("56a868a6-0ad4-11ce-b03a-0020af0ba770")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IFileSourceFilter 
	{
		/// <summary>Load a file and assign it the given media type.</summary>
		/// <param name="pszFileName">Pointer to absolute path of file to open</param>
		/// <param name="pmt">Media type of file - can be NULL</param>
		void Load(
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
