// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>
	/// The AmMediaType structure is the primary structure used to describe media formats 
	/// for the objects of the Windows Media Format SDK.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public class AmMediaType
	{
		/// <summary>Major type of the media sample. For example, WMMEDIATYPE_Video specifies a video stream.</summary>
		public Guid majortype;
		/// <summary>Subtype of the media sample. The subtype defines a specific format (usually an encoding scheme) within a major media type.</summary>
		public Guid subtype;
		/// <summary>
		/// Set to true if the samples are of a fixed size. Compressed audio samples are of a fixed size while video samples are not.
		/// </summary>
		[MarshalAs(UnmanagedType.Bool)]
		public bool bFixedSizeSamples;
		/// <summary>
		/// Set to true if the samples are temporally compressed. Temporal compression is compression where some 
		/// samples describe the changes in content from the previous sample, instead of describing the sample in its entirety.
		/// </summary>
		[MarshalAs(UnmanagedType.Bool)]
		public bool bTemporalCompression;
		/// <summary>Long integer containing the size of the sample, in bytes.</summary>
		public uint lSampleSize;
		/// <summary>GUID of the format type.</summary>
		public Guid formattype;
		/// <summary>Not used. Should be NULL.</summary>
		public IntPtr pUnk;
		/// <summary>Size, in bytes, of the structure pointed to by pbFormat.</summary>
		public uint cbFormat;
		/// <summary>Pointer to the format structure of the media type.</summary>
		public IntPtr pbFormat;
	};
}