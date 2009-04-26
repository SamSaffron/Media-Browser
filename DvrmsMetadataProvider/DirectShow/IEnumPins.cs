// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>Enumerates pins on a filter.</summary>
	[ComImport]
	[Guid("56A86892-0AD4-11CE-B03A-0020AF0BA770")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IEnumPins
	{
		/// <summary>The Next method retrieves a specified number of pins in the enumeration sequence.</summary>
		/// <param name="cPins">Number of pins to retrieve.</param>
		/// <param name="pin">Array of size cPins that is filled with IPin pointers.</param>
		/// <param name="pcFetched">Pointer to a variable that receives the number of pins retrieved.</param>
		/// <remarks>Should always be called with cPins set to 1.</remarks>
		void Next([In] uint cPins, [Out] out IPin pin, [Out] out uint pcFetched);

		/// <summary>The Skip method skips over a specified number of pins.</summary>
		/// <param name="cPins">Number of pins to skip.</param>
		void Skip([In] uint cPins);

		/// <summary>The Reset method resets the enumeration sequence to the beginning.</summary>
		void Reset();
		
		/// <summary>The Clone method makes a copy of the enumerator with the same enumeration state.</summary>
		/// <returns>Address of a variable that receives a pointer to the IEnumPins interface of the new enumerator.</returns>
		IEnumPins Clone();
	}
}