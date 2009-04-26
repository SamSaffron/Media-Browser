// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>This structure contains information about a filter.</summary>
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
	public class FilterInfo
	{
		/// <summary>Name of the filter.</summary>
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)]
		public string achName;
		/// <summary>Pointer to the IFilterGraph interface to which the filter is connected.</summary>
		[MarshalAs(UnmanagedType.IUnknown)]
		public object pUnk;
	}

	/// <summary>This interface abstracts an object that has typed input and output connections and can be aggregated dynamically.</summary>
	[ComImport]
	[Guid("56A86895-0AD4-11CE-B03A-0020AF0BA770")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IBaseFilter
	{
		/// <summary>Fills the pClsID parameter with the class identifier of this filter.</summary>
		/// <param name="pClsID">Pointer to the class identifier to be filled out.</param>
		void GetClassID([Out] out Guid pClsID);

		/// <summary>This method informs the filter to transition to the new state.</summary>
		void Stop();

		/// <summary>This method informs the filter to transition to the new state.</summary>
		void Pause();

		/// <summary>
		/// This method informs the filter to transition to the new (running) state. 
		/// Passes a time value to synchronize independent streams.</summary>
		/// <param name="tStart">Time value of the reference clock.</param>
		void Run([In] ulong tStart);

		/// <summary>This method determines the filter's state.</summary>
		/// <param name="dwMilliSecsTimeout">Duration of the time-out, in milliseconds.</param>
		/// <returns>
		/// Returned state of the filter. States include stopped, paused, running, or 
		/// intermediate (in the process of changing).
		/// </returns>
		int GetState([In] uint dwMilliSecsTimeout);

		/// <summary>This method identifies the reference clock to which the filter should synchronize activity.</summary>
		/// <param name="pClock">Pointer to the IReferenceClock interface.</param>
		void SetSyncSource([In] IReferenceClock pClock);

		/// <summary>This method retrieves the current reference clock in use by this filter.</summary>
		/// <returns>Pointer to a reference clock; it will be set to the IReferenceClock interface.</returns>
		IReferenceClock GetSyncSource();

		/// <summary>This method enumerates all the pins available on this filter.</summary>
		/// <returns>Pointer to the IEnumPins interface to retrieve.</returns>
		IEnumPins EnumPins();

		/// <summary>This method retrieves the pin with the specified identifier.</summary>
		/// <param name="Id">Identifier of the pin.</param>
		/// <returns>Pointer to the IPin interface for this pin after the filter has been restored.</returns>
		IPin FindPin([In, MarshalAs(UnmanagedType.LPWStr)] string Id);

		/// <summary>This method returns information about the filter.</summary>
		/// <returns>Pointer to a FilterInfo structure.</returns>
		FilterInfo QueryFilterInfo();

		/// <summary>This method notifies a filter that it has joined a filter graph.</summary>
		/// <param name="pGraph">Pointer to the filter graph to join.</param>
		/// <param name="pName">Name of the filter being added.</param>
		void JoinFilterGraph([In] IGraphBuilder pGraph, [In, MarshalAs(UnmanagedType.LPWStr)] string pName);

		/// <summary>This method returns a vendor information string.</summary>
		/// <returns>Pointer to a string containing vendor information.</returns>
		[return: MarshalAs(UnmanagedType.LPWStr)]
		string QueryVendorInfo();
	}
}