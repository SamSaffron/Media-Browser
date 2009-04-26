// Stephen Toub
// stoub@microsoft.com

using System;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.DvrMs.DirectShow
{
	[ComImport]
	[Guid("56A868B2-0AD4-11CE-B03A-0020AF0BA770")]
	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	public interface IMediaPosition
	{
		double get_Duration();

		void put_CurrentPosition([In] double llTime);
		void get_CurrentPosition([Out] out double pllTime);

		void get_StopTime([Out] out double pllTime);
		void put_StopTime([In] double llTime);

		void get_PrerollTime([Out] out double pllTime);
		void put_PrerollTime([In] double llTime);

		void put_Rate([In] double dRate);
		void get_Rate([Out] out double pdRate);

		void CanSeekForward([Out] out int pCanSeekForward);
		void CanSeekBackward([Out] out int pCanSeekBackward);
	}
}
