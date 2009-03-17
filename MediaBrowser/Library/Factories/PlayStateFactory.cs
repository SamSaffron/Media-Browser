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

        private PlayStateFactory()
        {
        }

        public PlayState Create(UniqueName ownerName)
        {
            PlayState mine = new PlayState();
            mine.Assign(ownerName);
            return mine;
        }

      
    }
}
