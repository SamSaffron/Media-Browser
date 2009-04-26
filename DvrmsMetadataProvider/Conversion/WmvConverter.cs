// Stephen Toub
// stoub@microsoft.com

using System;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Toub.MediaCenter.Dvrms;
using Toub.MediaCenter.Dvrms.DirectShow;
using Toub.MediaCenter.Dvrms.Utilities;

namespace Toub.MediaCenter.Dvrms.Conversion
{
	/// <summary>Generates a WMV file from the audio in a DVR-MS file.</summary>
	public sealed class WmvConverter : Converter
	{
		/// <summary>Optional path to the profile to use for transcoding.</summary>
		private string _profilePath;

		/// <summary>Initializes the converter.</summary>
		/// <param name="input">The path to the input DVR-MS file.</param>
		/// <param name="output">The target path for the output WMV file.</param>
		public WmvConverter(string input, string output) : this(input, output, null) {}

		/// <summary>Initializes the converter.</summary>
		/// <param name="input">The path to the input DVR-MS file.</param>
		/// <param name="output">The target path for the output WMV file.</param>
		/// <param name="profilePath">Optional path to the Windows Media profile to use for transcoding.</param>
		public WmvConverter(string input, string output, string profilePath) : base(input, output)
		{
			// Validate the parameters.  Base class stores them but
			// allows them to be null.  Store the profile, too.
			if (input == null) throw new ArgumentNullException("input");
			if (output == null) throw new ArgumentNullException("output");
			_profilePath = profilePath;
		}

		/// <summary>Do the conversion from DVR-MS to WAV.</summary>
		/// <returns>Null; ignored.</returns>
		protected override object DoWork()
		{
			// Get the filter graph
			object filterGraph = ClassId.CoCreateInstance(ClassId.FilterGraph);
			DisposalCleanup.Add(filterGraph);
			IGraphBuilder graph = (IGraphBuilder)filterGraph;

			// Add the ASF writer and set the output name
			IBaseFilter asfWriterFilter = (IBaseFilter)ClassId.CoCreateInstance(ClassId.WMAsfWriter);
			DisposalCleanup.Add(asfWriterFilter);
			graph.AddFilter(asfWriterFilter, null);
			IFileSinkFilter sinkFilter = (IFileSinkFilter)asfWriterFilter;
			sinkFilter.SetFileName(OutputFilePath, null);

			// Set the profile to be used for conversion
			if (_profilePath != null && _profilePath.Trim().Length > 0)
			{
				// Load the profile XML contents
				string profileData;
				using(StreamReader reader = new StreamReader(File.OpenRead(_profilePath)))
				{
					profileData = reader.ReadToEnd();
				}

				// Create an appropriate IWMProfile from the data
				IWMProfileManager profileManager = ProfileManager.CreateInstance();
				DisposalCleanup.Add(profileManager);
				IntPtr wmProfile = profileManager.LoadProfileByData(profileData);
				DisposalCleanup.Add(wmProfile);

				// Set the profile on the writer
				IConfigAsfWriter2 configWriter = (IConfigAsfWriter2)asfWriterFilter;
				configWriter.ConfigureFilterUsingProfile(wmProfile);
			}

			// Add the source filter; should connect automatically through the appropriate transform filters
			graph.RenderFile(InputFilePath, null);

			// Run the graph to completion
			RunGraph(graph, asfWriterFilter);
		
			return null;
		}
	}
}