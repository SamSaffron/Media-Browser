// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>The IPropertyBag interface provides an object with a property bag in which the object can persistently save its properties.</summary>
	[ComImport]
	[Guid("55272A00-42CB-11CE-8135-00AA004BB851")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IPropertyBag
	{
		/// <summary>Asks the property bag to read the named property into a caller-initialized VARIANT.</summary>
		/// <param name="pszPropName">Address of the name of the property to read. This cannot be NULL.</param>
		/// <param name="pVar">Address of the caller-initialized VARIANT that is to receive the property value on output.</param>
		/// <param name="pErrorLog">Address of the caller's error log in which the property bag stores any errors that occur during reads.</param>
		/// <returns></returns>
		void Read([In, MarshalAs(UnmanagedType.LPWStr)] string pszPropName,
			[In, Out, MarshalAs(UnmanagedType.Struct)] ref object pVar, 
			[In, Out] IntPtr pErrorLog);

		/// <summary>Asks the property bag to save the named property in a caller-initialized VARIANT.</summary>
		/// <param name="pszPropName">Address of a string containing the name of the property to write. This cannot be NULL.</param>
		/// <param name="pVar">Address of the caller-initialized VARIANT that holds the property value to save.</param>
		void Write([In, MarshalAs(UnmanagedType.LPWStr)] string pszPropName,
			[In, MarshalAs(UnmanagedType.Struct)] ref object pVar);
	}
}