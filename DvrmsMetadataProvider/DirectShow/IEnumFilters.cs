// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>The IEnumFilters interface enumerates the filters in a filter graph.</summary>
	[ComImport]
	[Guid("56A86893-0AD4-11CE-B03A-0020AF0BA770")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IEnumFilters
	{
		/// <summary>The Next method retrieves the specified number of filters in the enumeration sequence.</summary>
		/// <param name="cFilters">Number of filters to retrieve.</param>
		/// <param name="ppFilter">Array of size cFilters that is filled with IBaseFilter interface pointers.</param>
		/// <param name="pcFetched">Pointer to a variable that receives the number of filters retrieved.</param>
		/// <remarks>Should always be called with cFilters set to 1.</remarks>
		void Next([In] uint cFilters, [Out] out IBaseFilter ppFilter, [Out] out uint pcFetched);
		
		/// <summary>The Skip method skips over a specified number of filters.</summary>
		/// <param name="cFilter">Number of filters to skip.</param>
		void Skip([In] uint cFilter);

		/// <summary>Resets the enumeration sequence to the beginning.</summary>
		void Reset();

		/// <summary>
		/// The Clone method makes a copy of the enumerator object. The returned object starts with the 
		/// same enumeration state as the original.
		/// </summary>
		/// <param name="ppEnum">Address of a variable that receives a pointer to the IEnumFilters interface of the new enumerator.</param>
		void Clone([Out] out IEnumFilters ppEnum);
	}
}