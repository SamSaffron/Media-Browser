// Stephen Toub
// stoub@microsoft.com

// NOTE: This code works with NTSC and non-HD content due to some
// inconsistencies with the PAL and HD formats.  For a system
// that implements the necessary workarounds, see Alex Seigler's
// dvr2wmv DLL and application.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Toub.MediaCenter.Dvrms.DirectShow;
using Toub.MediaCenter.Dvrms.Utilities;

namespace Toub.MediaCenter.Dvrms.Conversion
{
	/// <summary>Generates a WAV file from the audio in a DVR-MS file.</summary>
	public sealed class WavConverter : Converter
	{
		/// <summary>Initializes the converter.</summary>
		/// <param name="input">The path to the input DVR-MS file.</param>
		/// <param name="output">The target path for the output WAV file.</param>
		public WavConverter(string input, string output) : base(input, output)
		{
			// Validate the parameters.  Base class stores them but
			// allows them to be null.
			if (input == null) throw new ArgumentNullException("input");
			if (output == null) throw new ArgumentNullException("output");
		}

		/// <summary>Do the conversion from DVR-MS to WAV.</summary>
		/// <returns>Null; ignored.</returns>
		protected override object DoWork()
		{
			// Get the filter graph
			object filterGraph = ClassId.CoCreateInstance(ClassId.FilterGraph);
			DisposalCleanup.Add(filterGraph);
			IGraphBuilder graph = (IGraphBuilder)filterGraph;

			// Add the source filter for the dvr-ms file
			IBaseFilter DvrmsSourceFilter = graph.AddSourceFilter(InputFilePath, null);
			DisposalCleanup.Add(DvrmsSourceFilter);

			// Add the file writer to the graph
			IBaseFilter wavFilter = (IBaseFilter)ClassId.CoCreateInstance(ClassId.FileWriter);
			DisposalCleanup.Add(wavFilter);
			graph.AddFilter(wavFilter, null);
			IFileSinkFilter sinkFilter = (IFileSinkFilter)wavFilter;
			sinkFilter.SetFileName(OutputFilePath, null);

			// Add the Wav Dest filter to the graph
			IBaseFilter wavDest = (IBaseFilter)ClassId.CoCreateInstance(ClassId.WavDest);
			DisposalCleanup.Add(wavDest);
			graph.AddFilter(wavDest, null);

			// Add the decrypter node to the graph
			IBaseFilter decrypter = (IBaseFilter)ClassId.CoCreateInstance(ClassId.DecryptTag);
			DisposalCleanup.Add(decrypter);
			graph.AddFilter(decrypter, null);

			// Connect the dvr-ms source to the decrypter, the decrypter to the wav dest,
			// and the wav dest to the file writer
			Connect(graph, DvrmsSourceFilter, "DVR Out - 1", decrypter, "In(Enc/Tag)");
			Connect(graph, decrypter, "Out", wavDest, "In");
			Connect(graph, wavDest, "Out", wavFilter, "in");

			// Run the graph to convert the audio to wav
			RunGraph(graph);

			return null;
		}
	}
}