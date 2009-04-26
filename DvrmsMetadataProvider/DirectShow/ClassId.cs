// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.DirectShow
{
	/// <summary>Stores CLSIDs and methods that use them.</summary>
	public sealed class ClassId
	{
		/// <summary>Prevent instantiation.</summary>
		private ClassId(){}

		/// <summary>The File Writer filter can be used to write files to disc regardless of format. </summary>
		public static readonly Guid FileWriter = new Guid("8596E5F0-0DA5-11D0-BD21-00A0C911CE86");

		/// <summary>The Filter Graph Manager builds and controls filter graphs.</summary>
		public static readonly Guid FilterGraph = new Guid("E436EBB3-524F-11CE-9F53-0020AF0BA770");

		/// <summary>The WM ASF Writer filter accepts a variable number of input streams and creates an ASF file.</summary>
		public static readonly Guid WMAsfWriter = new Guid("7C23220E-55BB-11D3-8B16-00C04FB6BD3D");

		/// <summary>The RecComp object creates new content recordings by concatenating existing recordings.</summary>
		public static readonly Guid RecComp = new Guid("D682C4BA-A90A-42FE-B9E1-03109849C423");

		/// <summary>The Recording object creates permanent recordings from streams that the Stream Buffer Sink filter captures.</summary>
		public static readonly Guid RecordingAttributes = new Guid("CCAA63AC-1057-4778-AE92-1206AB9ACEE6");

		/// <summary>The WavDes filter writes an audio stream to a WAV file.</summary>
		public static readonly Guid WavDest = new Guid("3C78B8E2-6C4D-11d1-ADE2-0000F8754B99");

		/// <summary>The Decrypter/Detagger filter conditionally decrypts samples that are encrypted by the Encrypter/Tagger filter.</summary>
		public static readonly Guid DecryptTag = new Guid("C4C4C4F2-0049-4E2B-98FB-9537F6CE516D");

		/// <summary>Creates an instance of a COM object by class ID.</summary>
		/// <param name="id">The class ID of the component to instantiate.</param>
		/// <returns>A new instance of the class.</returns>
		public static object CoCreateInstance(Guid id)
		{
			return Activator.CreateInstance(Type.GetTypeFromCLSID(id));
		}
	}
}