// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>
	/// The IPersistStream interface provides methods for saving and loading objects that 
	/// use a simple serial stream for their storage needs.
	/// </summary>
	[ComImport]
	[Guid("00000109-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IPersistStream
	{
		/// <summary>Retrieves the class identifier (CLSID) of an object.</summary>
		/// <returns>
		/// The CLSID is a globally unique identifier (GUID) that uniquely represents an object 
		/// class that defines the code that can manipulate the object's data.
		/// </returns>
		Guid GetClassID();

		/// <summary>This method checks the object for changes since it was last saved.</summary>
		/// <returns>S_OK if the object has changed since it was last saved; otherwise, S_FALSE.</returns>
		[PreserveSig]
		int IsDirty();

		/// <summary>This method initializes an object from the stream where it was previously saved.</summary>
		/// <param name="pStm">IStream pointer to the stream from which the object should be loaded.</param>
		void Load([In] IStream pStm);

		/// <summary>This method saves an object to the specified stream.</summary>
		/// <param name="pStm">IStream pointer to the stream into which the object should be saved.</param>
		/// <param name="fClearDirty">
		/// Indicates whether to clear the dirty flag after the save is complete. If TRUE, the flag should be cleared. 
		/// If FALSE, the flag should be left unchanged.
		/// </param>
		void Save([In] IStream pStm, [In, MarshalAs(UnmanagedType.Bool)] bool fClearDirty);
		
		/// <summary>This method returns the size in bytes of the stream needed to save the object.</summary>
		/// <returns>64-bit unsigned integer value indicating the size in bytes of the stream needed to save this object.</returns>
		ulong GetSizeMax();
	};
}
