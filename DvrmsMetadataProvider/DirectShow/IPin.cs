// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>This enumeration indicates a pin's direction.</summary>
	public enum PinDirection
	{
		/// <summary>Input pin.</summary>
		Input,
		/// <summary>Output pin.</summary>
		Output
	}

	/// <summary>
	/// This interface represents a single, unidirectional connection point on a filter. A pin connects to 
	/// exactly one other pin on another filter. Other objects can use this interface on this pin. The interface 
	/// between the filter and the pin is private to the implementation of a specific filter.
	/// </summary>
	[ComImport]
	[Guid("56A86891-0AD4-11CE-B03A-0020AF0BA770")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IPin
	{
		/// <summary>This method initiates a connection from this pin to the other pin.</summary>
		/// <param name="pReceivePin">Other pin to connect to.</param>
		/// <param name="pmt">Type to use for the connections (optional).</param>
		void Connect([In] IPin pReceivePin, [In, MarshalAs(UnmanagedType.LPStruct)] AmMediaType pmt);

		/// <summary>This method makes a connection to the calling pin.</summary>
		/// <param name="pReceivePin">Connecting pin.</param>
		/// <param name="pmt">Media type of the samples to be streamed.</param>
		void ReceiveConnection([In] IPin pReceivePin, [In, MarshalAs(UnmanagedType.LPStruct)] AmMediaType pmt);

		/// <summary>This method breaks a connection.</summary>
		void Disconnect();

		/// <summary>If this pin is connected to another pin, this method returns a pointer to that pin.</summary>
		/// <returns>
		/// Pointer to an IPin pointer to the IPin interface of the other pin (if any) to which this pin is connected. 
		/// If there is no connection, the other pin interface pointer will be NULL. 
		/// </returns>
		IPin ConnectedTo();

		/// <summary>
		/// This method determines the media type associated with the current connection of the pin. 
		/// This method fails if the pin is unconnected.
		/// </summary>
		/// <returns>
		/// Pointer to an AM_MEDIA_TYPE structure. If the pin is connected, the media type is returned. 
		/// Otherwise, the structure is initialized to a default state in which all elements are 0, 
		/// with the exception of lSampleSize, which is set to 1, and bFixedSizeSamples, which is set to TRUE. 
		/// </returns>
		[return: MarshalAs(UnmanagedType.LPStruct)]
		AmMediaType ConnectionMediaType();

		/// <summary>Retrieves information about the pin.</summary>
		/// <param name="pInfo">Pointer to a PIN_INFO structure.</param>
		void QueryPinInfo(IntPtr pInfo);

		/// <summary>This method retrieves the direction for this pin.</summary>
		/// <returns>The direction of the pin.</returns>
		PinDirection QueryDirection();

		/// <summary>This method retrieves an identifier for the pin.</summary>
		/// <returns>Pin identifier.</returns>
		[return: MarshalAs(UnmanagedType.LPWStr)]
		string QueryId();

		/// <summary>This method determines if the pin could accept the format type.</summary>
		/// <param name="pmt">Pointer to a proposed media type.</param>
		/// <returns>S_OK if it can accept the format; S_FALSE, otherwise.</returns>
		[PreserveSig]
		int QueryAccept([In, MarshalAs(UnmanagedType.LPStruct)] AmMediaType pmt);

		/// <summary>This method provides an enumerator for this pin's preferred media types.</summary>
		/// <returns>Pointer to an enumerator for the media types.</returns>
		[return: MarshalAs(UnmanagedType.Interface)]
		object EnumMediaTypes();

		/// <summary>
		/// This method provides an array of pointers to the IPin interface of the pins to which this pin internally connects.
		/// </summary>
		/// <param name="apPin">Array of IPin pointers.</param>
		/// <param name="nPin">Upon input, indicates the number of array elements; upon output, indicates the number of pins.</param>
		void QueryInternalConnections([Out] IntPtr apPin, [In, Out] ref uint nPin);

		/// <summary>This method informs the input pin that no additional data is expected until a new run command is issued.</summary>
		void EndOfStream();

		/// <summary>This method informs the pin to begin a flush operation.</summary>
		void BeginFlush();

		/// <summary>This method informs the pin to end a flush operation.</summary>
		void EndFlush();

		/// <summary>This method specifies that samples following this method are grouped as a segment with a given start time, stop time, and rate.</summary>
		/// <param name="tStart">Start time of the segment.</param>
		/// <param name="tStop">Stop time of the segment.</param>
		/// <param name="dRate">Rate of the segment.</param>
		void NewSegment(ulong tStart, ulong tStop, double dRate);
	}
}