using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MediaBrowser.Library
{
    class ItemFactory
    {
        public static readonly ItemFactory Instance = new ItemFactory();
        private Stack<Item> buffer = new Stack<Item>();
        private AutoResetEvent moreAvailable = new AutoResetEvent(false);

        public Item Create(ItemSource source)
        {
            Item mine = GetFreeItem();
            mine.Assign(source);
            return mine;
        }

        public Item Create(ItemSource source, MediaMetadata metadata)
        {
            Item mine = GetFreeItem();
            mine.Assign(source, metadata);
            return mine;
        }

        private Item GetFreeItem()
        {
            Item mine = null;
            while (mine == null)
            {
                lock (buffer)
                    if (buffer.Count > 0)
                        mine = buffer.Pop();
                if (mine == null)
                    lock(this)
                        if (buffer.Count==0)
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
                moreAvailable.WaitOne(500,false);
            }
        }

        private void BufferMore(object nothing)
        {
            lock (buffer)
                if (buffer.Count < 10)
                    for (int i = 0; i < 100; ++i)
                        buffer.Push(new Item());
            moreAvailable.Set();
        }


    }
}
