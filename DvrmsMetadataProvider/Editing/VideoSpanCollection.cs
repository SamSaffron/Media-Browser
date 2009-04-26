// Stephen Toub
// stoub@microsoft.com

using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Toub.MediaCenter.Dvrms.Editing
{
	/// <summary>A collection of VideoSpans representing segments to spliced together.</summary>
	[Serializable]
	public sealed class VideoSpanCollection : CollectionBase
	{
		/// <summary>Adds a span to the collection.</summary>
		/// <param name="span">The span to be added.</param>
		/// <returns>The index of the added span.</returns>
		public int Add(VideoSpan span)
		{
			if (span == null) throw new ArgumentNullException("span");
			return base.InnerList.Add(span);
		}

		/// <summary>Removes a span from the collection.</summary>
		/// <param name="span">The span to be removed.</param>
		public void Remove(VideoSpan span)
		{
			base.InnerList.Remove(span);
		}

		/// <summary>Removes a span from the collection.</summary>
		/// <param name="index">The index of the span to be removed.</param>
		public new void RemoveAt(int index)
		{
			base.InnerList.RemoveAt(index);
		}

		/// <summary>Copies the collection of spans to a new array.</summary>
		/// <returns>An array of the spans in the collection.</returns>
		public VideoSpan [] ToArray()
		{
			return (VideoSpan[])base.InnerList.ToArray(typeof(VideoSpan));
		}

		/// <summary>Copies the collection of spans to the specified array.</summary>
		/// <param name="spans">The target array.</param>
		/// <param name="arrayIndex">The starting index to which items should be copied.</param>
		public void CopyTo(VideoSpan [] spans, int arrayIndex)
		{
			base.InnerList.CopyTo(spans, arrayIndex);
		}

		/// <summary>Inserts a span into the collection at the specified index.</summary>
		/// <param name="index">The index at which to insert the span.</param>
		/// <param name="span">The span to be inserted.</param>
		public void Insert(int index, VideoSpan span)
		{
			base.InnerList.Insert(index, span);
		}

		/// <summary>Gets or sets the VideoSpan at the specified index.</summary>
		public VideoSpan this[int index]
		{
			get { return (VideoSpan)base.InnerList[index]; }
			set { base.InnerList[index] = value; }
		}

		/// <summary>Validates a value.</summary>
		/// <param name="value">The value to be validated.</param>
		protected override void OnValidate(object value)
		{
			if (!(value is VideoSpan)) throw new ArgumentException("value");
		}
	}
}