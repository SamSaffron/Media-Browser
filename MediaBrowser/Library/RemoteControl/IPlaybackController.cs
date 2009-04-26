using System;
namespace MediaBrowser.Library.RemoteControl {


    public interface IPlaybackController {
        void GoToFullScreen();
        bool IsPaused { get; }
        bool IsPlaying { get; }
        bool IsStopped { get; }
        event EventHandler<PlaybackStateEventArgs> OnProgress;
        void PlayDVD(string path);
        void PlayVideo(string path);
        void Seek(long position);
        void Pause();
        bool CanPlay(string filename);

        void ProcessCommand(RemoteCommand command);

    }
}
