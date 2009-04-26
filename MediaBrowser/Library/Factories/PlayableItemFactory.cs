using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Playables;

namespace MediaBrowser.Library.Factories {
    public class PlayableItemFactory {
        public static PlayableItemFactory Instance = new PlayableItemFactory(); 

        private PlayableItemFactory ()
	    {
	    }

        public PlayableItem Create(Video video) {

            PlayableItem playable = null;

            if (PlayableExternal.CanPlay(video.Path))
                playable = new PlayableExternal(video.Path);
            else if (PlayableVideoFile.CanPlay(video))
                playable = new PlayableVideoFile(video);
            else if (PlayableIso.CanPlay(video))
                playable = new PlayableIso(video);
            else if (PlayableMultiFileVideo.CanPlay(video))
                playable = new PlayableMultiFileVideo(video);
            else if (PlayableDvd.CanPlay(video))
                playable = new PlayableDvd(video);

            foreach (var controller in Application.CurrentInstance.LibraryConfig.PlaybackControllers) {
                if (controller.CanPlay(playable.Filename)) {
                    playable.PlaybackController = controller;
                }
            }

            return playable;
        
        }
    }
}
