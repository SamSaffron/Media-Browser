// Stephen Toub
// stoub@microsoft.com

using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>Exception for errors when working with DirectShow.</summary>
	[Serializable]
	public class DirectShowException : Exception
	{
		/// <summary>Initialize the exception.</summary>
		public DirectShowException() : 
			base("A DirectShow error occurred.") {}

		/// <summary>Initialize the exception.</summary>
		/// <param name="message">A description of the exception.</param>
		public DirectShowException(string message) : 
			base(message) {}

		/// <summary>Initialize the exception.</summary>
		/// <param name="innerException">The exception that caused this exception.</param>
		public DirectShowException(Exception innerException) : 
			this(Marshal.GetHRForException(innerException), innerException) {}

		/// <summary>Initialize the exception.</summary>
		/// <param name="errorCode">The HRESULT that caused the exception.</param>
		/// <param name="innerException">The exception that caused this exception.</param>
		public DirectShowException(int errorCode, Exception innerException) : 
			this(GetMessage(errorCode, innerException), innerException) {}

		/// <summary>Initialize the exception.</summary>
		/// <param name="message">A description of the exception.</param>
		/// <param name="innerException">The exception that caused this exception.</param>
		public DirectShowException(string message, Exception innerException) : 
			base(message, innerException) {}

		/// <summary>Initialize the exception.</summary>
		/// <param name="info">For exception deserialization.</param>
		/// <param name="context">For exception deserialization.</param>
		protected DirectShowException(SerializationInfo info, StreamingContext context) : 
			base (info, context) {}

		/// <summary>Gets an exception message based on an HRESULT and an inner exception.</summary>
		/// <param name="code">The HRESULT that caused the exception.</param>
		/// <param name="exception">The exception that caused this exception.</param>
		/// <returns>The message to use.</returns>
		private static string GetMessage(int code, Exception exception)
		{
			string message = (string)_errorTable[code];
			if (message == null)
			{
				if (exception != null) message = exception.Message;
				else message = new Win32Exception(code).Message;
			}
			return message;
		}

		/// <summary>Hashtable of error messages by HRESULT.</summary>
		private static Hashtable _errorTable;	

		/// <summary>Initialize the error messages table.</summary>
		static DirectShowException()
		{
			_errorTable = new Hashtable();
			_errorTable[0x00040103] = @"Reached the end of the list; no more items in the list. (Filter developers: The CBasePin::GetMediaType method is expected to return this value.)";
			_errorTable[0x0004022D] = @"An attempt to add a filter with a duplicate name succeeded with a modified name.";
			_errorTable[0x00040237] = @"The state transition is not complete.";
			_errorTable[0x00040242] = @"Some of the streams in this movie are in an unsupported format.";
			_errorTable[0x00040245] = @"The file contained some property settings that were not used.";
			_errorTable[0x00040246] = @"Some connections failed and were deferred.";
			_errorTable[0x00040250] = @"The resource specified is no longer needed.";
			_errorTable[0x00040254] = @"Could not connect with the media type in the persistent graph.";
			_errorTable[0x00040257] = @"Cannot play back the video stream: could not find a suitable renderer.";
			_errorTable[0x00040258] = @"Cannot play back the audio stream: could not find a suitable renderer.";
			_errorTable[0x0004025A] = @"Cannot play back the video stream: format 'RPZA' is not supported.";
			_errorTable[0x00040260] = @"The value returned had to be estimated. Its accuracy can't be guaranteed.";
			_errorTable[0x00040263] = @"This success code is reserved for internal purposes within DirectShow.";
			_errorTable[0x00040267] = @"The stream was turned off.";
			_errorTable[0x00040268] = @"The filter is active, but cannot deliver data. See IMediaFilter::GetState.";
			_errorTable[0x0004027E] = @"Preview was rendered throught the Smart Tee filter, because the capture filter does not have a preview pin.";
			_errorTable[0x00040280] = @"The current title is not a sequential set of chapters (PGC), so the timing information might not be continuous.";
			_errorTable[0x0004028C] = @"The audio stream does not contain enough information to determine the contents of each channel.";
			_errorTable[0x0004028D] = @"The seek operation on the DVD was not frame accurate.";
			_errorTable[0x80040200] = @"The specified media type is invalid.";
			_errorTable[0x80040201] = @"The specified media subtype is invalid.";
			_errorTable[0x80040202] = @"This object can only be created as an aggregated object.";
			_errorTable[0x80040203] = @"The state of the enumerated object has changed and is now inconsistent with the state of the enumerator. Discard any data obtained from previous calls to the enumerator and then update the enumerator by calling the enumerator's Reset method.";
			_errorTable[0x80040204] = @"At least one of the pins involved in the operation is already connected.";
			_errorTable[0x80040205] = @"This operation cannot be performed because the filter is active.";
			_errorTable[0x80040206] = @"One of the specified pins supports no media types.";
			_errorTable[0x80040207] = @"There is no common media type between these pins.";
			_errorTable[0x80040208] = @"Two pins of the same direction cannot be connected.";
			_errorTable[0x80040209] = @"The operation cannot be performed because the pins are not connected.";
			_errorTable[0x8004020A] = @"No sample buffer allocator is available.";
			_errorTable[0x8004020B] = @"A run-time error occurred.";
			_errorTable[0x8004020C] = @"No buffer space has been set.";
			_errorTable[0x8004020D] = @"The buffer is not big enough.";
			_errorTable[0x8004020E] = @"An invalid alignment was specified.";
			_errorTable[0x8004020F] = @"The allocator was not committed. See IMemAllocator::Commit.";
			_errorTable[0x80040210] = @"One or more buffers are still active.";
			_errorTable[0x80040211] = @"Cannot allocate a sample when the allocator is not active.";
			_errorTable[0x80040212] = @"Cannot allocate memory because no size has been set.";
			_errorTable[0x80040213] = @"Cannot lock for synchronization because no clock has been defined.";
			_errorTable[0x80040214] = @"Quality messages could not be sent because no quality sink has been defined.";
			_errorTable[0x80040215] = @"A required interface has not been implemented.";
			_errorTable[0x80040216] = @"An object or name was not found.";
			_errorTable[0x80040217] = @"No combination of intermediate filters could be found to make the connection.";
			_errorTable[0x80040218] = @"No combination of filters could be found to render the stream.";
			_errorTable[0x80040219] = @"Could not change formats dynamically.";
			_errorTable[0x8004021A] = @"No color key has been set.";
			_errorTable[0x8004021B] = @"Current pin connection is not using the IOverlay transport.";
			_errorTable[0x8004021C] = @"Current pin connection is not using the IMemInputPin transport.";
			_errorTable[0x8004021D] = @"Setting a color key would conflict with the palette already set.";
			_errorTable[0x8004021E] = @"Setting a palette would conflict with the color key already set.";
			_errorTable[0x8004021F] = @"No matching color key is available.";
			_errorTable[0x80040220] = @"No palette is available.";
			_errorTable[0x80040221] = @"Display does not use a palette.";
			_errorTable[0x80040222] = @"Too many colors for the current display settings.";
			_errorTable[0x80040223] = @"The state changed while waiting to process the sample.";
			_errorTable[0x80040224] = @"The operation could not be performed because the filter is not stopped.";
			_errorTable[0x80040225] = @"The operation could not be performed because the filter is not paused.";
			_errorTable[0x80040226] = @"The operation could not be performed because the filter is not running.";
			_errorTable[0x80040227] = @"The operation could not be performed because the filter is in the wrong state.";
			_errorTable[0x80040228] = @"The sample start time is after the sample end time.";
			_errorTable[0x80040229] = @"The supplied rectangle is invalid.";
			_errorTable[0x8004022A] = @"This pin cannot use the supplied media type.";
			_errorTable[0x8004022B] = @"This sample cannot be rendered.";
			_errorTable[0x8004022C] = @"This sample cannot be rendered because the end of the stream has been reached.";
			_errorTable[0x8004022D] = @"An attempt to add a filter with a duplicate name failed.";
			_errorTable[0x8004022E] = @"A time-out has expired.";
			_errorTable[0x8004022F] = @"The file format is invalid.";
			_errorTable[0x80040230] = @"The list has already been exhausted.";
			_errorTable[0x80040231] = @"The filter graph is circular.";
			_errorTable[0x80040232] = @"Updates are not allowed in this state.";
			_errorTable[0x80040233] = @"An attempt was made to queue a command for a time in the past.";
			_errorTable[0x80040234] = @"The queued command was already canceled.";
			_errorTable[0x80040235] = @"Cannot render the file because it is corrupt.";
			_errorTable[0x80040236] = @"An IOverlay advise link already exists.";
			_errorTable[0x80040238] = @"No full-screen modes are available.";
			_errorTable[0x80040239] = @"This advise cannot be canceled because it was not successfully set.";
			_errorTable[0x8004023A] = @"Full-screen mode is not available.";
			_errorTable[0x8004023B] = @"Cannot call IVideoWindow methods while in full-screen mode.";
			_errorTable[0x80040240] = @"The media type of this file is not recognized.";
			_errorTable[0x80040241] = @"The source filter for this file could not be loaded.";
			_errorTable[0x80040243] = @"A file appeared to be incomplete.";
			_errorTable[0x80040244] = @"The file's version number is invalid.";
			_errorTable[0x80040247] = @"This file is corrupt: it contains an invalid class identifier.";
			_errorTable[0x80040248] = @"This file is corrupt: it contains an invalid media type.";
			_errorTable[0x80040249] = @"No time stamp has been set for this sample.";
			_errorTable[0x80040251] = @"No media time was set for this sample.";
			_errorTable[0x80040252] = @"No media time format was selected.";
			_errorTable[0x80040253] = @"Cannot change balance because audio device is monoaural only.";
			_errorTable[0x80040255] = @"Cannot play back the video stream: could not find a suitable decompressor.";
			_errorTable[0x80040256] = @"Cannot play back the audio stream: no audio hardware is available, or the hardware is not supported.";
			_errorTable[0x80040259] = @"Cannot play back the video stream: format 'RPZA' is not supported.";
			_errorTable[0x8004025B] = @"DirectShow cannot play MPEG movies on this processor.";
			_errorTable[0x8004025C] = @"Cannot play back the audio stream: the audio format is not supported.";
			_errorTable[0x8004025D] = @"Cannot play back the video stream: the video format is not supported.";
			_errorTable[0x8004025E] = @"DirectShow cannot play this video stream because it falls outside the constrained standard.";
			_errorTable[0x8004025F] = @"Cannot perform the requested function on an object that is not in the filter graph.";
			_errorTable[0x80040261] = @"Cannot access the time format on an object.";
			_errorTable[0x80040262] = @"Could not make the connection because the stream is read-only and the filter alters the data.";
			_errorTable[0x80040264] = @"The buffer is not full enough.";
			_errorTable[0x80040265] = @"Cannot play back the file: the format is not supported.";
			_errorTable[0x80040266] = @"Pins cannot connect because they don't support the same transport.";
			_errorTable[0x80040269] = @"The Video CD can't be read correctly by the device or the data is corrupt.";
			_errorTable[0x80040270] = @"The sample had a start time but not a stop time. In this case, the stop time that is returned is set to the start time plus one.";
			_errorTable[0x80040271] = @"There is not enough video memory at this display resolution and number of colors. Reducing resolution might help.";
			_errorTable[0x80040272] = @"The video port connection negotiation process has failed.";
			_errorTable[0x80040273] = @"Either Microsoft DirectDraw has not been installed or the video card capabilities are not suitable. Make sure the display is not in 16-color mode.";
			_errorTable[0x80040274] = @"No video port hardware is available, or the hardware is not responding.";
			_errorTable[0x80040275] = @"No capture hardware is available, or the hardware is not responding.";
			_errorTable[0x80040276] = @"This user operation is prohibited by DVD content at this time.";
			_errorTable[0x80040277] = @"This operation is not permitted in the current domain.";
			_errorTable[0x80040278] = @"Requested button is not available.";
			_errorTable[0x80040279] = @"DVD-Video playback graph has not been built yet.";
			_errorTable[0x8004027A] = @"DVD-Video playback graph building failed.";
			_errorTable[0x8004027B] = @"DVD-Video playback graph could not be built due to insufficient decoders.";
			_errorTable[0x8004027C] = @"The DirectDraw version number is not suitable. Make sure to install DirectX 5 or higher.";
			_errorTable[0x8004027D] = @"Copy protection could not be enabled.";
			_errorTable[0x8004027F] = @"Seek command timed out.";
			_errorTable[0x80040281] = @"The operation cannot be performed at the current playback speed.";
			_errorTable[0x80040282] = @"The specified DVD menu does not exist.";
			_errorTable[0x80040283] = @"The specified command was cancelled or no longer exists.";
			_errorTable[0x80040284] = @"The DVD state information contains the wrong version number.";
			_errorTable[0x80040285] = @"The DVD state information is corrupted.";
			_errorTable[0x80040286] = @"The DVD state information is from another disc and not the current disc.";
			_errorTable[0x80040287] = @"The region is not compatible with the drive.";
			_errorTable[0x80040288] = @"The requested attributes do not exist.";
			_errorTable[0x80040289] = @"The operation cannot be performed because no GoUp program chain (PGC) is available.";
			_errorTable[0x8004028A] = @"The operation is prohibited because the parental level is too low.";
			_errorTable[0x8004028B] = @"The DVD Navigator is not in karaoke mode.";
			_errorTable[0x8004028E] = @"Frame stepping is not supported.";
			_errorTable[0x8004028F] = @"The requested stream is disabled.";
			_errorTable[0x80040290] = @"The operation requires a title number, but there is no current title. This error can occur when the DVD Navigator is not in the Title domain or the Video Title Set Menu (VTSM) domain.";
			_errorTable[0x80040291] = @"The specified path is not a valid DVD disc.";
			_errorTable[0x80040292] = @"The Resume operation could not be completed, because there is no resume information.";
			_errorTable[0x80040293] = @"Pin is already blocked on the calling thread.";
			_errorTable[0x80040294] = @"Pin is already blocked on another thread.";
			_errorTable[0x80040295] = @"Use of this filter is restricted by a software key. The application must unlock the filter.";
			_errorTable[0x80040296] = @"The Video Mixing Renderer (VMR) is not in mixing mode. Call IVMRFilterConfig::SetNumberOfStreams (VMR-7) or IVMRFilterConfig9::SetNumberOfStreams (VMR-9).";
			_errorTable[0x80040297] = @"The application has not yet provided the VMR filter with a valid allocator-presenter object.";
			_errorTable[0x80040298] = @"The VMR could not find any de-interlacing hardware on the current display device.";
			_errorTable[0x80040299] = @"The VMR could not find any hardware that supports ProcAmp controls on the current display device.";
			_errorTable[0x8004029A] = @"The hardware decoder uses video port extensions (VPE), which are not compatible with the VMR-9 filter.";
			_errorTable[0x800403F2] = @"A registry entry is corrupt.";
		}
	}
}