// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>The IMediaControl interface provides methods for controlling the flow of data through the filter graph.</summary>
	[ComImport]
	[Guid("56A868B1-0AD4-11CE-B03A-0020AF0BA770")]
	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	public interface IMediaControl
	{
		/// <summary>
		/// The Run method runs all the filters in the filter graph. While the graph is running, 
		/// data moves through the graph and is rendered.
		/// </summary>
		void Run();

		/// <summary>The Pause method pauses all the filters in the filter graph.</summary>
		void Pause();

		/// <summary>The Stop method stops all the filters in the graph.</summary>
		void Stop();

		/// <summary>The GetState method retrieves the state of the filter graph—paused, running, or stopped.</summary>
		/// <param name="msTimeout">Duration of the time-out, in milliseconds, or INFINITE to specify an infinite time-out.</param>
		/// <returns>A member of the FILTER_STATE enumeration.</returns>
		int GetState([In] uint msTimeout);

		/// <summary>The RenderFile method builds a filter graph that renders the specified file.</summary>
		/// <param name="strFilename">Name of the file to render.</param>
		void RenderFile([In] string strFilename);

		/// <summary>
		/// This method adds to the graph the source filter that can read the given file name, 
		/// and returns an IDispatch interface pointer representing the filter. 
		/// </summary>
		/// <param name="strFilename">Name of the file containing the source video.</param>
		object AddSourceFilter([In] string strFilename);

		/// <summary>The get_FilterCollection method retrieves a collection of the filters in the filter graph.</summary>
		object get_FilterCollection();

		/// <summary>The get_RegFilterCollection method retrieves a collection of all the filters listed in the registry.</summary>
		object get_RegFilterCollection();

		/// <summary>The StopWhenReady method pauses the filter graph, allowing filters to queue data, and then stops the filter graph.</summary>
		void StopWhenReady();
	}
}
