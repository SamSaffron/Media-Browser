// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;
using Toub.MediaCenter.Dvrms.DirectShow;

namespace Toub.MediaCenter.DvrMs.DirectShow
{
	[ComImport]
	[Guid("56a86899-0ad4-11ce-b03a-0020af0ba770")]
	[InterfaceType( ComInterfaceType.InterfaceIsIUnknown )]
	public interface IMediaFilter
	{
		#region "IPersist Methods"
		void GetClassID(
			[Out]									out Guid		pClassID );
		#endregion

		void Stop();

		void Pause();

		void Run( long tStart );

		void GetState( int dwMilliSecsTimeout, out int filtState );

		void SetSyncSource( [In] IReferenceClock pClock );

		void GetSyncSource( [Out] out IReferenceClock pClock );
	}
}
