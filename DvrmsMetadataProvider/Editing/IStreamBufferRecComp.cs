// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;
using Toub.MediaCenter.Dvrms.Utilities;
using Toub.MediaCenter.Dvrms.DirectShow;

namespace Toub.MediaCenter.Dvrms.Editing
{
	/// <summary>
	/// The IStreamBufferRecComp interface is used to create new content recordings by concatenating 
	/// existing recordings. The new recording can be created from a mix of reference and content recordings.
	/// </summary>
	[ComImport]
	[Guid("9E259A9B-8815-42AE-B09F-221970B154FD")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IStreamBufferRecComp
	{
		/// <summary>
		/// The Initialize method sets the file name and the profile for the new recording. 
		/// Call this method once, after creating the RecComp object.
		/// </summary>
		/// <param name="pszTargetFilename">The output file.</param>
		/// <param name="pszSBRecProfileRef">A file generated with the profile to use for the target file.</param>
		void Initialize(
			[In, MarshalAs(UnmanagedType.LPWStr)] string pszTargetFilename, 
			[In, MarshalAs(UnmanagedType.LPWStr)] string pszSBRecProfileRef);

		/// <summary>The Append method appends an entire recording to the target file.</summary>
		/// <param name="pszSBRecording">String that contains the name of the file to append.</param>
		void Append([In, MarshalAs(UnmanagedType.LPWStr)] string pszSBRecording);
		
		/// <summary>The AppendEx method appends part of a recording to the target file.</summary>
		/// <param name="pszSBRecording">String that contains the name of the file to append.</param>
		/// <param name="rtStart">Specifies the start time of the segment to append, in 100-nanosecond units.</param>
		/// <param name="rtStop">Specifies the stop time of the segment to append, in 100-nanosecond units.</param>
		void AppendEx(
			[In, MarshalAs(UnmanagedType.LPWStr)] string pszSBRecording,
			[In] ulong rtStart, [In] ulong rtStop);
		
		/// <summary>The GetCurrentLength method retrieves the length of the target file.</summary>
		/// <returns>The current length, in seconds.</returns>
		uint GetCurrentLength();
		
		/// <summary>The Close method closes the target file.</summary>
		void Close();
		
		/// <summary>The Cancel method cancels an append operation, if one is in progress. Otherwise, it has no effect.</summary>
		void Cancel();
	}
}