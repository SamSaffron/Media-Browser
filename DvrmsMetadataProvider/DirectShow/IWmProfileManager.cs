// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>Used to create instances of the WM profile manager.</summary>
	public sealed class ProfileManager
	{
		/// <summary>Prevent instantiation.</summary>
		private ProfileManager(){}

		/// <summary>Creates a profile manager object.</summary>
		/// <returns>The IWMProfileManager interface of the newly created profile manager object.</returns>
		public static IWMProfileManager CreateInstance() { return WMCreateProfileManager(); }

		/// <summary>The WMCreateProfileManager function creates a profile manager object.</summary>
		/// <returns>The IWMProfileManager interface of the newly created profile manager object.</returns>
		[DllImport("WMVCore.dll", EntryPoint="WMCreateProfileManager", PreserveSig=false, SetLastError=true, ExactSpelling=true)]
		private static extern IWMProfileManager WMCreateProfileManager();
	}

	/// <summary>
	/// The IWMProfileManager interface is used to create profiles, load existing profiles, and save profiles. 
	/// It can be used with both system profiles and application-defined custom profiles. To make changes to a 
	/// profile, you must load it into a profile object using one of the loading methods of this interface. You
	/// can then access the profile data through the use of the interfaces of the profile object.
	/// </summary>
	[ComImport]
	[Guid("D16679F2-6CA0-472D-8D31-2F5D55AEE155")]
	[InterfaceType( ComInterfaceType.InterfaceIsIUnknown )]
	public interface IWMProfileManager
	{
		/// <summary>The CreateEmptyProfile method creates an empty profile object.</summary>
		/// <param name="dwVersion">DWORD containing one member of the WMT_VERSION enumeration type.</param>
		/// <returns>A pointer to an IWMProfile interface.</returns>
		IntPtr CreateEmptyProfile([In] uint dwVersion);

		/// <summary>The LoadProfileByID method loads a system profile identified by its globally unique identifier.</summary>
		/// <param name="guidProfile">GUID identifying the profile.</param>
		/// <returns>A pointer to an IWMProfile interface.</returns>
		IntPtr LoadProfileByID([In,Out] ref Guid guidProfile);
		
		/// <summary>The LoadProfileByData method creates a profile object and populates it with data from a stored string.</summary>
		/// <param name="pwszProfile">
		/// Pointer to a wide-character null-terminated string containing the profile. 
		/// Profile strings are limited to 153600 wide characters.
		/// </param>
		/// <returns>A pointer to an IWMProfile interface.</returns>
		IntPtr LoadProfileByData([In] string pwszProfile);

		/// <summary>The SaveProfile method saves a profile into an XML-formatted string.</summary>
		/// <param name="pProfile">Pointer to the IWMProfile interface of the object containing the profile data to be saved.</param>
		/// <param name="pwszProfile">
		/// Pointer to a wide-character null-terminated string containing the profile. Set this to NULL to 
		/// retrieve the length of string required.
		/// </param>
		/// <param name="pdwLength">
		/// On input, specifies the length of the pwszProfile string. On output, if the method succeeds, 
		/// specifies a pointer to a DWORD containing the number of characters, including the terminating 
		/// null character, required to hold the profile.
		/// </param>
		void SaveProfile([In] IntPtr pProfile, [In] string pwszProfile, [In, Out] ref uint pdwLength);
		
		/// <summary>The GetSystemProfileCount method retrieves the number of system profiles.</summary>
		/// <returns>The number of system profiles.</returns>
		uint GetSystemProfileCount();

		/// <summary>The LoadSystemProfile method loads a system profile identified by its index.</summary>
		/// <param name="dwProfileIndex">DWORD containing the profile index.</param>
		/// <returns>A pointer to an IWMProfile interface.</returns>
		IntPtr LoadSystemProfile([In] uint dwProfileIndex);
	}
}