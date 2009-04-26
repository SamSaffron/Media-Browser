// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>This interface provides methods that enable an application to build a filter graph.</summary>
	[ComImport]
	[Guid("56A868A9-0AD4-11CE-B03A-0020AF0BA770")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IGraphBuilder
	{
		/// <summary>This method adds a filter to the graph and names it by using the pName parameter.</summary>
		/// <param name="pFilter">Filter to add to the graph. </param>
		/// <param name="pName">Name of the filter.</param>
		void AddFilter([In] IBaseFilter pFilter, [In, MarshalAs(UnmanagedType.LPWStr)] string pName);

		/// <summary>Removes a filter from the graph.</summary>
		/// <param name="pFilter">Pointer to the filter to be removed from the graph.</param>
		void RemoveFilter([In] IBaseFilter pFilter);

		/// <summary>This method provides an enumerator for all filters in the graph.</summary>
		/// <returns>Enumerator.</returns>
		IEnumFilters EnumFilters();

		/// <summary>This method finds a filter that was added to the filter graph with a specific name.</summary>
		/// <param name="pName">Name to search for.</param>
		/// <returns>Pointer to an IBaseFilter interface on the found filter.</returns>
		IBaseFilter FindFilterByName([In, MarshalAs(UnmanagedType.LPWStr)] string pName);

		/// <summary>This method connects the two pins directly (without intervening filters).</summary>
		/// <param name="ppinOut">Output pin.</param>
		/// <param name="ppinIn">Input pin.</param>
		/// <param name="pmt">Media type to use for the connection.</param>
		void ConnectDirect([In] IPin ppinOut, [In] IPin ppinIn, [In, MarshalAs(UnmanagedType.LPStruct)] AmMediaType pmt);

		/// <summary>The Reconnect method disconnects a pin and then reconnects it to the same pin.</summary>
		/// <param name="ppin">Pointer to the pin to disconnect and reconnect.</param>
		void Reconnect([In] IPin ppin);

		/// <summary>The Disconnect method disconnects this pin.</summary>
		/// <param name="ppin">Pointer to the pin to disconnect.</param>
		void Disconnect([In] IPin ppin);

		/// <summary>The SetDefaultSyncSource method sets the reference clock to the default clock.</summary>
		void SetDefaultSyncSource();

		/// <summary>This method connects the two pins, using intermediates if necessary.</summary>
		/// <param name="ppinOut">Output pin.</param>
		/// <param name="ppinIn">Input pin.</param>
		void Connect([In] IPin ppinOut, [In] IPin ppinIn);

		/// <summary>This method builds a filter graph that renders the data from this output pin.</summary>
		/// <param name="ppinOut">Output pin.</param>
		void Render([In] IPin ppinOut);

		/// <summary>This method builds a filter graph that renders the specified file.</summary>
		/// <param name="lpwstrFile">Name of the file containing the data to be rendered.</param>
		/// <param name="lpwstrPlayList">Playlist name. Reserved; must be NULL.</param>
		void RenderFile(
			[In, MarshalAs(UnmanagedType.LPWStr)] string lpwstrFile,
			[In, MarshalAs(UnmanagedType.LPWStr)] string lpwstrPlayList);

		/// <summary>This method adds a source filter to the filter graph for a specific file.</summary>
		/// <param name="lpwstrFileName">Pointer to the file.</param>
		/// <param name="lpwstrFilterName">Name to give the source filter when it is added.</param>
		/// <returns>Pointer to an IBaseFilter interface on the filter that was added.</returns>
		IBaseFilter AddSourceFilter(
			[In, MarshalAs(UnmanagedType.LPWStr)] string lpwstrFileName,
			[In, MarshalAs(UnmanagedType.LPWStr)] string lpwstrFilterName);

		/// <summary>This method sets the file into which actions taken in attempting to perform an operation are logged.</summary>
		/// <param name="hFile">Handle to the log file.</param>
		void SetLogFile(IntPtr hFile);

		/// <summary>The Abort method requests the Filter Graph Manager to halt its current task as quickly as possible.</summary>
		void Abort();

		/// <summary>The ShouldOperationContinue method queries whether the current operation should continue.</summary>
		void ShouldOperationContinue();
	}
}
