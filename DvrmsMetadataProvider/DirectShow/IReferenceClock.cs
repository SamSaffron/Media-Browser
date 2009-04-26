// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>
	/// The IReferenceClock interface represents a system reference clock. The DirectMusic master clock and a port's 
	/// latency clock implement this interface.
	/// </summary>
	[ComImport]
	[Guid("56A86897-0AD4-11CE-B03A-0020AF0BA770")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IReferenceClock
	{
		/// <summary>This method retrieves the current time.</summary>
		/// <returns>Current time.</returns>
		ulong GetTime();

		/// <summary>This method requests an asynchronous notification that a duration has elapsed.</summary>
		/// <param name="rtBaseTime">Base reference time.</param>
		/// <param name="rtStreamTime">Stream offset time.</param>
		/// <param name="hEvent">Handle of an event through which to advise.</param>
		/// <returns>Destination of the token.</returns>
		int AdviseTime([In] ulong rtBaseTime, [In] ulong rtStreamTime, [In] IntPtr hEvent);

		/// <summary>This method requests an asynchronous, periodic notification that a duration has elapsed.</summary>
		/// <param name="rtStartTime">Time that the notification should begin.</param>
		/// <param name="rtPeriodTime">Period of time between notifications.</param>
		/// <param name="hSemaphore">Handle of a semaphore through which to advise.</param>
		/// <returns>Identifier of the request.</returns>
		int AdvisePeriodic([In] ulong rtStartTime, [In] ulong rtPeriodTime, [In] IntPtr hSemaphore);

		/// <summary>This method cancels a request for notification. </summary>
		/// <param name="dwAdviseCookie">
		/// Identifier of the request that is to be canceled, as set in the IReferenceClock::AdviseTime or 
		/// the IReferenceClock::AdvisePeriodic method. 
		/// </param>
		void Unadvise([In] uint dwAdviseCookie);
	}
}