// Stephen Toub
// stoub@microsoft.com

using System;
using System.IO;
using System.Runtime.InteropServices;
using Toub.MediaCenter.Dvrms.DirectShow;
using Toub.MediaCenter.Dvrms.Utilities;

namespace Toub.MediaCenter.Dvrms.Conversion
{
	/// <summary>Base class for all converters that use a DVR-MS file as the input parameter.</summary>
	public abstract class Converter
	{
		/// <summary>Path to the input file.</summary>
		private string _input;
		/// <summary>Path to the output file.</summary>
		private string _output;
		/// <summary>Frequency with which the conversion status should be polled and listeners alerted.</summary>
		private int _pollFrequency = 1000;

		/// <summary>Initialize the converter.</summary>
		protected Converter() : this(null) {}

		/// <summary>Initialize the converter.</summary>
		/// <param name="input">The input DVR-MS file path.</param>
		protected Converter(string input) : this(input, null) {}

		/// <summary>Initialize the converter.</summary>
		/// <param name="input">The input DVR-MS file path.</param>
		/// <param name="output">The output file path.</param>
		protected Converter(string input, string output)
		{
			_input = input;
			_output = output;
			ConversionComplete += new ConversionCompletedEventHandler(ResetWhenConversionComplete);
		}

		/// <summary>
		/// Gets or sets the frequency with which the conversion status should 
		/// be polled and listeners alerted.
		/// </summary>
		/// <remarks>A value of Timeout.Infinite (-1) will result in no polling.</remarks>
		public int PollFrequency
		{
			get { return _pollFrequency; }
			set
			{
				if (value <= 0) _pollFrequency = -1;
				_pollFrequency = value;
			}
		}

		/// <summary>Runs the conversion synchronously.</summary>
		/// <returns>The result of the conversion.</returns>
		public object Convert() 
		{ 
			_cancellationPending = false;
			try
			{
				object result;
				using(_dc = new DisposalCleanup())
				{
					// Do the actual work
					result = DoWork();
				}
				OnConversionComplete(null, result);
				return result;
			}
			catch(DirectShowException exc)
			{
				OnConversionComplete(exc, null);
				throw;
			}
			catch(Exception exc)
			{
				exc = new DirectShowException(exc);
				OnConversionComplete(exc, null);
				throw exc;
			}
		}

		/// <summary>Allows for easy cleanup of COM resources.</summary>
		private DisposalCleanup _dc;

		/// <summary>Allows for easy cleanup of COM resources.</summary>
		protected DisposalCleanup DisposalCleanup { get { return _dc; } }

		/// <summary>Used to invoke the conversion process asynchronously.</summary>
		private delegate object ConvertHandler();

		/// <summary>Whether there is currently an asynchronous conversion running.</summary>
		private volatile bool _asyncRunning = false;

		/// <summary>Starts an asynchronous conversion.</summary>
		public void ConvertAsync() 
		{
			if (_asyncRunning) throw new InvalidOperationException();
			_asyncRunning = true;
			new ConvertHandler(Convert).BeginInvoke(null, null);
		}

		/// <summary>Issues a cancellation for any currently running asynchronous operations.</summary>
		public void CancelAsync() { _cancellationPending = true; }
		
		/// <summary>Resets state when an asynchronous conversion completes.</summary>
		/// <param name="sender">Ignored.</param>
		/// <param name="e">Ignored.</param>
		private void ResetWhenConversionComplete(object sender, ConversionCompletedEventArgs e)
		{
			_asyncRunning = false;
		}

		/// <summary>Whether this is currently a cancellation request pending.</summary>
		private volatile bool _cancellationPending = false;

		/// <summary>Gets whether this is currently a cancellation request pending.</summary>
		protected bool CancellationPending { get { return _cancellationPending; } }

		/// <summary>Raises the ProgressChanged event.</summary>
		/// <param name="progress">Current percentage of work completed.</param>
		protected void OnProgressChanged(double progress)
		{
			ProgressChangedEventHandler ev = ProgressChanged;
			if (ev != null) ev(this, new ProgressChangedEventArgs(progress));
		}

		/// <summary>Raises the ConversionComplete event.</summary>
		/// <param name="error">Any exception that prevented the work from completing.</param>
		/// <param name="result">The result of the conversion.</param>
		protected void OnConversionComplete(Exception error, object result)
		{
			ConversionCompletedEventHandler ev = ConversionComplete;
			if (ev != null) ev(this, new ConversionCompletedEventArgs(error, result));
		}

		/// <summary>Raised when a conversion has completed..</summary>
		public event ConversionCompletedEventHandler ConversionComplete;

		/// <summary>Raised when there's a progress update for a conversion.</summary>
		public event ProgressChangedEventHandler ProgressChanged;

		/// <summary>Gets the path to the input file.</summary>
		protected string InputFilePath { get { return _input; } }

		/// <summary>Gets the path to the output file.</summary>
		protected string OutputFilePath { get { return _output; } }

		/// <summary>Performs the actual conversion synchronously.</summary>
		/// <returns>The results of the conversion.</returns>
		protected abstract object DoWork();

