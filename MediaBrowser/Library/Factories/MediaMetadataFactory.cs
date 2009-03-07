using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace MediaBrowser.Library
{
    class MediaMetadataFactory : IDisposable
    {
        public static readonly MediaMetadataFactory Instance = new MediaMetadataFactory();
        private Stack<MediaMetadata> buffer = new Stack<MediaMetadata>();
        private AutoResetEvent moreAvailable = new AutoResetEvent(false);

        private MediaMetadataFactory()
        {
            IncreaseBuffer();
        }

        public MediaMetadata Create(MediaMetadataStore store, ItemType type)
        {
            MediaMetadata mine = GetFreeMediaMetadata();
            mine.Assign(store,type);
            return mine;
        }

        public MediaMetadata Create(UniqueName ownerName, ItemType type)
        {
            MediaMetadata mine = GetFreeMediaMetadata();
            mine.Assign(ownerName, type);
            return mine;
        }

        private MediaMetadata GetFreeMediaMetadata()
        {
            MediaMetadata mine = null;
            while (mine == null)
            {
                lock (buffer)
                    if (buffer.Count > 0)
                        mine = buffer.Pop();
                
                lock(this)
                    if (buffer.Count<20)
                        IncreaseBuffer();
            }
            return mine;
        }

        private void IncreaseBuffer()
        {
            if (Microsoft.MediaCenter.UI.Application.IsApplicationThread)
            {
                this.BufferMore(null);
                moreAvailable.Reset();
            }
            else
            {
                moreAvailable.Reset();
                Microsoft.MediaCenter.UI.Application.DeferredInvoke(this.BufferMore);
                if (!moreAvailable.WaitOne(500,false))
                    Trace.TraceWarning("Wait on buffer increase in mediametadatafactory failed");
                        
            }
        }

        private void BufferMore(object nothing)
        {
            lock (buffer)
                if (buffer.Count < 20)
                    for (int i = 0; i < 100; ++i)
                        buffer.Push(new MediaMetadata());
            moreAvailable.Set();
        }



        #region IDisposable Members

        public void Dispose() {
            if (moreAvailable != null)
                moreAvailable.Close();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
