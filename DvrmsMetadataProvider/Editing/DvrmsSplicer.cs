// Stephen Toub
// stoub@microsoft.com

using System;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Toub.MediaCenter.Dvrms.DirectShow;
using Toub.MediaCenter.Dvrms.Conversion;
using Toub.MediaCenter.Dvrms.Metadata;

namespace Toub.MediaCenter.Dvrms.Editing
{
	/// <summary>Splices together segments from multiple DVR-MS files into new DVR-MS files.</summary>
	public sealed class DvrmsSplicer : Converter
	{
		/// <summary>The target file to be generated from multiple file segments.</summary>
		private readonly FileInfo _target;
		/// <summary>The spans to be spliced together.</summary>
		private readonly VideoSpanCollection _spans;
		/// <summary>The total length of the output file.</summary>
		private double _length;
		/// <summary>Whether to copy source metadata to the target file.</summary>
		private bool _copyMetadata;

		/// <summary>Initializes the splicer.</summary>
		/// <param name="target">The output file.</param>
		/// <param name="spans">The collection of spans to be spliced together.</param>
		/// <param name="copyMetadata">Whether to copy metadata from a source span's file to the output file.</param>
		public DvrmsSplicer(FileInfo target, VideoSpanCollection spans, bool copyMetadata) : base(null, target.FullName)
		{
			if (target == null) throw new ArgumentNullException("target");
			if (spans == null) throw new ArgumentNullException("spans");
			if (spans.Count == 0) throw new ArgumentOutOfRangeException("spans");
			
			if (target.Exists) target.Delete();

			_target = target;
			_spans = spans;
			_copyMetadata = copyMetadata;

			// Compute the length of the output file
			double length = 0;
			foreach(VideoSpan span in spans)
			{
				length += (span.StopPosition - span.StartPosition);
			}
			if (length <= 0) throw new ArgumentOutOfRangeException("spans");
			_length = length;
		}

		/// <summary>Splices together all of the spans.</summary>
		/// <returns></returns>
		protected override object DoWork()
		{
			IStreamBufferRecComp recComp = null;
			Timer timer = null;
			try
			{
				// Timer used for updating progress
				timer = new Timer(new TimerCallback(HandleProgressUpdate), null, PollFrequency, PollFrequency);

				// Create the RecComp and initialize it
				recComp = (IStreamBufferRecComp)ClassId.CoCreateInstance(ClassId.RecComp);
				if (File.Exists(_target.FullName)) File.Delete(_target.FullName);
				recComp.Initialize(_target.FullName, _spans[0].File.FullName);
				_recComp = recComp; // only valid during this call

				// Add each span to the output file
				FileInfo dvrTarget = _target;
				for(int i=0; i<_spans.Count; i++)
				{
					// If the user has requested cancellation, stop processing
					if (CancellationPending) break;

					// Do the append
					VideoSpan span = _spans[i];
					ulong start = VideoSpan.SecondsToHundredNanoseconds(span.StartPosition);
					ulong stop = VideoSpan.SecondsToHundredNanoseconds(span.StopPosition);
					recComp.AppendEx(span.File.FullName, start, stop);
				}
			}
			finally
			{
				// Clean up after the RecComp object and the timer
				if (timer != null) timer.Dispose();
				if (recComp != null) recComp.Close();
				while(Marshal.ReleaseComObject(recComp) > 0);
			}

			// Copy the metadata if requested... use that from the first span.
			if (_copyMetadata) 
			{
				
				using(MetadataEditor sourceEditor = new DvrmsMetadataEditor(_spans[0].File.FullName))
				{
					using(MetadataEditor destEditor = new AsfMetadataEditor(_target.FullName))
					{
						MetadataEditor.MigrateMetadata(sourceEditor, destEditor);
					}
				}
			}

			// Notify that we're done
			OnProgressChanged(100);
			return null;
		}

		/// <summary>Stores current RecComp in use.</summary>
		private IStreamBufferRecComp _recComp;

		/// <summary>Handles notification of progress updates.</summary>
		/// <param name="ignored">Not used.</param>
		private void HandleProgressUpdate(object ignored)
		{
			// Cancel if the user has requested it.
			if (CancellationPending) _recComp.Cancel();

			// Note: this could result in null ref exception if timer fires 
			// before disposal, but won't cause any problems here, at least not in v1.x.
			uint progress = _recComp.GetCurrentLength();
			OnProgressChanged(progress * 100.0 / _length);
		}
	}
}