		/// <summary>Connects together to graph filters.</summary>
		/// <param name="graph">The graph on which the filters exist.</param>
		/// <param name="source">The source filter.</param>
		/// <param name="outPinName">The name of the output pin on the source filter.</param>
		/// <param name="destination">The destination filter.</param>
		/// <param name="inPinName">The name of the input pin on the destination filter.</param>
		protected void Connect(IGraphBuilder graph, IBaseFilter source, string outPinName, 
			IBaseFilter destination, string inPinName)
		{
			IPin outPin = source.FindPin(outPinName);
			DisposalCleanup.Add(outPin);
			
			IPin inPin = destination.FindPin(inPinName);
			DisposalCleanup.Add(inPin);
			
			graph.Connect(outPin, inPin);
		}

		/// <summary>Runs the graph</summary>
		/// <param name="graphBuilder">The graph to be run.</param>
		protected void RunGraph(IGraphBuilder graphBuilder)
		{
			RunGraph(graphBuilder, (IBaseFilter)null);
		}

		/// <summary>Runs the graph</summary>
		/// <param name="graphBuilder">The graph to be run.</param>
		/// <param name="seekableFilter">The filter to use for computing percent complete. Must implement IMediaSeeking.</param>
		protected void RunGraph(IGraphBuilder graphBuilder, IFileSinkFilter seekableFilter)
		{
			RunGraph(graphBuilder, (IBaseFilter)seekableFilter);
		}

		/// <summary>Determines whether the specified IMediaSeeking can be used to retrieve duration and current position.</summary>
		/// <param name="seeking">The interface to check.</param>
		/// <returns>true if it can be used to retrieve duration and current position; false, otherwise.</returns>
		private static bool CanGetPositionAndDuration(IMediaSeeking seeking)
		{
			if (seeking == null) return false;
			SeekingCapabilities caps = seeking.GetCapabilities();
			if ((caps & SeekingCapabilities.CanGetDuration) != SeekingCapabilities.CanGetDuration) return false;
			if ((caps & SeekingCapabilities.CanGetCurrentPos) != SeekingCapabilities.CanGetCurrentPos) return false;
			return true;
		}

		/// <summary>Runs the graph</summary>
		/// <param name="graphBuilder">The graph to be run.</param>
		/// <param name="seekableFilter">The filter to use for computing percent complete. Must implement IMediaSeeking.</param>
		protected void RunGraph(IGraphBuilder graphBuilder, IBaseFilter seekableFilter)
		{
			// Get the necessary control and event interfaces
			IMediaControl mediaControl = (IMediaControl)graphBuilder;
			IMediaEvent mediaEvent = (IMediaEvent)graphBuilder;

			// Get the media seeking interface to use for computing status and progress updates
			IMediaSeeking mediaSeeking = seekableFilter as IMediaSeeking;
			if (!CanGetPositionAndDuration(mediaSeeking)) 
			{
				mediaSeeking = graphBuilder as IMediaSeeking;
				if (!CanGetPositionAndDuration(mediaSeeking)) mediaSeeking = null;
			}

			// Publish the graph to the running object table and to a temporary file for examination/debugging purposes
			using(new GraphPublisher(graphBuilder, Path.GetTempPath()+Guid.NewGuid().ToString("N")+".grf"))
			{
				// Run the graph
				mediaControl.Run();
				try
				{
					OnProgressChanged(0); // initial progress update stating 0% done
					bool done = false;
					while(!CancellationPending && !done) // continue until we're done/cancelled
					{
						// Poll to see how we're doing
						EventCode statusCode = EventCode.None;
						int hr = mediaEvent.WaitForCompletion(PollFrequency, out statusCode);
						switch(statusCode)
						{
							case EventCode.Complete:
								done = true;
								break;
							case EventCode.None: 
								// Get an update on where we are with the conversion
								if (mediaSeeking != null)
								{
									ulong curPos = mediaSeeking.GetCurrentPosition();
									ulong length = mediaSeeking.GetDuration();
									double progress = curPos * 100.0 / (double)length;
									if (progress > 0) OnProgressChanged(progress);
								}
								break;
							default:
								// Error, so throw exception
								throw new DirectShowException(hr, null);
						}
					}
					OnProgressChanged(100); // final progress update stating 100% done
				}
				finally
				{
					// We're done converting, so stop the graph
					mediaControl.Stop();
				}
			}
		}
	}

	/// <summary>Event arguments for the ProcessChanged event.</summary>
	public class ProgressChangedEventArgs : EventArgs
	{
		/// <summary>Percentage of the conversion currently completed.</summary>
		public readonly double ProgressPercentage;

		/// <summary>Initialize the event args.</summary>
		/// <param name="percentage">Percentage of the conversion currently completed.</param>
		public ProgressChangedEventArgs(double percentage)
		{
			ProgressPercentage = percentage;
		}
	}

	/// <summary>Event arguments for the ConversionCompleted event.</summary>
	public class ConversionCompletedEventArgs : EventArgs
	{
		/// <summary>Any result of the conversion.</summary>
		public readonly object Result;
		/// <summary>Any exception thrown during the conversion that caused it to stop.</summary>
		public readonly Exception Error;

		/// <summary>Initialize the event args.</summary>
		/// <param name="error">Any result of the conversion.</param>
		/// <param name="result">Any exception thrown during the conversion that caused it to stop.</param>
		public ConversionCompletedEventArgs(Exception error, object result)
		{
			Error = error;
			Result = result;
		}
	}

	/// <summary>Handler used for the ProgressChanged event.</summary>
	public delegate void ProgressChangedEventHandler(object sender,ProgressChangedEventArgs e);

	/// <summary>Handler used for the ConversionCompleted event.</summary>
	public delegate void ConversionCompletedEventHandler(object sender, ConversionCompletedEventArgs e);
}
