// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>Seeking capability flags.</summary>
	[Flags]
	public enum SeekingCapabilities : uint
	{
		/// <summary>Can seek to an absolute position.</summary>
		CanSeekAbsolute = 0x001,
		/// <summary>Can seek to forwards.</summary>
		CanSeekForwards = 0x002,
		/// <summary>Can seek backwards.</summary>
		CanSeekBackwards = 0x004,
		/// <summary>Can retrieve the current position.</summary>
		CanGetCurrentPos = 0x008,
		/// <summary>Can retrieve the stop position.</summary>
		CanGetStopPos = 0x010,
		/// <summary>Can retrieve the duration.</summary>
		CanGetDuration = 0x020,
		/// <summary>Can play in reverse.</summary>
		CanPlayBackwards = 0x040,
		/// <summary>Can do segments.</summary>
		CanDoSegments = 0x080,
		/// <summary>Source.</summary>
		Source = 0x100  
	}

	/// <summary>
	/// The IMediaSeeking interface contains methods for seeking to a position within a stream, 
	/// and for setting the playback rate. The Filter Graph Manager exposes this interface, and 
	/// so do individual filters.
	/// </summary>
	[ComImport]
	[Guid("36B73880-C2C8-11CF-8B46-00805F6CEF60")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IMediaSeeking 
	{
		/// <summary>This method returns the seeking capabilities of the media stream.</summary>
		/// <returns>Seeking capability flags.</returns>
		SeekingCapabilities GetCapabilities();

		/// <summary>Determines which capabilities exist on a media stream by applying seeking capability flags and checking the returned value.</summary>
		/// <param name="pCapabilities">Pointer to an AM_SEEKING_SEEKING_CAPABILITIES enum type containing the seeking capabilities flags to apply.</param>
		void CheckCapabilities([In,Out] ref SeekingCapabilities pCapabilities);

		/// <summary>This method determines if a specified time format is supported.</summary>
		/// <param name="pFormat">Time format to compare.</param>
		/// <returns>
		/// The default DirectShow implementation returns S_OK if pFormat is supported; otherwise returns S_FALSE. 
		/// </returns>
		[PreserveSig]
		int IsFormatSupported([In] ref Guid pFormat);

		/// <summary>This method retrieves the preferred time format to be used by the interface.</summary>
		/// <returns>Time format preferred by the interface.</returns>
		Guid QueryPreferredFormat();

		/// <summary>This method retrieves the current time format, which determines the format of units used during seeking.</summary>
		/// <returns>Time format currently supported by this interface.</returns>
		Guid GetTimeFormat();
		
		/// <summary>This method determines if the time format being used in the call is the same as the one currently in use by the interface.</summary>
		/// <param name="pFormat">Time format to check.</param>
		/// <returns>
		/// The default DirectShow implementation returns S_OK if pFormat is the current time format; otherwise returns S_FALSE. 
		/// </returns>
		[PreserveSig]
		int IsUsingTimeFormat([In] ref Guid pFormat);

		/// <summary>This method sets the time format, which determines the format of units used during seeking.</summary>
		/// <param name="pFormat">Time format to be supported by this interface.</param>
		void SetTimeFormat([In] ref Guid pFormat);

		/// <summary>This method retrieves the length of time that the media stream will play.</summary>
		/// <returns>Returned length of the media stream.</returns>
		ulong GetDuration();
		
		/// <summary>This method retrieves the time at which the media stream stops.</summary>
		/// <returns>Returned stop time.</returns>
		ulong GetStopPosition();

		/// <summary>This method retrieves the current position in terms of the total length of the media stream.</summary>
		/// <returns>Current position in current time format units.</returns>
		ulong GetCurrentPosition();

		/// <summary>This method converts a time from one format to another.</summary>
		/// <param name="pTarget">Time in converted format.</param>
		/// <param name="pTargetFormat">GUID of the format to convert to, or the currently selected format if NULL.</param>
		/// <param name="Source">Time in original format.</param>
		/// <param name="pSourceFormat">GUID of the format to be converted from, or the currently selected format if NULL.</param>
		void ConvertTimeFormat([Out] out ulong pTarget, [In] ref Guid pTargetFormat, [In] ulong Source, [In] ref Guid pSourceFormat);

		/// <summary>This method sets current and stop positions and applies flags to both.</summary>
		/// <param name="pCurrent">Start position if stopped, or position from which to continue if paused.</param>
		/// <param name="dwCurrentFlags">When seeking, one of these flags must be set to indicate the type of seek.</param>
		/// <param name="pStop">Position in the stream at which to quit.</param>
		/// <param name="dwStopFlags">Stop position seeking options to be applied.</param>
		void SetPositions([In,Out] ref ulong pCurrent, [In] uint dwCurrentFlags, [In,Out] ref ulong pStop, [In] uint dwStopFlags);

		/// <summary>This method returns the current and stop position settings.</summary>
		/// <param name="pCurrent">Start time in the current time format.</param>
		/// <param name="pStop">Stop time in the current time format.</param>
		void GetPositions([Out] out ulong pCurrent, [Out] out ulong pStop);

		/// <summary>This method returns the range of times in which seeking is efficient.</summary>
		/// <param name="pEarliest">Earliest time that can be efficiently seeked to.</param>
		/// <param name="pLatest">Latest time that can be efficiently seeked to.</param>
		void GetAvailable([Out] out ulong pEarliest, [Out] out ulong pLatest);

		/// <summary>This method sets a new playback rate.</summary>
		/// <param name="dRate">New rate, where 1 is the normal rate, 2 is twice as fast, and so on.</param>
		void SetRate([In] double dRate);

		/// <summary>This method retrieves the current rate.</summary>
		/// <returns>Current rate, where 1 is the normal rate.</returns>
		double GetRate();

		/// <summary>This method retrieves the preroll settings.</summary>
		/// <returns>The time prior to the start position that the filter graph begins any nonrandom access device rolling.</returns>
		ulong GetPreroll();
	}
}