// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>Event that terminated a wait.</summary>
	public enum EventCode : uint
	{
		/// <summary>Operation has not completed.</summary>
		None = 0x0,
		/// <summary>Operation completed.</summary>
		Complete = 0x01,
		/// <summary>User terminated the operation.</summary>
		UserAbort = 0x02,
		/// <summary>Error. Playback can't continue.</summary>
		ErrorAbort = 0x03,
		/// <summary>Time has expired.</summary>
		Time = 0x04
	}

	/// <summary>
	/// The IMediaEvent interface contains methods for retrieving event notifications and for overriding 
	/// the Filter Graph Manager's default handling of events.
	/// </summary>
	[ComImport]
	[Guid("56A868B6-0AD4-11CE-B03A-0020AF0BA770")]
	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	public interface IMediaEvent
	{
		/// <summary>
		/// The GetEventHandle method retrieves a handle to a manual-reset event that remains signaled while 
		/// the queue contains event notifications.
		/// </summary>
		/// <returns>Handle for the event.</returns>
		IntPtr GetEventHandle();

		/// <summary>This method retrieves the next notification event.</summary>
		/// <param name="lEventCode">Next event notification.</param>
		/// <param name="lParam1">First parameter of the event.</param>
		/// <param name="lParam2">Second parameter of the event.</param>
		/// <param name="msTimeout">Time, in milliseconds, to wait before assuming that there are no events.</param>
		void GetEvent([Out] out uint lEventCode, [Out] out uint lParam1, [Out] out uint lParam2, [In] uint msTimeout);

		/// <summary>This method blocks execution of the application thread until the graph's operation finishes.</summary>
		/// <param name="msTimeout">Duration of the time-out, in milliseconds. Pass zero to return immediately.</param>
		/// <param name="pEvCode">Event that terminated the wait.</param>
		/// <returns>HRESULT</returns>
		[PreserveSig]
		int WaitForCompletion([In] int msTimeout, [Out] out EventCode pEvCode);

		/// <summary>
		/// This method cancels any default handling by the filter graph of the specified 
		/// event and ensures that it is passed to the application.
		/// </summary>
		/// <param name="lEvCode">Event code for which to cancel default handling.</param>
		void CancelDefaultHandling([In] uint lEvCode);

		/// <summary>This method reinstates the normal default handling by a filter graph for the specified event, if there is one.</summary>
		/// <param name="lEvCode">Event to restore.</param>
		void RestoreDefaultHandling([In] uint lEvCode);

		/// <summary>This method frees resources associated with the parameters of an event.</summary>
		/// <param name="lEvCode">Next event notification.</param>
		/// <param name="lParam1">First parameter of the event.</param>
		/// <param name="lParam2">Second parameter of the event.</param>
		void FreeEventParams([In] uint lEvCode, [In] uint lParam1, [In] uint lParam2);
	}
}
