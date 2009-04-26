// Stephen Toub
// stoub@microsoft.com

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.Editing
{
	/// <summary>Represents a segment of video.</summary>
	[Serializable]
	public sealed class VideoSpan
	{
		/// <summary>The file in which this span exists.</summary>
		private FileInfo _file;
		/// <summary>Starting time of the video segment.</summary>
		private double _startPosition;
		/// <summary>Stopping time of the video segment.</summary>
		private double _stopPosition;

		/// <summary>Initialize the span.</summary>
		public VideoSpan(){}

		/// <summary>Gets or sets the file in which this span exists.</summary>
		public FileInfo File { get { return _file; } set { _file = value; } }
		/// <summary>Gets or sets the starting time of the video segment.</summary>
		public double StartPosition { get { return _startPosition; } set { _startPosition = value; } }
		/// <summary>Gets or sets the topping time of the video segment.</summary>
		public double StopPosition { get { return _stopPosition; } set { _stopPosition = value; } }

		/// <summary>Converts a number of seconds into hundred nanoseconds.</summary>
		/// <param name="seconds">Number of seconds to convert.</param>
		/// <returns>Converted number of hundred nanoseconds.</returns>
		internal static ulong SecondsToHundredNanoseconds(double seconds)
		{
			return (ulong)(seconds * 10000000);
		}
	}
}