using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace MediaBrowser.Library
{
    class PlayStateFactory
    {
        public static readonly PlayStateFactory Instance = new PlayStateFactory();
        private Stack<PlayState> buffer = new Stack<PlayState>();
        private AutoResetEvent moreAvailable = new AutoResetEvent(false);

        private PlayStateFactory()
        {
            IncreaseBuffer();
        }

        public PlayState Create(UniqueName ownerName)
        {
            PlayState mine = GetFreePlayState();
            mine.Assign(ownerName);
            return mine;
        }

       
        private PlayState GetFreePlayState()
        {
            PlayState mine = null;
            while (mine == null)
            {
                try
                {
                    lock (buffer)
                        if (buffer.Count > 0)
                            mine = buffer.Pop();
                    //if (mine == null)
                    lock (this)
                        if (buffer.Count < 20)
                            IncreaseBuffer();
                }
                catch (Exception e)
                {
                    Trace.WriteLine("Failed during GetFreePlayState: " + e.ToString());
                }
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
                // During navigation microsoft will abandon the deferred invoke (sometimes), ensure this always comes back and does not cause a hang. 
                if (!moreAvailable.WaitOne(500, false))
                    Trace.TraceWarning("Wait on buffer increase in PlayStateFactory failed");
            }
        }

        private void BufferMore(object nothing)
        {
            lock (buffer)
                if (buffer.Count < 20)
                    for (int i = 0; i < 100; ++i)
                        buffer.Push(new PlayState());
            moreAvailable.Set();
        }
    }
}
